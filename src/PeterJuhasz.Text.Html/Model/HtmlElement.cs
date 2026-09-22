using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
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
	public string TextContent => _textContent ??= _source.TextContent;

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

	// Finds the elements at any depth inside this element that have the given element name (any if null), class
	// and all of the given attributes with the given values, in document order.
	// Attribute memory is retained without copying; keep its storage valid until enumeration completes.
	public IEnumerable<HtmlElement> QuerySelectorAll(string? element = null, string? className = null, ReadOnlyMemory<KeyValuePair<string, string>> attributes = default)
		=> Query(Nodes, element, className, attributes);

	// Finds the first element at any depth inside this element that has the given element name (any if null), class
	// and all of the given attributes with the given values.
	public bool TryQuerySelector([NotNullWhen(true)] out HtmlElement? result, string? element = null, string? className = null, ReadOnlyMemory<KeyValuePair<string, string>> attributes = default)
	{
		result = QuerySelectorAll(element: element, className: className, attributes: attributes).FirstOrDefault();
		return result is not null;
	}

	// Enumerates the elements among the nodes, in order.
	internal static IEnumerable<HtmlElement> Elements(ImmutableArray<HtmlNode> nodes)
	{
		foreach (var node in nodes)
		{
			if (node is HtmlElement element)
				yield return element;
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
					pending.Push(element);
			}
		}
	}

	// Arguments are validated eagerly; the attribute memory is retained for deferred enumeration.
	internal static IEnumerable<HtmlElement> Query(ImmutableArray<HtmlNode> nodes, string? element, string? className, ReadOnlyMemory<KeyValuePair<string, string>> attributes)
	{
		ElementQuery.ValidateArguments(element, className, attributes.Span);
		return Descendants(nodes).Where(candidate => candidate.Matches(element, className, attributes));
	}

	// Same rules as the lazy layer: names are case-insensitive, values are compared as written (case-sensitively, without decoding)
	// and an attribute without a value matches "".
	private bool Matches(string? element, string? className, ReadOnlyMemory<KeyValuePair<string, string>> attributes)
	{
		if (element is not null && !string.Equals(Name, element, StringComparison.OrdinalIgnoreCase))
			return false;

		if (className is not null && !(TryGetAttribute("class", out var classAttribute) && ElementQuery.HasClass(classAttribute.ValueSpan, className)))
			return false;

		foreach (var (attributeName, value) in attributes.Span)
		{
			if (!TryGetAttribute(attributeName, out var attribute) || !attribute.ValueSpan.SequenceEqual(value))
				return false;
		}

		return true;
	}
}

public static partial class Extensions
{
	extension(HtmlElement source)
	{
		public HtmlElement? QuerySelector(string? element = null, string? className = null, ReadOnlyMemory<KeyValuePair<string, string>> attributes = default)
			=> source.TryQuerySelector(out var child, element: element, className: className, attributes: attributes) ? child : null;

		public bool TryGetElementById(string id, [NotNullWhen(true)] out HtmlElement? result)
		{
			ArgumentException.ThrowIfNullOrEmpty(id);
			KeyValuePair<string, string>[] attributes = [new("id", id)];
			return source.TryQuerySelector(out result, attributes: attributes);
		}

		public HtmlElement? GetElementById(string id) => source.TryGetElementById(id, out var result) ? result : null;

		// Matches the raw name attribute value case-sensitively, not the tag name.
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
			return source.QuerySelectorAll(className: className);
		}

		public HtmlAttribute? GetAttribute(string name)
			=> source.TryGetAttribute(name, out var attribute) ? attribute : null;
	}
}
