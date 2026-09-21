using Microsoft.Extensions.Primitives;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace PeterJuhasz.Text.Html.Lazy;

[PerformanceCritical]
public readonly struct LazyHtmlText
{
	private readonly StringSegment _document;
	private readonly int _start;
	private readonly int _end;

	// The text is the [start, end) range of the document; the enumerator guarantees it is not empty.
	internal LazyHtmlText(StringSegment document, int start, int end)
	{
		Debug.Assert(start < end && end <= document.Length);

		_document = document;
		_start = start;
		_end = end;
	}

	// Text as written (character references are not decoded).
	public ReadOnlySpan<char> TextSpan => _document.AsSpan().Slice(_start, _end - _start);

	public string Text => TextSpan.ToString();
}
