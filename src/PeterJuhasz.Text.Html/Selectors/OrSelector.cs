using PeterJuhasz.Text.Html.Model;
using System.Collections.Immutable;
using System.Text;

namespace PeterJuhasz.Text.Html.Selectors;

/// <summary>
/// A selector list like <c>a, b</c>: matches elements that match any of the selectors.
/// </summary>
/// <param name="Selectors">The selectors, in the order they are written.</param>
internal sealed record class OrSelector(ImmutableArray<ElementSelector> Selectors) : ElementSelector
{
	/// <inheritdoc/>
	public override bool Matches(HtmlElement element, in SelectorContext context)
	{
		foreach (var selector in Selectors)
		{
			if (selector.Matches(element, context))
			{
				return true;
			}
		}

		return false;
	}

	/// <inheritdoc/>
	internal override void AppendTo(StringBuilder builder)
	{
		for (var i = 0; i < Selectors.Length; i++)
		{
			if (i > 0)
			{
				builder.Append(", ");
			}

			Selectors[i].AppendTo(builder);
		}
	}

	/// <summary>
	/// Compares the selectors item by item.
	/// </summary>
	public bool Equals(OrSelector? other) => other is not null && ItemsEqual(Selectors, other.Selectors);

	/// <inheritdoc/>
	public override int GetHashCode() => GetItemsHashCode(Selectors);
}
