using PeterJuhasz.Text.Html.Selectors;
using static PeterJuhasz.Text.Html.Tests.Selectors.TestHelpers;

namespace PeterJuhasz.Text.Html.Tests.Selectors;

[TestClass]
public sealed class NotSelectorTests : SelectorListPseudoClassTests
{
	private protected override string FunctionName => "not";

	private protected override SimpleSelector Create(ElementSelector argument) => new NotSelector(Selector: argument);

	[TestMethod]
	public void ParsesTheSameAsTheNotOperator()
		=> AssertParses(":not(a)", Compound(!Element("a")));

	[TestMethod]
	[DataRow("p:not(.x)", new[] { "b", "c" })]
	[DataRow(":not(p)", new[] { "d" })]
	[DataRow(":not(p, .x)", new[] { "d" })]
	[DataRow("p:not(div > p)", new[] { "c" })]
	[DataRow("p:not(div p)", new[] { "c" })]
	[DataRow("*:not(:not(.x))", new[] { "a" })]
	public void SelectsElementsThatDoNotMatchTheArgument(string selector, string[] expected)
		=> AssertSelects("<div id=d><p id=a class=x></p><p id=b></p></div><p id=c></p>", selector, expected);

	[TestMethod]
	[DataRow(":not(> a)", "unexpected '>'")]
	[DataRow(":not(+ a)", "unexpected '+'")]
	public void RejectsRelativeSelectors(string selector, string error)
		=> AssertInvalid(selector, error);
}
