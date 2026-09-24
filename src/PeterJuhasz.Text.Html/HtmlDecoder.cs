using System.Buffers;
using System.Globalization;
using System.Net;
using System.Text;

namespace PeterJuhasz.Text.Html;

public static partial class HtmlDecoder
{
	public static string Decode(string str)
	{
		return WebUtility.HtmlDecode(str);
	}

	public static string Decode(ReadOnlySpan<char> span)
	{
		if (!SyntaxFacts.NeedsDecoding(span))
		{
			return new string(span);
		}

		if (span.Length <= StackAllocThreshold)
		{
			Span<char> buffer = stackalloc char[span.Length];
			Decode(span, buffer, out int charsWritten);
			return new(buffer[..charsWritten]);
		}
		else
		{
			using var array = ArrayPool<char>.Shared.GetPooledArray(span.Length);
			Decode(span, array, out int charsWritten);
			return new(array.Array.AsSpan(0, charsWritten));
		}
	}

	internal const int StackAllocThreshold = 1024;

	public static void Decode(ReadOnlySpan<char> span, StringBuilder builder)
	{
		if (!SyntaxFacts.NeedsDecoding(span))
		{
			builder.Append(span);
			return;
		}
		
		if (span.Length <= StackAllocThreshold)
		{
			Span<char> buffer = stackalloc char[span.Length];
			Decode(span, buffer, out int charsWritten);
			builder.Append(buffer[..charsWritten]);
			return;
		}
		else
		{
			using var array = ArrayPool<char>.Shared.GetPooledArray(span.Length);
			Decode(span, array, out int charsWritten);
			builder.Append(array.Array.AsSpan(0, charsWritten));
		}
	}

	// Decodes named (like `&amp;`) and numeric (like `&#60;` or `&#x3C;`) character references; unknown or invalid ones are copied as written.
	// Decoding never makes the text longer, so the output only has to be as long as the input, and it may even be the input itself.
	public static void Decode(ReadOnlySpan<char> input, Span<char> output, out int charsWritten)
	{
		if (output.Length < input.Length)
		{
			throw new ArgumentException("The output must be at least as long as the input.", nameof(output));
		}

		int written = 0;
		int literalStart = 0;
		int searchStart = 0;

		while (true)
		{
			int offset = input[searchStart..].IndexOf(SyntaxFacts.CharacterReferenceStart);
			if (offset < 0)
			{
				break;
			}

			// A reference ends at the next ';', unless another '&' comes first, which makes this '&' a literal one.
			int nameStart = searchStart + offset + 1;
			int nameLength = input[nameStart..].IndexOfAny(SyntaxFacts.CharacterReferenceNameTerminators);
			if (nameLength < 0)
			{
				break;
			}

			int nameEnd = nameStart + nameLength;
			if (input[nameEnd] == SyntaxFacts.CharacterReferenceStart)
			{
				searchStart = nameEnd;
				continue;
			}

			searchStart = nameEnd + 1;
			int decodedLength = DecodeReference(input[nameStart..nameEnd], out char first, out char second);
			if (decodedLength == 0)
			{
				continue;
			}

			var literal = input[literalStart..(nameStart - 1)];
			literal.CopyTo(output[written..]);
			written += literal.Length;
			output[written++] = first;
			if (decodedLength == 2)
			{
				output[written++] = second;
			}

			literalStart = searchStart;
		}

		var rest = input[literalStart..];
		rest.CopyTo(output[written..]);
		charsWritten = written + rest.Length;
	}

	// Decodes the text between the '&' and ';' of a character reference into one character, or two for a surrogate pair,
	// and returns how many; zero when the reference is unknown or invalid.
	private static int DecodeReference(ReadOnlySpan<char> reference, out char first, out char second)
	{
		second = '\0';

		if (reference.Length > 1 && reference[0] == SyntaxFacts.NumericCharacterReferenceStart)
		{
			// The # syntax can be in decimal or hex, e.g.
			//      &#229;  --> decimal
			//      &#xE5;  --> same char in hex
			// See http://www.w3.org/TR/REC-html40/charset.html#entities
			bool parsedSuccessfully = reference[1] is 'x' or 'X'
				? uint.TryParse(reference[2..], NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out uint parsedValue)
				: uint.TryParse(reference[1..], NumberStyles.Integer, CultureInfo.InvariantCulture, out parsedValue);

			if (!parsedSuccessfully || !UnicodeUtility.IsValidUnicodeScalar(parsedValue))
			{
				first = '\0';
				return 0;
			}

			if (UnicodeUtility.IsBmpCodePoint(parsedValue))
			{
				first = (char)parsedValue;
				return 1;
			}

			UnicodeUtility.GetUtf16SurrogatesFromSupplementaryPlaneScalar(parsedValue, out first, out second);
			return 2;
		}

		first = Lookup(reference);
		return first == '\0' ? 0 : 1;
	}
}