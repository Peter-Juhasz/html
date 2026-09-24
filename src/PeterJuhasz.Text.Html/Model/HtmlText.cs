using PeterJuhasz.Text.Html.Lazy;
using System.Text;

namespace PeterJuhasz.Text.Html.Model;

public sealed class HtmlText : HtmlNode
{
	// The text in the source; the span is read from there on each access, so the tree keeps the source alive.
	private readonly LazyHtmlText _source;

	// The decoded text is created on first access and kept for the next ones.
	private string? _text;

	internal HtmlText(HtmlDocument document, HtmlElement? parent, LazyHtmlText source)
		: base(document, parent)
	{
		_source = source;
	}

	// Text as written (character references are not decoded).
	public ReadOnlySpan<char> TextSpan => _source.TextSpan;

	// Text with character references decoded, except for the content of script and style, which is taken literally.
	public string Text => _text ??= _source.Text;

	// Appends the same text as `Text`, reusing the decoded text when it has already been created.
	internal void AppendTo(StringBuilder builder)
	{
		if (_text is not null)
		{
			builder.Append(_text);
		}
		else
		{
			_source.AppendTo(builder);
		}
	}

	public override ReadOnlySpan<char> OuterSpan => _source.TextSpan;
}
