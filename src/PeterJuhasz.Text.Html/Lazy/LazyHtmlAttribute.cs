using Microsoft.Extensions.Primitives;

namespace PeterJuhasz.Text.Html.Lazy;

public struct LazyHtmlAttribute(StringSegment document, int startIndex)
{
	public ReadOnlySpan<char> NameSpan { get; }
	public string Name { get; }
	public ReadOnlySpan<char> ValueSpan { get; }
	public bool TryGetValue(out ReadOnlySpan<char> valueSpan);
	public bool HasValue { get; }
	public string? Value { get; }
}
