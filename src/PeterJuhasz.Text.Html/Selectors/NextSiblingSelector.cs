using PeterJuhasz.Text.Html.Model;

namespace PeterJuhasz.Text.Html.Selectors;

/// <summary>
/// <c>Left + Right</c>: the sibling element right before the element matches <see cref="ComplexSelector.Left"/>; text and comments between them are skipped.
/// The previous sibling is found by scanning the siblings up to the element, as the model does not keep positions.
/// </summary>
internal sealed record class NextSiblingSelector(ElementSelector Left, CompoundSelector Right) : ComplexSelector(Left, Right)
{
	/// <inheritdoc/>
	protected override string Combinator => " + ";

	/// <inheritdoc/>
	protected override MatchResult MatchLeft(HtmlElement element, in SelectorContext context)
	{
		var siblings = element.Parent?.Nodes ?? element.Document.Nodes;

		// the element is among the nodes of its parent, so the scan stops at it
		HtmlElement? previous = null;
		for (var i = 0; siblings[i] != element; i++)
		{
			if (siblings[i] is HtmlElement sibling)
			{
				previous = sibling;
			}
		}

		// the first sibling has no previous one, but its ancestors may still have, so a descendant combinator further out keeps trying
		if (previous is null)
		{
			return MatchResult.NotMatched;
		}

		return Left.Match(previous, context);
	}
}
