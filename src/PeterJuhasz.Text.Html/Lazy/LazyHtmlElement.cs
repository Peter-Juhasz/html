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

	public string Name => NameSpan.ToString();

	public ReadOnlySpan<char> OuterSpan => _document.AsSpan().Slice(_start, _end - _start);

	public ReadOnlySpan<char> InnerSpan => _document.AsSpan().Slice(_contentStart, _contentEnd - _contentStart);

	public AttributesEnumerator Attributes() => new(_document, _start, _start + 1 + _nameLength, _contentStart);

	public bool TryGetAttribute(ReadOnlySpan<char> name, out LazyHtmlAttribute attribute) => Attributes().TryFind(name, out attribute);

	public bool HasAttribute(ReadOnlySpan<char> name) => TryGetAttribute(name, out _);

	public ElementsEnumerator Elements() => SyntaxFacts.IsRawTextElement(NameSpan)
		? new(_document, _contentEnd, _contentEnd)
		: new(_document, _contentStart, _contentEnd);

	// Enumerates the elements, text and comments directly inside this element; raw text content is a single text node.
	public NodesEnumerator Nodes() => new(_document, _contentStart, _contentEnd, isRawText: SyntaxFacts.IsRawTextElement(NameSpan));

	// Finds the elements at any depth inside this element that have the given name (any name if null), id, class
	// and all of the given attributes with the given values, in document order.
	public ElementsQueryEnumerator QuerySelectorAll(string? name = null, string? id = null, string? className = null, ReadOnlySpan<KeyValuePair<string, string>> attributes = default)
		=> SyntaxFacts.IsRawTextElement(NameSpan)
			? new(_document, name, id, className, attributes, _contentEnd, _contentEnd)
			: new(_document, name, id, className, attributes, _contentStart, _contentEnd);

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
			var text = _document.AsSpan().Slice(0, _contentEnd);
			if (SyntaxFacts.IsRawTextElement(NameSpan) || !text.Slice(_contentStart).Contains(SyntaxFacts.OpenTag))
				return InnerSpan.ToString();

			using var pooled = StringBuilderPool.GetPooledObject(out var builder);
			var position = _contentStart;
			while (position < _contentEnd)
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