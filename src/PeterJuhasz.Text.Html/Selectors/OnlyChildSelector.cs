using PeterJuhasz.Text.Html.Model;
using System.Text;

namespace PeterJuhasz.Text.Html.Selectors;

/// <summary>
/// <c>:only-child</c>: matches elements without sibling elements; text and comments around them do not count.
/// With <see cref="OfType"/>, <c>:only-of-type</c>: matches elements without sibling elements of the same name.
/// The same as the first and last child together, but scans the siblings once and stops at the first one that counts.
/// </summary>
/// <param name="OfType">Whether only the siblings with the same name as the element count, compared case-insensitively.</param>
internal sealed record class OnlyChildSelector(bool OfType = false) : SimpleSelector
{
	/// <summary>
	/// <c>:only-child</c>: the only element among its siblings.
	/// </summary>
	public static readonly OnlyChildSelector OnlyChild = new(OfType: false);

	/// <summary>
	/// <c>:only-of-type</c>: the only element with its name among its siblings.
	/// </summary>
	public static readonly OnlyChildSelector OnlyOfType = new(OfType: true);

	/// <inheritdoc/>
	public override bool Matches(HtmlElement element, in SelectorContext context)
	{
		var siblings = element.Parent?.Nodes ?? element.Document.Nodes;
		foreach (var node in siblings)
		{
			if (node is HtmlElement sibling && sibling != element && (!OfType || sibling.Name.Equals(element.Name, StringComparison.OrdinalIgnoreCase)))
			{
				return false;
			}
		}

		return true;
	}

	/// <inheritdoc/>
	internal override void AppendTo(StringBuilder builder) => builder.Append(OfType ? ":only-of-type" : ":only-child");
}
