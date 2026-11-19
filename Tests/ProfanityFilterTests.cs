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
	[InlineData("The U S A, a b c, and a s s e t s")]
	[InlineData("\u041F\u0440\u0438\u0432\u0435\u0442, caf\u00E9 na\u00EFve \U0001F600")]
	public void Censor_CleanText_IsUnchanged(string text) => Assert.Equal(text, ProfanityFilter.Censor(text));

	[Theory]
	[InlineData("well damn it", "well #### it")]
	[InlineData("daaaamn you", "####### you")]
	[InlineData("d4mn it", "#### it")]
	[InlineData("crappy craps here", "###### ##### here")]
	[InlineData("utter dogbollocks.", "utter ###########.")]
	[InlineData("d a m n it", "####### it")]
	[InlineData("well d.a.m.n", "well #######")]
	[InlineData("d\u00E4mn it", "#### it")]
	[InlineData("da\u0301mn it", "##### it")]
	[InlineData("\uFF44\uFF41\uFF4D\uFF4E it", "#### it")]
	[InlineData("\u0441rap it", "#### it")]
	public void Censor_Profanity_IsMasked(string text, string expected) =>
		Assert.Equal(expected, ProfanityFilter.Censor(text));

	[Theory]
	[InlineData("DAMN")]
	[InlineData("crap!!! damn?")]
	public void Censor_OnlyProfanity_IsRedacted(string text) =>
		Assert.Equal(ProfanityFilter.Redacted, ProfanityFilter.Censor(text));
}
