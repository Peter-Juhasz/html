using Microsoft.Extensions.Primitives;

namespace PeterJuhasz.Text.Html.Lazy;

public struct LazyHtmlElement(StringSegment document, int startIndex)
{
	public ReadOnlySpan<char> NameSpan { get; }
	public string Name { get; }
	public bool TryGetAttribute(string name, out LazyHtmlAttribute attribute);
	public bool HasAttribute(string name);
	public ElementsEnumerator Elements();
	public string TextContent { get; } // use StringBuilderPool
	public ReadOnlySpan<char> OuterSpan { get; }
	public ReadOnlySpan<char> InnerSpan { get; }
}
