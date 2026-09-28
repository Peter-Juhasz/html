using PeterJuhasz.Text.Html.Selectors;
using static PeterJuhasz.Text.Html.Tests.Selectors.TestHelpers;

namespace PeterJuhasz.Text.Html.Tests.Selectors;

/// <summary>
/// Tests shared by the pseudo-classes that take a selector list, <c>:not()</c> and <c>:has()</c>.
/// </summary>
public abstract class SelectorListPseudoClassTests
{
	/// <summary>
	/// The name of the pseudo-class, without the colon and the parentheses.
	/// </summary>
	private protected abstract string FunctionName { get; }

	/// <summary>
	/// Creates the pseudo-class expected for an argument written without a leading combinator.
	/// </summary>
	private protected abstract SimpleSelector Create(ElementSelector argument);

	[TestMethod]
	public void ParsesTheArgument()
		=> AssertParses($":{FunctionName}(a)", Compound(Create(Element("a"))));

	[TestMethod]
	public void IgnoresWhitespaceAroundTheArgument()
		=> AssertParses($":{FunctionName}( \t a \n )", Compound(Create(Element("a"))));

	[TestMethod]
	public void NameIsCaseInsensitive()
		=> AssertParses($":{FunctionName.ToUpperInvariant()}(a)", Compound(Create(Element("a"))));

	[TestMethod]
	public void ParsesASelectorList()
		=> AssertParses($":{FunctionName}(a, .b, c > d)", Compound(Create(Element("a") | Compound(Class("b")) | new ChildSelector(Left: Element("c"), Right: Element("d")))));

	[TestMethod]
	public void ParsesComplexSelectors()
	{
		var expected = new DescendantSelector(
			Left: new NextSiblingSelector(
				Left: new ChildSelector(Left: Element("div"), Right: Element("p")),
				Right: Element("span")),
			Right: Element("em"));

		AssertParses($":{FunctionName}(div > p + span em)", Compound(Create(expected)));
	}

	[TestMethod]
	public void FollowsOtherSimpleSelectors()
		=> AssertParses($"p.x:{FunctionName}(a)", Compound(ElementName("p"), Class("x"), Create(Element("a"))));

	[TestMethod]
	public void NestsUpToTheLimit()
	{
		ElementSelector expected = Element("a");
		for (var i = 0; i < ElementSelectorParser.MaxDepth; i++)
		{
			expected = Compound(Create(expected));
		}

		AssertParses(Nest(ElementSelectorParser.MaxDepth), expected);
		AssertInvalid(Nest(ElementSelectorParser.MaxDepth + 1), "nested too deeply");

		string Nest(int depth) => string.Concat(Enumerable.Repeat($":{FunctionName}(", depth)) + "a" + new string(')', depth);
	}

	[TestMethod]
	public void RejectsMissingOrIncompleteArguments()
	{
		AssertInvalid($":{FunctionName}", "unsupported pseudo-class");
		AssertInvalid($":{FunctionName}()", "unexpected ')'");
		AssertInvalid($":{FunctionName}( )", "unexpected ')'");
		AssertInvalid($":{FunctionName}(a", "expected ')'");
		AssertInvalid($":{FunctionName}(a,)", "unexpected ')'");
		AssertInvalid($":{FunctionName}(,a)", "unexpected ','");
	}

	[TestMethod]
	public void IsWrittenBack()
		=> AssertPrints($":{FunctionName}(a,.b)", $":{FunctionName}(a, .b)");
}
