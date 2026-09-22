using System.Net;
using System.Text;

namespace PeterJuhasz.Text.Html;

public static class HtmlDecoder
{
	public static string HtmlDecode(ReadOnlySpan<char> span)
	{
		if (!SyntaxFacts.NeedsDecoding(span))
		{
			return new string(span);
		}

		return Decode(span);
	}

	public static void HtmlDecode(ReadOnlySpan<char> span, StringBuilder builder)
	{
		if (!SyntaxFacts.NeedsDecoding(span))
		{
			builder.Append(span);
			return;
		}
		
		builder.Append(Decode(span));
	}

	// Decodes unconditionally; callers are expected to check `SyntaxFacts.NeedsDecoding` first to skip the copy.
	internal static string Decode(ReadOnlySpan<char> span)
	{
		return WebUtility.HtmlDecode(new string(span));
	}
}