using Microsoft.Extensions.Primitives;
using System.Diagnostics.CodeAnalysis;

namespace System.Text.Html.Lazy;

// Enumerates the elements matching a name and/or attributes at any depth inside a range of the document, in document order.
// A ref struct so the attributes can be kept as a span, letting callers pass them without allocating.
[PerformanceCritical]
public ref struct ElementsQueryEnumerator
{
	private readonly StringSegment _document;
	private readonly string? _name;
	private readonly ReadOnlySpan<KeyValuePair<string, string>> _attributes;
	private readonly int _end;
	private int _position;
	private LazyHtmlElement _current;

	internal ElementsQueryEnumerator(StringSegment document, string? name, ReadOnlySpan<KeyValuePair<string, string>> attributes, int start, int end)
	{
		if (name is { Length: 0 })
			throw new ArgumentException("The element name must not be empty.", nameof(name));

		foreach (var attribute in attributes)
			ArgumentException.ThrowIfNullOrEmpty(attribute.Key, nameof(attributes));

		_document = document;
		_name = name;
		_attributes = attributes;
		_position = start;
		_end = end;
	}

	public readonly LazyHtmlElement Current => _current;

	public readonly ElementsQueryEnumerator GetEnumerator() => this;

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

			// the attributes are only looked at when the name matches, and the element is only scanned when everything matches
			if ((_name is null || name.Equals(_name, StringComparison.OrdinalIgnoreCase))
				&& (_attributes.IsEmpty || HasAttributes(index + 1 + nameLength, _position)))
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

	// Checks that the start tag in the given range has every required attribute with the required value.
	private readonly bool HasAttributes(int start, int end)
	{
		foreach (var (name, value) in _attributes)
		{
			if (!new AttributesEnumerator(_document, start, end).TryFind(name, out var attribute) || !attribute.ValueSpan.SequenceEqual(value))
				return false;
		}

		return true;
	}
}
