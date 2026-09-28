namespace PeterJuhasz.Text.Html.Selectors;

/// <summary>
/// How an <see cref="AttributeValueSelector"/> compares the attribute value with its value.
/// </summary>
internal enum AttributeOperator
{
	/// <summary>
	/// <c>[name=value]</c>: the attribute value is exactly the value.
	/// </summary>
	Exact,

	/// <summary>
	/// <c>[name~=value]</c>: one of the whitespace-separated words of the attribute value is exactly the value.
	/// </summary>
	ContainsWord,

	/// <summary>
	/// <c>[name|=value]</c>: the attribute value is exactly the value, or starts with it followed by <c>-</c>, like <c>en</c> and <c>en-US</c>.
	/// </summary>
	HyphenPrefix,

	/// <summary>
	/// <c>[name^=value]</c>: the attribute value starts with the value.
	/// </summary>
	StartsWith,

	/// <summary>
	/// <c>[name$=value]</c>: the attribute value ends with the value.
	/// </summary>
	EndsWith,

	/// <summary>
	/// <c>[name*=value]</c>: the attribute value contains the value.
	/// </summary>
	Contains,
}
