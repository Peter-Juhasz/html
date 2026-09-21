using Microsoft.Extensions.Primitives;
using System.Diagnostics.CodeAnalysis;

namespace System.Text.Html.Lazy;

[PerformanceCritical]
public readonly struct LazyHtmlDocument(StringSegment document)
{
	public ElementsEnumerator Elements() => new(document, 0, document.Length);

	// Finds the elements with the given name at any depth in the document, in document order.
	public ElementsByNameEnumerator QuerySelectorAll(string name) => new(document, name, 0, document.Length);

	// Finds the first element with the given name at any depth in the document.
	public bool TryQuerySelector(string name, out LazyHtmlElement element)
	{
		var elements = QuerySelectorAll(name);
		var found = elements.MoveNext();
		element = found ? elements.Current : default;
		return found;
	}
}
