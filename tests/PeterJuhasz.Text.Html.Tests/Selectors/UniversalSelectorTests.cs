using PeterJuhasz.Text.Html.Selectors;
using static PeterJuhasz.Text.Html.Tests.Selectors.TestHelpers;

namespace PeterJuhasz.Text.Html.Tests.Selectors;

[TestClass]
public sealed class UniversalSelectorTests
{
	[TestMethod]
	public void ParsesToTheSharedInstance()
		=> Assert.AreSame(UniversalSelector.Instance, ParseSimple("*"));

	[TestMethod]
	public void IsFollowedByOtherSimpleSelectors()
		=> AssertParses("*.x#y[z]:empty", Compound(UniversalSelector.Instance, Class("x"), Id("y"), new AttributeSelector(Name: "z"), EmptySelector.Instance));

	[TestMethod]
	public void StandsInForAnElementNameAroundCombinators()
		=> AssertParses("* > *", new ChildSelector(Left: Compound(UniversalSelector.Instance), Right: Compound(UniversalSelector.Instance)));

	[TestMethod]
	public void RejectsAnotherElementNameOrANamespaceAfterIt()
	{
		AssertInvalid("**", "unexpected '*'");
		AssertInvalid("*div", "unexpected 'd'");
		AssertInvalid("*|div", "unexpected '|'");
	}

	[TestMethod]
	public void SelectsEveryElement()
		=> AssertSelects("<div id=a><p id=b></p></div><!-- comment -->text<span id=c></span>", "*", "a", "b", "c");

	[TestMethod]
	public void SelectsEveryElementInsideTheScope()
		=> AssertSelectsWithin("<div id=a><p id=b><i id=c></i></p></div><span id=d></span>", "a", "*", "b", "c");

	[TestMethod]
	public void IsWrittenBack()
		=> AssertPrints("*>*", "* > *");
}
