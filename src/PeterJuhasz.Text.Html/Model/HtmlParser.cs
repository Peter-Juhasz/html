using System.Collections.Immutable;
using System.Text.Html.Lazy;

namespace System.Text.Html.Model;

// Builds the model tree by walking the lazy layer; an element is created before its attributes and children so they can reference it.
internal static class HtmlParser
{
	public static HtmlDocument Parse(LazyHtmlDocument source)
	{
		var document = new HtmlDocument();
		document.Elements = ParseElements(document, parent: null, source.Elements(), depth: 0);
		return document;
	}

	private static ImmutableArray<HtmlElement> ParseElements(HtmlDocument document, HtmlElement? parent, ElementsEnumerator elements, int depth)
	{
		using var builder = new PooledArrayBuilder<HtmlElement>();
		foreach (var element in elements)
			builder.Add(ParseElement(document, parent, element, depth));
		return builder.ToImmutableArray();
	}

	private static HtmlElement ParseElement(HtmlDocument document, HtmlElement? parent, LazyHtmlElement source, int depth)
	{
		var element = new HtmlElement(document, parent, source);
		element.Attributes = ParseAttributes(element, source.Attributes());

		// the scanner does not descend deeper than this either, so the recursion stays bounded for pathological input
		element.Children = depth < HtmlScanner.MaxDepth ? ParseElements(document, element, source.Elements(), depth + 1) : [];
		return element;
	}

	private static ImmutableArray<HtmlAttribute> ParseAttributes(HtmlElement element, AttributesEnumerator attributes)
	{
		using var builder = new PooledArrayBuilder<HtmlAttribute>();
		foreach (var attribute in attributes)
			builder.Add(new HtmlAttribute(element, attribute.Name, attribute.Value));
		return builder.ToImmutableArray();
	}
}
