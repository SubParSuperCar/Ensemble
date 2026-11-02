using System.Collections.Frozen;
using Godot;

namespace EnsembleRoot.Common.Input;

/// <summary>
///     The keys a creation's controller is pressing, as a mask over the keys assets can bind. It always holds the whole
///     state, so a lost update can't leave a key inverted.
/// </summary>
public readonly record struct InputState(ulong Mask)
{
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

	/// <summary>Whether a key is pressed; keys outside the bindable table never are.</summary>
	public bool IsPressed(Key key) => BitsByKey.TryGetValue(key, out var bit) && ((Mask >> bit) & 1) is not 0;
}
