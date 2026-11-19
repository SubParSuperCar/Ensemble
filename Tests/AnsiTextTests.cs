using EnsembleRoot.Common.Text;
using Xunit;

namespace EnsembleRoot.Tests;

public sealed class AnsiTextTests
{
	[Fact]
	public void Parse_PlainText_PassesThrough()
	{
		var (text, runs) = AnsiText.Parse("[12:00:00.000 INF] Hello");

		Assert.Equal("[12:00:00.000 INF] Hello", text);
		Assert.Empty(runs);
	}

	[Fact]
	public void Parse_StyledText_SplitsIntoRuns()
	{
		var (text, runs) = AnsiText.Parse(
			"\e[37;1mINF\e[0m Set \e[38;5;0081mscale\e[0m to \e[38;5;151m\e[48;5;0238m2\e[m\e[2K.");

		Assert.Equal("INF Set scale to 2.", text);
		Assert.Equal(
			[
				new AnsiRun(0, 3, new AnsiStyle(7, true)), new AnsiRun(8, 5, new AnsiStyle(81, false)),
				new AnsiRun(17, 1, new AnsiStyle(151, false))
			],
			runs);
	}
}
