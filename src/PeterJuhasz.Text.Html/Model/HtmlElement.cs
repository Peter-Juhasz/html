using Microsoft.Extensions.Primitives;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using PeterJuhasz.Text.Html.Lazy;

namespace PeterJuhasz.Text.Html.Model;

public sealed class HtmlElement : HtmlNode
{
	// The element in the source text; spans and text are read from it, so the tree keeps the source alive.
	private readonly LazyHtmlElement _source;

	// The text content is created on first access and kept for the next ones.
	private string? _textContent;

	// Attributes and nodes reference the element, so they are set by the parser after construction.
	internal HtmlElement(HtmlDocument document, HtmlElement? parent, LazyHtmlElement source)
		: base(document, parent)
	{
		Name = source.Name;
		_source = source;
	}

	public string Name { get; }

	public ImmutableArray<HtmlAttribute> Attributes { get; internal set; }

	// The elements, text and comments directly inside this element, in document order.
	public ImmutableArray<HtmlNode> Nodes { get; internal set; }

	// The elements directly inside this element, in document order.
	public IEnumerable<HtmlElement> Elements() => Elements(Nodes);

	public override ReadOnlySpan<char> OuterSpan => _source.OuterSpan;

	public ReadOnlySpan<char> InnerSpan => _source.InnerSpan;

	// Concatenated text of the content with all markup removed and character references decoded,
	// except for the content of script and style, which is taken literally.
	public string TextContent => _textContent ??= CreateTextContent();

	// Walks the tree instead of scanning the source again; the result is the same as the lazy layer gives.
	private string CreateTextContent()
	{
		// a single text child is the common case for leaves; its decoded text is shared instead of copied
		if (Nodes is [HtmlText text])
		{
			return text.Text;
		}

		if (Nodes.IsEmpty)
		{
			return IsTruncated ? _source.TextContent : string.Empty;
		}

		using var pooled = StringBuilderPool.GetPooledObject(out var builder);
		AppendTextContent(builder);
		return builder.ToString();
	}

	// Appends the text of the descendants in document order; the recursion is bounded by the depth the parser descends to.
	private void AppendTextContent(StringBuilder builder)
	{
		foreach (var node in Nodes)
		{
			switch (node)
			{
				case HtmlText text:
					text.AppendTo(builder);
					break;

				case HtmlElement { _textContent: { } textContent }:
					builder.Append(textContent);
					break;

				case HtmlElement { IsTruncated: true } element:
					builder.Append(element._source.TextContent);
					break;

				case HtmlElement element:
					element.AppendTextContent(builder);
					break;
			}
		}
	}

	// Content the parser did not descend into (nesting deeper than it follows) is only available in the source.
	// Content that is made of skipped markup only looks the same, but that has no text either way.
	private bool IsTruncated => Nodes.IsEmpty && !InnerSpan.IsEmpty;

	// Finds the first attribute with the given name, compared case-insensitively.
	public bool TryGetAttribute(ReadOnlySpan<char> name, [NotNullWhen(true)] out HtmlAttribute? attribute)
	{
		foreach (var candidate in Attributes)
		{
			if (candidate.Name.AsSpan().Equals(name, StringComparison.OrdinalIgnoreCase))
			{
				attribute = candidate;
				return true;
			}
		}

		attribute = null;
		return false;
	}

	public bool HasAttribute(ReadOnlySpan<char> name) => TryGetAttribute(name, out _);

	// Enumerates the elements at any depth inside this element, in document order.
	public IEnumerable<HtmlElement> Descendants() => Descendants(Nodes);

	// Finds the elements at any depth inside this element that have the given element name (any if null), all of the given classes
	// and all of the given attributes with the given values, in document order.
	// Attribute memory is retained without copying; keep its storage valid until enumeration completes.
	public IEnumerable<HtmlElement> QuerySelectorAll(string? element = null, StringValues classNames = default, ReadOnlyMemory<KeyValuePair<string, string>> attributes = default)
		=> Query(Nodes, element, classNames, attributes);

	// Finds the elements at any depth inside this element that match a selector like "a.button[rel=next]", in document order.
	// Only an element name or '*' followed by classes, IDs and exact attribute values is supported.
	public IEnumerable<HtmlElement> QuerySelectorAll(string selector) => Query(Nodes, selector);

	// Finds the first element at any depth inside this element that has the given element name (any if null), all of the given classes
	// and all of the given attributes with the given values.
	public bool TryQuerySelector([NotNullWhen(true)] out HtmlElement? result, string? element = null, StringValues classNames = default, ReadOnlyMemory<KeyValuePair<string, string>> attributes = default)
	{
		result = QuerySelectorAll(element: element, classNames: classNames, attributes: attributes).FirstOrDefault();
		return result is not null;
	}

	// Enumerates the elements among the nodes, in order.
	internal static IEnumerable<HtmlElement> Elements(ImmutableArray<HtmlNode> nodes)
	{
		foreach (var node in nodes)
		{
			if (node is HtmlElement element)
			{
				yield return element;
			}
		}
	}

	// Enumerates the elements among the nodes and all of their descendants in document order; iterative, so the depth of the tree does not matter.
	internal static IEnumerable<HtmlElement> Descendants(ImmutableArray<HtmlNode> nodes)
	{
		var pending = new Stack<HtmlElement>();
		PushReversed(pending, nodes);
		while (pending.TryPop(out var element))
		{
			yield return element;
			PushReversed(pending, element.Nodes);
		}

		static void PushReversed(Stack<HtmlElement> pending, ImmutableArray<HtmlNode> nodes)
		{
			for (var i = nodes.Length - 1; i >= 0; i--)
			{
				if (nodes[i] is HtmlElement element)
				{
					pending.Push(element);
				}
			}
		}
	}

