using PeterJuhasz.Text.Html.Model;
using System.Text;

namespace PeterJuhasz.Text.Html.Selectors;

/// <summary>
/// <c>p</c>: matches elements with the name, compared case-insensitively.
/// </summary>
/// <param name="Name">The element name to match.</param>
internal sealed record class ElementNameSelector(string Name) : SimpleSelector
{
	/// <inheritdoc/>
	public override bool Matches(HtmlElement element, in SelectorContext context)
		=> element.Name.Equals(Name, StringComparison.OrdinalIgnoreCase);

	/// <inheritdoc/>
	internal override void AppendTo(StringBuilder builder) => builder.Append(Name);
}
