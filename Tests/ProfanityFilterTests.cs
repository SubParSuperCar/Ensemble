using EnsembleRoot.Common.Text;
using Xunit;

namespace EnsembleRoot.Tests;

public sealed class ProfanityFilterTests
{
	[Theory]
	[InlineData("A classic grape scraper from Scunthorpe")]
	[InlineData("Spicy cumin on the skyscraper's parapet")]
	[InlineData("Hello, therapist! Assassins pass the glass.")]
	[InlineData("Shiite scholars read Dickens")]
	public void Censor_CleanText_IsUnchanged(string text) => Assert.Equal(text, ProfanityFilter.Censor(text));

	[Theory]
	[InlineData("well damn it", "well #### it")]
	[InlineData("daaaamn you", "####### you")]
	[InlineData("d4mn it", "#### it")]
	[InlineData("crappy craps here", "###### ##### here")]
	[InlineData("utter dogbollocks.", "utter ###########.")]
	public void Censor_Profanity_IsMasked(string text, string expected) =>
		Assert.Equal(expected, ProfanityFilter.Censor(text));

	[Theory]
	[InlineData("DAMN")]
	[InlineData("crap!!! damn?")]
	public void Censor_OnlyProfanity_IsRedacted(string text) =>
		Assert.Equal(ProfanityFilter.Redacted, ProfanityFilter.Censor(text));
}
