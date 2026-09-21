using PeterJuhasz.Text.Html.Lazy;

namespace PeterJuhasz.Text.Html.Model;

public sealed class HtmlComment : HtmlNode
{
	// The comment in the source; it is read from there on each access, so the tree keeps the source alive.
	private readonly LazyHtmlComment _source;

	internal HtmlComment(HtmlDocument document, HtmlElement? parent, LazyHtmlComment source)
		: base(document, parent)
	{
		_source = source;
	}

	// The content between the delimiters, as written.
	public ReadOnlySpan<char> TextSpan => _source.TextSpan;

	public string Text => _source.Text;

	public override ReadOnlySpan<char> OuterSpan => _source.OuterSpan;
}
