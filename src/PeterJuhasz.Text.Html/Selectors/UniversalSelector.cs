using PeterJuhasz.Text.Html.Model;
using System.Text;

namespace PeterJuhasz.Text.Html.Selectors;

/// <summary>
/// <c>*</c>: matches any element, in place of an element name.
/// </summary>
internal sealed record class UniversalSelector : SimpleSelector
{
	/// <summary>
	/// The only instance, as the selector has no parameters.
	/// </summary>
	public static readonly UniversalSelector Instance = new();

	private UniversalSelector()
	{
	}

	/// <inheritdoc/>
	public override bool Matches(HtmlElement element, in SelectorContext context) => true;

	/// <inheritdoc/>
	internal override void AppendTo(StringBuilder builder) => builder.Append('*');
}
