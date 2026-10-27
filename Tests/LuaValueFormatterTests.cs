using EnsembleRoot.Execution;
using Lua;
using Xunit;

namespace EnsembleRoot.Tests;

public sealed class LuaValueFormatterTests
{
	[Theory]
	[InlineData("return nil", "nil")]
	[InlineData("return true", "true")]
	[InlineData("return 1.5", "1.5")]
	[InlineData("return 'a\"b'", "a\"b")]
	[InlineData("return function() end", "<function>")]
	[InlineData("return {}", "{}")]
	[InlineData("return {1, 2, 3}", "{1, 2, 3}")]
	[InlineData("return {'a\\nb'}", "{\"a\\nb\"}")]
	[InlineData("return {1, nil, 3}", "{1, [3] = 3}")]
	[InlineData("return {x = 1, ['a b'] = 2, ['end'] = 3, [2.5] = 4, [true] = 5}",
		"{[true] = 5, [\"a b\"] = 2, [\"end\"] = 3, x = 1, [2.5] = 4}")]
	[InlineData("return {1, x = {2}}", "{1, x = {2}}")]
	[InlineData("return {{{{1}}}}", "{{{{...}}}}")]
	[InlineData("local t = {} t.self = t return t", "{self = <cycle>}")]
	[InlineData("local s = {1} return {a = s, b = s}", "{a = {1}, b = {1}}")]
	public async Task Format_ShowsContents(string source, string expected) =>
		Assert.Equal(expected, LuaValueFormatter.Format(await RunAsync(source)));

	[Fact]
	public async Task Format_QuotesTopLevelStringsOnlyWhenAsked() =>
		Assert.Equal("\"a\"", LuaValueFormatter.Format(await RunAsync("return 'a'"), true));

	[Fact]
	public async Task Format_CutsLongTablesShort()
	{
		var formatted = LuaValueFormatter.Format(await RunAsync("local t = {} for i = 1, 40 do t[i] = i end return t"));

		Assert.StartsWith("{1, 2, ", formatted, StringComparison.Ordinal);
		Assert.EndsWith(", 32, ...}", formatted, StringComparison.Ordinal);
	}

	private static async Task<LuaValue> RunAsync(string source)
	{
		var state = LuaState.Create();
		var results = await state.DoStringAsync(source, cancellationToken: TestContext.Current.CancellationToken)
			.ConfigureAwait(false);

		return results is [var result, ..] ? result : LuaValue.Nil;
	}
}
