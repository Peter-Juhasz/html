using PeterJuhasz.Text.Html.Selectors;
using static PeterJuhasz.Text.Html.Tests.Selectors.TestHelpers;

namespace PeterJuhasz.Text.Html.Tests.Selectors;

[TestClass]
public sealed class HasSelectorTests : SelectorListPseudoClassTests
{
	private protected override string FunctionName => "has";

	private protected override SimpleSelector Create(ElementSelector argument) => HasSelector.Descendant(argument);

	[TestMethod]
	public void ParsesTheChildCombinatorAtTheStart()
	{
		AssertParses(":has(> a)", Compound(HasSelector.Child(Element("a"))));
		AssertParses(":has(>a)", Compound(HasSelector.Child(Element("a"))));
		AssertParses(":has( > a )", Compound(HasSelector.Child(Element("a"))));
	}

	[TestMethod]
	public void ParsesTheNextSiblingCombinatorAtTheStart()
	{
		AssertParses(":has(+ a)", Compound(HasSelector.NextSibling(Element("a"))));
		AssertParses(":has(+a)", Compound(HasSelector.NextSibling(Element("a"))));
	}

	[TestMethod]
	public void CombinatorAtTheStartJoinsTheAnchorToTheFirstCompoundSelector()
		=> AssertParses(":has(> a b)", Compound(new HasSelector(Selector: new DescendantSelector(Left: new ChildSelector(Left: AnchorSelector.Instance, Right: Element("a")), Right: Element("b")))));

	[TestMethod]
	public void EachSelectorOfTheListHasItsOwnCombinatorAtTheStart()
	{
		var expected = new HasSelector(Selector: new OrSelector(Selectors:
		[
			new ChildSelector(Left: AnchorSelector.Instance, Right: Element("a")),
			new NextSiblingSelector(Left: AnchorSelector.Instance, Right: Element("b")),
			new DescendantSelector(Left: AnchorSelector.Instance, Right: Element("c")),
		]));

		AssertParses(":has(> a, + b, c)", Compound(expected));
	}

	private const string List =
		"<ul id=u1><li id=l1><img id=i1></li><li id=l2><span id=s1><img id=i2></span></li><li id=l3></li></ul>" +
		"<h1 id=h1></h1><!-- comment --><p id=p1></p><h2 id=h2></h2>";

	[TestMethod]
	[DataRow("li:has(img)", new[] { "l1", "l2" })]
	[DataRow("li:has(> img)", new[] { "l1" })]
	[DataRow("li:has(> span img)", new[] { "l2" })]
	[DataRow("li:has(> img, > span)", new[] { "l1", "l2" })]
	[DataRow("li:not(:has(img))", new[] { "l3" })]
	[DataRow("ul:has(> li:has(> img))", new[] { "u1" })]
	[DataRow("ul:has(li:empty)", new[] { "u1" })]
	[DataRow("h1:has(+ p)", new[] { "h1" })]
	[DataRow("h2:has(+ p)", new string[0])]
	[DataRow(":has(+ h2)", new[] { "p1" })]
	public void SelectsElementsFromWhichTheRelativeSelectorReachesAMatch(string selector, string[] expected)
		=> AssertSelects(List, selector, expected);

	[TestMethod]
	public void RelativeSelectorStaysInsideTheElement()
	{
		const string html = "<form id=f><div id=d><img id=i></div></form>";

		AssertSelects(html, "div:has(form img)");
		AssertSelects(html, "form:has(div img)", "f");
	}

	[TestMethod]
	public void SelectsWithinAnElement()
		=> AssertSelectsWithin(List, "u1", "li:has(img)", "l1", "l2");

	[TestMethod]
	[DataRow(":has(~ a)", "unexpected '~'")]
	[DataRow(":has(> > a)", "unexpected '>'")]
	[DataRow(":has(>)", "unexpected ')'")]
	[DataRow(":has(> a,)", "unexpected ')'")]
	public void RejectsUnsupportedOrIncompleteCombinatorsAtTheStart(string selector, string error)
		=> AssertInvalid(selector, error);

	[TestMethod]
	public void AnchorDoesNotCountTowardsTheLimitOfCompoundSelectors()
	{
		var chain = string.Join(" > ", Enumerable.Repeat("a", ElementSelectorParser.MaxCompoundSelectors));

		Assert.IsTrue(ElementSelector.TryParse($":has(> {chain})", out _));
		AssertInvalid($":has(> {chain} > a)", "too many compound selectors");
	}

	[TestMethod]
	public void IsWrittenBackWithTheCombinatorsAtTheStart()
		=> AssertPrints(":has(>a,+b,c)", ":has(> a, + b, c)");
}
