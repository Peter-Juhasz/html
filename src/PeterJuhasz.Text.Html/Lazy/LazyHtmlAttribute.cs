using Microsoft.Extensions.Primitives;
using System.Diagnostics.CodeAnalysis;

namespace System.Text.Html.Lazy;

[PerformanceCritical]
public readonly struct LazyHtmlAttribute
{
	private readonly StringSegment _document;
	private readonly int _elementStart;
	private readonly int _start;
	private readonly int _nameLength;
	private readonly bool _hasValue;
	private readonly int _valueStart;
	private readonly int _valueLength;

	// `elementStartIndex` is the index of the start tag the attribute belongs to; it is only used to construct `Element`.
	public LazyHtmlAttribute(StringSegment document, int elementStartIndex, int startIndex)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(elementStartIndex);
		ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(startIndex, elementStartIndex);
		ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(startIndex, document.Length);

		var text = document.AsSpan();
		if (!HtmlScanner.IsStartTagAt(text, elementStartIndex))
			throw new ArgumentException("The index does not point to a start tag.", nameof(elementStartIndex));

		_document = document;
		_elementStart = elementStartIndex;
		_start = startIndex;
		End = HtmlScanner.ScanAttribute(text, startIndex, out _nameLength, out _hasValue, out _valueStart, out _valueLength);
	}

	// Index right after the attribute, used to continue enumeration.
	internal int End { get; }

	// The element the attribute belongs to; scanned on each access.
	public LazyHtmlElement Element => new(_document, _elementStart);

	public ReadOnlySpan<char> NameSpan => _document.AsSpan().Slice(_start, _nameLength);

	public string Name => NameSpan.ToString();

	public bool HasValue => _hasValue;

	// Value as written, without quotes; empty when the attribute has no value.
	public ReadOnlySpan<char> ValueSpan => _hasValue ? _document.AsSpan().Slice(_valueStart, _valueLength) : default;

	public bool TryGetValue(out ReadOnlySpan<char> valueSpan)
	{
		valueSpan = ValueSpan;
		return _hasValue;
	}

	public string? Value => _hasValue ? ValueSpan.ToString() : null;
}
