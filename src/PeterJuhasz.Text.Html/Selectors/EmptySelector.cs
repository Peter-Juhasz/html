using PeterJuhasz.Text.Html.Lazy;
using PeterJuhasz.Text.Html.Model;
using System.Text;

namespace PeterJuhasz.Text.Html.Selectors;

/// <summary>
/// <c>:empty</c>: matches elements without child elements or text. Comments do not count, but whitespace does, as in browsers.
/// </summary>
internal sealed record class EmptySelector : SimpleSelector
{
	/// <summary>
	/// The only instance, as the selector has no parameters.
	/// </summary>
	public static readonly EmptySelector Instance = new();

	private EmptySelector()
	{
	}

	/// <inheritdoc/>
	public override bool Matches(HtmlElement element, in SelectorContext context)
	{
		foreach (var node in element.Nodes)
		{
			if (node is HtmlElement or HtmlText)
			{
				return false;
			}
		}

		// an element without nodes can still have content: markup the parser skips, like a stray end tag, which leaves it empty,
		// or content below the depth the parser descends to, which is not known, so the element is not treated as empty then
		return !element.Nodes.IsEmpty || element.InnerSpan.IsEmpty || !IsBelowDepthLimit(element);
	}

	/// <summary>
	/// Checks whether the element has as many ancestors as the parser descends to, so its content was not parsed into nodes.
	/// </summary>
	private static bool IsBelowDepthLimit(HtmlElement element)
	{
		var depth = 0;
		for (var ancestor = element.Parent; ancestor is not null; ancestor = ancestor.Parent)
		{
			if (++depth >= HtmlScanner.MaxDepth)
			{
				return true;
			}
		}

		return false;
	}

	/// <inheritdoc/>
	internal override void AppendTo(StringBuilder builder) => builder.Append(":empty");
}
