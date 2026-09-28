using PeterJuhasz.Text.Html.Selectors;
using static PeterJuhasz.Text.Html.Tests.Selectors.TestHelpers;

namespace PeterJuhasz.Text.Html.Tests.Selectors;

[TestClass]
public sealed class ElementNameSelectorTests
{
	[TestMethod]
	[DynamicData(nameof(Identifiers), typeof(TestHelpers))]
	public void ParsesTheName(string written, string name)
		=> AssertParses(written, Element(name));

	[TestMethod]
	[DynamicData(nameof(InvalidIdentifiers), typeof(TestHelpers))]
	public void RejectsNamesThatAreNotIdentifiers(string written)
		=> AssertInvalid(written);

	[TestMethod]
	public void KeepsTheCaseOfTheName()
	{
		AssertParses("DIV", Element("DIV"));
		AssertParses("My-Element", Element("My-Element"));
	}

	[TestMethod]
	public void KnownNamesAreSharedStrings()
		=> Assert.AreSame(((ElementNameSelector)ParseSimple("div")).Name, ((ElementNameSelector)ParseSimple("div")).Name);

	[TestMethod]
	public void IgnoresWhitespaceAroundTheName()
		=> AssertParses(" \t div \n ", Element("div"));

	[TestMethod]
	public void MustComeFirstInACompoundSelector()
	{
		AssertInvalid("[a]div", "unexpected 'd'");
		AssertInvalid(".x*", "unexpected '*'");
		AssertInvalid("div*", "unexpected '*'");
	}

	[TestMethod]
	[DataRow("div")]
	[DataRow("DIV")]
	[DataRow("\\64 iv")]
	public void SelectsElementsByNameWhateverTheCase(string selector)
		=> AssertSelects("<div id=a></div><DIV id=b><Div id=c></Div></DIV><p id=d></p><divs id=e></divs>", selector, "a", "b", "c");

	[TestMethod]
	public void SelectsCustomElements()
		=> AssertSelects("<my-element id=a></my-element><my-elements id=b></my-elements>", "my-element", "a");

	[TestMethod]
	public void IsWrittenBack()
		=> AssertPrints(" div ", "div");
}
