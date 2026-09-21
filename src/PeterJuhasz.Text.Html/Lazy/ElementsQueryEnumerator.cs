using Microsoft.Extensions.Primitives;
using System.Diagnostics.CodeAnalysis;

namespace System.Text.Html.Lazy;

// Enumerates the elements matching a name, id, class and/or attributes at any depth inside a range of the document, in document order.
// A ref struct so the attributes can be kept as a span, letting callers pass them without allocating.
[PerformanceCritical]
public ref struct ElementsQueryEnumerator
{
	private readonly StringSegment _document;
	private readonly string? _name;
	private readonly string? _id;
	private readonly string? _className;
	private readonly ReadOnlySpan<KeyValuePair<string, string>> _attributes;
	private readonly bool _filtersAttributes;
	private readonly int _end;
	private int _position;
	private LazyHtmlElement _current;

	internal ElementsQueryEnumerator(StringSegment document, string? name, string? id, string? className, ReadOnlySpan<KeyValuePair<string, string>> attributes, int start, int end)
	{
		ElementQuery.ValidateArguments(name, id, className, attributes);

		_document = document;
		_name = name;
		_id = id;
		_className = className;
		_attributes = attributes;
		_filtersAttributes = id is not null || className is not null || !attributes.IsEmpty;
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
				&& (!_filtersAttributes || HasAttributes(index, index + 1 + nameLength, _position)))
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

	// Checks that the start tag at `elementStart`, whose attributes are in [start, end), has the required id, class
	// and every required attribute with the required value.
	private readonly bool HasAttributes(int elementStart, int start, int end)
	{
		if (_id is not null && !(TryFindAttribute(elementStart, start, end, "id", out var id) && id.ValueSpan.SequenceEqual(_id)))
			return false;

		if (_className is not null && !(TryFindAttribute(elementStart, start, end, "class", out var @class) && ElementQuery.HasClass(@class.ValueSpan, _className)))
			return false;

		foreach (var (name, value) in _attributes)
		{
			if (!TryFindAttribute(elementStart, start, end, name, out var attribute) || !attribute.ValueSpan.SequenceEqual(value))
				return false;
		}

		return true;
	}

	private readonly bool TryFindAttribute(int elementStart, int start, int end, ReadOnlySpan<char> name, out LazyHtmlAttribute attribute)
		=> new AttributesEnumerator(_document, elementStart, start, end).TryFind(name, out attribute);
}
