using Microsoft.Extensions.Primitives;
using System.Diagnostics.CodeAnalysis;

namespace PeterJuhasz.Text.Html.Lazy;

// Enumerates the elements matching an element name, classes and/or attributes at any depth inside a range of the document, in document order.
// A ref struct so the attributes can be kept as a span, letting callers pass them without allocating.
[PerformanceCritical]
public ref struct ElementsQueryEnumerator
{
	private readonly StringSegment _document;
	private readonly string? _element;
	private readonly StringValues _classNames;
	private readonly ReadOnlySpan<KeyValuePair<string, string>> _attributes;
	private readonly bool _filtersAttributes;
	private readonly int _end;
	private int _position;
	private LazyHtmlElement _current;

	internal ElementsQueryEnumerator(StringSegment document, string? element, StringValues classNames, ReadOnlySpan<KeyValuePair<string, string>> attributes, int start, int end)
	{
		ElementQuery.ValidateArguments(element, classNames, attributes);

		_document = document;
		_element = element;
		_classNames = classNames;
		_attributes = attributes;
		_filtersAttributes = classNames.Count > 0 || !attributes.IsEmpty;
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
			if ((_element is null || name.Equals(_element, StringComparison.OrdinalIgnoreCase))
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

	// Checks that the start tag at `elementStart`, whose attributes are in [start, end), has all the required classes
	// and every required attribute with the required (decoded) value.
	private readonly bool HasAttributes(int elementStart, int start, int end)
	{
		if (_classNames.Count > 0 && !(TryFindAttribute(elementStart, start, end, "class", out var @class) && ElementQuery.HasClasses(@class.ValueSpan, _classNames)))
			return false;

		foreach (var (name, value) in _attributes)
		{
			if (!TryFindAttribute(elementStart, start, end, name, out var attribute) || !ElementQuery.HasAttributeValue(attribute.ValueSpan, value))
				return false;
		}

		return true;
	}

	private readonly bool TryFindAttribute(int elementStart, int start, int end, ReadOnlySpan<char> name, out LazyHtmlAttribute attribute)
		=> new AttributesEnumerator(_document, elementStart, start, end).TryFind(name, out attribute);
}

public static partial class Extensions
{
	extension(ElementsQueryEnumerator enumerator)
	{
		public LazyHtmlElement[] ToArray()
		{
			using var builder = new PooledArrayBuilder<LazyHtmlElement>();
			foreach (var element in enumerator)
				builder.Add(element);
			return builder.ToArray();
		}
	}
}