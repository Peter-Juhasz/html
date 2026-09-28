using PeterJuhasz.Text.Html.Model;
using System.Text;

namespace PeterJuhasz.Text.Html.Selectors;

/// <summary>
/// The start of a relative selector in <c>:has()</c>, which stands for the element that <c>:has()</c> checks, so <c>:has(> img)</c> is a child selector from the anchor.
/// It is written as nothing, and outside of <c>:has()</c> it matches nothing.
/// </summary>
internal sealed record class AnchorSelector : SimpleSelector
{
	/// <summary>
	/// The only instance, as the selector has no parameters; the anchor it matches comes from the <see cref="SelectorContext"/>.
	/// </summary>
	public static readonly AnchorSelector Instance = new();

	private AnchorSelector()
	{
	}

	/// <inheritdoc/>
	public override bool Matches(HtmlElement element, in SelectorContext context) => element == context.Anchor;

	/// <inheritdoc/>
	internal override void AppendTo(StringBuilder builder)
	{
	}
}
