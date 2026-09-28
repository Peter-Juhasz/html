using PeterJuhasz.Text.Html.Model;
using System.Collections.Frozen;
using System.Text;

namespace PeterJuhasz.Text.Html.Selectors;

/// <summary>
/// <c>:disabled</c>: matches the form controls that the markup disables, as HTML defines it: a button, input, select, textarea or fieldset
/// with the disabled attribute or inside a fieldset with it (except inside that fieldset's legend), an optgroup with the attribute,
/// and an option with the attribute or directly in an optgroup with it.
/// Form-associated custom elements are not recognized, as that is only known to scripts.
/// </summary>
internal sealed record class DisabledSelector : SimpleSelector
{
	/// <summary>
	/// The elements that a fieldset disables along with their own disabled attribute.
	/// </summary>
	private static readonly FrozenSet<string> FormControls = FrozenSet.Create<string>(StringComparer.OrdinalIgnoreCase, "button", "input", "select", "textarea", "fieldset");

	/// <summary>
	/// The only instance, as the selector has no parameters.
	/// </summary>
	public static readonly DisabledSelector Instance = new();

	private DisabledSelector()
	{
	}

	/// <inheritdoc/>
	public override bool Matches(HtmlElement element, in SelectorContext context)
	{
		if (IsNamed(element, "option"))
		{
			return element.HasAttribute("disabled") || element.Parent is { } parent && IsNamed(parent, "optgroup") && parent.HasAttribute("disabled");
		}

		if (IsNamed(element, "optgroup"))
		{
			return element.HasAttribute("disabled");
		}

		return FormControls.Contains(element.Name) && (element.HasAttribute("disabled") || IsInDisabledFieldset(element));
	}

	/// <summary>
	/// Checks whether an ancestor fieldset with the disabled attribute disables the element, which it does unless the element is inside the first legend of that fieldset.
	/// </summary>
	private static bool IsInDisabledFieldset(HtmlElement element)
	{
		var child = element;
		for (var ancestor = element.Parent; ancestor is not null; child = ancestor, ancestor = ancestor.Parent)
		{
			if (IsNamed(ancestor, "fieldset") && ancestor.HasAttribute("disabled") && !IsFirstLegend(ancestor, child))
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Checks whether the child is the first legend element among the children of the fieldset.
	/// </summary>
	private static bool IsFirstLegend(HtmlElement fieldset, HtmlElement child)
	{
		if (!IsNamed(child, "legend"))
		{
			return false;
		}

		foreach (var node in fieldset.Nodes)
		{
			if (node is HtmlElement legend && IsNamed(legend, "legend"))
			{
				return legend == child;
			}
		}

		return false;
	}

	/// <summary>
	/// Checks whether the element has the name, compared case-insensitively.
	/// </summary>
	private static bool IsNamed(HtmlElement element, string name) => element.Name.Equals(name, StringComparison.OrdinalIgnoreCase);

	/// <inheritdoc/>
	internal override void AppendTo(StringBuilder builder) => builder.Append(":disabled");
}
