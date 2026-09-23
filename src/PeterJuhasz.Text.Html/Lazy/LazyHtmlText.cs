using Microsoft.Extensions.Primitives;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace PeterJuhasz.Text.Html.Lazy;

[PerformanceCritical]
public readonly struct LazyHtmlText
{
	private readonly StringSegment _document;
	private readonly int _start;
	private readonly int _end;
	private readonly bool _isLiteral;

	// The text is the [start, end) range of the document; the enumerator guarantees it is not empty.
	// `isLiteral` is true for the content of script and style, which is taken literally.
	internal LazyHtmlText(StringSegment document, int start, int end, bool isLiteral = false)
	{
		Debug.Assert(start < end && end <= document.Length);

		_document = document;
		_start = start;
		_end = end;
		_isLiteral = isLiteral;
	}

	// Text as written (character references are not decoded).
	public ReadOnlySpan<char> TextSpan => _document.AsSpan()[_start.._end];

	// Text with character references decoded, except for the content of script and style, which is taken literally.
	public string Text => _isLiteral ? TextSpan.ToString() : HtmlDecoder.HtmlDecode(TextSpan);

	// Appends the same text as `Text` without creating a string for it.
	internal void AppendTo(StringBuilder builder)
	{
		if (_isLiteral)
			builder.Append(TextSpan);
		else
			HtmlDecoder.HtmlDecode(TextSpan, builder);
	}

	public bool IsLiteral => _isLiteral;
}
