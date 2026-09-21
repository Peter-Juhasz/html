using Microsoft.Extensions.Primitives;
using System.Diagnostics.CodeAnalysis;

namespace System.Text.Html.Lazy;

// Enumerates the elements with a given name at any depth inside a range of the document, in document order.
[PerformanceCritical]
public struct ElementsByNameEnumerator
{
	private readonly StringSegment _document;
	private readonly string _name;
	private readonly int _end;
	private int _position;
	private LazyHtmlElement _current;

	internal ElementsByNameEnumerator(StringSegment document, string name, int start, int end)
	{
		ArgumentException.ThrowIfNullOrEmpty(name);

		_document = document;
		_name = name;
		_position = start;
		_end = end;
	}

	public readonly LazyHtmlElement Current => _current;

	public readonly ElementsByNameEnumerator GetEnumerator() => this;

	public bool MoveNext()
	{
		var text = _document.AsSpan().Slice(0, _end);
		while (_position < _end)
		{
			var kind = HtmlScanner.FindMarkup(text, _position, out var index);
			if (kind == MarkupKind.None)
				break;

			if (kind != MarkupKind.StartTag)
			{
				_position = HtmlScanner.SkipMarkup(text, index);
				continue;
			}

			// the start tag is scanned as a whole so a '<' inside an attribute value is not mistaken for markup
			_position = HtmlScanner.ScanStartTag(text, index, out var nameLength, out var isSelfClosing);
			var name = text.Slice(index + 1, nameLength);
			var isRawText = !isSelfClosing && SyntaxFacts.IsRawTextElement(name);

			if (name.Equals(_name, StringComparison.OrdinalIgnoreCase))
			{
				_current = new LazyHtmlElement(_document, index);

				// continue inside the content so nested matches are found too; raw text has no elements inside
				_position = isRawText ? _current.End : _current.ContentStart;
				return true;
			}

			if (isRawText)
				_position = HtmlScanner.SkipMarkup(text, HtmlScanner.FindRawTextEnd(text, _position, name));
		}

		return false;
	}
}
