using Lua;
using Lua.Standard;
using Serilog;

namespace Root.Common.Execution;

public static partial class LuaExecutor
{
	public static async Task<LuaValue[]> Execute(
		string source,
		CancellationToken cancellationToken = default)
	{
		source = source.Trim();
		Log.Information(">\n{Source}", source);

		var state = LuaState.Create();
		state.OpenStandardLibraries();

		AddFunctions(state.Environment);

		var results = await state.DoStringAsync(source, cancellationToken: cancellationToken).ConfigureAwait(false);
		Log.Information("< [{Results}]", string.Join(", ", results.Select(v => v.ToString())));

		return results;
	}
}
