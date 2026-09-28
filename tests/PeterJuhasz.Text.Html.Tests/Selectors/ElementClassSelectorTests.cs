using PeterJuhasz.Text.Html.Selectors;
using static PeterJuhasz.Text.Html.Tests.Selectors.TestHelpers;

namespace PeterJuhasz.Text.Html.Tests.Selectors;

[TestClass]
public sealed class ElementClassSelectorTests : IdentifierSelectorTests
{
	private protected override char Prefix => '.';

	private protected override SimpleSelector Create(string identifier) => Class(identifier);

	[TestMethod]
	public void SelectsElementsWithTheClassAmongOthers()
		=> AssertSelects("<p id=a class=x></p><p id=b class='y x z'></p><p id=c class=xy></p><p id=d class=X></p><p id=e></p>", ".x", "a", "b");

	[TestMethod]
	public void SelectsElementsWithEveryClass()
		=> AssertSelects("<p id=a class='x y'></p><p id=b class=y></p><p id=c class='y z x'></p>", ".x.y", "a", "c");

	[TestMethod]
	public void MatchesTheDecodedClassAttribute()
	{
		const string html = "<p id=a class='a&amp;b'></p><p id=b class='md:flex'></p>";

		AssertSelects(html, ".a\\&b", "a");
		AssertSelects(html, ".md\\:flex", "b");
	}
}
