using PeterJuhasz.Text.Html.Selectors;
using static PeterJuhasz.Text.Html.Tests.Selectors.TestHelpers;

namespace PeterJuhasz.Text.Html.Tests.Selectors;

[TestClass]
public sealed class CompoundSelectorTests
{
	[TestMethod]
	public void KeepsTheSimpleSelectorsInTheirWrittenOrder()
		=> AssertParses("a.b#c[d]:empty", Compound(ElementName("a"), Class("b"), Id("c"), new AttributeSelector(Name: "d"), EmptySelector.Instance));

	[TestMethod]
	[DataRow("a")]
	[DataRow("*")]
	[DataRow(".x")]
	[DataRow(":empty")]
	public void WrapsASingleSimpleSelector(string selector)
		=> Assert.IsInstanceOfType<CompoundSelector>(ElementSelector.Parse(selector));

	[TestMethod]
	public void DifferentOrderIsADifferentSelector()
		=> Assert.AreNotEqual(ElementSelector.Parse(".a.b"), ElementSelector.Parse(".b.a"));

	[TestMethod]
	public void WhitespaceSeparatesCompoundSelectors()
		=> AssertParses("a .b", new DescendantSelector(Left: Element("a"), Right: Compound(Class("b"))));

	[TestMethod]
	[DataRow("", "expected a selector")]
	[DataRow(" ", "expected a selector")]
	[DataRow("&", "unexpected '&'")]
	[DataRow("a&", "unexpected '&'")]
	public void RejectsTextWithoutASimpleSelector(string selector, string error)
		=> AssertInvalid(selector, error);

	[TestMethod]
	[DataRow("a.x.y[href=v]", new[] { "a1" })]
	[DataRow("a.x", new[] { "a1", "a2" })]
	[DataRow("a.y.x#a1", new[] { "a1" })]
	[DataRow(".x.y[href]", new[] { "a1", "p1" })]
	[DataRow("p.x:empty", new[] { "p1" })]
	[DataRow("p.z", new string[0])]
	public void SelectsElementsMatchingEveryPart(string selector, string[] expected)
		=> AssertSelects("<a id=a1 class='x y' href=v></a><a id=a2 class=x></a><p id=p1 class='x y' href=v></p><p id=p2 class=x>text</p>", selector, expected);

	[TestMethod]
	public void IsWrittenBack()
		=> AssertPrints("A.b#c[HREF]:EMPTY", "A.b#c[HREF]:empty");
}
