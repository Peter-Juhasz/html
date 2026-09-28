using PeterJuhasz.Text.Html.Model;

namespace PeterJuhasz.Text.Html.Selectors;

/// <summary>
/// <c>Left > Right</c>: the parent of the element matches <see cref="ComplexSelector.Left"/>.
/// </summary>
internal sealed record class ChildSelector(ElementSelector Left, CompoundSelector Right) : ComplexSelector(Left, Right)
{
	/// <inheritdoc/>
	protected override string Combinator => " > ";

	/// <inheritdoc/>
	protected override MatchResult MatchLeft(HtmlElement element, in SelectorContext context) => element.Parent switch
	{
		{ } parent => Left.Match(parent, context),

		// a top-level element has no parent, and neither would the elements higher up that the combinators further out could try instead
		null => MatchResult.NotMatchedGlobally,
	};
}
