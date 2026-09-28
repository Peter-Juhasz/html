using PeterJuhasz.Text.Html.Model;
using System.Text;

namespace PeterJuhasz.Text.Html.Selectors;

/// <summary>
/// <c>:nth-child(An+B)</c> and its variations: matches elements whose position among their sibling elements, counted from 1, is <c>A*n+B</c> for some n ≥ 0.
/// The static fields hold the common variations and the static methods create the ones with a parameter; <c>with</c> adds <see cref="FromEnd"/> or <see cref="Of"/> to any of them.
/// Positions are found by scanning the siblings when an element is checked, as the model does not keep them for the few queries that need them.
/// </summary>
/// <param name="A">The step between the matching positions; 0 matches position <paramref name="B"/> only, and a negative step counts down from it.</param>
/// <param name="B">The first matching position.</param>
/// <param name="FromEnd">Whether the positions are counted from the last sibling, as with <c>:nth-last-child</c>.</param>
/// <param name="Of">If set, only the siblings that match this selector are counted and the element must match it too, as with <c>:nth-child(An+B of S)</c>.</param>
internal sealed record class NthChildSelector(int A, int B, bool FromEnd = false, ElementSelector? Of = null) : SimpleSelector
{
	/// <summary>
	/// <c>:first-child</c>: the first of its siblings.
	/// </summary>
	public static readonly NthChildSelector First = new(A: 0, B: 1);

	/// <summary>
	/// <c>:last-child</c>: the last of its siblings.
	/// </summary>
	public static readonly NthChildSelector Last = new(A: 0, B: 1, FromEnd: true);

	/// <summary>
	/// <c>:nth-child(odd)</c>: the 1st, 3rd, 5th and so on.
	/// </summary>
	public static readonly NthChildSelector Odd = new(A: 2, B: 1);

	/// <summary>
	/// <c>:nth-child(even)</c>: the 2nd, 4th, 6th and so on.
	/// </summary>
	public static readonly NthChildSelector Even = new(A: 2, B: 0);

	/// <summary>
	/// <c>:nth-child(-n+count)</c>: the first <paramref name="count"/> of the siblings.
	/// </summary>
	public static NthChildSelector Take(int count)
	{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);
		return new(A: -1, B: count);
	}

	/// <summary>
	/// <c>:nth-last-child(-n+count)</c>: the last <paramref name="count"/> of the siblings.
	/// </summary>
	public static NthChildSelector TakeLast(int count)
	{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);
		return new(A: -1, B: count, FromEnd: true);
	}

	/// <summary>
	/// <c>:nth-child(position)</c>: the sibling at the position, counted from 1.
	/// </summary>
	public static NthChildSelector At(int position)
	{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(position);
		return new(A: 0, B: position);
	}

	/// <summary>
	/// <c>:nth-last-child(position)</c>: the sibling at the position counted from the end, from 1, so 2 is the one before the last.
	/// </summary>
	public static NthChildSelector AtFromEnd(int position)
	{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(position);
		return new(A: 0, B: position, FromEnd: true);
	}

	/// <inheritdoc/>
	public override bool Matches(HtmlElement element, in SelectorContext context)
	{
		if (Of is not null && !Of.Matches(element, context))
		{
			return false;
		}

		// with a step of 0 or less no position after B matches, so the scan stops there, which keeps :first-child and :nth-child(3) short
		var maxPosition = A <= 0 ? B : int.MaxValue;
		return TryGetPosition(element, maxPosition, context, out var position) && IsMatchingPosition(position);
	}

	/// <summary>
	/// Checks whether the position is <c>A*n+B</c> for some n ≥ 0.
	/// </summary>
	private bool IsMatchingPosition(int position)
	{
		// computed as long, so that extreme values of B cannot overflow
		var difference = (long)position - B;
		return A switch
		{
			0 => difference == 0,
			_ => difference % A == 0 && difference / A >= 0,
		};
	}

	/// <summary>
	/// Finds the position of the element by counting the sibling elements before it, or after it when counting from the end, that match <see cref="Of"/>, if set.
	/// Returns false without scanning further once the position is known to be greater than <paramref name="maxPosition"/>.
	/// </summary>
	private bool TryGetPosition(HtmlElement element, int maxPosition, in SelectorContext context, out int position)
	{
		var siblings = element.Parent?.Nodes ?? element.Document.Nodes;
		var (start, step) = FromEnd ? (siblings.Length - 1, -1) : (0, 1);

		// the element is among the nodes of its parent, so the scan stops at it
		position = 1;
		for (var i = start; siblings[i] != element; i += step)
		{
			if (siblings[i] is HtmlElement sibling && (Of is null || Of.Matches(sibling, context)) && ++position > maxPosition)
			{
				return false;
			}
		}

		return position <= maxPosition;
	}

	/// <inheritdoc/>
	internal override void AppendTo(StringBuilder builder)
	{
		builder.Append(FromEnd ? ":nth-last-child(" : ":nth-child(");
		AppendFormula(builder);

		if (Of is not null)
		{
			builder.Append(" of ");
			Of.AppendTo(builder);
		}

		builder.Append(')');
	}

	/// <summary>
	/// Writes <see cref="A"/> and <see cref="B"/> the way CSS serializes them, like <c>2n+1</c>, <c>-n+3</c> or <c>5</c>.
	/// </summary>
	private void AppendFormula(StringBuilder builder)
	{
		switch (A)
		{
			case 0:
				builder.Append(B);
				return;

			case 1:
				builder.Append('n');
				break;

			case -1:
				builder.Append("-n");
				break;

			default:
				builder.Append(A).Append('n');
				break;
		}

		if (B > 0)
		{
			builder.Append('+');
		}

		if (B is not 0)
		{
			builder.Append(B);
		}
	}
}
