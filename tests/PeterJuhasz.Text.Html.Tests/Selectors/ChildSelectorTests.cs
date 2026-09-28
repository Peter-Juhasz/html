using PeterJuhasz.Text.Html.Selectors;
using static PeterJuhasz.Text.Html.Tests.Selectors.TestHelpers;

namespace PeterJuhasz.Text.Html.Tests.Selectors;

[TestClass]
public sealed class ChildSelectorTests : CombinatorSelectorTests
{
	private protected override IEnumerable<string> Spellings => [">", " >", "> ", " > ", "\t>\n"];

	private protected override string Written => " > ";

	private protected override ComplexSelector Combine(ElementSelector left, CompoundSelector right) => new ChildSelector(Left: left, Right: right);

	private protected override string[] Selected => ["p1", "p3", "p4"];

	private protected override string[] SelectedWithinSection => ["p3"];

	[TestMethod]
	public void ChainsFollowTheParents()
		=> AssertSelects(Document, "div > section > div > p", "p3");

	[TestMethod]
	public void RequiresSelectorsOnBothSides()
		=> AssertRequiresSelectorsOnBothSides('>');
}
