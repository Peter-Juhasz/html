using Microsoft.Extensions.Primitives;
using System.Diagnostics.CodeAnalysis;

namespace PeterJuhasz.Text.Html.Lazy;

[PerformanceCritical]
public readonly struct LazyHtmlDocument(StringSegment document)
{
	public static LazyHtmlDocument Parse(string html) => Parse(new StringSegment(html));
	public static LazyHtmlDocument Parse(StringSegment html) => new(html);

	public ElementsEnumerator Elements() => new(document, 0, document.Length);

	// Enumerates the elements, text and comments at the top level of the document.
	public NodesEnumerator Nodes() => new(document, 0, document.Length);

	// Finds the elements at any depth in the document that have the given element name (any if empty), all of the given classes
	// and all of the given attributes with the given values, in document order.
	public ElementsQueryEnumerator QuerySelectorAll(ReadOnlySpan<char> element = default, StringValues classNames = default, ReadOnlySpan<KeyValuePair<string, string>> attributes = default)
		=> new(document, element, classNames, attributes, 0, document.Length);

	// Finds the first element at any depth in the document that has the given element name (any if empty), all of the given classes
	// and all of the given attributes with the given values.
	public bool TryQuerySelector(out LazyHtmlElement result, ReadOnlySpan<char> element = default, StringValues classNames = default, ReadOnlySpan<KeyValuePair<string, string>> attributes = default)
	{
		var elements = QuerySelectorAll(element: element, classNames: classNames, attributes: attributes);
		var found = elements.MoveNext();
		result = found ? elements.Current : default;
		return found;
	}
}

public static partial class Extensions
{
	extension(LazyHtmlDocument document)
	{
		public LazyHtmlElement? QuerySelector(ReadOnlySpan<char> element = default, StringValues classNames = default, ReadOnlySpan<KeyValuePair<string, string>> attributes = default)
		{
			if (document.TryQuerySelector(out var result, element: element, classNames: classNames, attributes: attributes))
			{
				return result;
			}

			return null;
		}

		public LazyHtmlElement? GetElementById(string id)
		{
			ArgumentException.ThrowIfNullOrEmpty(id);
			return QuerySelector(document, attributes: [new("id", id)]);
		}

		// Matches the decoded name attribute value case-sensitively, not the tag name.
		public ElementsQueryEnumerator GetElementsByName(string name)
		{
			ArgumentException.ThrowIfNullOrEmpty(name);
			// The returned enumerator retains the filter span, so it needs heap-backed storage.
			KeyValuePair<string, string>[] attributes = [new("name", name)];
			return document.QuerySelectorAll(attributes: attributes);
		}

		public ElementsQueryEnumerator GetElementsByTagName(string tagName)
		{
			ArgumentException.ThrowIfNullOrEmpty(tagName);
			return document.QuerySelectorAll(element: tagName);
		}

		public ElementsQueryEnumerator GetElementsByClassName(string className)
		{
			ArgumentException.ThrowIfNullOrEmpty(className);
			return document.QuerySelectorAll(classNames: className);
		}

		// Finds the elements that have all of the given classes.
		public ElementsQueryEnumerator GetElementsByClassName(StringValues classNames)
		{
			ElementQuery.ValidateClassNames(classNames);
			return document.QuerySelectorAll(classNames: classNames);
		}


		public bool TryGetHead(out LazyHtmlElement result) => document.TryQuerySelector(out result, element: "head");

		public bool TryGetBody(out LazyHtmlElement result) => document.TryQuerySelector(out result, element: "body");
	}
}