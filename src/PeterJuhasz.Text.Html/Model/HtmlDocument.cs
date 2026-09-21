using Microsoft.Extensions.Primitives;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text.Html.Lazy;

namespace System.Text.Html.Model;

public sealed class HtmlDocument
{
	// Nodes reference the document, so they are set by the parser after construction.
	internal HtmlDocument()
	{
	}

	public static HtmlDocument Parse(string html) => Parse(new StringSegment(html));

	public static HtmlDocument Parse(StringSegment html) => Parse(LazyHtmlDocument.Parse(html));

	// Materializes the whole tree of a lazily parsed document.
	public static HtmlDocument Parse(LazyHtmlDocument html) => HtmlParser.Parse(html);

	// The elements, text and comments at the top level of the document, in document order.
	public ImmutableArray<HtmlNode> Nodes { get; internal set; }

	// The elements at the top level of the document, in document order.
	public IEnumerable<HtmlElement> Elements() => HtmlElement.Elements(Nodes);

	// Enumerates the elements at any depth in the document, in document order.
	public IEnumerable<HtmlElement> Descendants() => HtmlElement.Descendants(Nodes);

	// Finds the elements at any depth in the document that have the given name (any name if null), id, class
	// and all of the given attributes with the given values, in document order.
	public IEnumerable<HtmlElement> QuerySelectorAll(string? name = null, string? id = null, string? className = null, ReadOnlySpan<KeyValuePair<string, string>> attributes = default)
		=> HtmlElement.Query(Nodes, name, id, className, attributes);

	// Finds the first element at any depth in the document that has the given name (any name if null), id, class
	// and all of the given attributes with the given values.
	public bool TryQuerySelector([NotNullWhen(true)] out HtmlElement? element, string? name = null, string? id = null, string? className = null, ReadOnlySpan<KeyValuePair<string, string>> attributes = default)
	{
		element = QuerySelectorAll(name: name, id: id, className: className, attributes: attributes).FirstOrDefault();
		return element is not null;
	}
}

public static partial class Extensions
{
	extension(HtmlDocument document)
	{
		public HtmlElement? QuerySelector(string? name = null, string? id = null, string? className = null, ReadOnlySpan<KeyValuePair<string, string>> attributes = default)
			=> document.TryQuerySelector(out var element, name: name, id: id, className: className, attributes: attributes) ? element : null;

		public HtmlElement? GetElementById(string id)
		{
			ArgumentException.ThrowIfNullOrEmpty(id);
			return QuerySelector(document, id: id);
		}

		public IEnumerable<HtmlElement> GetElementsByTagName(string tagName)
		{
			ArgumentException.ThrowIfNullOrEmpty(tagName);
			return document.QuerySelectorAll(name: tagName);
		}

		public IEnumerable<HtmlElement> GetElementsByClassName(string className)
		{
			ArgumentException.ThrowIfNullOrEmpty(className);
			return document.QuerySelectorAll(className: className);
		}
	}
}
