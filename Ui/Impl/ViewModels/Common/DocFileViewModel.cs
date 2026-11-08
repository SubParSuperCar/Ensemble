using System.Collections.Frozen;
using System.Diagnostics;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnsembleRoot.Common.Networking;
using EnsembleRoot.Ui.Abstractions;
using Godot;
using LiveMarkdown.Avalonia;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Serilog;
using CodeInline = Markdig.Syntax.Inlines.CodeInline;
using Dispatcher = Avalonia.Threading.Dispatcher;

namespace EnsembleRoot.Ui.ViewModels;

public sealed partial class DocFileViewModel : ViewModelBase
{
	// Exports publish the license files beside the assemblies; editor runs have the whole repository on disk
	private static readonly string[] LocalRoots = OS.HasFeature("template")
		? [AppContext.BaseDirectory]
		: [ProjectSettings.GlobalizePath(ResourceScheme)];

	// Whoever swaps a source out of this field owns and disposes it
	private CancellationTokenSource? _cts;

	// GitHub's anchor for each heading, in document order
	private List<string> _headingAnchors = [];

	// LiveMarkdown makes each image a link to itself, which wins over a link around it, so linked images map to their
	// link's target
	private IReadOnlyDictionary<string, string> _linkTargetsByImageUrl = FrozenDictionary<string, string>.Empty;

	// Where a link to another document points in it, until it loads
	private string? _pendingAnchor;

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

	/// <summary>
	///     Occurs when a heading (by its index in the document) should scroll into view, or the top for null.
	/// </summary>
	public event Action<int?>? ScrollRequested;

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

			var (document, linkTargetsByImageUrl, headingAnchors) = await Task
				.Run(() => ResolveUrls(Markdown.Parse(markdown, MarkdownUpdateProducer.DefaultPipeline), file), token)
				.ConfigureAwait(false);

			await Dispatcher.UIThread.InvokeAsync(() =>
			{
				// A newer load or disposal cancels this one, so cancellation also marks it stale
				if (token.IsCancellationRequested)
					return;

				_linkTargetsByImageUrl = linkTargetsByImageUrl;
				_headingAnchors = headingAnchors;
				Document = new MarkdownDocumentUpdate.Full(document);

				ScrollToAnchor(Interlocked.Exchange(ref _pendingAnchor, null));

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

		if (href is not { IsAbsoluteUri: true } || TryOpenLocally(href))
			return;

		// Other schemes (e.g., file:) could run programs
		if (href.Scheme is "https" or "http" or "mailto")
			OS.ShellOpen(href.AbsoluteUri);
	}

	// Links within the repository's documents stay in this viewer; ResolveUrls made them all absolute
	private bool TryOpenLocally(Uri href)
	{
		var url = href.GetLeftPart(UriPartial.Path);

		if (!url.StartsWith(DocFile.RepositoryBlobUrl, StringComparison.Ordinal))
			return false;

		var path = Uri.UnescapeDataString(url[DocFile.RepositoryBlobUrl.Length..]);

		if (Files.FirstOrDefault(file => string.Equals(file.Path, path, StringComparison.Ordinal)) is not { } target)
			return false;

		var anchor = Uri.UnescapeDataString(href.Fragment.TrimStart('#'));

		if (target == SelectedFile)
			ScrollToAnchor(anchor);
		else
		{
			_pendingAnchor = anchor;
			SelectedFile = target;
		}

		return true;
	}

	private void ScrollToAnchor(string? anchor)
	{
		var index = string.IsNullOrEmpty(anchor) ? -1 : _headingAnchors.IndexOf(anchor);
		ScrollRequested?.Invoke(index < 0 ? null : index);
	}

	// GitHub's rule: lower case, without punctuation, with hyphens for spaces, and numbered when repeated
	private static List<string> GetHeadingAnchors(MarkdownDocument document)
	{
		var counts = new Dictionary<string, int>(StringComparer.Ordinal);
		var anchors = new List<string>();

		foreach (var heading in document.Descendants<HeadingBlock>())
		{
			var text = string.Concat(
				heading.Inline?.Descendants<LeafInline>().Select(static inline => inline switch
				{
					LiteralInline literal => literal.Content.ToString(),
					CodeInline code => code.Content,
					_ => string.Empty
				}) ?? []);

			var slug = string.Concat(
				text.ToLowerInvariant()
					.Where(static c => char.IsLetterOrDigit(c) || c is ' ' or '-' or '_')
					.Select(static c => c is ' ' ? '-' : c));

			var count = counts.GetValueOrDefault(slug);

			counts[slug] = count + 1;
			anchors.Add(count is 0 ? slug : string.Create(CultureInfo.InvariantCulture, $"{slug}-{count}"));
		}

		return anchors;
	}

	/// <summary>
	///     Points relative URLs at the repository on GitHub, even for local copies: images at their raw files, and
	///     links at their pages. LiveMarkdown would otherwise resolve them as local file paths. Also maps each linked
	///     image's URL to its link's target.
	/// </summary>
	private static (
		MarkdownDocument Document,
		IReadOnlyDictionary<string, string> LinkTargetsByImageUrl,
		List<string> HeadingAnchors) ResolveUrls(MarkdownDocument document, DocFile file)
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

		return (document, linkTargetsByImageUrl, GetHeadingAnchors(document));
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
