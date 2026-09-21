using Microsoft.Extensions.Primitives;
using System.Diagnostics.CodeAnalysis;

namespace System.Text.Html.Lazy;

[PerformanceCritical]
public readonly struct LazyHtmlDocument(StringSegment document)
{
	public static LazyHtmlDocument Parse(string html) => Parse(new StringSegment(html));
	public static LazyHtmlDocument Parse(StringSegment html) => new(html);

	public ElementsEnumerator Elements() => new(document);

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

public static partial class Extensions
{
	extension(LazyHtmlDocument document)
	{
		public LazyHtmlElement? QuerySelector(string? name = null, string? id = null, string? className = null, ReadOnlySpan<KeyValuePair<string, string>> attributes = default)
		{
			if (document.TryQuerySelector(out var element, name: name, id: id, className: className, attributes: attributes))
			{
				return element;
			}

			return null;
		}

		public LazyHtmlElement? GetElementById(string id)
		{
			ArgumentException.ThrowIfNullOrEmpty(id);
			return QuerySelector(document, id: id);
		}

		public ElementsQueryEnumerator GetElementsByTagName(string tagName)
		{
			ArgumentException.ThrowIfNullOrEmpty(tagName);
			return document.QuerySelectorAll(name: tagName);
		}

		public ElementsQueryEnumerator GetElementsByClassName(string className)
		{
			ArgumentException.ThrowIfNullOrEmpty(className);
			return document.QuerySelectorAll(className: className);
		}
	}
}