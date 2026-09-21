using Microsoft.Extensions.Primitives;

namespace System.Text.Html.Lazy;

public readonly struct LazyHtmlDocument(StringSegment document)
{
	public ElementsEnumerator Elements() => new(document, 0, document.Length);
}
