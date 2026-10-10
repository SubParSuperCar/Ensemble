using Lua;

namespace EnsembleRoot.Execution;

public static partial class LuaExecutor
{
	private static void InjectCustomFunctions(LuaTable env)
	{
		#region Function Registrations

		env[nameof(add_rand_insts)] = new LuaFunction(add_rand_insts);
		env[nameof(cap_fps)] = new LuaFunction(cap_fps);
		env[nameof(chat)] = new LuaFunction(chat);
		env[nameof(clr_insts)] = new LuaFunction(clr_insts);
		env[nameof(clr_log)] = new LuaFunction(clr_log);
		env[nameof(dmp_asm_info)] = new LuaFunction(dmp_asm_info);
		env[nameof(dmp_chat)] = new LuaFunction(dmp_chat);
		env[nameof(dmp_env)] = new LuaFunction(dmp_env);
		env[nameof(dmp_inp_map)] = new LuaFunction(dmp_inp_map);
		env[nameof(dmp_peers)] = new LuaFunction(dmp_peers);
		env[nameof(dmp_vsync_modes)] = new LuaFunction(dmp_vsync_modes);
		env[nameof(gc)] = new LuaFunction(gc);
		env[nameof(help)] = new LuaFunction(help);
		env[nameof(kick)] = new LuaFunction(kick);
		env[nameof(log_lan_ip4_addr)] = new LuaFunction(log_lan_ip4_addr);
		env[nameof(log_wan_ip4_addr)] = new LuaFunction(log_wan_ip4_addr);
		env[nameof(perf_mod)] = new LuaFunction(perf_mod);
		env[nameof(print)] = new LuaFunction(print);
		env[nameof(quit)] = new LuaFunction(quit);
		env[nameof(restart)] = new LuaFunction(restart);
		env[nameof(set_chat_filter_on)] = new LuaFunction(set_chat_filter_on);
		env[nameof(set_chat_log_on)] = new LuaFunction(set_chat_log_on);
		env[nameof(set_static_shader_on)] = new LuaFunction(set_static_shader_on);
		env[nameof(set_time)] = new LuaFunction(set_time);
		env[nameof(set_ui_dark_theme_on)] = new LuaFunction(set_ui_dark_theme_on);
		env[nameof(set_ui_scale)] = new LuaFunction(set_ui_scale);
		env[nameof(set_vsync_mode)] = new LuaFunction(set_vsync_mode);
		env[nameof(tp_char)] = new LuaFunction(tp_char);
		env[nameof(tts)] = new LuaFunction(tts);
		env[nameof(wait)] = new LuaFunction(wait);

		#endregion
	}

	extension(LuaFunctionExecutionContext context)
	{
		private T GetArgumentOrDefault<T>(int index, T fallback = default!) =>
			context.HasArgument(index) ? context.GetArgument<T>(index) : fallback;

		private ValueTask<int> ReturnNothing() => new(context.Return());
	}
}
