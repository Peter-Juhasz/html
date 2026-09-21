using Microsoft.Extensions.Primitives;
using System.Diagnostics.CodeAnalysis;

namespace System.Text.Html.Lazy;

// Enumerates the elements, text and comments directly inside a range of the document, in document order.
// Doctypes, processing instructions, CDATA sections and stray end tags are skipped; empty text is not reported.
[PerformanceCritical]
public struct NodesEnumerator
{
	private readonly StringSegment _document;
	private readonly int _end;
	private readonly bool _isRawText;
	private int _position;
	private LazyHtmlNode _current;

	// `isRawText` reports the whole range as a single text node, for the content of raw text elements.
	internal NodesEnumerator(StringSegment document, int start, int end, bool isRawText = false)
	{
		_document = document;
		_position = start;
		_end = end;
		_isRawText = isRawText;
	}

	public readonly LazyHtmlNode Current => _current;

	public readonly NodesEnumerator GetEnumerator() => this;

	public bool MoveNext()
	{
		if (_position >= _end)
			return false;

		if (_isRawText)
		{
			_current = new(LazyHtmlNodeKind.Text, _document, _position, _end);
			_position = _end;
			return true;
		}

		var text = _document.AsSpan().Slice(0, _end);
		while (_position < _end)
		{
			var kind = HtmlScanner.FindMarkup(text, _position, out var index);

			// the text before the markup, or up to the end of the range when there is no more markup
			if (index > _position)
			{
				_current = new(LazyHtmlNodeKind.Text, _document, _position, index);
				_position = index;
				return true;
			}

			switch (kind)
			{
				case MarkupKind.StartTag:
					_current = new(new LazyHtmlElement(_document, index));
					_position = _current.End;
					return true;

				case MarkupKind.Other when text.Slice(index).StartsWith(SyntaxFacts.CommentStart):
					_position = HtmlScanner.SkipMarkup(text, index);
					_current = new(LazyHtmlNodeKind.Comment, _document, index, _position);
					return true;

				default:
					_position = HtmlScanner.SkipMarkup(text, index);
					break;
			}
		}

		return false;
	}
}
