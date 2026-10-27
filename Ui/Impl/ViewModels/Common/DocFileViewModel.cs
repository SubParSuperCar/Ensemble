using System.Collections.Frozen;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnsembleRoot.Common.Networking;
using EnsembleRoot.Ui.Impl.Abstractions;
using Godot;
using LiveMarkdown.Avalonia;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Serilog;
using Dispatcher = Avalonia.Threading.Dispatcher;

namespace EnsembleRoot.Ui.Impl.ViewModels;

public sealed partial class DocFileViewModel : ViewModelBase
{
	// Exports publish the license files beside the assemblies; editor runs have the whole repository on disk
	private static readonly string[] LocalRoots = OS.HasFeature("template")
		? [AppContext.BaseDirectory]
		: [ProjectSettings.GlobalizePath(ResourceScheme)];

	// Whoever swaps a source out of this field owns and disposes it
	private CancellationTokenSource? _cts;

	// LiveMarkdown makes each image a link to itself, which wins over a link around it, so linked images map to their
	// link's target
	private IReadOnlyDictionary<string, string> _linkTargetsByImageUrl = FrozenDictionary<string, string>.Empty;

	public DocFileViewModel()
	{
		SelectedFile = Files[0];
	}

	public IReadOnlyList<DocFile> Files { get; } =
	[
		new("README.md", ".github/README.md"),
		new("CONTRIBUTING.md", ".github/CONTRIBUTING.md"),
		new("TECHNICAL.md", ".github/TECHNICAL.md"),
		new("LIVE-CREATIONS.md", ".github/LIVE-CREATIONS.md"),
		new("ROADMAP.md", ".github/ROADMAP.md"),
		new("LICENSE.md", "LICENSE.md"),
		new("LICENSE-ASSETS.txt", ".github/LICENSE-ASSETS.txt"),
		new("LICENSE-CODE.txt", ".github/LICENSE-CODE.txt")
	];

	[ObservableProperty] public partial DocFile SelectedFile { get; set; }
	[ObservableProperty] public partial MarkdownDocumentUpdate? Document { get; private set; }

	protected override void OnDispose() => CancelAndDispose(Interlocked.Exchange(ref _cts, null));

	partial void OnSelectedFileChanged(DocFile value) => _ = LoadAsync(value);

	private async Task LoadAsync(DocFile file)
	{
		Log.Debug("Loading {FileName}...", file.Name);
		var stopwatch = Stopwatch.StartNew();

		var cts = new CancellationTokenSource();
		var token = cts.Token;

		CancelAndDispose(Interlocked.Exchange(ref _cts, cts));

		try
		{
			var (markdown, source) = await ReadAsync(file, token).ConfigureAwait(false);

			var (document, linkTargetsByImageUrl) = await Task
				.Run(() => ResolveUrls(Markdown.Parse(markdown, MarkdownUpdateProducer.DefaultPipeline), file), token)
				.ConfigureAwait(false);

			await Dispatcher.UIThread.InvokeAsync(() =>
			{
				// A newer load or disposal cancels this one, so cancellation also marks it stale
				if (token.IsCancellationRequested)
					return;

				_linkTargetsByImageUrl = linkTargetsByImageUrl;
				Document = new MarkdownDocumentUpdate.Full(document);

				stopwatch.Stop();
				Log.Debug(
					"Loaded {FileName} from {Source} in {ElapsedMs:F3} ms",
					file.Name, source, stopwatch.Elapsed.TotalMilliseconds);
			});
		}
		// Nothing observes this task, so every failure ends here
		catch (Exception exception)
		{
			if (!token.IsCancellationRequested)
				Log.Error(exception, "Failed to load {FileName} from {Uri}", file.Name, file.Uri);
		}
		finally
		{
			if (ReferenceEquals(Interlocked.CompareExchange(ref _cts, null, cts), cts))
				cts.Dispose();
		}
	}

	[RelayCommand]
	private void OpenLink(LinkClickedEventArgs e)
	{
		var href = e.HRef is { } clicked && _linkTargetsByImageUrl.TryGetValue(clicked.OriginalString, out var target)
			? new Uri(target, UriKind.RelativeOrAbsolute)
			: e.HRef;

		// Other schemes (e.g., file:) could run programs
		if (href is { IsAbsoluteUri: true, Scheme: "https" or "http" or "mailto" })
			OS.ShellOpen(href.AbsoluteUri);
	}

	/// <summary>
	///     Points relative URLs at the repository on GitHub, even for local copies: images at their raw files, and
	///     links at their pages. LiveMarkdown would otherwise resolve them as local file paths. Also maps each linked
	///     image's URL to its link's target.
	/// </summary>
	private static (MarkdownDocument Document, IReadOnlyDictionary<string, string> LinkTargetsByImageUrl) ResolveUrls(
		MarkdownDocument document, DocFile file)
	{
		foreach (var link in document.Descendants<LinkInline>())
		{
			if (link.Url is not { Length: > 0 } url || Uri.IsWellFormedUriString(url, UriKind.Absolute))
				continue;

			var root = link.IsImage ? DocFile.RepositoryRawUrl : DocFile.RepositoryBlobUrl;

			// Like on GitHub, a leading "/" starts from the repository's root
			if (url.StartsWith('/'))
				link.Url = root + url[1..];
			else if (Uri.TryCreate(new Uri(root + file.Path), url, out var resolved))
				link.Url = resolved.ToString();
		}

		var linkTargetsByImageUrl = new Dictionary<string, string>(StringComparer.Ordinal);

		foreach (var link in document.Descendants<LinkInline>())
			if (link is { IsImage: true, Url: { } url, Parent: LinkInline { IsImage: false, Url: { } target } })
				linkTargetsByImageUrl[url] = target;

		return (document, linkTargetsByImageUrl);
	}

	// Prefers a local copy, falling back to the repository on GitHub
	private static async Task<(string Markdown, string Source)> ReadAsync(DocFile file, CancellationToken token)
	{
		foreach (var root in LocalRoots)
		{
			var path = Path.Combine(root, file.Path);

			if (File.Exists(path))
				return (await File.ReadAllTextAsync(path, token).ConfigureAwait(false), path);
		}

		return (await Http.Client.GetStringAsync(file.Uri, token).ConfigureAwait(false), file.Uri);
	}

	private static void CancelAndDispose(CancellationTokenSource? cts)
	{
		if (cts is null)
			return;

		cts.Cancel();
		cts.Dispose();
	}
}

/// <param name="Path">The repository-relative path, also used to find a local copy.</param>
public sealed record DocFile(string Name, string Path)
{
	public const string RepositoryRawUrl =
		HttpsScheme + "raw.githubusercontent.com/" + GitHubRepoPath + "/refs/heads/main/";

	public const string RepositoryBlobUrl = GitHubRepoUrl + "/blob/main/";

	public string Uri => RepositoryRawUrl + Path;
}
