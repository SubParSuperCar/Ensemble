using Lua;

// ReSharper disable NotAccessedPositionalProperty.Global

namespace EnsembleRoot.Execution;

public enum LuaOutcome : byte
{
	Succeeded,
	Failed,
	Canceled,
	TimedOut
}

public readonly record struct LuaResult(LuaOutcome Outcome, LuaValue[] Values);
