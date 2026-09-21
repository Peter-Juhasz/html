using Microsoft.Extensions.Primitives;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Text.Html.Lazy;

[PerformanceCritical]
public readonly struct LazyHtmlComment
{
	private readonly StringSegment _document;
	private readonly int _start;
	private readonly int _end;

	// The comment is the [start, end) range of the document, starting with "<!--"; the enumerator guarantees this.
	internal LazyHtmlComment(StringSegment document, int start, int end)
	{
		Debug.Assert(end <= document.Length && document.AsSpan().Slice(start, end - start).StartsWith(SyntaxFacts.CommentStart));

		_document = document;
		_start = start;
		_end = end;
	}

	// The whole comment, including the delimiters.
	public ReadOnlySpan<char> OuterSpan => _document.AsSpan().Slice(_start, _end - _start);

	// The content between the delimiters, as written; an unterminated comment runs to the end of the document.
	public ReadOnlySpan<char> TextSpan
	{
		get
		{
			var content = OuterSpan.Slice(SyntaxFacts.CommentStart.Length);
			return content.EndsWith(SyntaxFacts.CommentEnd) ? content.Slice(0, content.Length - SyntaxFacts.CommentEnd.Length) : content;
		}
	}

	public string Text => TextSpan.ToString();
}
