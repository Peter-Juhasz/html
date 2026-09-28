using PeterJuhasz.Text.Html.Selectors;
using static PeterJuhasz.Text.Html.Tests.Selectors.TestHelpers;

namespace PeterJuhasz.Text.Html.Tests.Selectors;

[TestClass]
public sealed class IdSelectorTests : IdentifierSelectorTests
{
	private protected override char Prefix => '#';

	private protected override SimpleSelector Create(string identifier) => Id(identifier);

	[TestMethod]
	public void SelectsElementsWithTheIdCaseSensitively()
		=> AssertSelects("<p id=a></p><p id=b></p><p id=A></p>", "#a", "a");

	[TestMethod]
	public void SelectsEveryElementWithADuplicateId()
		=> AssertSelects("<p id=x></p><div><p id=x></p></div>", "#x", "x", "x");

	[TestMethod]
	public void MatchesTheDecodedIdAttribute()
	{
		const string html = "<p id='a&amp;b'></p><p id=10></p>";

		AssertSelects(html, "#a\\&b", "a&b");
		AssertSelects(html, "#\\31 0", "10");
	}

	[TestMethod]
	public void CombinesWithTheElementName()
		=> AssertSelects("<p id=main></p><div id=main></div>", "div#main", "main");

	[TestMethod]
	public void IsNotParsedAsAnAttributeSelector()
		=> Assert.AreNotEqual(ElementSelector.Parse("[id=x]"), ElementSelector.Parse("#x"));
}
