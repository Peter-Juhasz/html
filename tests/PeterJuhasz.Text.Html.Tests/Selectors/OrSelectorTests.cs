using PeterJuhasz.Text.Html.Selectors;
using static PeterJuhasz.Text.Html.Tests.Selectors.TestHelpers;

namespace PeterJuhasz.Text.Html.Tests.Selectors;

[TestClass]
public sealed class OrSelectorTests
{
	[TestMethod]
	[DataRow("a,b")]
	[DataRow("a, b")]
	[DataRow(" a , b ")]
	[DataRow("a\n,\tb")]
	public void JoinsSelectorsSeparatedByCommas(string selector)
		=> AssertParses(selector, new OrSelector(Selectors: [Element("a"), Element("b")]));

	[TestMethod]
	public void KeepsTheListFlat()
		=> AssertParses("a, b, c", new OrSelector(Selectors: [Element("a"), Element("b"), Element("c")]));

	[TestMethod]
	public void ParsesTheSameAsTheOrOperator()
		=> AssertParses("a, b, c", Element("a") | Element("b") | Element("c"));

	[TestMethod]
	public void DoesNotWrapASingleSelector()
		=> Assert.IsNotInstanceOfType<OrSelector>(ElementSelector.Parse("a"));

	[TestMethod]
	public void BranchesCanHaveCombinators()
		=> AssertParses("a > b, c d", new OrSelector(Selectors: [new ChildSelector(Left: Element("a"), Right: Element("b")), new DescendantSelector(Left: Element("c"), Right: Element("d"))]));

	[TestMethod]
	public void DifferentOrderIsADifferentSelector()
		=> Assert.AreNotEqual(ElementSelector.Parse("a, b"), ElementSelector.Parse("b, a"));

	[TestMethod]
	[DataRow("a,", "expected a selector")]
	[DataRow(",a", "unexpected ','")]
	[DataRow("a,,b", "unexpected ','")]
	[DataRow("a, ,b", "unexpected ','")]
	[DataRow(",", "unexpected ','")]
	public void RejectsEmptyBranches(string selector, string error)
		=> AssertInvalid(selector, error);

	[TestMethod]
	public void SelectsEachMatchOnceInDocumentOrder()
		=> AssertSelects("<p id=a class=x></p><div id=b></div><p id=c></p>", "div, p, .x", "a", "b", "c");

	[TestMethod]
	public void SelectsWithBranchesOfAnyKind()
		=> AssertSelects("<ul id=u><li id=l1></li><li id=l2></li></ul><p id=p></p>", "li:first-child, ul + p", "l1", "p");

	[TestMethod]
	public void IsWrittenBack()
		=> AssertPrints("a,b>c", "a, b > c");
}
