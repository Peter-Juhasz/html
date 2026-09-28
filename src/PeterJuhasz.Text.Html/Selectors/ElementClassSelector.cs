using PeterJuhasz.Text.Html.Model;
using System.Text;

namespace PeterJuhasz.Text.Html.Selectors;

/// <summary>
/// <c>.name</c>: matches elements that have the class, compared case-sensitively.
/// </summary>
/// <param name="ClassName">The class to match.</param>
internal sealed record class ElementClassSelector(string ClassName) : SimpleSelector
{
	/// <inheritdoc/>
	public override bool Matches(HtmlElement element, in SelectorContext context) => element.HasClass(ClassName);

	/// <inheritdoc/>
	internal override void AppendTo(StringBuilder builder) => builder.Append('.').Append(ClassName);
}
