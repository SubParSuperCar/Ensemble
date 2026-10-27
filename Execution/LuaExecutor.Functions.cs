using EnsembleRoot.GdCore.Players;
using EnsembleRoot.GdCore.Plots;
using EnsembleRoot.Scripts.Plots;
using Godot;
using Lua;
using Serilog;

namespace EnsembleRoot.Execution;

public static partial class LuaExecutor
{
	// Shown by help(); "?" marks optional parameters, "|" separates alternatives, "{...}" is a table of options, "loc_"
	// marks functions that skip the network (Single-Player only), and a player is a peer ID, a player ID, or a unique
	// display name
	private static readonly (LuaFunction Function, string Parameters, string Description)[] Functions =
	[
		(new LuaFunction(nameof(add_inst), add_inst), "asset_id, x?, y?, z?, rx?, ry?, rz?",
			"Places an asset on your plot (grid units, degrees), resting on the floor by default"),
		(new LuaFunction(nameof(ban_addr), ban_addr), "address, reason?",
			"Kicks everyone at an IP address and bans it for the session"),
		(new LuaFunction(nameof(ban_peer), ban_peer), "player, reason?", "Bans a player's IP address for the session"),
		(new LuaFunction(nameof(cap_fps), cap_fps), "fps?", "Caps the frame rate (none or 0 uncaps it)"),
		(new LuaFunction(nameof(chat), chat), "message", "Sends a chat message"),
		(new LuaFunction(nameof(clr_insts), clr_insts), "", "Removes every instance from your plot"),
		(new LuaFunction(nameof(clr_log), clr_log), "", "Clears the log view"),
		(new LuaFunction(nameof(dmp_asm_info), dmp_asm_info), "", "Logs the build and the loaded assemblies"),
		(new LuaFunction(nameof(dmp_assets), dmp_assets), "", "Lists the assets with their IDs, caps, and properties"),
		(new LuaFunction(nameof(dmp_bans), dmp_bans), "", "Lists the banned addresses"),
		(new LuaFunction(nameof(dmp_chat), dmp_chat), "", "Logs the chat history"),
		(new LuaFunction(nameof(dmp_creation), dmp_creation), "plot_id?",
			"Logs a spawned creation's assemblies and simulator"),
		(new LuaFunction(nameof(dmp_env), dmp_env), "", "Logs every global in the Lua environment"),
		(new LuaFunction(nameof(dmp_inp_map), dmp_inp_map), "", "Logs every input action and its bindings"),
		(new LuaFunction(nameof(dmp_insts), dmp_insts), "plot_id?", "Lists a plot's instances"),
		(new LuaFunction(nameof(dmp_peers), dmp_peers), "", "Logs the session and its peers"),
		(new LuaFunction(nameof(dmp_plots), dmp_plots), "", "Lists the plots with their instances and occupants"),
		(new LuaFunction(nameof(dmp_saves), dmp_saves), "", "Lists the saved creations"),
		(new LuaFunction(nameof(dmp_scripts), dmp_scripts), "", "Lists the scripts in user://scripts/"),
		(new LuaFunction(nameof(dmp_vsync_modes), dmp_vsync_modes), "", "Lists the VSync modes"),
		(new LuaFunction(nameof(gc), gc), "", "Runs a full garbage collection and logs the memory reclaimed"),
		(new LuaFunction(nameof(help), help), "query?", "Lists the functions, or those whose names contain the query"),
		(new LuaFunction(nameof(host_mp), host_mp), "{name, port, password, max_clients, upnp}?",
			"Hosts a multiplayer session, by default with the menu's remembered settings"),
		(new LuaFunction(nameof(host_server), host_server), "{port, password, max_clients, upnp, profanity_filter}?",
			"Starts a headless server on this machine and returns its PID once it listens"),
		(new LuaFunction(nameof(join_mp), join_mp), "address, {name, password}?",
			"Joins a session at host or host:port"),
		(new LuaFunction(nameof(kick_occupant), kick_occupant), "player", "Removes a player from their plot"),
		(new LuaFunction(nameof(kick_peer), kick_peer), "player, reason?", "Disconnects a player from the session"),
		(new LuaFunction(nameof(load_creation), load_creation), "name, password?",
			"Adds a saved creation to your plot through the network"),
		(new LuaFunction(nameof(loc_add_rand_insts), loc_add_rand_insts), "plot_id, count",
			"Scatters random instances on a plot"),
		(new LuaFunction(nameof(loc_clr_insts), loc_clr_insts), "plot_id", "Removes every instance from a plot"),
		(new LuaFunction(nameof(loc_load_creation), loc_load_creation), "plot_id, name, password?",
			"Adds a saved creation to a plot directly"),
		(new LuaFunction(nameof(loc_stress_test), loc_stress_test), "plot_id, count?, seconds?",
			"Measures frame times with random instances, returning the average and worst in ms"),
		(new LuaFunction(nameof(log_lan_ip4_addr), log_lan_ip4_addr), "", "Logs this machine's LAN IPv4 address"),
		(new LuaFunction(nameof(log_wan_ip4_addr), log_wan_ip4_addr), "",
			"Logs this network's public IPv4 address (asks api.ipify.org)"),
		(new LuaFunction(nameof(perf_mod), perf_mod), "",
			"Turns off costly effects (antialiasing, sky, glow, terrain, lights)"),
		(new LuaFunction(nameof(print), print), "...", "Logs its arguments, showing tables' contents"),
		(new LuaFunction(nameof(quit), quit), "force?", "Quits the game (force skips the shutdown sequence)"),
		(new LuaFunction(nameof(redo), redo), "", "Redoes the last undone tool edit"),
		(new LuaFunction(nameof(restart), restart), "", "Restarts the game with the same arguments"),
		(new LuaFunction(nameof(rm_creation), rm_creation), "name, permanently?",
			"Moves a saved creation to the OS trash, or deletes it for good after a dialog"),
		(new LuaFunction(nameof(rm_insts), rm_insts), "instance_id, ...", "Removes instances from your plot by ID"),
		(new LuaFunction(nameof(run_script), run_script), "name",
			"Runs a script from user://scripts/ and returns its results"),
		(new LuaFunction(nameof(save_creation), save_creation),
			"name, {plot_id, compression, level, password, checksum}?",
			"Saves a plot (yours by default) to user://creations/"),
		(new LuaFunction(nameof(set_chat_log_on), set_chat_log_on), "is_on", "Turns chat file logging on or off"),
		(new LuaFunction(nameof(set_chat_tts_on), set_chat_tts_on), "is_on", "Turns reading chat aloud on or off"),
		(new LuaFunction(nameof(set_clock_offset), set_clock_offset), "seconds?",
			"Shifts the game's clock by seconds (none resets it)"),
		(new LuaFunction(nameof(set_construct_asset), set_construct_asset), "asset_id",
			"Picks the asset the construct (Place) tool places"),
		(new LuaFunction(nameof(set_construct_rot), set_construct_rot), "rx?, ry?, rz?",
			"Sets the construct tool's rotation in degrees"),
		(new LuaFunction(nameof(set_inst_prop), set_inst_prop), "instance_id, key, value?",
			"Sets an instance's property (nil resets it to the asset's default)"),
		(new LuaFunction(nameof(set_net_sim), set_net_sim), "latency_ms?, jitter_ms?, loss?",
			"Simulates latency, jitter, and loss on incoming live state"),
		(new LuaFunction(nameof(set_plot), set_plot), "plot_id?", "Claims a plot, or leaves yours with none"),
		(new LuaFunction(nameof(set_plot_simulator), set_plot_simulator), "plot_id, player?",
			"Overrides who simulates a plot's creation (host only; none resets it)"),
		(new LuaFunction(nameof(set_plot_spawned), set_plot_spawned), "is_spawned",
			"Spawns or despawns your plot's creation"),
		(new LuaFunction(nameof(set_profanity_filter_on), set_profanity_filter_on), "is_on",
			"Turns the profanity filter on or off (host only)"),
		(new LuaFunction(nameof(set_sky_time), set_sky_time), "hours?",
			"Sets the time of day in hours, or follows the system clock with none"),
		(new LuaFunction(nameof(set_static_shader_on), set_static_shader_on), "is_on",
			"Shows or hides the temporal static shader"),
		(new LuaFunction(nameof(set_tool_on), set_tool_on), "tool, is_on",
			"Turns the construct, destruct, or property tool on or off"),
		(new LuaFunction(nameof(set_ui_dark_theme_on), set_ui_dark_theme_on), "is_on?",
			"Picks the dark or light UI theme, or the system's with none"),
		(new LuaFunction(nameof(set_ui_scale), set_ui_scale), "scale", "Sets the UI render scale"),
		(new LuaFunction(nameof(set_vsync_mode), set_vsync_mode), "mode", "Sets the VSync mode by name or number"),
		(new LuaFunction(nameof(start_sp), start_sp), "name?",
			"Starts a single-player session, by default with the remembered name"),
		(new LuaFunction(nameof(stop_servers), stop_servers), "", "Stops the headless servers host_server started"),
		(new LuaFunction(nameof(stop_session), stop_session), "", "Ends the current session"),
		(new LuaFunction(nameof(tp_char), tp_char), "player? | x, y, z",
			"Teleports your character to a player or position, or resets it with none"),
		(new LuaFunction(nameof(tts), tts), "text, culture?, rate?, pitch?, volume?", "Reads text aloud"),
		(new LuaFunction(nameof(unban_addr), unban_addr), "address", "Lifts an address's ban"),
		(new LuaFunction(nameof(unban_all), unban_all), "", "Lifts every ban"),
		(new LuaFunction(nameof(undo), undo), "", "Undoes the last tool edit"),
		(new LuaFunction(nameof(wait), wait), "seconds?",
			"Yields for seconds (a frame by default) and returns the time waited")
	];

