using PeterJuhasz.Text.Html.Selectors;
using static PeterJuhasz.Text.Html.Tests.Selectors.TestHelpers;

namespace PeterJuhasz.Text.Html.Tests.Selectors;

[TestClass]
public sealed class NthChildSelectorTests
{
	/// <summary>
	/// An+B as written, with the step and the offset it stands for.
	/// </summary>
	public static IEnumerable<object[]> AnPlusB =>
	[
		["odd", 2, 1],
		["ODD", 2, 1],
		["even", 2, 0],
		["Even", 2, 0],
		["3", 0, 3],
		["+3", 0, 3],
		["-3", 0, -3],
		["0", 0, 0],
		["n", 1, 0],
		["N", 1, 0],
		["+n", 1, 0],
		["-n", -1, 0],
		["2n", 2, 0],
		["+2n", 2, 0],
		["-2n", -2, 0],
		["0n+5", 0, 5],
		["2n+1", 2, 1],
		["2n-1", 2, -1],
		["2n + 1", 2, 1],
		["2n +1", 2, 1],
		["2n+ 1", 2, 1],
		["2n - 1", 2, -1],
		["2n -1", 2, -1],
		["2n- 1", 2, -1],
		["-n+3", -1, 3],
		["-n + 3", -1, 3],
		["n-1", 1, -1],
		["-n-1", -1, -1],
		["10n+20", 10, 20],
		["  2n+1  ", 2, 1],
	];

	[TestMethod]
	[DynamicData(nameof(AnPlusB))]
	public void ParsesAnPlusB(string written, int a, int b)
		=> AssertParses($":nth-child({written})", Compound(new NthChildSelector(A: a, B: b)));

	[TestMethod]
	[DynamicData(nameof(AnPlusB))]
	public void ParsesAnPlusBCountedFromTheEnd(string written, int a, int b)
		=> AssertParses($":nth-last-child({written})", Compound(new NthChildSelector(A: a, B: b, FromEnd: true)));

	[TestMethod]
	public void ParsesTheWellKnownVariationsToSharedInstances()
	{
		Assert.AreSame(NthChildSelector.First, ParseSimple(":first-child"));
		Assert.AreSame(NthChildSelector.First, ParseSimple(":FIRST-CHILD"));
		Assert.AreSame(NthChildSelector.First, ParseSimple(":nth-child(1)"));
		Assert.AreSame(NthChildSelector.Last, ParseSimple(":last-child"));
		Assert.AreSame(NthChildSelector.Last, ParseSimple(":nth-last-child(1)"));
		Assert.AreSame(NthChildSelector.Odd, ParseSimple(":nth-child(odd)"));
		Assert.AreSame(NthChildSelector.Odd, ParseSimple(":NTH-CHILD(odd)"));
		Assert.AreSame(NthChildSelector.Last, ParseSimple(":Nth-Last-Child(1)"));
		Assert.AreSame(NthChildSelector.Odd, ParseSimple(":nth-child(2n+1)"));
		Assert.AreSame(NthChildSelector.Even, ParseSimple(":nth-child(even)"));
		Assert.AreSame(NthChildSelector.Even, ParseSimple(":nth-child(2n)"));
	}

	[TestMethod]
	public void ParsesTheSameAsTheFactories()
	{
		Assert.AreEqual(NthChildSelector.Take(3), ParseSimple(":nth-child(-n+3)"));
		Assert.AreEqual(NthChildSelector.TakeLast(3), ParseSimple(":nth-last-child(-n+3)"));
		Assert.AreEqual(NthChildSelector.At(4), ParseSimple(":nth-child(4)"));
		Assert.AreEqual(NthChildSelector.AtFromEnd(4), ParseSimple(":nth-last-child(4)"));
	}

	[TestMethod]
	public void ParsesTheSelectorAfterOf()
	{
		AssertParses(":nth-child(2n+1 of .x)", Compound(NthChildSelector.Odd with { Of = Compound(Class("x")) }));
		AssertParses(":nth-child(1 OF a, b)", Compound(NthChildSelector.First with { Of = Element("a") | Element("b") }));
		AssertParses(":nth-child(1 of.x)", Compound(NthChildSelector.First with { Of = Compound(Class("x")) }));
		AssertParses(":nth-last-child(-n+2 of div > p)", Compound(NthChildSelector.TakeLast(2) with { Of = new ChildSelector(Left: Element("div"), Right: Element("p")) }));
	}

