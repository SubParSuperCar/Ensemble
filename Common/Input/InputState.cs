using System.Collections.Frozen;
using Godot;

namespace EnsembleRoot.Common.Input;

/// <summary>
///     The keys a creation's controller is pressing, as a mask over the keys assets can bind. It always holds the whole
///     state, so a lost update can't leave a key inverted.
/// </summary>
public readonly record struct InputState(ulong Mask)
{
	private const string BindingSuffix = "Key";

	private static readonly Key[] Keys =
	[
		.. Enumerable.Range(0, 26).Select(static letter => Key.A + letter),
		.. Enumerable.Range(0, 10).Select(static digit => Key.Key0 + digit),
		Key.Up, Key.Down, Key.Left, Key.Right, Key.Space, Key.Shift, Key.Ctrl, Key.Alt, Key.Tab, Key.Enter,
		Key.Backspace,
		Key.Comma, Key.Period, Key.Slash, Key.Semicolon, Key.Apostrophe, Key.Bracketleft, Key.Bracketright, Key.Minus,
		Key.Equal, Key.Backslash, Key.Quoteleft
	];

	private static readonly FrozenDictionary<Key, int> BitsByKey =
		Keys.Index().ToFrozenDictionary(static pair => pair.Item, static pair => pair.Index);

	/// <summary>Whether a property's name marks it as a key binding, holding a Godot key code.</summary>
	public static bool IsBinding(string propertyName) => propertyName.EndsWith(BindingSuffix, StringComparison.Ordinal);

	public static bool IsBindable(Key key) => BitsByKey.ContainsKey(key);

	/// <summary>The bindable keys pressed right now; none while the UI sinks input.</summary>
	public static InputState Sample()
	{
		if (InputSink.IsSunk)
			return default;

		var mask = 0ul;

		foreach (var (key, bit) in BitsByKey)
			if (Godot.Input.IsKeyPressed(key))
				mask |= 1ul << bit;

		return new InputState(mask);
	}

	/// <summary>Whether a key is pressed; keys outside the bindable table never are.</summary>
	public bool IsPressed(Key key) => BitsByKey.TryGetValue(key, out var bit) && ((Mask >> bit) & 1) is not 0;
}
