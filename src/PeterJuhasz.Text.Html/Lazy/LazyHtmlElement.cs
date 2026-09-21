using Microsoft.Extensions.Primitives;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Text.Html.Lazy;

[PerformanceCritical]
public readonly struct LazyHtmlElement
{
	private readonly StringSegment _document;
	private readonly int _start;
	private readonly int _nameLength;
	private readonly int _contentStart;
	// Both are -1 until the content has been scanned, which is done when the end is first needed unless it was known on construction.
	private readonly int _contentEnd;
	private readonly int _end;
	private readonly ContentKind _contentKind;

	// `startIndex` must point to a start tag; the enumerators guarantee this so the check is only asserted.
	// Only the start tag is scanned here; the content is scanned when its end is first needed, on each access.
	internal LazyHtmlElement(StringSegment document, int startIndex)
	{
		var text = document.AsSpan();
		Debug.Assert(HtmlScanner.IsStartTagAt(text, startIndex));

		_document = document;
		_start = startIndex;
		_contentStart = HtmlScanner.ScanStartTag(text, startIndex, out _nameLength, out var isSelfClosing);
		_contentKind = HtmlScanner.GetContentKind(text.Slice(startIndex + 1, _nameLength), isSelfClosing);
		// an element without content ends right after its start tag
		_contentEnd = _end = _contentKind == ContentKind.None ? _contentStart : -1;
	}

	// For an element that has already been scanned as a whole, by an enumerator that needed its end to continue.
	internal LazyHtmlElement(StringSegment document, int startIndex, int nameLength, int contentStart, ContentKind contentKind, int contentEnd, int end)
	{
		_document = document;
		_start = startIndex;
		_nameLength = nameLength;
		_contentStart = contentStart;
		_contentKind = contentKind;
		_contentEnd = contentEnd;
		_end = end;
	}

	// Index right after the element, used to continue enumeration; the content is scanned on each access if it was not scanned on construction.
	internal int End => _end >= 0 ? _end : HtmlScanner.ScanContent(_document.AsSpan(), _start, _nameLength, _contentStart, _contentKind, out _);

	// Index right after the start tag, used to continue enumeration inside the element.
	internal int ContentStart => _contentStart;

	// Index right after the content; the content is scanned on each access if it was not scanned on construction.
	private int ContentEnd
	{
		get
		{
			if (_contentEnd >= 0)
				return _contentEnd;

			HtmlScanner.ScanContent(_document.AsSpan(), _start, _nameLength, _contentStart, _contentKind, out var contentEnd);
			return contentEnd;
		}
	}

	public ReadOnlySpan<char> NameSpan => _nameLength == 0 ? default : _document.AsSpan().Slice(_start + 1, _nameLength);

	public string Name => NameSpan.ToString();

	public ReadOnlySpan<char> OuterSpan => _document.AsSpan().Slice(_start, End - _start);

	public ReadOnlySpan<char> InnerSpan => _document.AsSpan().Slice(_contentStart, ContentEnd - _contentStart);

	public AttributesEnumerator Attributes() => new(_document, _start, _start + 1 + _nameLength, _contentStart);

	public bool TryGetAttribute(ReadOnlySpan<char> name, out LazyHtmlAttribute attribute) => Attributes().TryFind(name, out attribute);

	public bool HasAttribute(ReadOnlySpan<char> name) => TryGetAttribute(name, out _);

	// When the end of the content is not known yet, it is found while enumerating instead of scanning the content up front.
	public ElementsEnumerator Elements()
	{
		if (_contentKind != ContentKind.Elements)
			return default;

		return _contentEnd >= 0
			? new(_document, _contentStart, _contentEnd)
			: new(_document, _contentStart, _start, _nameLength);
	}

	// Finds the elements at any depth inside this element that have the given name (any name if null), id, class
	// and all of the given attributes with the given values, in document order.
	public ElementsQueryEnumerator QuerySelectorAll(string? name = null, string? id = null, string? className = null, ReadOnlySpan<KeyValuePair<string, string>> attributes = default)
		=> _contentKind == ContentKind.Elements
			? new(_document, name, id, className, attributes, _contentStart, ContentEnd)
			: new(_document, name, id, className, attributes, _contentStart, _contentStart);

	// Finds the first element at any depth inside this element that has the given name (any name if null), id, class
	// and all of the given attributes with the given values.
	public bool TryQuerySelector(out LazyHtmlElement element, string? name = null, string? id = null, string? className = null, ReadOnlySpan<KeyValuePair<string, string>> attributes = default)
	{
		var elements = QuerySelectorAll(name: name, id: id, className: className, attributes: attributes);
		var found = elements.MoveNext();
		element = found ? elements.Current : default;
		return found;
	}

	// Concatenated text of the content with all markup removed, as written (character references are not decoded).
	public string TextContent
	{
		get
		{
			var contentEnd = ContentEnd;
			var text = _document.AsSpan().Slice(0, contentEnd);
			var content = text.Slice(_contentStart);
			if (_contentKind != ContentKind.Elements || !content.Contains(SyntaxFacts.OpenTag))
				return content.ToString();

			using var pooled = StringBuilderPool.GetPooledObject(out var builder);
			var position = _contentStart;
			while (position < contentEnd)
			{
				var kind = HtmlScanner.FindMarkup(text, position, out var index);
				builder.Append(text[position..index]);
				switch (kind)
				{
					case MarkupKind.None:
						position = index;
						break;

					case MarkupKind.StartTag:
						position = HtmlScanner.ScanStartTag(text, index, out var nameLength, out var isSelfClosing);
						var name = text.Slice(index + 1, nameLength);
						if (!isSelfClosing && SyntaxFacts.IsRawTextElement(name))
						{
							var rawTextEnd = HtmlScanner.FindRawTextEnd(text, position, name);
							builder.Append(text[position..rawTextEnd]);
							position = HtmlScanner.SkipMarkup(text, rawTextEnd);
						}
						break;

					default:
						position = HtmlScanner.SkipMarkup(text, index);
						break;
				}
			}

			return builder.ToString();
		}
	}
}

public static partial class Extensions
{
	extension(LazyHtmlElement element)
	{
		public LazyHtmlElement? QuerySelector(string? name = null, string? id = null, string? className = null, ReadOnlySpan<KeyValuePair<string, string>> attributes = default)
		{
			if (element.TryQuerySelector(out var child, name: name, id: id, className: className, attributes: attributes))
			{
				return child;
			}

			return null;
		}

		public LazyHtmlElement? GetElementById(string id)
		{
			ArgumentException.ThrowIfNullOrEmpty(id);
			return QuerySelector(element, id: id);
		}

		public ElementsQueryEnumerator GetElementsByTagName(string tagName)
		{
			ArgumentException.ThrowIfNullOrEmpty(tagName);
			return element.QuerySelectorAll(name: tagName);
		}

		public ElementsQueryEnumerator GetElementsByClassName(string className)
		{
			ArgumentException.ThrowIfNullOrEmpty(className);
			return element.QuerySelectorAll(className: className);
		}

		public LazyHtmlAttribute? GetAttribute(string name)
		{
			return element.TryGetAttribute(name, out var attribute) ? attribute : null;
		}
	}
}