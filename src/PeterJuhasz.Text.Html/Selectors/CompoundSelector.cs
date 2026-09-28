using PeterJuhasz.Text.Html.Model;
using System.Collections.Immutable;
using System.Text;

namespace PeterJuhasz.Text.Html.Selectors;

/// <summary>
/// Simple selectors that must all match the same element, like <c>a.button</c>.
/// </summary>
/// <param name="Selectors">The element name or <c>*</c> first, if any, then the rest.</param>
internal sealed record class CompoundSelector(ImmutableArray<SimpleSelector> Selectors) : ElementSelector
{
	/// <inheritdoc/>
	public override bool Matches(HtmlElement element, in SelectorContext context)
	{
		foreach (var selector in Selectors)
		{
			if (!selector.Matches(element, context))
			{
				return false;
			}
		}

		return true;
	}

	/// <inheritdoc/>
	internal override void AppendTo(StringBuilder builder)
	{
		foreach (var selector in Selectors)
		{
			selector.AppendTo(builder);
		}
	}

	/// <summary>
	/// Compares the selectors item by item.
	/// </summary>
	public bool Equals(CompoundSelector? other) => other is not null && ItemsEqual(Selectors, other.Selectors);

	/// <inheritdoc/>
	public override int GetHashCode() => GetItemsHashCode(Selectors);
}
