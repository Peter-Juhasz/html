using System.Diagnostics.CodeAnalysis;

namespace System.Text.Html.Model;

public sealed class HtmlAttribute
{
	internal HtmlAttribute(HtmlElement element, string name, string? value)
	{
		Element = element;
		Name = name;
		Value = value;
	}

	public HtmlElement Element { get; }

	public string Name { get; }

	// Value as written, without quotes; null when the attribute has no value.
	public string? Value { get; }

	[MemberNotNullWhen(true, nameof(Value))]
	public bool HasValue => Value is not null;
}
