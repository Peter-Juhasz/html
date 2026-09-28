using PeterJuhasz.Text.Html.Selectors;
using static PeterJuhasz.Text.Html.Tests.Selectors.TestHelpers;

namespace PeterJuhasz.Text.Html.Tests.Selectors;

[TestClass]
public sealed class AnchorSelectorTests
{
	[TestMethod]
	[DataRow(":has(a)")]
	[DataRow(":has(> a)")]
	[DataRow(":has(+ a)")]
	[DataRow(":has(> a b + c)")]
	public void RelativeSelectorsStartFromTheSharedInstance(string selector)
	{
		var has = (HasSelector)ParseSimple(selector);

		Assert.AreSame(AnchorSelector.Instance, Leftmost(has.Selector));
	}

	[TestMethod]
	public void IsOnlyParsedInHas()
		=> Assert.AreNotSame(AnchorSelector.Instance, Leftmost(((NotSelector)ParseSimple(":not(a b)")).Selector));

	[TestMethod]
	public void StandsForTheElementThatHasChecks()
		=> AssertSelects("<div id=a><div id=b><p id=c></p></div></div>", "div:has(> div > p)", "a");

	[TestMethod]
	public void MatchesNothingOutsideOfHas()
		=> Assert.IsEmpty(HtmlDocument.Parse("<p id=a><b id=b></b></p>").QuerySelectorAll(Compound(AnchorSelector.Instance)));

	[TestMethod]
	public void IsWrittenAsNothing()
		=> Assert.AreEqual("", AnchorSelector.Instance.ToString());

	private static ElementSelector Leftmost(ElementSelector selector) => selector switch
	{
		ComplexSelector complex => Leftmost(complex.Left),
		CompoundSelector { Selectors: [var first, ..] } => first,
		_ => selector,
	};
}
