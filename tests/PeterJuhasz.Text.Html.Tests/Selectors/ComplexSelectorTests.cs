using PeterJuhasz.Text.Html.Selectors;
using System.Text;
using static PeterJuhasz.Text.Html.Tests.Selectors.TestHelpers;

namespace PeterJuhasz.Text.Html.Tests.Selectors;

/// <summary>
/// Tests of how chains of combinators are matched: a failure deep in a chain stops the search only when no other element could match,
/// and the recursion stays within what the limits of the parser allow.
/// </summary>
[TestClass]
public sealed class ComplexSelectorTests
{
	/// <summary>
	/// The element names of random documents and selectors; not HTML element names, so the parser closes none of them implicitly.
	/// </summary>
	private static readonly string[] Names = ["x", "y", "z"];

	private static readonly string[] Combinators = [" ", " > ", " + "];

	[TestMethod]
	// the nearest y is not a child of the x
	[DataRow("<x><y id=y1><w><y id=y2><z id=z></z></y></w></y></x>", "x > y z")]
	// the nearest y has no previous sibling
	[DataRow("<x></x><y id=y1><y id=y2><z id=z></z></y></y>", "x + y z")]
	// the previous sibling of the nearest y is not an x
	[DataRow("<x></x><y id=y1><w></w><y id=y2><z id=z></z></y></y>", "x + y z")]
	public void KeepsTryingAncestorsWhenTheCombinatorFailsForTheNearest(string html, string selector)
		=> AssertSelects(html, selector, "z");

	[TestMethod]
	public void FindsTheSameElementsAsTryingEveryRelatedElement()
	{
		var random = new Random(1);
		for (var i = 0; i < 2000; i++)
		{
			var document = HtmlDocument.Parse(RandomHtml(random, depth: 0));
			var selector = ElementSelector.Parse(RandomSelector(random));
			foreach (var element in document.Descendants())
			{
				Assert.AreEqual(MatchesByDefinition(selector, element), selector.Matches(element, default), $"Matching '{selector}' against {element}.");
			}
		}
	}

	[TestMethod]
	public void GivesUpOnceNoAncestorCanMatchTheStartOfTheChain()
	{
		// there is no p, so trying every combination of ancestors for the four divs would check them about depth^4 / 24 times
		const int depth = 100;
		var html = string.Concat(Enumerable.Repeat("<div>", depth)) + "<span></span>" + string.Concat(Enumerable.Repeat("</div>", depth));
		var counter = new CountingSelector();
		ElementSelector selector = Element("p");
		for (var i = 0; i < 4; i++)
		{
			selector = new DescendantSelector(Left: selector, Right: Compound(ElementName("div"), counter));
		}

		selector = new DescendantSelector(Left: selector, Right: Element("span"));

		Assert.IsEmpty(HtmlDocument.Parse(html).QuerySelectorAll(selector));
		Assert.IsLessThanOrEqualTo(depth, counter.Count);
	}

	[TestMethod]
	public void MatchesTheLongestChainsNestedAsDeeplyAsAllowed()
	{
		// matching recurses once per combinator, so this goes as deep as the limits allow: at every level of nesting a chain of + as long as allowed,
		// with the next level in its first compound, which is checked last, over enough siblings that every chain is followed to its start;
		// if the limits let the stack overflow, the test run crashes instead of failing this test
		var chain = string.Concat(Enumerable.Repeat(" + i", ElementSelectorParser.MaxCompoundSelectors - 1));
		var selector = "i" + chain;
		for (var level = 0; level < ElementSelectorParser.MaxDepth; level++)
		{
			selector = $"i:not({selector}){chain}";
		}

		// only the last element is matched against the whole chain, which keeps the test fast
		selector = selector[..^1] + "#last";
		var siblings = (ElementSelectorParser.MaxDepth + 1) * ElementSelectorParser.MaxCompoundSelectors;
		var html = "<div>" + string.Concat(Enumerable.Repeat("<i></i>", siblings)) + "<i id=last></i></div>";

		// the innermost chain matches there, and the negations around it cancel out, as there is an even number of them
		AssertSelects(html, selector, "last");
	}

	/// <summary>
	/// Matches a chain of combinators by their definitions, trying every related element, as the reference that stopping early must agree with.
	/// </summary>
	private static bool MatchesByDefinition(ElementSelector selector, HtmlElement element) => selector switch
	{
		DescendantSelector descendant => descendant.Right.Matches(element, default) && Ancestors(element).Any(ancestor => MatchesByDefinition(descendant.Left, ancestor)),
		ChildSelector child => child.Right.Matches(element, default) && element.Parent is { } parent && MatchesByDefinition(child.Left, parent),
		NextSiblingSelector sibling => sibling.Right.Matches(element, default) && PreviousSibling(element) is { } previous && MatchesByDefinition(sibling.Left, previous),
		_ => selector.Matches(element, default),
	};

	private static IEnumerable<HtmlElement> Ancestors(HtmlElement element)
	{
		for (var ancestor = element.Parent; ancestor is not null; ancestor = ancestor.Parent)
		{
			yield return ancestor;
		}
	}

	private static HtmlElement? PreviousSibling(HtmlElement element)
		=> (element.Parent?.Nodes ?? element.Document.Nodes).TakeWhile(node => node != element).OfType<HtmlElement>().LastOrDefault();

	/// <summary>
	/// Writes elements with random names, up to 3 of them at each level and 6 levels deep.
	/// </summary>
	private static string RandomHtml(Random random, int depth)
	{
		var count = depth switch
		{
			0 => random.Next(1, 4),
			< 6 => random.Next(0, 4),
			_ => 0,
		};

		var builder = new StringBuilder();
		for (var i = 0; i < count; i++)
		{
			var name = Names[random.Next(Names.Length)];
			builder.Append($"<{name}>{RandomHtml(random, depth + 1)}</{name}>");
		}

		return builder.ToString();
	}

	/// <summary>
	/// Writes 1 to 5 element names or <c>*</c>, joined by random combinators.
	/// </summary>
	private static string RandomSelector(Random random)
	{
		var builder = new StringBuilder(NameOrUniversal());
		for (var count = random.Next(1, 6); count > 1; count--)
		{
			builder.Append(Combinators[random.Next(Combinators.Length)]).Append(NameOrUniversal());
		}

		return builder.ToString();

		string NameOrUniversal() => random.Next(Names.Length + 1) < Names.Length ? Names[random.Next(Names.Length)] : "*";
	}

	/// <summary>
	/// Matches every element and counts how many times it is checked, to measure how much work matching does.
	/// </summary>
	private sealed record class CountingSelector : SimpleSelector
	{
		public int Count { get; private set; }

		public override bool Matches(HtmlElement element, in SelectorContext context)
		{
			Count++;
			return true;
		}

		internal override void AppendTo(StringBuilder builder) => builder.Append(":counting");
	}
}
