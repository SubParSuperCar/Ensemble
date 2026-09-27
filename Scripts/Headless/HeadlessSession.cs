using System.Globalization;
using System.Net;
using EnsembleRoot.Autoloading;
using Godot;
using Serilog;
using static EnsembleRoot.SessionManager.SessionManager;

namespace EnsembleRoot.Scripts.Headless;

// Hosts a dedicated session, or joins one as a client, from user args, e.g.:
// --headless -- --port=7777 --password=abc --max-clients=8
// --headless -- --join=127.0.0.1:7777 --password=abc --name=Bot
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
			if (arg.Split('=', 2) is [var key, var value])
				args[key.TrimStart('-')] = value;

		var password = args.GetValueOrDefault("password");
		GSessionManager.SessionFailed += OnSessionFailed;

		if (args.GetValueOrDefault("join") is { } join && IPEndPoint.TryParse(join, out var endPoint))
			GSessionManager.JoinMultiPlayer(
				endPoint.Address.ToString(),
				endPoint.Port,
				password,
				args.GetValueOrDefault("name"));
		else
			GSessionManager.HostMultiPlayer(
				GetInt(args, "port") ?? DefaultPort,
				password,
				string.Empty,
				GetInt(args, "max-clients") ?? Unlimited,
				true);
	}

	public override void _ExitTree() => GSessionManager.SessionFailed -= OnSessionFailed;

	private static int? GetInt(Dictionary<string, string> args, string key) =>
		int.TryParse(args.GetValueOrDefault(key), CultureInfo.InvariantCulture, out var value) ? value : null;

	private static void OnSessionFailed(string reason)
	{
		Log.Fatal("Headless session failed: {Reason}", reason);
		GMain.Quit();
	}
}
