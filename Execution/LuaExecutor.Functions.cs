using Lua;

namespace EnsembleRoot.Execution;

public static partial class LuaExecutor
{
	// Shown by help(); "?" marks optional parameters, and "|" separates alternatives
	private static readonly (LuaFunction Function, string Parameters)[] Functions =
	[
		(new LuaFunction(nameof(add_inst), add_inst), "asset_id, x?, y?, z?, rx?, ry?, rz?"),
		(new LuaFunction(nameof(add_rand_insts), add_rand_insts), "plot_id, count"),
		(new LuaFunction(nameof(cap_fps), cap_fps), "fps?"),
		(new LuaFunction(nameof(chat), chat), "message"),
		(new LuaFunction(nameof(clr_insts), clr_insts), "plot_id"),
		(new LuaFunction(nameof(clr_log), clr_log), ""),
		(new LuaFunction(nameof(dmp_asm_info), dmp_asm_info), ""),
		(new LuaFunction(nameof(dmp_chat), dmp_chat), ""),
		(new LuaFunction(nameof(dmp_env), dmp_env), ""),
		(new LuaFunction(nameof(dmp_inp_map), dmp_inp_map), ""),
		(new LuaFunction(nameof(dmp_insts), dmp_insts), ""),
		(new LuaFunction(nameof(dmp_peers), dmp_peers), ""),
		(new LuaFunction(nameof(dmp_vsync_modes), dmp_vsync_modes), ""),
		(new LuaFunction(nameof(gc), gc), ""),
		(new LuaFunction(nameof(help), help), ""),
		(new LuaFunction(nameof(kick_occupant), kick_occupant), "player"),
		(new LuaFunction(nameof(kick_peer), kick_peer), "peer_id, reason?"),
		(new LuaFunction(nameof(log_lan_ip4_addr), log_lan_ip4_addr), ""),
		(new LuaFunction(nameof(log_wan_ip4_addr), log_wan_ip4_addr), ""),
		(new LuaFunction(nameof(perf_mod), perf_mod), ""),
		(new LuaFunction(nameof(print), print), "..."),
		(new LuaFunction(nameof(quit), quit), "force?"),
		(new LuaFunction(nameof(restart), restart), ""),
		(new LuaFunction(nameof(rm_insts), rm_insts), "instance_id, ..."),
		(new LuaFunction(nameof(set_chat_log_on), set_chat_log_on), "is_on"),
		(new LuaFunction(nameof(set_chat_tts_on), set_chat_tts_on), "is_on"),
		(new LuaFunction(nameof(set_clock_offset), set_clock_offset), "seconds?"),
		(new LuaFunction(nameof(set_inst_prop), set_inst_prop), "instance_id, key, value"),
		(new LuaFunction(nameof(set_profanity_filter_on), set_profanity_filter_on), "is_on"),
		(new LuaFunction(nameof(set_sky_time), set_sky_time), "hours?"),
		(new LuaFunction(nameof(set_static_shader_on), set_static_shader_on), "is_on"),
		(new LuaFunction(nameof(set_ui_dark_theme_on), set_ui_dark_theme_on), "is_on?"),
		(new LuaFunction(nameof(set_ui_scale), set_ui_scale), "scale"),
		(new LuaFunction(nameof(set_vsync_mode), set_vsync_mode), "mode"),
		(new LuaFunction(nameof(stress_test), stress_test), "plot_id, count?, seconds?"),
		(new LuaFunction(nameof(tp_char), tp_char), "player? | x, y, z"),
		(new LuaFunction(nameof(tts), tts), "text, culture?, rate?, pitch?, volume?"),
		(new LuaFunction(nameof(wait), wait), "seconds?")
	];

	private static void InjectCustomFunctions(LuaTable env)
	{
		foreach (var (function, _) in Functions)
			env[function.Name] = function;
	}

	extension(LuaFunctionExecutionContext context)
	{
		// An explicit nil skips an optional argument, too
		private bool IsArgumentOmitted(int index) =>
			!context.HasArgument(index) || context.GetArgument<LuaValue>(index).Type is LuaValueType.Nil;

		private T GetArgumentOrDefault<T>(int index, T fallback = default!) =>
			context.IsArgumentOmitted(index) ? fallback : context.GetArgument<T>(index);

		private ValueTask<int> ReturnNothing() => new(context.Return());
	}
}
