using PeterJuhasz.Text.Html.Model;
using System.Text;

namespace PeterJuhasz.Text.Html.Selectors;

/// <summary>
/// <c>#id</c>: matches elements whose id attribute is the ID, compared case-sensitively with character references decoded.
/// </summary>
/// <param name="Id">The ID to match.</param>
internal sealed record class IdSelector(string Id) : SimpleSelector
{
	/// <inheritdoc/>
	public override bool Matches(HtmlElement element, in SelectorContext context)
	{
		if (!element.TryGetAttribute("id", out var attribute))
		{
			return false;
		}

		// the decoded value is reused when it was already created
		if (attribute._value is string decoded)
		{
			return decoded == Id;
		}

		return ElementQuery.HasAttributeValue(attribute.ValueSpan, Id);
	}

	/// <inheritdoc/>
	internal override void AppendTo(StringBuilder builder) => builder.Append('#').Append(Id);
}