	[TestMethod]
	public void SelectorAfterOfIsNotShared()
		=> Assert.AreNotSame(NthChildSelector.Odd, ParseSimple(":nth-child(odd of a)"));

	[TestMethod]
	[DataRow("")]
	[DataRow(" ")]
	[DataRow("2 n")]
	[DataRow("+ n")]
	[DataRow("- n")]
	[DataRow("+ 3")]
	[DataRow("--n")]
	[DataRow("3px")]
	[DataRow("n1")]
	[DataRow("2n1")]
	[DataRow("nth")]
	[DataRow("oddity")]
	[DataRow("odd+1")]
	[DataRow("2n+")]
	[DataRow("2n + -1")]
	[DataRow("2n+ +1")]
	[DataRow("1.5")]
	[DataRow("99999999999")]
	[DataRow("2n+99999999999")]
	[DataRow("1of a")]
	[DataRow("1 of")]
	[DataRow("1 of )")]
	[DataRow("1 offset")]
	public void RejectsInvalidArguments(string written)
		=> AssertInvalid($":nth-child({written})");

	[TestMethod]
	[DataRow(":only-child")]
	[DataRow(":nth-of-type(1)")]
	[DataRow(":first-of-type")]
	[DataRow(":first-child(1)")]
	public void RejectsOtherStructuralPseudoClasses(string selector)
		=> AssertInvalid(selector, "unsupported pseudo-class");

	private const string List =
		"<ul id=u><li id=l1></li><li id=l2 class=x></li><!-- comment --><li id=l3></li>text<li id=l4 class=x></li><li id=l5></li></ul>";

	[TestMethod]
	[DataRow("li:nth-child(odd)", new[] { "l1", "l3", "l5" })]
	[DataRow("li:nth-child(even)", new[] { "l2", "l4" })]
	[DataRow("li:nth-child(2)", new[] { "l2" })]
	[DataRow("li:nth-child(3n)", new[] { "l3" })]
	[DataRow("li:nth-child(-n+2)", new[] { "l1", "l2" })]
	[DataRow("li:nth-child(n+4)", new[] { "l4", "l5" })]
	[DataRow("li:first-child", new[] { "l1" })]
	[DataRow("li:last-child", new[] { "l5" })]
	[DataRow("li:nth-last-child(2)", new[] { "l4" })]
	[DataRow("li:nth-last-child(-n+2)", new[] { "l4", "l5" })]
	[DataRow("li:nth-child(1 of .x)", new[] { "l2" })]
	[DataRow("li:nth-last-child(1 of .x)", new[] { "l4" })]
	[DataRow("li:nth-child(even of .x)", new[] { "l4" })]
	[DataRow("li:nth-child(0)", new string[0])]
	[DataRow("li:nth-child(-n)", new string[0])]
	[DataRow("li:nth-child(6)", new string[0])]
	public void SelectsByThePositionAmongTheSiblingElements(string selector, string[] expected)
		=> AssertSelects(List, selector, expected);

	[TestMethod]
	public void TopLevelElementsAreSiblings()
	{
		const string html = "<p id=a></p><p id=b></p><p id=c></p>";

		AssertSelects(html, "p:first-child", "a");
		AssertSelects(html, "p:nth-last-child(2)", "b");
	}

	[TestMethod]
	[DataRow(":nth-child(odd)", ":nth-child(2n+1)")]
	[DataRow(":nth-child(even)", ":nth-child(2n)")]
	[DataRow(":nth-child(n)", ":nth-child(n)")]
	[DataRow(":nth-child(-n)", ":nth-child(-n)")]
	[DataRow(":nth-child(-n + 3)", ":nth-child(-n+3)")]
	[DataRow(":nth-child(-2n - 3)", ":nth-child(-2n-3)")]
	[DataRow(":nth-child(0n+5)", ":nth-child(5)")]
	[DataRow(":nth-child(+5)", ":nth-child(5)")]
	[DataRow(":first-child", ":nth-child(1)")]
	[DataRow(":last-child", ":nth-last-child(1)")]
	[DataRow(":nth-last-child(odd of .x)", ":nth-last-child(2n+1 of .x)")]
	public void IsWrittenBackInItsCanonicalForm(string selector, string expected)
		=> AssertPrints(selector, expected);
}
