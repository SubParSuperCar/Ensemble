using System.Text;
using Lua;

namespace EnsembleRoot.Execution;

/// <summary>Formats Lua values for the console, showing what tables hold instead of their addresses.</summary>
/// <remarks>
///     Tables list their sequence first, then their other keys sorted, so the output is stable. Nesting, entry counts,
///     and cycles are cut short (<c>{...}</c>, <c>...</c>, <c>&lt;cycle&gt;</c>).
/// </remarks>
public static class LuaValueFormatter
{
	private const int MaxDepth = 3;
	private const int MaxEntryCount = 32;

	private static readonly HashSet<string> Keywords = new(StringComparer.Ordinal)
	{
		"and", "break", "do", "else", "elseif", "end", "false", "for", "function", "goto", "if", "in", "local", "nil",
		"not", "or", "repeat", "return", "then", "true", "until", "while"
	};

	/// <param name="value">The value to format.</param>
	/// <param name="isQuoted">Whether a string is quoted; strings inside tables always are.</param>
	public static string Format(LuaValue value, bool isQuoted = false)
	{
		var builder = new StringBuilder();
		Append(builder, value, isQuoted, 0, []);

		return builder.ToString();
	}

	public static string Escape(string value) =>
		value
			.Replace("\\", @"\\", StringComparison.Ordinal)
			.Replace("\"", "\\\"", StringComparison.Ordinal)
			.Replace("\r", "\\r", StringComparison.Ordinal)
			.Replace("\n", "\\n", StringComparison.Ordinal);

	private static void Append(StringBuilder builder, LuaValue value, bool isQuoted, int depth, HashSet<LuaTable> path)
	{
		switch (value.Type)
		{
			case LuaValueType.String when isQuoted:
				builder.Append('"').Append(Escape(value.Read<string>())).Append('"');
				break;

			case LuaValueType.Table:
				AppendTable(builder, value.Read<LuaTable>(), depth, path);
				break;

			case LuaValueType.Function or LuaValueType.Thread or LuaValueType.UserData or LuaValueType.LightUserData:
				builder.Append('<').Append(value.TypeToString()).Append('>');
				break;

			default:
				builder.Append(value.ToString());
				break;
		}
	}

	// Only the tables being formatted count as cycles, so one shared by two entries still shows twice
	private static void AppendTable(StringBuilder builder, LuaTable table, int depth, HashSet<LuaTable> path)
	{
		if (path.Contains(table))
		{
			builder.Append("<cycle>");
			return;
		}

		var length = table.ArrayLength;
		var others = table.Where(entry => !IsSequenceKey(entry.Key, length)).ToArray();

		if (length + others.Length is 0)
		{
			builder.Append("{}");
			return;
		}

		if (depth == MaxDepth)
		{
			builder.Append("{...}");
			return;
		}

		path.Add(table);
		builder.Append('{');

		var count = 0;

		foreach (var (key, value) in GetEntries(table, length, others))
		{
			if (count > 0)
				builder.Append(", ");

			if (count++ == MaxEntryCount)
			{
				builder.Append("...");
				break;
			}

			if (key is { } entryKey)
				AppendKey(builder, entryKey, depth, path);

			Append(builder, value, true, depth + 1, path);
		}

		builder.Append('}');
		path.Remove(table);
	}

	private static IEnumerable<(LuaValue? Key, LuaValue Value)> GetEntries(
		LuaTable table, int length, KeyValuePair<LuaValue, LuaValue>[] others)
	{
		for (var i = 1; i <= length; i++)
			yield return (null, table[i]);

		var sorted = others
			.OrderBy(static entry => entry.Key.Type)
			.ThenBy(static entry => entry.Key.ToString(), StringComparer.Ordinal);

		foreach (var (key, value) in sorted)
			yield return (key, value);
	}

	private static void AppendKey(StringBuilder builder, LuaValue key, int depth, HashSet<LuaTable> path)
	{
		if (key.Type is LuaValueType.String && key.Read<string>() is var name && IsIdentifier(name))
			builder.Append(name);
		else
		{
			builder.Append('[');
			Append(builder, key, true, depth + 1, path);
			builder.Append(']');
		}

		builder.Append(" = ");
	}

	private static bool IsSequenceKey(LuaValue key, int length) =>
		key.Type is LuaValueType.Number && key.Read<double>() is var index && double.IsInteger(index) &&
		index >= 1 && index <= length;

	private static bool IsIdentifier(string name) =>
		name.Length > 0 && !char.IsAsciiDigit(name[0]) && !Keywords.Contains(name) &&
		name.All(static c => char.IsAsciiLetterOrDigit(c) || c is '_');
}
