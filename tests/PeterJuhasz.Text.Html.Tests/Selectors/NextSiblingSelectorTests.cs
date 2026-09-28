using PeterJuhasz.Text.Html.Selectors;
using static PeterJuhasz.Text.Html.Tests.Selectors.TestHelpers;

namespace PeterJuhasz.Text.Html.Tests.Selectors;

[TestClass]
public sealed class NextSiblingSelectorTests : CombinatorSelectorTests
{
	private protected override IEnumerable<string> Spellings => ["+", " +", "+ ", " + ", "\t+\n"];

	private protected override string Written => " + ";

	private protected override ComplexSelector Combine(ElementSelector left, CompoundSelector right) => new NextSiblingSelector(Left: left, Right: right);

	private protected override string[] Selected => ["p5"];

	private protected override string[] SelectedWithinSection => [];

	[TestMethod]
	public void SkipsTextAndCommentsBetweenTheSiblings()
		=> AssertSelects(Document, "section + p", "p4");

	[TestMethod]
	public void SelectsOnlyTheNextElementSibling()
		=> AssertSelects("<h1 id=h></h1>text<p id=a></p><p id=b></p>", "h1 + p", "a");

	[TestMethod]
	public void RequiresSelectorsOnBothSides()
		=> AssertRequiresSelectorsOnBothSides('+');

	[TestMethod]
	public void SubsequentSiblingCombinatorIsNotSupported()
		=> AssertInvalid("a ~ b", "unexpected '~'");
}
