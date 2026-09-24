using Microsoft.Extensions.Primitives;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using PeterJuhasz.Text.Html.Lazy;

namespace PeterJuhasz.Text.Html.Model;

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

	public IEnumerable<HtmlElement> Elements(string element) => HtmlElement.Elements(Nodes, element);

	// Enumerates the elements at any depth in the document, in document order.
	public IEnumerable<HtmlElement> Descendants() => HtmlElement.Descendants(Nodes);

	public IEnumerable<HtmlElement> Descendants(string element) => HtmlElement.Descendants(Nodes, element);

	// Finds the elements at any depth in the document that have the given element name (any if null), all of the given classes
	// and all of the given attributes with the given values, in document order.
	// Attribute memory is retained without copying; keep its storage valid until enumeration completes.
	public IEnumerable<HtmlElement> QuerySelectorAll(string? element = null, StringValues classNames = default, ReadOnlyMemory<KeyValuePair<string, string>> attributes = default)
		=> HtmlElement.Query(Nodes, element, classNames, attributes);

	// Finds the elements at any depth in the document that match a selector like "a.button[rel=next]", in document order.
	// Only an element name or '*' followed by classes, IDs and exact attribute values is supported.
	public IEnumerable<HtmlElement> QuerySelectorAll(string selector) => HtmlElement.Query(Nodes, selector);

	// Finds the first element at any depth in the document that has the given element name (any if null), all of the given classes
	// and all of the given attributes with the given values.
	public bool TryQuerySelector([NotNullWhen(true)] out HtmlElement? result, string? element = null, StringValues classNames = default, ReadOnlyMemory<KeyValuePair<string, string>> attributes = default)
	{
		result = QuerySelectorAll(element: element, classNames: classNames, attributes: attributes).FirstOrDefault();
		return result is not null;
	}
}

public static partial class Extensions
{
	extension(HtmlDocument document)
	{
		public HtmlElement? QuerySelector(string? element = null, StringValues classNames = default, ReadOnlyMemory<KeyValuePair<string, string>> attributes = default)
			=> document.TryQuerySelector(out var result, element: element, classNames: classNames, attributes: attributes) ? result : null;

		// Finds the first element at any depth in the document that matches a selector like "a.button[rel=next]".
		public HtmlElement? QuerySelector(string selector) => document.QuerySelectorAll(selector).FirstOrDefault();

		public bool TryGetElementById(string id, [NotNullWhen(true)] out HtmlElement? result)
		{
			ArgumentException.ThrowIfNullOrEmpty(id);
			KeyValuePair<string, string>[] attributes = [new("id", id)];
			return document.TryQuerySelector(out result, attributes: attributes);
		}

		public HtmlElement? GetElementById(string id) => document.TryGetElementById(id, out var result) ? result : null;

		// Matches the decoded name attribute value case-sensitively, not the tag name.
		public IEnumerable<HtmlElement> GetElementsByName(string name)
		{
			ArgumentException.ThrowIfNullOrEmpty(name);
			KeyValuePair<string, string>[] attributes = [new("name", name)];
			return document.QuerySelectorAll(attributes: attributes);
		}

		public IEnumerable<HtmlElement> GetElementsByTagName(string tagName)
		{
			ArgumentException.ThrowIfNullOrEmpty(tagName);
			return document.QuerySelectorAll(element: tagName);
		}

		public IEnumerable<HtmlElement> GetElementsByClassName(string className)
		{
			ArgumentException.ThrowIfNullOrEmpty(className);
			return document.QuerySelectorAll(classNames: className);
		}

		// Finds the elements that have all of the given classes.
		public IEnumerable<HtmlElement> GetElementsByClassName(StringValues classNames)
		{
			ElementQuery.ValidateClassNames(classNames);
			return document.QuerySelectorAll(classNames: classNames);
		}


		public bool TryGetHead([NotNullWhen(true)] out HtmlElement? result)
		{
			foreach (var rootNode in document.Nodes)
			{
				if (rootNode is not HtmlElement rootElement)
				{
					continue;
				}

				if (rootElement.Name.Equals("html", StringComparison.OrdinalIgnoreCase))
				{
					foreach (var insideHtmlNode in rootElement.Nodes)
					{
						if (insideHtmlNode is not HtmlElement insideHtmlElement)
						{
							continue;
						}

						if (insideHtmlElement.Name.Equals("head", StringComparison.OrdinalIgnoreCase))
						{
							result = insideHtmlElement;
							return true;
						}
					}
				}

				if (rootElement.Name.Equals("head", StringComparison.OrdinalIgnoreCase))
				{
					result = rootElement;
					return true;
				}
			}

			return document.TryQuerySelector(out result, element: "head");
		}

		public bool TryGetBody([NotNullWhen(true)] out HtmlElement? result)
		{
			foreach (var rootNode in document.Nodes)
			{
				if (rootNode is not HtmlElement rootElement)
				{
					continue;
				}

				if (rootElement.Name.Equals("html", StringComparison.OrdinalIgnoreCase))
				{
					foreach (var insideHtmlNode in rootElement.Nodes)
					{
						if (insideHtmlNode is not HtmlElement insideHtmlElement)
						{
							continue;
						}

						if (insideHtmlElement.Name.Equals("body", StringComparison.OrdinalIgnoreCase))
						{
							result = insideHtmlElement;
							return true;
						}
					}
				}

				if (rootElement.Name.Equals("body", StringComparison.OrdinalIgnoreCase))
				{
					result = rootElement;
					return true;
				}
			}

			return document.TryQuerySelector(out result, element: "body");
		}
	}
}
