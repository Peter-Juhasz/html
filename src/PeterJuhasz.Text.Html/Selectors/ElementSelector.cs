using PeterJuhasz.Text.Html.Model;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace PeterJuhasz.Text.Html.Selectors;

/// <summary>
/// A parsed CSS selector. Selectors are predicates: each one checks a single element, looking at its ancestors when it needs to,
/// so a query enumerates the candidates once, in document order, and keeps the ones that match.
/// </summary>
internal abstract record class ElementSelector
{
	/// <summary>
	/// Checks whether the element matches the selector.
	/// </summary>
	public abstract bool Matches(HtmlElement element, in SelectorContext context);

	/// <summary>
	/// Checks whether the element matches the selector, and if not, whether the combinator that led to it should try other elements.
	/// Only a <see cref="ComplexSelector"/> can fail globally; any other selector only tells about the element itself.
	/// </summary>
	internal virtual MatchResult Match(HtmlElement element, in SelectorContext context) => Matches(element, context) ? MatchResult.Matched : MatchResult.NotMatched;

	/// <summary>
	/// Combines two selectors into a selector list, which matches elements that match either of them.
	/// </summary>
	public static ElementSelector operator |(ElementSelector left, ElementSelector right) => new OrSelector(Selectors: [.. Branches(left), .. Branches(right)]);

	/// <summary>
	/// Negates a selector, like <c>:not(selector)</c>; the result is a simple selector, so it can be part of a compound selector.
	/// </summary>
	public static NotSelector operator !(ElementSelector selector) => new(Selector: selector);

	/// <summary>
	/// Returns the branches of a selector list, or the selector itself otherwise, so <c>a | b | c</c> makes a single list.
	/// </summary>
	private static ImmutableArray<ElementSelector> Branches(ElementSelector selector) => selector switch
	{
		OrSelector list => list.Selectors,
		_ => [selector],
	};

	/// <summary>
	/// Parses a selector written in CSS syntax; <see cref="ElementSelectorParser"/> lists what is supported.
	/// </summary>
	/// <exception cref="FormatException">The selector is not valid or not supported; the message tells why and where.</exception>
	public static ElementSelector Parse(string selector) => ElementSelectorParser.Parse(selector);

	/// <summary>
	/// Parses a selector written in CSS syntax, returning false if it is not valid or not supported.
	/// </summary>
	public static bool TryParse(ReadOnlySpan<char> selector, [NotNullWhen(true)] out ElementSelector? result) => ElementSelectorParser.TryParse(selector, out result);

	/// <summary>
	/// Returns the selector as CSS, so the debugger and test failures show it as it would be written.
	/// </summary>
	public sealed override string ToString()
	{
		using var pooled = StringBuilderPool.GetPooledObject(out var builder);
		AppendTo(builder);
		return builder.ToString();
	}

	/// <summary>
	/// Writes the selector as CSS; identifiers are written as they are, not escaped.
	/// </summary>
	internal abstract void AppendTo(StringBuilder builder);

	/// <summary>
	/// Compares two arrays item by item, for the selectors that hold one, as records compare <see cref="ImmutableArray{T}"/> by reference.
	/// </summary>
	protected static bool ItemsEqual<T>(ImmutableArray<T> left, ImmutableArray<T> right) where T : IEquatable<T>
		=> left.AsSpan().SequenceEqual(right.AsSpan());

	/// <summary>
	/// Combines the hash codes of the items, consistent with <see cref="ItemsEqual{T}(ImmutableArray{T}, ImmutableArray{T})"/>.
	/// </summary>
	protected static int GetItemsHashCode<T>(ImmutableArray<T> items)
	{
		var hash = new HashCode();
		foreach (var item in items)
		{
			hash.Add(item);
		}

		return hash.ToHashCode();
	}
}

internal static partial class Extensions
{
	extension(HtmlDocument document)
	{
		/// <summary>
		/// Finds the elements at any depth in the document that match the selector, in document order.
		/// </summary>
		public IEnumerable<HtmlElement> QuerySelectorAll(ElementSelector selector)
		{
			ArgumentNullException.ThrowIfNull(selector);
			return HtmlElement.Query(document.Nodes, selector);
		}
	}

	extension(HtmlElement element)
	{
		/// <summary>
		/// Finds the elements at any depth inside this element that match the selector, in document order.
		/// As in the DOM, the whole selector is matched against the document, so in <c>div p</c> the div may be outside this element.
		/// </summary>
		public IEnumerable<HtmlElement> QuerySelectorAll(ElementSelector selector)
		{
			ArgumentNullException.ThrowIfNull(selector);
			return HtmlElement.Query(element.Nodes, selector);
		}
	}
}
