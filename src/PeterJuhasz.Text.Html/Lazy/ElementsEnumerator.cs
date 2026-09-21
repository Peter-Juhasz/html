using Microsoft.Extensions.Primitives;
using System.Collections.Frozen;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Text.Html.Lazy;

// Enumerates the elements directly inside the document or inside an element's content, skipping text and other markup.
// Each element is scanned as a whole to find where the next one starts, so the elements know their end already.
// When the end of the content is not known up front, it is found along the way, the same way ScanContent finds it.
[PerformanceCritical]
public struct ElementsEnumerator
{
	private readonly StringSegment _document;
	// The end of the range to enumerate; the end of the document when the end of the content is found along the way.
	private readonly int _end;
	// The start tags that implicitly close the parent element, when its end is found along the way.
	private readonly FrozenSet<string>.AlternateLookup<ReadOnlySpan<char>> _closers;
	private readonly bool _hasClosers;
	// Whether the end of the content is found along the way, in which case any end tag closes the parent element.
	private readonly bool _findsEnd;
	private int _position;
	private LazyHtmlElement _current;

	// Enumerates the elements of the whole document.
	internal ElementsEnumerator(StringSegment document)
	{
		_document = document;
		_end = document.Length;
	}

	// Enumerates the elements in the range [start, end) of the document.
	internal ElementsEnumerator(StringSegment document, int start, int end)
	{
		_document = document;
		_position = start;
		_end = end;
	}

	// Enumerates the elements inside the content starting at `contentStart` of the element whose start tag is at `parentStart`,
	// finding the end of the content along the way.
	internal ElementsEnumerator(StringSegment document, int contentStart, int parentStart, int parentNameLength)
	{
		Debug.Assert(parentNameLength > 0);

		_document = document;
		_position = contentStart;
		_end = document.Length;
		_findsEnd = true;
		_hasClosers = SyntaxFacts.TryGetImplicitClosers(document.AsSpan().Slice(parentStart + 1, parentNameLength), out _closers);
	}

	public readonly LazyHtmlElement Current => _current;

	public readonly ElementsEnumerator GetEnumerator() => this;

	public bool MoveNext()
	{
		// markup is looked for within the range, but the elements are scanned in the whole document like everywhere else
		var document = _document.AsSpan();
		var text = document.Slice(0, _end);
		while (_position < _end)
		{
			var kind = HtmlScanner.FindMarkup(text, _position, out var index);
			switch (kind)
			{
				case MarkupKind.None:
					_position = index;
					return false;

				// any end tag closes the parent, a mismatched one implicitly (it is left for an ancestor)
				case MarkupKind.EndTag when _findsEnd:
					_position = _end;
					return false;

				case MarkupKind.StartTag:
					var contentStart = HtmlScanner.ScanStartTag(document, index, out var nameLength, out var isSelfClosing);
					var name = document.Slice(index + 1, nameLength);
					if (_hasClosers && _closers.Contains(name))
					{
						_position = _end;
						return false;
					}

					var contentKind = HtmlScanner.GetContentKind(name, isSelfClosing);
					var end = HtmlScanner.ScanContent(document, index, nameLength, contentStart, contentKind, out var contentEnd);
					_current = new LazyHtmlElement(_document, index, nameLength, contentStart, contentKind, contentEnd, end);
					_position = end;
					return true;

				default:
					_position = HtmlScanner.SkipMarkup(text, index);
					break;
			}
		}

		return false;
	}
}