using System.Collections.Immutable;
using System.Text.Html.Lazy;

namespace System.Text.Html.Model;

// Builds the model tree by walking the lazy layer; a node is created before its attributes and children so they can reference it.
internal static class HtmlParser
{
	public static HtmlDocument Parse(LazyHtmlDocument source)
	{
		var document = new HtmlDocument();
		document.Nodes = ParseNodes(document, parent: null, source.Nodes(), depth: 0);
		return document;
	}

	private static ImmutableArray<HtmlNode> ParseNodes(HtmlDocument document, HtmlElement? parent, NodesEnumerator nodes, int depth)
	{
		using var builder = new PooledArrayBuilder<HtmlNode>();
		foreach (var node in nodes)
		{
			builder.Add(node.Kind switch
			{
				LazyHtmlNodeKind.Element => ParseElement(document, parent, node.Element, depth),
				LazyHtmlNodeKind.Text => new HtmlText(document, parent, node.Text),
				LazyHtmlNodeKind.Comment => new HtmlComment(document, parent, node.Comment),
				_ => throw new InvalidOperationException($"Unexpected node kind '{node.Kind}'."),
			});
		}

		return builder.ToImmutableArray();
	}

	private static HtmlElement ParseElement(HtmlDocument document, HtmlElement? parent, LazyHtmlElement source, int depth)
	{
		var element = new HtmlElement(document, parent, source);
		element.Attributes = ParseAttributes(element, source.Attributes());

		// the scanner does not descend deeper than this either, so the recursion stays bounded for pathological input
		element.Nodes = depth < HtmlScanner.MaxDepth ? ParseNodes(document, element, source.Nodes(), depth + 1) : [];
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
