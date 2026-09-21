using PeterJuhasz.Text.Html.Lazy;

namespace PeterJuhasz.Text.Html.Model;

public sealed class HtmlText : HtmlNode
{
	// The text in the source; it is read from there on each access, so the tree keeps the source alive.
	private readonly LazyHtmlText _source;

	internal HtmlText(HtmlDocument document, HtmlElement? parent, LazyHtmlText source)
		: base(document, parent)
	{
		_source = source;
	}

	// Text as written (character references are not decoded).
	public ReadOnlySpan<char> TextSpan => _source.TextSpan;

	public string Text => _source.Text;

	public override ReadOnlySpan<char> OuterSpan => _source.TextSpan;
}
