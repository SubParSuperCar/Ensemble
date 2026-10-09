using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using EnsembleRoot.Common.Networking;
using EnsembleRoot.Ui.Impl.Abstractions;
using Godot;
using LiveMarkdown.Avalonia;
using Markdig;
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

	public DocFileViewModel()
	{
		SelectedFile = Files[0];
	}

	public IReadOnlyList<DocFile> Files { get; } =
	[
		new("README.md", ".github/README.md"),
		new("CONTRIBUTING.md", ".github/CONTRIBUTING.md"),
		new("LICENSE.md", "LICENSE.md"),
		new("LICENSE-ASSETS.txt", ".github/LICENSE-ASSETS.txt"),
		new("LICENSE-CODE.txt", ".github/LICENSE-CODE.txt"),
		new("ROADMAP.md", ".github/ROADMAP.md")
	];

	[ObservableProperty] public partial DocFile SelectedFile { get; set; }
	[ObservableProperty] public partial MarkdownDocumentUpdate? Document { get; private set; }
	[ObservableProperty] public partial string? ImageBasePath { get; private set; }

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

			var document = await Task
				.Run(() => Markdown.Parse(markdown, MarkdownUpdateProducer.DefaultPipeline), token)
				.ConfigureAwait(false);

			await Dispatcher.UIThread.InvokeAsync(() =>
			{
				// A newer load or disposal cancels this one, so cancellation also marks it stale
				if (token.IsCancellationRequested)
					return;

				// Relative images resolve against the repository even for local copies
				ImageBasePath = new Uri(new Uri(file.Uri), ".").ToString();
				Document = new MarkdownDocumentUpdate.Full(document);

				stopwatch.Stop();
				Log.Debug(
					"Loaded {FileName} from {Source} in {ElapsedMs:F3} ms",
					file.Name, source, stopwatch.Elapsed.TotalMilliseconds);
			});
		}
		catch (Exception exception)
			when (exception is HttpRequestException or IOException or OperationCanceledException)
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
	private const string RepositoryRawUrl =
		HttpsScheme + "raw.githubusercontent.com/" + GitHubRepoPath + "/refs/heads/main/";

	public string Uri => RepositoryRawUrl + Path;
}
