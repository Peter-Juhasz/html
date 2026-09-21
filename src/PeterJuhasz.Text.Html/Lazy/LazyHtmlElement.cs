using Microsoft.Extensions.Primitives;
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

	public LazyHtmlElement(StringSegment document, int startIndex)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(startIndex);
		ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(startIndex, document.Length);

		var text = document.AsSpan();
		if (HtmlScanner.FindMarkup(text, startIndex, out var index) != MarkupKind.StartTag || index != startIndex)
			throw new ArgumentException("The index does not point to a start tag.", nameof(startIndex));

		_document = document;
		_start = startIndex;
		_end = HtmlScanner.ScanElement(text, startIndex, out _nameLength, out _contentStart, out _contentEnd);
	}

	// Index right after the element, used to continue enumeration.
	internal int End => _end;

	// Index right after the start tag, used to continue enumeration inside the element.
	internal int ContentStart => _contentStart;

	public ReadOnlySpan<char> NameSpan => _nameLength == 0 ? default : _document.AsSpan().Slice(_start + 1, _nameLength);

	public string Name => NameSpan.ToString();

	public ReadOnlySpan<char> OuterSpan => _document.AsSpan().Slice(_start, _end - _start);

	public ReadOnlySpan<char> InnerSpan => _document.AsSpan().Slice(_contentStart, _contentEnd - _contentStart);

	public AttributesEnumerator Attributes() => new(_document, _start + 1 + _nameLength, _contentStart);

	public bool TryGetAttribute(ReadOnlySpan<char> name, out LazyHtmlAttribute attribute)
	{
		foreach (var candidate in Attributes())
		{
			if (candidate.NameSpan.Equals(name, StringComparison.OrdinalIgnoreCase))
			{
				attribute = candidate;
				return true;
			}
		}

		attribute = default;
		return false;
	}

	public bool HasAttribute(ReadOnlySpan<char> name) => TryGetAttribute(name, out _);

	public ElementsEnumerator Elements() => SyntaxFacts.IsRawTextElement(NameSpan)
		? new(_document, _contentEnd, _contentEnd)
		: new(_document, _contentStart, _contentEnd);

	// Finds the elements with the given name at any depth inside this element, in document order.
	public ElementsByNameEnumerator QuerySelectorAll(string name) => SyntaxFacts.IsRawTextElement(NameSpan)
		? new(_document, name, _contentEnd, _contentEnd)
		: new(_document, name, _contentStart, _contentEnd);

	// Finds the first element with the given name at any depth inside this element.
	public bool TryQuerySelector(string name, out LazyHtmlElement element)
	{
		var elements = QuerySelectorAll(name);
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
