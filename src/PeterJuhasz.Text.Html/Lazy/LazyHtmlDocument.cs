using Microsoft.Extensions.Primitives;
using System.Diagnostics.CodeAnalysis;

namespace System.Text.Html.Lazy;

[PerformanceCritical]
public readonly struct LazyHtmlDocument(StringSegment document)
{
	public ElementsEnumerator Elements() => new(document, 0, document.Length);

	// Finds the elements at any depth in the document that have the given name (any name if null), id, class
	// and all of the given attributes with the given values, in document order.
	public ElementsQueryEnumerator QuerySelectorAll(string? name = null, string? id = null, string? className = null, ReadOnlySpan<KeyValuePair<string, string>> attributes = default)
		=> new(document, name, id, className, attributes, 0, document.Length);

	// Finds the first element at any depth in the document that has the given name (any name if null), id, class
	// and all of the given attributes with the given values.
	public bool TryQuerySelector(out LazyHtmlElement element, string? name = null, string? id = null, string? className = null, ReadOnlySpan<KeyValuePair<string, string>> attributes = default)
	{
		var elements = QuerySelectorAll(name: name, id: id, className: className, attributes: attributes);
		var found = elements.MoveNext();
		element = found ? elements.Current : default;
		return found;
	}
}
