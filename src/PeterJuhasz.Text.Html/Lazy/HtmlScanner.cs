using System.Buffers;
using System.Diagnostics.CodeAnalysis;

namespace PeterJuhasz.Text.Html.Lazy;

internal enum MarkupKind
{
	None,
	StartTag,
	EndTag,
	// comment, declaration, processing instruction or bogus tag
	Other,
}

[PerformanceCritical]
internal static class HtmlScanner
{
	// Nesting deeper than this is not descended into, to keep pathological input from exhausting the stack.
	internal const int MaxDepth = 512;

	// Finds the next markup at or after `position`; a '<' that does not start markup is treated as text.
	public static MarkupKind FindMarkup(ReadOnlySpan<char> text, int position, out int index)
	{
		while (true)
		{
			var offset = text[position..].IndexOf(SyntaxFacts.OpenTag);
			if (offset < 0)
			{
				index = text.Length;
				return MarkupKind.None;
			}

			index = position + offset;
			var next = index + 1 < text.Length ? text[index + 1] : '\0';
			if (char.IsAsciiLetter(next))
				return MarkupKind.StartTag;

			if (next == SyntaxFacts.Slash && index + 2 < text.Length && char.IsAsciiLetter(text[index + 2]))
				return MarkupKind.EndTag;

			if (next is '!' or '?' or SyntaxFacts.Slash)
				return MarkupKind.Other;

			position = index + 1;
		}
	}

	// Checks whether a start tag begins exactly at `index`; the same as FindMarkup returning StartTag at that index, without searching.
	public static bool IsStartTagAt(ReadOnlySpan<char> text, int index)
		=> index + 1 < text.Length && text[index] == SyntaxFacts.OpenTag && char.IsAsciiLetter(text[index + 1]);

	// Returns the index right after the end tag, comment or declaration at `index`.
	public static int SkipMarkup(ReadOnlySpan<char> text, int index)
	{
		var rest = text[index..];
		if (rest.StartsWith(SyntaxFacts.CommentStart))
		{
			var end = rest[SyntaxFacts.CommentStart.Length..].IndexOf(SyntaxFacts.CommentEnd);
			return end < 0 ? text.Length : index + SyntaxFacts.CommentStart.Length + end + SyntaxFacts.CommentEnd.Length;
		}

		var close = rest.IndexOf(SyntaxFacts.CloseTag);
		return close < 0 ? text.Length : index + close + 1;
	}

	// Scans the start tag at `start` and returns the index right after its closing '>'.
	public static int ScanStartTag(ReadOnlySpan<char> text, int start, out int nameLength, out bool isSelfClosing)
	{
		var position = start + 1;
		nameLength = LengthUntil(text, position, SyntaxFacts.TagNameTerminators);
		position += nameLength;
		isSelfClosing = false;

		while (position < text.Length)
		{
			var c = text[position];
			if (c == SyntaxFacts.CloseTag)
				return position + 1;

			if (c == SyntaxFacts.Slash && position + 1 < text.Length && text[position + 1] == SyntaxFacts.CloseTag)
			{
				isSelfClosing = true;
				return position + 2;
			}

			if (c == SyntaxFacts.Slash || SyntaxFacts.Whitespace.Contains(c))
				position++;
			else
				position = ScanAttribute(text, position, out _, out _, out _, out _);
		}

		return text.Length;
	}

	// Scans the attribute at `start` and returns the index right after it; `hasValue` is false for a bare attribute name.
	public static int ScanAttribute(ReadOnlySpan<char> text, int start, out int nameLength, out bool hasValue, out int valueStart, out int valueLength)
	{
		nameLength = LengthUntil(text, start, SyntaxFacts.AttributeNameTerminators);
		var position = start + nameLength;
		if (position >= text.Length || text[position] != SyntaxFacts.EqualsSign)
		{
			hasValue = false;
			valueStart = position;
			valueLength = 0;
			return position;
		}

		hasValue = true;
		position++;
		if (position < text.Length && text[position] is SyntaxFacts.DoubleQuote or SyntaxFacts.SingleQuote)
		{
			var quote = text[position++];
			var end = text[position..].IndexOf(quote);
			if (end >= 0)
			{
				valueStart = position;
				valueLength = end;
				return position + end + 1;
			}
		}

		// unquoted, or quoted without a closing quote
		valueStart = position;
		valueLength = LengthUntil(text, position, SyntaxFacts.UnquotedValueTerminators);
		return position + valueLength;
	}

	// Scans the element at `start` and returns the index right after it.
	public static int ScanElement(ReadOnlySpan<char> text, int start, out int nameLength, out int contentStart, out int contentEnd, int depth = 0)
	{
		contentStart = ScanStartTag(text, start, out nameLength, out var isSelfClosing);
		var name = text.Slice(start + 1, nameLength);
		if (isSelfClosing || SyntaxFacts.IsVoidElement(name) || depth > MaxDepth)
		{
			contentEnd = contentStart;
			return contentStart;
		}

		if (SyntaxFacts.IsRawTextElement(name))
		{
			contentEnd = FindRawTextEnd(text, contentStart, name);
			return SkipMarkup(text, contentEnd);
		}

		var hasClosers = SyntaxFacts.TryGetImplicitClosers(name, out var closers);
		var position = contentStart;
		while (true)
		{
			var kind = FindMarkup(text, position, out var index);
			switch (kind)
			{
				case MarkupKind.None:
					contentEnd = text.Length;
					return text.Length;

				// a mismatched end tag implicitly closes this element and is left for an ancestor
				case MarkupKind.EndTag:
					contentEnd = index;
					return IsTagNameAt(text, index + 2, name) ? SkipMarkup(text, index) : index;

				case MarkupKind.StartTag when hasClosers && closers.Contains(TagNameAt(text, index + 1)):
					contentEnd = index;
					return index;

				case MarkupKind.StartTag:
					position = ScanElement(text, index, out _, out _, out _, depth + 1);
					break;

				default:
					position = SkipMarkup(text, index);
					break;
			}
		}
	}

	// Finds the '<' of the end tag that terminates the raw text content starting at `position`.
	public static int FindRawTextEnd(ReadOnlySpan<char> text, int position, ReadOnlySpan<char> name)
	{
		while (true)
		{
			var offset = text[position..].IndexOf(SyntaxFacts.EndTagStart);
			if (offset < 0)
				return text.Length;

			position += offset;
			if (IsTagNameAt(text, position + SyntaxFacts.EndTagStart.Length, name))
				return position;

			position += SyntaxFacts.EndTagStart.Length;
		}
	}

	// Checks whether the tag name at `position` is `name`, followed by a terminator or the end of the text.
	private static bool IsTagNameAt(ReadOnlySpan<char> text, int position, ReadOnlySpan<char> name)
	{
		var rest = text[position..];
		return rest.StartsWith(name, StringComparison.OrdinalIgnoreCase)
			&& (rest.Length == name.Length || SyntaxFacts.TagNameTerminators.Contains(rest[name.Length]));
	}

	private static ReadOnlySpan<char> TagNameAt(ReadOnlySpan<char> text, int position)
		=> text.Slice(position, LengthUntil(text, position, SyntaxFacts.TagNameTerminators));

	private static int LengthUntil(ReadOnlySpan<char> text, int position, SearchValues<char> terminators)
	{
		var length = text[position..].IndexOfAny(terminators);
		return length < 0 ? text.Length - position : length;
	}
}
