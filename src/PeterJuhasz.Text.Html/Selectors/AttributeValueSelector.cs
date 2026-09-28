using PeterJuhasz.Text.Html.Model;
using System.Buffers;
using System.Text;

namespace PeterJuhasz.Text.Html.Selectors;

/// <summary>
/// <c>[name=value]</c> and the other value operators: matches elements that have the attribute with a value that matches by the operator.
/// The attribute value is compared with its character references decoded; an attribute without a value has the value "".
/// </summary>
/// <param name="Name">The attribute name, compared case-insensitively.</param>
/// <param name="Operator">How the attribute value is compared with <paramref name="Value"/>.</param>
/// <param name="Value">The value the attribute value is compared with.</param>
/// <param name="IgnoreCase">Whether the values are compared case-insensitively, as with the <c>i</c> flag; otherwise they are compared case-sensitively.</param>
internal sealed record class AttributeValueSelector(string Name, AttributeOperator Operator, string Value, bool IgnoreCase = false) : SimpleSelector
{
	/// <summary>
	/// The comparison for the values, as set by <see cref="IgnoreCase"/>.
	/// </summary>
	private StringComparison Comparison => IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

	/// <inheritdoc/>
	public override bool Matches(HtmlElement element, in SelectorContext context)
	{
		if (!element.TryGetAttribute(Name, out var attribute))
		{
			return false;
		}

		// the decoded value is reused when it was already created
		if (attribute._value is string decoded)
		{
			return MatchesValue(decoded);
		}

		var valueSpan = attribute.ValueSpan;

		// decoding never makes the text longer, so a longer value cannot match with any operator and the decoding is skipped
		if (Value.Length > valueSpan.Length)
		{
			return false;
		}

		if (!SyntaxFacts.NeedsDecoding(valueSpan, out var referenceStart))
		{
			return MatchesValue(valueSpan);
		}

		if (valueSpan.Length <= HtmlDecoder.StackAllocThreshold)
		{
			Span<char> buffer = stackalloc char[valueSpan.Length];
			HtmlDecoder.Decode(valueSpan, referenceStart, buffer, out int charsWritten);
			return MatchesValue(buffer[..charsWritten]);
		}
		else
		{
			using var array = ArrayPool<char>.Shared.GetPooledArray(valueSpan.Length);
			HtmlDecoder.Decode(valueSpan, referenceStart, array, out int charsWritten);
			return MatchesValue(array.Array.AsSpan(0, charsWritten));
		}
	}

	/// <summary>
	/// Checks whether the decoded attribute value matches <see cref="Value"/> by <see cref="Operator"/>.
	/// As in CSS, an empty value never matches with the operators that look for a word, a prefix, a suffix or a substring.
	/// </summary>
	private bool MatchesValue(ReadOnlySpan<char> attributeValue) => Operator switch
	{
		AttributeOperator.Exact => attributeValue.Equals(Value, Comparison),
		AttributeOperator.ContainsWord => Value.Length > 0 && !Value.AsSpan().ContainsAny(SyntaxFacts.Whitespace) && ContainsWord(attributeValue),
		AttributeOperator.HyphenPrefix => attributeValue.StartsWith(Value, Comparison) && attributeValue[Value.Length..] is [] or ['-', ..],
		AttributeOperator.StartsWith => Value.Length > 0 && attributeValue.StartsWith(Value, Comparison),
		AttributeOperator.EndsWith => Value.Length > 0 && attributeValue.EndsWith(Value, Comparison),
		AttributeOperator.Contains => Value.Length > 0 && attributeValue.Contains(Value, Comparison),
		_ => throw new InvalidOperationException($"Unknown attribute operator '{Operator}'."),
	};

	/// <summary>
	/// Checks whether one of the whitespace-separated words of the decoded attribute value is <see cref="Value"/>.
	/// </summary>
	private bool ContainsWord(ReadOnlySpan<char> attributeValue)
	{
		foreach (var range in attributeValue.SplitAny(SyntaxFacts.Whitespace))
		{
			if (attributeValue[range].Equals(Value, Comparison))
			{
				return true;
			}
		}

		return false;
	}

	/// <inheritdoc/>
	internal override void AppendTo(StringBuilder builder)
	{
		builder.Append('[').Append(Name).Append(OperatorText).Append('"');

		// the value is written as a quoted string, where only quotes and backslashes are escaped
		foreach (var c in Value)
		{
			if (c is '"' or '\\')
			{
				builder.Append('\\');
			}

			builder.Append(c);
		}

		builder.Append('"');
		if (IgnoreCase)
		{
			builder.Append(" i");
		}

		builder.Append(']');
	}

	/// <summary>
	/// The operator as written between the attribute name and the value.
	/// </summary>
	private string OperatorText => Operator switch
	{
		AttributeOperator.Exact => "=",
		AttributeOperator.ContainsWord => "~=",
		AttributeOperator.HyphenPrefix => "|=",
		AttributeOperator.StartsWith => "^=",
		AttributeOperator.EndsWith => "$=",
		AttributeOperator.Contains => "*=",
		_ => throw new InvalidOperationException($"Unknown attribute operator '{Operator}'."),
	};
}
