using Microsoft.Extensions.Primitives;

namespace PeterJuhasz.Text.Html.Lazy;

public struct LazyHtmlDocument(StringSegment document)
{
	public ElementsEnumerator Elements() => throw new NotImplementedException();
}
