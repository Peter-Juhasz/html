using Microsoft.Extensions.Primitives;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace PeterJuhasz.Text.Html.Lazy;

public enum LazyHtmlNodeKind
{
	Element,
	Text,
	Comment,
}

// An element, text or comment; `Kind` tells which one, and the matching member gives the typed view.
// The element's scan result is kept, so viewing it as an element does not scan it again.
[PerformanceCritical]
public readonly struct LazyHtmlNode
{
	private readonly StringSegment _document;
	private readonly LazyHtmlNodeKind _kind;
	private readonly int _start;
	private readonly int _nameLength;
	private readonly int _contentStart;
	private readonly int _contentEnd;
	private readonly int _end;
	private readonly bool _isLiteral;

	internal LazyHtmlNode(LazyHtmlElement element)
	{
		_document = element.Document;
		_kind = LazyHtmlNodeKind.Element;
		_start = element.Start;
		_nameLength = element.NameLength;
		_contentStart = element.ContentStart;
		_contentEnd = element.ContentEnd;
		_end = element.End;
	}

	// A text or comment node spanning the [start, end) range of the document.
	// `isLiteral` is true for the text of script and style, which is taken literally; it is not used for comments.
	internal LazyHtmlNode(LazyHtmlNodeKind kind, StringSegment document, int start, int end, bool isLiteral = false)
	{
		Debug.Assert(kind is LazyHtmlNodeKind.Text or LazyHtmlNodeKind.Comment);

		_document = document;
		_kind = kind;
		_start = start;
		_end = end;
		_isLiteral = isLiteral;
	}

	// Index right after the node, used to continue enumeration.
	internal int End => _end;

	public LazyHtmlNodeKind Kind => _kind;

	// The whole node as written: the element with its tags, the text, or the comment with its delimiters.
	public ReadOnlySpan<char> OuterSpan => _document.AsSpan()[_start.._end];

	public LazyHtmlElement Element => TryGetElement(out var element) ? element : throw new InvalidOperationException("The node is not an element.");

	public LazyHtmlText Text => TryGetText(out var text) ? text : throw new InvalidOperationException("The node is not text.");

	public LazyHtmlComment Comment => TryGetComment(out var comment) ? comment : throw new InvalidOperationException("The node is not a comment.");

	public bool TryGetElement(out LazyHtmlElement element)
	{
		if (_kind != LazyHtmlNodeKind.Element)
		{
			element = default;
			return false;
		}

		element = new(_document, _start, _nameLength, _contentStart, _contentEnd, _end);
		return true;
	}

	public bool TryGetText(out LazyHtmlText text)
	{
		if (_kind != LazyHtmlNodeKind.Text)
		{
			text = default;
			return false;
		}

		text = new(_document, _start, _end, _isLiteral);
		return true;
	}

	public bool TryGetComment(out LazyHtmlComment comment)
	{
		if (_kind != LazyHtmlNodeKind.Comment)
		{
			comment = default;
			return false;
		}

		comment = new(_document, _start, _end);
		return true;
	}
}
