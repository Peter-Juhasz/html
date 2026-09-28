using PeterJuhasz.Text.Html.Model;
using System.Text;

namespace PeterJuhasz.Text.Html.Selectors;

/// <summary>
/// <c>[name]</c>: matches elements that have the attribute, with any value or none.
/// </summary>
/// <param name="Name">The attribute name, compared case-insensitively.</param>
internal sealed record class AttributeSelector(string Name) : SimpleSelector
{
	/// <inheritdoc/>
	public override bool Matches(HtmlElement element, in SelectorContext context) => element.HasAttribute(Name);

	/// <inheritdoc/>
	internal override void AppendTo(StringBuilder builder) => builder.Append('[').Append(Name).Append(']');
}