	private static void InjectCustomFunctions(LuaTable env)
	{
		foreach (var (function, _, _) in Functions)
			env[function.Name] = function;
	}

	private static (GdPlot Plot, PlotHandle Handle)? GetLocalPlot(string function)
	{
		if (LocalPlot is { } plot && GPlotManager.GetHandleOrNull(plot.Id) is { } handle)
			return (plot, handle);

		Log.Error("{Function} needs a plot; claim one first", function);
		return null;
	}

	// An omitted plot ID means the local plot
	private static GdPlot? FindPlot(int? plotId, string function)
	{
		if ((plotId ?? LocalPlot?.Id) is not { } id)
		{
			Log.Error("{Function} needs a plot ID or a claimed plot", function);
			return null;
		}

		var plot = GPlots.GetPlot(id);

		if (plot is null)
			Log.Error("Plot with ID {PlotId} not found", id);

		return plot;
	}

	/// <summary>Finds a player by peer ID (a number), player ID, or, if unique, display name.</summary>
	private static GdPlayer? FindPlayer(LuaValue query)
	{
		var player = query.Type switch
		{
			LuaValueType.Number when ToInt(query) is { } peerId && GSessionManager.GetPeer(peerId) is { } peer =>
				GPlayers.GetPlayer(peer.PlayerId),
			LuaValueType.String => FindPlayerByIdOrName(query.Read<string>()),
			_ => null
		};

		if (player is null)
			Log.Error("Player {Query} not found", query);

		return player;
	}

