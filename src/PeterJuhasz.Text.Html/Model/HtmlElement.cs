using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text.Html.Lazy;

namespace System.Text.Html.Model;

public sealed class HtmlElement : HtmlNode
{
	// The element in the source text; spans and text are read from it, so the tree keeps the source alive.
	private readonly LazyHtmlElement _source;

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

	// Concatenated text of the content with all markup removed, as written (character references are not decoded).
	public string TextContent => _source.TextContent;

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

	// Finds the elements at any depth inside this element that have the given name (any name if null), id, class
	// and all of the given attributes with the given values, in document order.
	public IEnumerable<HtmlElement> QuerySelectorAll(string? name = null, string? id = null, string? className = null, ReadOnlySpan<KeyValuePair<string, string>> attributes = default)
		=> Query(Nodes, name, id, className, attributes);

	// Finds the first element at any depth inside this element that has the given name (any name if null), id, class
	// and all of the given attributes with the given values.
	public bool TryQuerySelector([NotNullWhen(true)] out HtmlElement? element, string? name = null, string? id = null, string? className = null, ReadOnlySpan<KeyValuePair<string, string>> attributes = default)
	{
		element = QuerySelectorAll(name: name, id: id, className: className, attributes: attributes).FirstOrDefault();
		return element is not null;
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

	// The arguments are validated eagerly and the attributes are copied, because the enumeration is deferred.
	internal static IEnumerable<HtmlElement> Query(ImmutableArray<HtmlNode> nodes, string? name, string? id, string? className, ReadOnlySpan<KeyValuePair<string, string>> attributes)
	{
		ElementQuery.ValidateArguments(name, id, className, attributes);
		var required = attributes.ToArray();
		return Descendants(nodes).Where(element => element.Matches(name, id, className, required));
	}

	// Same rules as the lazy layer: names are case-insensitive, values are case-sensitive and an attribute without a value matches "".
	private bool Matches(string? name, string? id, string? className, KeyValuePair<string, string>[] attributes)
	{
		if (name is not null && !string.Equals(Name, name, StringComparison.OrdinalIgnoreCase))
			return false;

		if (id is not null && !(TryGetAttribute("id", out var idAttribute) && idAttribute.Value.AsSpan().SequenceEqual(id)))
			return false;

		if (className is not null && !(TryGetAttribute("class", out var classAttribute) && ElementQuery.HasClass(classAttribute.Value, className)))
			return false;

		foreach (var (attributeName, value) in attributes)
		{
			if (!TryGetAttribute(attributeName, out var attribute) || !attribute.Value.AsSpan().SequenceEqual(value))
				return false;
		}

		return true;
	}
}

public static partial class Extensions
{
	extension(HtmlElement element)
	{
		public HtmlElement? QuerySelector(string? name = null, string? id = null, string? className = null, ReadOnlySpan<KeyValuePair<string, string>> attributes = default)
			=> element.TryQuerySelector(out var child, name: name, id: id, className: className, attributes: attributes) ? child : null;

		public HtmlElement? GetElementById(string id)
		{
			ArgumentException.ThrowIfNullOrEmpty(id);
			return QuerySelector(element, id: id);
		}

		public IEnumerable<HtmlElement> GetElementsByTagName(string tagName)
		{
			ArgumentException.ThrowIfNullOrEmpty(tagName);
			return element.QuerySelectorAll(name: tagName);
		}

		public IEnumerable<HtmlElement> GetElementsByClassName(string className)
		{
			ArgumentException.ThrowIfNullOrEmpty(className);
			return element.QuerySelectorAll(className: className);
		}

		public HtmlAttribute? GetAttribute(string name)
			=> element.TryGetAttribute(name, out var attribute) ? attribute : null;
	}
}
