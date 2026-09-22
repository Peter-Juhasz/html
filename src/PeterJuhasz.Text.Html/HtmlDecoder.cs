using System.Net;
using System.Text;

namespace PeterJuhasz.Text.Html;

public static class HtmlDecoder
{
	public static string HtmlDecode(ReadOnlySpan<char> span)
	{
		if (!span.Contains('&'))
		{
			return new string(span);
		}

		return WebUtility.HtmlDecode(new string(span));
	}

	public static void HtmlDecode(ReadOnlySpan<char> span, StringBuilder builder)
	{
		if (!span.Contains('&'))
		{
			builder.Append(span);
			return;
		}
		
		builder.Append(WebUtility.HtmlDecode(new string(span)));
	}
}