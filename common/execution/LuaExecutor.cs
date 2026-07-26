using Lua;
using Lua.Standard;
using Serilog;

namespace Root.Common.Execution;

public static class LuaExecutor
{
	public static async Task<LuaValue[]> Execute(string source)
	{
		source = source.Trim();
		Log.Information(">\n{Source}\n", source);

		var state = LuaState.Create();
		state.OpenStandardLibraries();

		state.Environment["print"] = new LuaFunction(Print);

		var results = await state.DoStringAsync(source).ConfigureAwait(false);
		Log.Information("< [{Results}]", string.Join(", ", results.Select(v => v.ToString())));

		return results;
	}

	private static ValueTask<int> Print(LuaFunctionExecutionContext context, CancellationToken ct)
	{
		var args = new List<string>();

		for (var i = 0; i < context.ArgumentCount; i++)
			args.Add(context.GetArgument<LuaValue>(i).ToString());

		Log.Information("Lua: \"{Message}\"", string.Join(' ', args));

		context.Return();
		return default;
	}
}
