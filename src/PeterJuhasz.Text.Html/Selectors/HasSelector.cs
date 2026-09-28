using PeterJuhasz.Text.Html.Model;
using System.Text;

namespace PeterJuhasz.Text.Html.Selectors;

/// <summary>
/// <c>:has(selector)</c>: matches elements from which the relative selector reaches a matching element, like <c>:has(> img)</c> for elements with an img child.
/// A relative selector starts from an <see cref="AnchorSelector"/> that stands for the element being checked; the static methods add it to a selector.
/// </summary>
/// <param name="Selector">
/// The relative selector, or a list of them. It is matched against the descendants of the element,
/// or against its following siblings and their descendants when it starts with a sibling combinator.
/// </param>
internal sealed record class HasSelector(ElementSelector Selector) : SimpleSelector
{
	/// <summary>
	/// <c>:has(selector)</c>: an element inside the element matches the selector; in <c>:has(div img)</c> the div must be inside it too.
	/// </summary>
	public static HasSelector Descendant(ElementSelector selector)
		=> new(Selector: Anchor(selector, compound => new DescendantSelector(Left: AnchorSelector.Instance, Right: compound)));

	/// <summary>
	/// <c>:has(> selector)</c>: a child of the element starts a match of the selector.
	/// </summary>
	public static HasSelector Child(ElementSelector selector)
		=> new(Selector: Anchor(selector, compound => new ChildSelector(Left: AnchorSelector.Instance, Right: compound)));

	/// <summary>
	/// <c>:has(+ selector)</c>: the next sibling of the element starts a match of the selector.
	/// </summary>
	public static HasSelector NextSibling(ElementSelector selector)
		=> new(Selector: Anchor(selector, compound => new NextSiblingSelector(Left: AnchorSelector.Instance, Right: compound)));

	/// <summary>
	/// Makes the selector relative by joining the anchor to its leftmost compound selector, or to that of each selector of a list.
	/// </summary>
	private static ElementSelector Anchor(ElementSelector selector, Func<CompoundSelector, ComplexSelector> join) => selector switch
	{
		CompoundSelector compound => join(compound),
		SimpleSelector simple => join(new CompoundSelector(Selectors: [simple])),
		ComplexSelector complex => complex with { Left = Anchor(complex.Left, join) },
		OrSelector list => new OrSelector(Selectors: [.. list.Selectors.Select(branch => Anchor(branch, join))]),
		_ => throw new ArgumentException($"'{selector}' cannot be made relative.", nameof(selector)),
	};

	/// <inheritdoc/>
	public override bool Matches(HtmlElement element, in SelectorContext context)
	{
		// a context of its own, so that a :has() nested in the argument keeps its own anchor
		var anchored = context with { Anchor = element };
		if (Selector is OrSelector list)
		{
			foreach (var branch in list.Selectors)
			{
				if (MatchesRelative(element, branch, anchored))
				{
					return true;
				}
			}

			return false;
		}

		return MatchesRelative(element, Selector, anchored);
	}

	/// <summary>
	/// Checks whether any element that the relative selector can reach from the element matches it.
	/// </summary>
	private static bool MatchesRelative(HtmlElement element, ElementSelector selector, in SelectorContext context)
	{
		foreach (var candidate in Candidates(element, selector))
		{
			if (selector.Matches(candidate, context))
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Enumerates the elements that the relative selector can reach from the element: its children or its next sibling when the selector is a single step from the anchor,
	/// its following siblings and their descendants when it starts with a sibling combinator, and its descendants otherwise.
	/// </summary>
	private static IEnumerable<HtmlElement> Candidates(HtmlElement element, ElementSelector selector) => selector switch
	{
		ChildSelector { Left: AnchorSelector } => element.Elements(),
		NextSiblingSelector { Left: AnchorSelector } => FollowingSiblings(element).Take(1),
		_ when StartsWithSiblingCombinator(selector) => FollowingSiblingsAndDescendants(element),
		_ => element.Descendants(),
	};

	/// <summary>
	/// Checks whether the combinator after the anchor leads to the siblings of the element rather than inside it.
	/// </summary>
	private static bool StartsWithSiblingCombinator(ElementSelector selector) => selector switch
	{
		NextSiblingSelector { Left: AnchorSelector } => true,
		ComplexSelector complex => StartsWithSiblingCombinator(complex.Left),
		_ => false,
	};

	/// <summary>
	/// Enumerates the sibling elements after the element, in document order.
	/// </summary>
	private static IEnumerable<HtmlElement> FollowingSiblings(HtmlElement element)
	{
		var siblings = element.Parent?.Nodes ?? element.Document.Nodes;
		for (var i = siblings.IndexOf(element) + 1; i < siblings.Length; i++)
		{
			if (siblings[i] is HtmlElement sibling)
			{
				yield return sibling;
			}
		}
	}

	/// <summary>
	/// Enumerates the sibling elements after the element and their descendants, in document order.
	/// </summary>
	private static IEnumerable<HtmlElement> FollowingSiblingsAndDescendants(HtmlElement element)
	{
		foreach (var sibling in FollowingSiblings(element))
		{
			yield return sibling;
			foreach (var descendant in sibling.Descendants())
			{
				yield return descendant;
			}
		}
	}

	/// <inheritdoc/>
	internal override void AppendTo(StringBuilder builder)
	{
		builder.Append(":has(");
		Selector.AppendTo(builder);
		builder.Append(')');
	}
}
