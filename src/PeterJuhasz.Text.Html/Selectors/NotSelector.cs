using PeterJuhasz.Text.Html.Model;
using System.Text;

namespace PeterJuhasz.Text.Html.Selectors;

/// <summary>
/// <c>:not(selector)</c>: matches elements that do not match the selector, which may be a list or contain combinators, like <c>:not(.a, div > p)</c>.
/// </summary>
/// <param name="Selector">The selector the element must not match; combinators in it look at ancestors beyond the scope of the query too.</param>
internal sealed record class NotSelector(ElementSelector Selector) : SimpleSelector
{
	/// <inheritdoc/>
	public override bool Matches(HtmlElement element, in SelectorContext context) => !Selector.Matches(element, context);

	/// <inheritdoc/>
	internal override void AppendTo(StringBuilder builder)
	{
		builder.Append(":not(");
		Selector.AppendTo(builder);
		builder.Append(')');
	}
}
