using PeterJuhasz.Text.Html.Model;

namespace PeterJuhasz.Text.Html.Selectors;

/// <summary>
/// <c>Left Right</c>: an ancestor of the element matches <see cref="ComplexSelector.Left"/>.
/// </summary>
internal sealed record class DescendantSelector(ElementSelector Left, CompoundSelector Right) : ComplexSelector(Left, Right)
{
	/// <inheritdoc/>
	protected override string Combinator => " ";

	/// <inheritdoc/>
	protected override MatchResult MatchLeft(HtmlElement element, in SelectorContext context)
	{
		for (var ancestor = element.Parent; ancestor is not null; ancestor = ancestor.Parent)
		{
			var result = Left.Match(ancestor, context);
			if (result is not MatchResult.NotMatched)
			{
				return result;
			}
		}

		// no ancestor fits, and any element the combinators further out could try instead is higher up, with only some of these ancestors
		return MatchResult.NotMatchedGlobally;
	}
}