	/// <summary>Like <see cref="FindPlayer(LuaValue)" />, but takes any peer ID, registered or not.</summary>
	private static int? FindPeerId(LuaValue query)
	{
		if (query.Type is not LuaValueType.Number)
			return FindPlayer(query) is { } player ? GSessionManager.GetPeerByPlayerId(player.Id)?.Id : null;

		var peerId = ToInt(query);

		if (peerId is null)
			Log.Error("Invalid peer ID: {PeerId}", query);

		return peerId;
	}

	private static int? ToInt(LuaValue value) =>
		value.Read<double>() is var number && double.IsInteger(number) && number is >= int.MinValue and <= int.MaxValue
			? (int)number
			: null;

	private static GdPlayer? FindPlayerByIdOrName(string query)
	{
		var players = GPlayers.GetAll();

		if (players.FirstOrDefault(player => string.Equals(player.Id, query, StringComparison.Ordinal)) is { } byId)
			return byId;

		var named = players
			.Where(other => string.Equals(other.Name, query, StringComparison.Ordinal))
			.Take(2)
			.ToArray();

		if (named.Length > 1)
			Log.Error("Several players are named {Name}; use a peer or player ID instead", query);

		return named.Length is 1 ? named[0] : null;
	}

	extension(LuaFunctionExecutionContext context)
	{
		// An explicit nil skips an optional argument, too
		private bool IsArgumentOmitted(int index) =>
			!context.HasArgument(index) || context.GetArgument<LuaValue>(index).Type is LuaValueType.Nil;

		private T GetArgumentOrDefault<T>(int index, T fallback = default!) =>
			context.IsArgumentOmitted(index) ? fallback : context.GetArgument<T>(index);

		private T? GetArgumentOrNull<T>(int index) where T : struct =>
			context.IsArgumentOmitted(index) ? null : context.GetArgument<T>(index);

		private ValueTask<int> ReturnNothing() => new(context.Return());

		// Euler angles in degrees, like the Godot editor's, each 0 when omitted
		private Quaternion GetRotationArguments(int index) =>
			Quaternion.FromEuler(new Vector3(
				float.DegreesToRadians(context.GetArgumentOrDefault(index, 0f)),
				float.DegreesToRadians(context.GetArgumentOrDefault(index + 1, 0f)),
				float.DegreesToRadians(context.GetArgumentOrDefault(index + 2, 0f)))).Normalized();
	}
}
