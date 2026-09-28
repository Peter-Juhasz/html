using PeterJuhasz.Text.Html.Selectors;
using static PeterJuhasz.Text.Html.Tests.Selectors.TestHelpers;

namespace PeterJuhasz.Text.Html.Tests.Selectors;

[TestClass]
public sealed class DescendantSelectorTests : CombinatorSelectorTests
{
	private protected override IEnumerable<string> Spellings => [" ", "  ", "\t", "\n", "\r\n", "\f", " \t\n "];

	private protected override string Written => " ";

	private protected override ComplexSelector Combine(ElementSelector left, CompoundSelector right) => new DescendantSelector(Left: left, Right: right);

	private protected override string[] Selected => ["p1", "p2", "p3", "p4"];

	private protected override string[] SelectedWithinSection => ["p2", "p3"];

	[TestMethod]
	public void TriesEveryAncestorNotOnlyTheNearest()
		=> AssertSelects("<article><div id=d1><div id=d2><i id=t></i></div></div></article>", "article > div i", "t");

	[TestMethod]
	[DataRow(" a")]
	[DataRow("a ")]
	[DataRow("\ta\n")]
	public void WhitespaceAtTheEdgesIsNotACombinator(string selector)
		=> AssertParses(selector, Element("a"));
}
