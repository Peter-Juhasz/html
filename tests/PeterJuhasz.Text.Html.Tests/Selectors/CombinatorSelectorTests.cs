using PeterJuhasz.Text.Html.Selectors;
using static PeterJuhasz.Text.Html.Tests.Selectors.TestHelpers;

namespace PeterJuhasz.Text.Html.Tests.Selectors;

/// <summary>
/// Tests shared by the combinators, which join two selectors into a <see cref="ComplexSelector"/>.
/// </summary>
public abstract class CombinatorSelectorTests
{
	/// <summary>
	/// A document where each combinator relates different elements: nested divs and a section, and siblings with a comment between them.
	/// </summary>
	private protected const string Document =
		"<div id=outer>" +
			"<p id=p1></p>" +
			"<section id=s1><p id=p2></p><div id=inner><p id=p3></p></div></section>" +
			"<!-- comment -->" +
			"<p id=p4></p>" +
		"</div>" +
		"<p id=p5></p>";

	/// <summary>
	/// The IDs of the elements that <c>div</c> and <c>p</c> joined by the combinator select in <see cref="Document"/>.
	/// </summary>
	private protected abstract string[] Selected { get; }

	/// <summary>
	/// The IDs of the elements that the same selector selects within the section of <see cref="Document"/>, whose ancestors are outside of it.
	/// </summary>
	private protected abstract string[] SelectedWithinSection { get; }

	/// <summary>
	/// The ways the combinator can be written between two selectors, with the whitespace around it.
	/// </summary>
	private protected abstract IEnumerable<string> Spellings { get; }

	/// <summary>
	/// The combinator as it is written back, with the spaces around it.
	/// </summary>
	private protected abstract string Written { get; }

	/// <summary>
	/// Joins two selectors with the combinator.
	/// </summary>
	private protected abstract ComplexSelector Combine(ElementSelector left, CompoundSelector right);

	[TestMethod]
	public void JoinsTwoCompoundSelectors()
	{
		foreach (var spelling in Spellings)
		{
			AssertParses($"a{spelling}b", Combine(Element("a"), Element("b")));
		}
	}

	[TestMethod]
	public void JoinsCompoundSelectorsOfSeveralParts()
		=> AssertParses($"div.x{Written}p#y[z]", Combine(Compound(ElementName("div"), Class("x")), Compound(ElementName("p"), Id("y"), new AttributeSelector(Name: "z"))));

	[TestMethod]
	public void NestsChainsToTheLeft()
		=> AssertParses($"a{Written}b{Written}c", Combine(Combine(Element("a"), Element("b")), Element("c")));

	[TestMethod]
	public void ChainsUpToTheLimit()
	{
		ElementSelector expected = Element("a");
		for (var i = 1; i < ElementSelectorParser.MaxCompoundSelectors; i++)
		{
			expected = Combine(expected, Element("a"));
		}

		AssertParses(Chain(ElementSelectorParser.MaxCompoundSelectors), expected);
		AssertInvalid(Chain(ElementSelectorParser.MaxCompoundSelectors + 1), "too many compound selectors");

		string Chain(int count) => string.Join(Written, Enumerable.Repeat("a", count));
	}

	[TestMethod]
	public void MixesWithTheOtherCombinators()
	{
		AssertParses($"a b{Written}c", Combine(new DescendantSelector(Left: Element("a"), Right: Element("b")), Element("c")));
		AssertParses($"a > b{Written}c", Combine(new ChildSelector(Left: Element("a"), Right: Element("b")), Element("c")));
		AssertParses($"a + b{Written}c", Combine(new NextSiblingSelector(Left: Element("a"), Right: Element("b")), Element("c")));
		AssertParses($"a{Written}b > c", new ChildSelector(Left: Combine(Element("a"), Element("b")), Right: Element("c")));
	}

	[TestMethod]
	public void IgnoresWhitespaceAroundTheWholeSelector()
		=> AssertParses($" \t a{Written}b \n ", Combine(Element("a"), Element("b")));

	[TestMethod]
	public void EndsAtACommaOrAClosingParenthesis()
	{
		AssertParses($"a{Written}b , c", Combine(Element("a"), Element("b")) | Element("c"));
		AssertParses($":not(a{Written}b )", Compound(!Combine(Element("a"), Element("b"))));
	}

	[TestMethod]
	public void SelectsTheElementsRelatedByTheCombinator()
		=> AssertSelects(Document, $"div{Written}p", Selected);

	[TestMethod]
	public void SelectsWithinAnElementLookingAtItsAncestorsToo()
		=> AssertSelectsWithin(Document, "s1", $"div{Written}p", SelectedWithinSection);

	[TestMethod]
	public void IsWrittenBackWithSpacesAround()
	{
		foreach (var spelling in Spellings)
		{
			AssertPrints($"a{spelling}b", $"a{Written}b");
		}
	}

	/// <summary>
	/// Asserts that a combinator written as a symbol is rejected without a selector on both sides of it.
	/// </summary>
	private protected static void AssertRequiresSelectorsOnBothSides(char combinator)
	{
		AssertInvalid($"{combinator} a", $"unexpected '{combinator}'");
		AssertInvalid($"a {combinator}", "expected a selector");
		AssertInvalid($"a {combinator} {combinator} b", $"unexpected '{combinator}'");
		AssertInvalid($"a{combinator}{combinator}b", $"unexpected '{combinator}'");
		AssertInvalid($"a {combinator}, b", "unexpected ','");
		AssertInvalid($":not(a {combinator})", "unexpected ')'");
	}
}
