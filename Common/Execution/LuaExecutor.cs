using Lua;
using Lua.Standard;
using Serilog;

namespace Root.Common.Execution;

public static partial class LuaExecutor
{
	public static async Task<LuaValue[]> ExecuteAsync(
		string source,
		CancellationToken cancellationToken = default)
	{
		source = source.Trim();
		Log.Information(">\n{Source}", source);

		var state = LuaState.Create();
		state.OpenStandardLibraries();

		InjectCustomFunctions(state.Environment);

		var results = await state.DoStringAsync(source, cancellationToken: cancellationToken).ConfigureAwait(false);
		Log.Information("< [{Results}]", string.Join(", ", results.Select(value => value.ToString())));

		return results;
	}
}
