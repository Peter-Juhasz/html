using Microsoft.Extensions.Primitives;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace PeterJuhasz.Text.Html.Lazy;

// Enumerates the elements, text and comments directly inside a range of the document, in document order.
// Doctypes, processing instructions, CDATA sections and stray end tags are skipped; empty text is not reported.
[PerformanceCritical]
public struct NodesEnumerator
{
	// The position while the element reported last has not been moved past, as its end is not known yet.
	private const int AfterUnscannedElement = -1;

	private readonly StringSegment _document;
	private readonly int _end;
	private readonly bool _isRawText;
	private readonly bool _isLiteral;
	private readonly bool _isLazy;

	// The element whose content is enumerated up to whatever ends it, when the range is not known.
	private readonly OpenElement _parent;

	private int _position;
	private int _parentEnd;
	private LazyHtmlNode _current;

	// `isRawText` reports the whole range as a single text node, for the content of raw text elements;
	// `isLiteral` is true when that text is taken literally (script and style), so its character references are not decoded.
	// `isLazy` reports elements without scanning their content, which is then scanned when moving past them unless `SkipElement` tells their end.
	internal NodesEnumerator(StringSegment document, int start, int end, bool isRawText = false, bool isLiteral = false, bool isLazy = false)
	{
		_document = document;
		_position = start;
		_end = end;
		_isRawText = isRawText;
		_isLiteral = isLiteral;
		_isLazy = isLazy;
		_parentEnd = end;
	}

	// Enumerates the content of the element. When the end of the element is not known yet, its content is enumerated up to whatever ends it
	// instead of being scanned first; `ParentEnd` tells the end afterwards.
	internal NodesEnumerator(LazyHtmlElement parent, bool isLazy = false)
	{
		var name = parent.NameSpan;
		_document = parent.Document;
		_position = parent.ContentStart;
		_isLazy = isLazy;

		// a void element has no content, and raw text has no markup inside, so finding their ends is cheap
		if (parent.IsScanned || SyntaxFacts.IsVoidElement(name) || SyntaxFacts.IsRawTextElement(name))
		{
			_parentEnd = parent.GetEnd(out _end);
			_isRawText = SyntaxFacts.IsRawTextElement(name);
			_isLiteral = _isRawText && !SyntaxFacts.IsEscapableRawTextElement(name);
			return;
		}

		// without anything ending it, the content runs to the end of the document
		_end = _parentEnd = _document.Length;
		_parent = new(parent);
	}

	public readonly LazyHtmlNode Current => _current;

	// Index right after the element whose content is enumerated, once the enumeration has ended.
	internal readonly int ParentEnd => _parentEnd;

	public readonly NodesEnumerator GetEnumerator() => this;

	// Moves past the element reported last, whose end is already known from elsewhere, so its content is not scanned again.
	internal void SkipElement(int end)
	{
		Debug.Assert(_position == AfterUnscannedElement && _current.Kind == LazyHtmlNodeKind.Element);
		_position = end;
	}

	public bool MoveNext()
	{
		if (_position == AfterUnscannedElement)
		{
			_position = _current.End;
		}

		if (_position >= _end)
		{
			return false;
		}

		if (_isRawText)
		{
			_current = new(LazyHtmlNodeKind.Text, _document, _position, _end, _isLiteral);
			_position = _end;
			return true;
		}

		var text = _document.AsSpan()[.._end];
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

			if (_parent.Exists && _parent.Ends(text, kind, index, out var parentEnd))
			{
				_parentEnd = parentEnd;
				_position = _end;
				return false;
			}

			switch (kind)
			{
				case MarkupKind.StartTag when _isLazy:
					var contentStart = HtmlScanner.ScanStartTag(text, index, out var nameLength, out var isSelfClosing);
					_current = new(new LazyHtmlElement(_document, index, nameLength, contentStart, isSelfClosing));
					_position = AfterUnscannedElement;
					return true;

				case MarkupKind.StartTag:
					_current = new(new LazyHtmlElement(_document, index));
					_position = _current.End;
					return true;

				case MarkupKind.Other when text[index..].StartsWith(SyntaxFacts.CommentStart):
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

public static partial class Extensions
{
	extension(NodesEnumerator enumerator)
	{
		public IEnumerable<LazyHtmlNode> AsEnumerable()
		{
			foreach (var node in enumerator)
			{
				yield return node;
			}
		}
	}
}