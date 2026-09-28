using PeterJuhasz.Text.Html.Model;
using System.Text;

namespace PeterJuhasz.Text.Html.Selectors;

/// <summary>
/// Two selectors joined by a combinator, like <c>div > p</c>; each combinator is a derived type that defines how the elements are related.
/// Matched right to left: the element must match <see cref="Right"/>, then the combinator looks for related elements that match <see cref="Left"/>.
/// A failure deeper in a chain reports whether other related elements are still worth trying, as a <see cref="MatchResult"/>, which keeps the search linear.
/// </summary>
/// <param name="Left">The selector the related elements must match; may itself be a complex selector, so <c>a b > c</c> is a child selector whose left is <c>a b</c>.</param>
/// <param name="Right">The selector the element itself must match.</param>
internal abstract record class ComplexSelector(ElementSelector Left, CompoundSelector Right) : ElementSelector
{
	/// <summary>
	/// The combinator as written between the two selectors.
	/// </summary>
	protected abstract string Combinator { get; }

	/// <inheritdoc/>
	public sealed override bool Matches(HtmlElement element, in SelectorContext context) => Match(element, context) is MatchResult.Matched;

	/// <inheritdoc/>
	internal sealed override MatchResult Match(HtmlElement element, in SelectorContext context)
	{
		if (!Right.Matches(element, context))
		{
			return MatchResult.NotMatched;
		}

		return MatchLeft(element, context);
	}

	/// <summary>
	/// Checks whether an element related to this one by the combinator matches <see cref="Left"/>, with <see cref="ElementSelector.Match"/>.
	/// Trying further related elements after a failure, not just the nearest, is what lets <c>a > b c</c> find a b whose parent is an a further up.
	/// </summary>
	protected abstract MatchResult MatchLeft(HtmlElement element, in SelectorContext context);

	/// <inheritdoc/>
	internal sealed override void AppendTo(StringBuilder builder)
	{
		Left.AppendTo(builder);

		// a relative selector starts with its combinator, like "> img" in :has(> img), where the descendant combinator is not written
		if (Left is AnchorSelector)
		{
			builder.Append(Combinator.AsSpan().TrimStart());
		}
		else
		{
			builder.Append(Combinator);
		}

		Right.AppendTo(builder);
	}
}
