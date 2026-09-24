using Microsoft.Extensions.Primitives;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;

namespace PeterJuhasz.Text.Html;

// Parses a single compound selector into the filters of a structured query: an optional element name or '*', followed by any number of
// classes (.name), IDs (#name) and attributes with an exact value ([name=value], [name="value"] or [name='value']).
// Combinators, selector lists, pseudo-classes, other attribute matchers and escapes are not supported.
[PerformanceCritical]
internal static class SelectorParser
{
	// Every ASCII character other than letters, digits, '-' and '_'; non-ASCII characters are part of identifiers, as in CSS.
	private static readonly SearchValues<char> IdentifierTerminators = SearchValues.Create(CreateIdentifierTerminators());

	// The closing quote, or a character that is not supported inside a string: an escape or a newline.
	private static readonly SearchValues<char> DoubleQuotedValueTerminators = SearchValues.Create("\"\\\n\r\f");

	private static readonly SearchValues<char> SingleQuotedValueTerminators = SearchValues.Create("'\\\n\r\f");

	// Parses the selector of a query method, throwing if it is not a selector this parser supports.
	public static void ParseSelector(string selector, out ReadOnlySpan<char> element, out StringValues classNames, out KeyValuePair<string, string>[] attributes)
	{
		ArgumentNullException.ThrowIfNull(selector);

		if (!TryParseSelector(selector, out element, out classNames, out attributes))
		{
			throw new ArgumentException($"'{selector}' is not a supported selector. Only an element name or '*' followed by classes (.name), IDs (#name) and exact attribute values ([name=value]) are supported.", nameof(selector));
		}
	}

	// The element name is a slice of the selector, and it is empty when the selector does not restrict the element name.
	// An ID is returned as an exact match of the id attribute.
	public static bool TryParseSelector(ReadOnlySpan<char> selector, out ReadOnlySpan<char> element, out StringValues classNames, out KeyValuePair<string, string>[] attributes)
	{
		element = default;
		classNames = default;
		attributes = [];

		var rest = SkipWhitespace(selector);
		rest = rest[..(rest.LastIndexOfAnyExcept(SyntaxFacts.Whitespace) + 1)];
		if (rest.IsEmpty)
		{
			return false;
		}

		// the universal selector is the same as no element name
		var name = ReadOnlySpan<char>.Empty;
		if (rest[0] == '*')
		{
			rest = rest[1..];
		}
		else if (rest[0] is not ('.' or '#' or '[') && !TryReadIdentifier(ref rest, out name))
		{
			return false;
		}

		using var classes = new PooledArrayBuilder<string>();
		using var filters = new PooledArrayBuilder<KeyValuePair<string, string>>();
		while (!rest.IsEmpty)
		{
			var kind = rest[0];
			rest = rest[1..];
			switch (kind)
			{
				case '.' when TryReadIdentifier(ref rest, out var className):
					classes.Add(className.ToString());
					break;

				case '#' when TryReadIdentifier(ref rest, out var id):
					filters.Add(new("id", id.ToString()));
					break;

				case '[' when TryReadAttribute(ref rest, out var attribute):
					filters.Add(attribute);
					break;

				default:
					return false;
			}
		}

		element = name;
		classNames = classes.ToStringValues();
		attributes = filters.ToArray();
		return true;
	}

	// Reads an attribute selector after its '[': a name, '=', an identifier or quoted value and the ']', with optional whitespace between them.
	private static bool TryReadAttribute(scoped ref ReadOnlySpan<char> text, out KeyValuePair<string, string> attribute)
	{
		attribute = default;

		var rest = SkipWhitespace(text);
		if (!TryReadIdentifier(ref rest, out var name))
		{
			return false;
		}

		rest = SkipWhitespace(rest);
		if (rest is not [SyntaxFacts.EqualsSign, ..])
		{
			return false;
		}

		rest = SkipWhitespace(rest[1..]);
		ReadOnlySpan<char> value;
		if (rest is [(SyntaxFacts.DoubleQuote or SyntaxFacts.SingleQuote) and var quote, ..])
		{
			rest = rest[1..];
			var end = rest.IndexOfAny(quote == SyntaxFacts.DoubleQuote ? DoubleQuotedValueTerminators : SingleQuotedValueTerminators);
			if (end < 0 || rest[end] != quote)
			{
				return false;
			}

			value = rest[..end];
			rest = rest[(end + 1)..];
		}
		else if (!TryReadIdentifier(ref rest, out value))
		{
			return false;
		}

		rest = SkipWhitespace(rest);
		if (rest is not [']', ..])
		{
			return false;
		}

		text = rest[1..];
		attribute = new(SyntaxFacts.ToName(name), value.ToString());
		return true;
	}

	// Reads a CSS identifier, which must not start with a digit, or with a '-' followed by a digit or nothing.
	private static bool TryReadIdentifier(scoped ref ReadOnlySpan<char> text, out ReadOnlySpan<char> identifier)
	{
		var length = text.IndexOfAny(IdentifierTerminators);
		identifier = length < 0 ? text : text[..length];
		if (identifier is [] or ['-'] or [>= '0' and <= '9', ..] or ['-', >= '0' and <= '9', ..])
		{
			return false;
		}

		text = text[identifier.Length..];
		return true;
	}

	private static ReadOnlySpan<char> SkipWhitespace(ReadOnlySpan<char> text)
	{
		var start = text.IndexOfAnyExcept(SyntaxFacts.Whitespace);
		return start < 0 ? [] : text[start..];
	}

	private static char[] CreateIdentifierTerminators()
	{
		var terminators = new List<char>();
		for (var c = '\0'; c < '\x80'; c++)
		{
			if (!char.IsAsciiLetterOrDigit(c) && c is not ('-' or '_'))
			{
				terminators.Add(c);
			}
		}

		return [.. terminators];
	}
}
