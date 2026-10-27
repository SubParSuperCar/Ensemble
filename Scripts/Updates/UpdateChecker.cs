#if EXPORT
using System.Net.Http.Headers;
using System.Text.Json;
using EnsembleRoot.Autoloading;
using EnsembleRoot.Common.Interop;
using EnsembleRoot.Common.Networking;
using EnsembleRoot.Common.Utils;
using Godot;
using Serilog;
using TinyDialogsNet;

namespace EnsembleRoot.Scripts.Updates;

/// <summary>Checks GitHub for a newer release on startup and offers to open its page.</summary>
/// <remarks>Each newer version is only offered once; the last offered version is kept in the user data.</remarks>
[GlobalClass]
[Autoload(
	Scope = AutoloadScope.RegularClient,
	Order = AutoloadOrder.Late + 1,
	FailurePolicy = AutoloadFailurePolicy.LogAndContinue)]
public partial class UpdateChecker : Node, IAutoload
{
	private const string LatestReleaseApiUrl =
		HttpsScheme + "api.github.com/repos/" + GitHubRepoPath + "/releases/latest";

	private const string Section = "updates";
	private const string OfferedVersionKey = "offered_version";

	private readonly CancellationTokenSource _cts = new();

	public void Initialize() => _ = CheckAsync();

	public override void _ExitTree() => _cts.Cancel();

	// The page opens in the browser, so it must stay on GitHub
	private static bool IsGitHubUrl(string? url) =>
		Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri is { Scheme: "https", Host: "github.com" };

	private static bool TryParseVersion(string? text, out Version version) =>
		Version.TryParse(text?.TrimStart('v', 'V'), out version!);

	// Runs on the thread pool, unobserved, and dialogs can fail (e.g., without a dialog tool on Linux)
	private static void Offer(Version latest, Version current, string url)
	{
		try
		{
			var isAccepted = YesNoDialog.Ask(
				"Ensemble Update Available",
				$"Ensemble v{latest} is available (you have v{current}).\n\n" +
				"Open its release page to download it?\nYou will not be asked again for this version.",
				MessageBoxIconType.Question, true);

			// Only once answered, so a failed dialog asks again next launch
			Callable.From(() => UserData.SetValue(Section, OfferedVersionKey, latest.ToString())).CallDeferred();

			if (isAccepted)
				Callable.From(() => OS.ShellOpen(url)).CallDeferred();
		}
		catch (Exception exception)
		{
			Log.Warning(exception, "Failed to offer update v{Latest}", latest);
		}
	}

	private async Task CheckAsync()
	{
		try
		{
			if (!TryParseVersion(GameVersion, out var current))
			{
				Log.Warning("Skipped update check: unparsable version {Version}", GameVersion);
				return;
			}

			using var request = new HttpRequestMessage(HttpMethod.Get, LatestReleaseApiUrl);
			request.Headers.UserAgent.Add(new ProductInfoHeaderValue("Ensemble", current.ToString()));
			request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

			using var response = await Http.Client.SendAsync(request, _cts.Token).ConfigureAwait(true);
			response.EnsureSuccessStatusCode();

			var stream = await response.Content.ReadAsStreamAsync(_cts.Token).ConfigureAwait(true);
			using var document =
				await JsonDocument.ParseAsync(stream, cancellationToken: _cts.Token).ConfigureAwait(true);

			var release = document.RootElement;

			if (!TryParseVersion(release.GetProperty("tag_name").GetString(), out var latest))
				return;

			var offeredText = UserData.GetValue(Section, OfferedVersionKey, string.Empty).AsString();
			var offered = TryParseVersion(offeredText, out var version) ? version : null;

			if (latest <= current || latest <= offered)
			{
				Log.Debug("No update to offer (Latest={Latest}, Current={Current})", latest, current);
				return;
			}

			Log.Information("Update available: v{Latest} (current v{Current})", latest, current);

			var url = release.TryGetProperty("html_url", out var htmlUrl) ? htmlUrl.GetString() : null;
			_ = Task.Run(() => Offer(latest, current, IsGitHubUrl(url) ? url! : GitHubRepoUrl + "/releases/latest"));
		}
		catch (OperationCanceledException) when (_cts.IsCancellationRequested) { }
		catch (Exception exception)
		{
			Log.Information(exception, "Failed to check for updates");
		}
		finally
		{
			Callable.From(QueueFree).CallDeferred();
		}
	}
}

#endif
