using Microsoft.Extensions.Primitives;
using System.Diagnostics.CodeAnalysis;

namespace System.Text.Html.Lazy;

[PerformanceCritical]
public readonly struct LazyHtmlDocument(StringSegment document)
{
	public ElementsEnumerator Elements() => new(document, 0, document.Length);
}
