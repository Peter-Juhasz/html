namespace PeterJuhasz.Text.Html.Selectors;

/// <summary>
/// The result of matching a <see cref="ComplexSelector"/>, which tells apart the failures that trying other elements can still recover from,
/// so that the combinators further out know when to stop trying.
/// </summary>
internal enum MatchResult
{
	/// <summary>
	/// The element matches.
	/// </summary>
	Matched,

	/// <summary>
	/// The element does not match, but a descendant combinator further out may still find a match through another ancestor.
	/// </summary>
	NotMatched,

	/// <summary>
	/// The element does not match, and no other element that the combinators further out could try instead would either, as those are higher up
	/// with fewer ancestors. Matching stops, so that a chain of descendant combinators does not try every combination of ancestors.
	/// </summary>
	NotMatchedGlobally,
}