	// Arguments are validated eagerly; the attribute memory is retained for deferred enumeration.
	internal static IEnumerable<HtmlElement> Query(ImmutableArray<HtmlNode> nodes, string? element, StringValues classNames, ReadOnlyMemory<KeyValuePair<string, string>> attributes)
	{
		ElementQuery.ValidateArguments(element, classNames, attributes.Span);
		return Descendants(nodes).Where(candidate => candidate.Matches(element, classNames, attributes));
	}

	// The selector is parsed eagerly, so an unsupported one throws before enumeration.
	internal static IEnumerable<HtmlElement> Query(ImmutableArray<HtmlNode> nodes, string selector)
	{
		SelectorParser.ParseSelector(selector, out var element, out var classNames, out var attributes);
		return Query(nodes, element.IsEmpty ? null : SyntaxFacts.ToName(element), classNames, attributes);
	}

	// Same rules as the lazy layer: names are case-insensitive, attribute values and class names are compared case-sensitively
	// against the decoded values, and an attribute without a value matches "".
	private bool Matches(string? element, StringValues classNames, ReadOnlyMemory<KeyValuePair<string, string>> attributes)
	{
		if (element is not null && !string.Equals(Name, element, StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		if (classNames.Count > 0 && !(TryGetAttribute("class", out var classAttribute) && ElementQuery.HasClasses(classAttribute.ValueSpan, classNames)))
		{
			return false;
		}

		foreach (var (attributeName, value) in attributes.Span)
		{
			if (!TryGetAttribute(attributeName, out var attribute) || !ElementQuery.HasAttributeValue(attribute.ValueSpan, value))
			{
				return false;
			}
		}

		return true;
	}

	public StringValues Classes()
	{
		if (!TryGetAttribute("class", out var attribute))
		{
			return StringValues.Empty;
		}

		if (!attribute.HasValue)
		{
			return StringValues.Empty;
		}

		var valueSpan = attribute.ValueSpan.Trim();
		if (valueSpan.IsEmpty)
		{
			return StringValues.Empty;
		}

		var separator = valueSpan.IndexOfAny(SyntaxFacts.Whitespace);
		if (separator < 0)
		{
			return attribute._value is { } value && value.Length == valueSpan.Length ? value : SyntaxFacts.DecodeIfNeeded(valueSpan.Trim()).ToString();
		}

		using var builder = new PooledArrayBuilder<string>();
		builder.Add(valueSpan[..separator].ToString());

		var rest = valueSpan[(separator + 1)..];
		foreach (var range in rest.SplitAny(SyntaxFacts.Whitespace))
		{
			var classSpan = rest[range];
			if (classSpan.IsEmpty)
			{
				continue;
			}

			builder.Add(SyntaxFacts.DecodeIfNeeded(classSpan).ToString());
		}

		return builder.ToStringValues();
	}
}

public static partial class Extensions
{
	extension(HtmlElement source)
	{
		public HtmlElement? QuerySelector(string? element = null, StringValues classNames = default, ReadOnlyMemory<KeyValuePair<string, string>> attributes = default)
			=> source.TryQuerySelector(out var child, element: element, classNames: classNames, attributes: attributes) ? child : null;

		// Finds the first element at any depth inside this element that matches a selector like "a.button[rel=next]".
		public HtmlElement? QuerySelector(string selector) => source.QuerySelectorAll(selector).FirstOrDefault();

		public bool TryGetElementById(string id, [NotNullWhen(true)] out HtmlElement? result)
		{
			ArgumentException.ThrowIfNullOrEmpty(id);
			KeyValuePair<string, string>[] attributes = [new("id", id)];
			return source.TryQuerySelector(out result, attributes: attributes);
		}

		public HtmlElement? GetElementById(string id) => source.TryGetElementById(id, out var result) ? result : null;

		// Matches the decoded name attribute value case-sensitively, not the tag name.
		public IEnumerable<HtmlElement> GetElementsByName(string name)
		{
			ArgumentException.ThrowIfNullOrEmpty(name);
			KeyValuePair<string, string>[] attributes = [new("name", name)];
			return source.QuerySelectorAll(attributes: attributes);
		}

		public IEnumerable<HtmlElement> GetElementsByTagName(string tagName)
		{
			ArgumentException.ThrowIfNullOrEmpty(tagName);
			return source.QuerySelectorAll(element: tagName);
		}

		public IEnumerable<HtmlElement> GetElementsByClassName(string className)
		{
			ArgumentException.ThrowIfNullOrEmpty(className);
			return source.QuerySelectorAll(classNames: className);
		}

		// Finds the elements that have all of the given classes.
		public IEnumerable<HtmlElement> GetElementsByClassName(StringValues classNames)
		{
			ElementQuery.ValidateClassNames(classNames);
			return source.QuerySelectorAll(classNames: classNames);
		}

		public HtmlAttribute? GetAttribute(string name)
			=> source.TryGetAttribute(name, out var attribute) ? attribute : null;

		public bool HasClass(string className)
		{
			ArgumentException.ThrowIfNullOrEmpty(className);
			return source.TryGetAttribute("class", out var attribute) && ElementQuery.HasClass(attribute.ValueSpan, className);
		}

		public bool HasClass(StringValues classNames)
		{
			return source.TryGetAttribute("class", out var attribute) && ElementQuery.HasClasses(attribute.ValueSpan, classNames);
		}
	}
}
