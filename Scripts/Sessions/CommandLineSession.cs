using System.Globalization;
using EnsembleRoot.Autoloading;
using EnsembleRoot.Common.Networking;
using EnsembleRoot.Scripts.Profanity;
using EnsembleRoot.Sessions.Api;
using Godot;
using Serilog;
using static EnsembleRoot.Sessions.SessionManager;

namespace EnsembleRoot.Scripts.Sessions;

/// <summary>
///     Hosts a session, or joins one, from user args, e.g.:
///     <c lang="shell">--headless -- --port=7777 --password=abc --max-clients=8 --upnp --profanity-filter=false</c> or
///     <c lang="shell">--headless -- --join=127.0.0.1:7777 --password=abc --name=Bot</c>. Each arg also has a short
///     form (see <see cref="ArgAliases" />), e.g., <c lang="shell">--p=7777 --pw=abc</c>.
/// </summary>
/// <remarks>
///     A headless server always starts one, hosting a dedicated session unless told to join, and quits when it ends.
///     A regular client only starts one when given a port or an address (hosting as a player), e.g., to try a host
///     and a client side by side, and returns to the menu when it ends.
/// </remarks>
[GlobalClass]
[Autoload(Order = AutoloadOrder.Late + 2, FailurePolicy = AutoloadFailurePolicy.FailFast)]
public partial class CommandLineSession : Node, IAutoload
{
	private static readonly Dictionary<string, string> ArgAliases = new(StringComparer.OrdinalIgnoreCase)
	{
		["j"] = "join",
		["mc"] = "max-clients",
		["n"] = "name",
		["p"] = "port",
		["pf"] = "profanity-filter",
		["pw"] = "password",
		["u"] = "upnp"
	};

	public void Initialize()
	{
		var args = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		foreach (var arg in OS.GetCmdlineUserArgs())
		{
			if (arg.Split('=', 2) is not [var key, .. var rest])
				continue;

			var name = key.TrimStart('-');
			args[ArgAliases.GetValueOrDefault(name, name)] = rest is [var value] ? value : string.Empty;
		}

		// A headless server quits when its session ends; a regular client opens the menu unless asked to host or join
		if (Main.IsHeadlessServer)
			GSessionManager.SessionFailed += OnSessionFailed;
		else if (!args.ContainsKey("join") && !args.ContainsKey("port"))
			return;

		var password = args.GetValueOrDefault("password");
		var displayName = args.GetValueOrDefault("name");

		if (args.GetValueOrDefault("join") is { } join)
		{
			if (HostEndPoint.TryParse(join, out var endPoint))
				GSessionManager.JoinMultiPlayer(new JoinConfig(endPoint.Host, endPoint.Port, password), displayName);
			else
				Callable.From(() => OnSessionFailed($"Invalid join address: {join}")).CallDeferred();
		}
		else
		{
			ProfanityFilterManager.Override = GetBool(args, "profanity-filter");

			var maxClientCount = GetInt(args, "max-clients") is { } count and not Unlimited ? count : (int?)null;
			var config = new HostConfig(
				GetInt(args, "port") ?? DefaultPort, password, maxClientCount, Main.IsHeadlessServer,
				GetFlag(args, "upnp"));

			// A dedicated host has no player, so no name
			GSessionManager.HostMultiPlayer(config, Main.IsHeadlessServer ? null : displayName);
		}
	}

	public override void _ExitTree() => GSessionManager.SessionFailed -= OnSessionFailed;

	private static int? GetInt(Dictionary<string, string> args, string key) =>
		int.TryParse(args.GetValueOrDefault(key), CultureInfo.InvariantCulture, out var value) ? value : null;

	private static bool GetFlag(Dictionary<string, string> args, string key) => GetBool(args, key) is true;

	// Present without a value means true
	private static bool? GetBool(Dictionary<string, string> args, string key) =>
		args.TryGetValue(key, out var value) ? value.Length is 0 || (bool.TryParse(value, out var flag) && flag) : null;

	private static void OnSessionFailed(string reason)
	{
		Log.Warning("Command-line session ended: {Reason}", reason);

		if (Main.IsHeadlessServer)
			GMain.Quit();
	}
}
