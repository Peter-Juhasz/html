using PeterJuhasz.Text.Html.Model;

namespace PeterJuhasz.Text.Html.Selectors;

/// <summary>
/// The state of a query that selectors may need besides the element they check. An immutable value passed by reference,
/// so that <c>:has()</c> can derive one for its argument with <c>with</c> without allocating.
/// </summary>
/// <param name="Anchor">The element that the relative selectors of <c>:has()</c> start from, which <see cref="AnchorSelector"/> matches; null outside of <c>:has()</c>.</param>
internal readonly record struct SelectorContext(HtmlElement? Anchor);
