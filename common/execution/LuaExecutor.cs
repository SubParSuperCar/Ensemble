using Lua;
using Lua.Standard;
using Serilog;

namespace Root.Common.Execution;

public static partial class LuaExecutor
{
	public static async Task<LuaValue[]> Execute(string source)
	{
		source = source.Trim();
		Log.Information(">\n{Source}\n", source);

		var state = LuaState.Create();
		state.OpenStandardLibraries();

		AddFunctions(state.Environment);

		var results = await state.DoStringAsync(source).ConfigureAwait(false);
		Log.Information("< [{Results}]", string.Join(", ", results.Select(v => v.ToString())));

		return results;
	}

	private static void AddFunctions(LuaTable env)
	{
		env[nameof(print)] = new LuaFunction(print);
		env[nameof(quit)] = new LuaFunction(quit);
		env[nameof(add_test_insts)] = new LuaFunction(add_test_insts);
		env[nameof(clear_insts)] = new LuaFunction(clear_insts);
	}
}
