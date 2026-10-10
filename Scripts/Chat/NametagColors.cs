using System.Text;
using Godot;

namespace EnsembleRoot.Scripts.Chat;

/// <summary>Picks a display name's chat nametag color, like Roblox's legacy chat did.</summary>
/// <remarks>Ported from Baja Builders' Luau, edited from https://devforum.roblox.com/t/957515.</remarks>
public static class NametagColors
{
	// Bright red, Bright blue, Earth green, Bright violet, Bright orange, Bright yellow, Light reddish violet, and
	// Brick yellow, as Roblox BrickColors
	private static readonly Color[] Palette =
	[
		Color.Color8(196, 40, 28), Color.Color8(13, 105, 172), Color.Color8(39, 70, 45), Color.Color8(107, 50, 124),
		Color.Color8(218, 133, 65), Color.Color8(245, 205, 48), Color.Color8(232, 186, 200),
		Color.Color8(215, 197, 154)
	];

	public static Color For(string name)
	{
		var bytes = Encoding.UTF8.GetBytes(name);
		var value = 0;

		for (var index = 0; index < bytes.Length; index++)
		{
			var inverse = bytes.Length - index;

			if ((bytes.Length & 1) is 1)
				inverse--;

			value += (inverse & 2) is 2 ? -bytes[index] : bytes[index];
		}

		return Palette[Mathf.PosMod(value, Palette.Length)];
	}
}
