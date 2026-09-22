using Microsoft.Extensions.Primitives;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace PeterJuhasz.Text.Html.Lazy;

[PerformanceCritical]
public readonly struct LazyHtmlElement
{
	private readonly StringSegment _document;
	private readonly int _start;
	private readonly int _nameLength;
	private readonly int _contentStart;
	private readonly int _contentEnd;
	private readonly int _end;

	// `startIndex` must point to a start tag; the enumerators guarantee this so the checks are only asserted.
	internal LazyHtmlElement(StringSegment document, int startIndex)
	{
		var text = document.AsSpan();
		Debug.Assert(HtmlScanner.IsStartTagAt(text, startIndex));

		_document = document;
		_start = startIndex;
		_end = HtmlScanner.ScanElement(text, startIndex, out _nameLength, out _contentStart, out _contentEnd);
	}

	// Rebuilds an element from an earlier scan, so a node does not have to scan it again.
	internal LazyHtmlElement(StringSegment document, int start, int nameLength, int contentStart, int contentEnd, int end)
	{
		_document = document;
		_start = start;
		_nameLength = nameLength;
		_contentStart = contentStart;
		_contentEnd = contentEnd;
		_end = end;
	}

	internal StringSegment Document => _document;

	internal int Start => _start;

	internal int NameLength => _nameLength;

	// Index right after the element, used to continue enumeration.
	internal int End => _end;

	// Index right after the start tag, used to continue enumeration inside the element.
	internal int ContentStart => _contentStart;

	// Index right after the content, at the end tag or at whatever implicitly closed the element.
	internal int ContentEnd => _contentEnd;

	public ReadOnlySpan<char> NameSpan => _nameLength == 0 ? default : _document.AsSpan().Slice(_start + 1, _nameLength);

	public string Name => SyntaxFacts.ToName(NameSpan);

	public ReadOnlySpan<char> OuterSpan => _document.AsSpan().Slice(_start, _end - _start);

	public ReadOnlySpan<char> InnerSpan => _document.AsSpan().Slice(_contentStart, _contentEnd - _contentStart);

	public AttributesEnumerator Attributes() => new(_document, _start, _start + 1 + _nameLength, _contentStart);

	public bool TryGetAttribute(ReadOnlySpan<char> name, out LazyHtmlAttribute attribute) => Attributes().TryFind(name, out attribute);

	public bool HasAttribute(ReadOnlySpan<char> name) => TryGetAttribute(name, out _);

	public ElementsEnumerator Elements() => SyntaxFacts.IsRawTextElement(NameSpan)
		? new(_document, _contentEnd, _contentEnd)
		: new(_document, _contentStart, _contentEnd);

	// Enumerates the elements, text and comments directly inside this element; raw text content is a single text node.
	public NodesEnumerator Nodes()
	{
		var isRawText = SyntaxFacts.IsRawTextElement(NameSpan);
		return new(_document, _contentStart, _contentEnd, isRawText, isLiteral: isRawText && !SyntaxFacts.IsEscapableRawTextElement(NameSpan));
	}

	// Finds the elements at any depth inside this element that have the given element name (any if null), all of the given classes
	// and all of the given attributes with the given values, in document order.
	public ElementsQueryEnumerator QuerySelectorAll(string? element = null, StringValues classNames = default, ReadOnlySpan<KeyValuePair<string, string>> attributes = default)
		=> SyntaxFacts.IsRawTextElement(NameSpan)
			? new(_document, element, classNames, attributes, _contentEnd, _contentEnd)
			: new(_document, element, classNames, attributes, _contentStart, _contentEnd);

	// Finds the first element at any depth inside this element that has the given element name (any if null), all of the given classes
	// and all of the given attributes with the given values.
	public bool TryQuerySelector(out LazyHtmlElement result, string? element = null, StringValues classNames = default, ReadOnlySpan<KeyValuePair<string, string>> attributes = default)
	{
		var elements = QuerySelectorAll(element: element, classNames: classNames, attributes: attributes);
		var found = elements.MoveNext();
		result = found ? elements.Current : default;
		return found;
	}

	// Concatenated text of the content with all markup removed and character references decoded,
	// except for the content of script and style, which is taken literally.
	public string TextContent
	{
		get
		{
			var text = _document.AsSpan().Slice(0, _contentEnd);
			if (SyntaxFacts.IsRawTextElement(NameSpan))
				return SyntaxFacts.IsEscapableRawTextElement(NameSpan) ? HtmlDecoder.HtmlDecode(InnerSpan) : InnerSpan.ToString();

			if (!text.Slice(_contentStart).Contains(SyntaxFacts.OpenTag))
				return HtmlDecoder.HtmlDecode(InnerSpan);

			using var pooled = StringBuilderPool.GetPooledObject(out var builder);
			var position = _contentStart;
			while (position < _contentEnd)
			{
				var kind = HtmlScanner.FindMarkup(text, position, out var index);
				HtmlDecoder.HtmlDecode(text[position..index], builder);
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
							if (SyntaxFacts.IsEscapableRawTextElement(name))
								HtmlDecoder.HtmlDecode(text[position..rawTextEnd], builder);
							else
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
	extension(LazyHtmlElement source)
	{
		public LazyHtmlElement? QuerySelector(string? element = null, StringValues classNames = default, ReadOnlySpan<KeyValuePair<string, string>> attributes = default)
		{
			if (source.TryQuerySelector(out var child, element: element, classNames: classNames, attributes: attributes))
			{
				return child;
			}

			return null;
		}

		public LazyHtmlElement? GetElementById(string id)
		{
			ArgumentException.ThrowIfNullOrEmpty(id);
			return QuerySelector(source, attributes: [new("id", id)]);
		}

		// Matches the raw name attribute value case-sensitively, not the tag name.
		public ElementsQueryEnumerator GetElementsByName(string name)
		{
			ArgumentException.ThrowIfNullOrEmpty(name);
			// The returned enumerator retains the filter span, so it needs heap-backed storage.
			KeyValuePair<string, string>[] attributes = [new("name", name)];
			return source.QuerySelectorAll(attributes: attributes);
		}

		public ElementsQueryEnumerator GetElementsByTagName(string tagName)
		{
			ArgumentException.ThrowIfNullOrEmpty(tagName);
			return source.QuerySelectorAll(element: tagName);
		}

		public ElementsQueryEnumerator GetElementsByClassName(string className)
		{
			ArgumentException.ThrowIfNullOrEmpty(className);
			return source.QuerySelectorAll(classNames: className);
		}

		// Finds the elements that have all of the given classes.
		public ElementsQueryEnumerator GetElementsByClassName(StringValues classNames)
		{
			ElementQuery.ValidateClassNames(classNames);
			return source.QuerySelectorAll(classNames: classNames);
		}

		public LazyHtmlAttribute? GetAttribute(string name)
		{
			return source.TryGetAttribute(name, out var attribute) ? attribute : null;
		}
	}
}