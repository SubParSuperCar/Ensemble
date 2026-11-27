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
///     and a client side by side, or <c lang="shell">--single-player</c>, and returns to the menu when it ends.
/// </remarks>
[GlobalClass]
[Autoload(Order = AutoloadOrder.Late + 2, FailurePolicy = AutoloadFailurePolicy.FailFast)]
public partial class CommandLineSession : Node, IAutoload
{
	public const string MaxClientsArg = "max-clients";
	public const string PasswordArg = "password";
	public const string PortArg = "port";
	public const string ProfanityFilterArg = "profanity-filter";
	public const string UpnpArg = "upnp";

	private const string JoinArg = "join";
	private const string NameArg = "name";
	private const string SinglePlayerArg = "single-player";

	private static readonly Dictionary<string, string> ArgAliases = new(StringComparer.OrdinalIgnoreCase)
	{
		["j"] = JoinArg,
		["mc"] = MaxClientsArg,
		["n"] = NameArg,
		["p"] = PortArg,
		["pf"] = ProfanityFilterArg,
		["pw"] = PasswordArg,
		["sp"] = SinglePlayerArg,
		["u"] = UpnpArg
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

		// A headless server quits when its session ends; a regular client opens the menu unless asked for a session
		if (Main.IsHeadlessServer)
			GSessionManager.SessionFailed += OnSessionFailed;
		else if (!args.ContainsKey(JoinArg) && !args.ContainsKey(PortArg) && !args.ContainsKey(SinglePlayerArg))
			return;

		var password = args.GetValueOrDefault(PasswordArg);
		var displayName = args.GetValueOrDefault(NameArg);

		if (!Main.IsHeadlessServer && args.ContainsKey(SinglePlayerArg))
			GSessionManager.StartSinglePlayer(displayName);
		else if (args.GetValueOrDefault(JoinArg) is { } join)
		{
			if (HostEndPoint.TryParse(join, out var endPoint))
				GSessionManager.JoinMultiPlayer(new JoinConfig(endPoint.Host, endPoint.Port, password), displayName);
			else
				Callable.From(() => OnSessionFailed($"Invalid join address: {join}")).CallDeferred();
		}
		else
		{
			ProfanityFilterManager.Override = GetBool(args, ProfanityFilterArg);

			var maxClientCount = GetInt(args, MaxClientsArg) is { } count and not Unlimited ? count : (int?)null;
			var config = new HostConfig(
				GetInt(args, PortArg) ?? DefaultPort, password, maxClientCount, Main.IsHeadlessServer,
				GetFlag(args, UpnpArg));

			// A dedicated host has no player, so no name
			GSessionManager.HostMultiPlayer(config, Main.IsHeadlessServer ? null : displayName);
		}
	}

	/// <summary>Formats a user arg as this class reads it, e.g., to start another instance.</summary>
	public static string FormatArg(string name, string? value = null) =>
		value is null ? $"--{name}" : $"--{name}={value}";

	/// <summary>
	///     Formats the user args that start a session like the current one: the same kind, port or address, password,
	///     and name. None outside a session.
	/// </summary>
	public static IReadOnlyList<string> FormatCurrentSession()
	{
		var args = new List<string>();

		switch (GSessionManager.Config)
		{
			case HostConfig host:
				args.Add(FormatArg(PortArg, host.Port.ToString(CultureInfo.InvariantCulture)));

				if (host.MaxClientCount is { } count)
					args.Add(FormatArg(MaxClientsArg, count.ToString(CultureInfo.InvariantCulture)));

				if (host.IsUpnpEnabled)
					args.Add(FormatArg(UpnpArg));

				break;

			case JoinConfig join:
				args.Add(FormatArg(JoinArg, new HostEndPoint(join.Address, join.Port).ToString()));
				break;

			case null when GSessionManager.Mode is SessionMode.SinglePlayer:
				args.Add(FormatArg(SinglePlayerArg));
				break;

			default:
				return args;
		}

		if (GSessionManager.Config?.Password is { Length: > 0 } password)
			args.Add(FormatArg(PasswordArg, password));

		if (GSessionManager.LocalPeer?.DisplayName is { Length: > 0 } name)
			args.Add(FormatArg(NameArg, name));

		return args;
	}

	public override void _ExitTree() => GSessionManager.SessionFailed -= OnSessionFailed;

	private static int? GetInt(Dictionary<string, string> args, string key)
	{
		if (!args.TryGetValue(key, out var text))
			return null;

		if (int.TryParse(text, CultureInfo.InvariantCulture, out var value))
			return value;

		Log.Warning("Ignored {Arg}, which isn't a whole number", FormatArg(key, text));
		return null;
	}

	private static bool GetFlag(Dictionary<string, string> args, string key) => GetBool(args, key) is true;

	// Present without a value means true
	private static bool? GetBool(Dictionary<string, string> args, string key)
	{
		if (!args.TryGetValue(key, out var text))
			return null;

		if (text.Length is 0)
			return true;

		if (bool.TryParse(text, out var value))
			return value;

		Log.Warning("Read {Arg} as false, as it's neither true nor false", FormatArg(key, text));
		return false;
	}

	private static void OnSessionFailed(string reason)
	{
		Log.Warning("Command-line session ended: {Reason}", reason);

		if (Main.IsHeadlessServer)
			GMain.Quit();
	}
}
