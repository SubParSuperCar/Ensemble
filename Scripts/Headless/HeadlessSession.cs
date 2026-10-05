using System.Globalization;
using EnsembleRoot.Autoloading;
using EnsembleRoot.Common.Networking;
using Godot;
using Serilog;
using static EnsembleRoot.SessionManager.SessionManager;

namespace EnsembleRoot.Scripts.Headless;

/// <summary>
///     Hosts a dedicated session, or joins one as a client, from user args, e.g.:
///     <c lang="shell">--headless -- --port=7777 --password=abc --max-clients=8 --upnp</c> or
///     <c lang="shell">--headless -- --join=127.0.0.1:7777 --password=abc --name=Bot</c>.
/// </summary>
[GlobalClass]
[Autoload(
	Scope = AutoloadScope.HeadlessServer,
	Order = AutoloadOrder.Late + 1,
	FailurePolicy = AutoloadFailurePolicy.FailFast)]
public partial class HeadlessSession : Node, IAutoload
{
	public void Initialize()
	{
		var args = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		foreach (var arg in OS.GetCmdlineUserArgs())
			if (arg.Split('=', 2) is [var key, .. var rest])
				args[key.TrimStart('-')] = rest is [var value] ? value : string.Empty;

		var password = args.GetValueOrDefault("password");
		GSessionManager.SessionFailed += OnSessionFailed;

		if (args.GetValueOrDefault("join") is { } join)
		{
			if (HostEndPoint.TryParse(join, out var endPoint))
				GSessionManager.JoinMultiPlayer(endPoint.Host, endPoint.Port, password, args.GetValueOrDefault("name"));
			else
				Callable.From(() => OnSessionFailed($"Invalid join address: {join}")).CallDeferred();
		}
		else
			GSessionManager.HostMultiPlayer(
				GetInt(args, "port") ?? DefaultPort,
				password,
				string.Empty,
				GetInt(args, "max-clients") ?? Unlimited,
				isDedicated: true,
				GetFlag(args, "upnp"));
	}

	public override void _ExitTree() => GSessionManager.SessionFailed -= OnSessionFailed;

	private static int? GetInt(Dictionary<string, string> args, string key) =>
		int.TryParse(args.GetValueOrDefault(key), CultureInfo.InvariantCulture, out var value) ? value : null;

	private static bool GetFlag(Dictionary<string, string> args, string key) =>
		args.TryGetValue(key, out var value) && (value.Length is 0 || (bool.TryParse(value, out var flag) && flag));

	private static void OnSessionFailed(string reason)
	{
		Log.Warning("Headless session ended: {Reason}", reason);
		GMain.Quit();
	}
}
