using EnsembleRoot.Scripts.Chat;
using Godot;
using Xunit;

namespace EnsembleRoot.Tests;

// Expected values come from the original Luau algorithm
public sealed class NametagColorsTests
{
	[Theory]
	[InlineData("SubParSuperCar", 196, 40, 28)]
	[InlineData("NoodleSnake", 13, 105, 172)]
	[InlineData("Alice", 218, 133, 65)]
	[InlineData("Bob", 215, 197, 154)]
	[InlineData("Player AWR9JCerTE-kJMucyIZWpQ", 218, 133, 65)]
	public void For_Name_MatchesLuau(string name, int red, int green, int blue) =>
		Assert.Equal(Color.Color8((byte)red, (byte)green, (byte)blue), NametagColors.For(name));
}
