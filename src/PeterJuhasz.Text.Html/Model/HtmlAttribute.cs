using System.Diagnostics.CodeAnalysis;
using PeterJuhasz.Text.Html.Lazy;

namespace PeterJuhasz.Text.Html.Model;

public sealed class HtmlAttribute
{
	// The attribute in the source text; the value span is read from it, so the tree keeps the source alive.
	private readonly LazyHtmlAttribute _source;

	// The decoded value is created on first access and kept for the next ones.
	internal string? _value;

	internal HtmlAttribute(HtmlElement element, LazyHtmlAttribute source)
	{
		Element = element;
		_source = source;
		Name = source.Name;
	}

	public HtmlElement Element { get; }

	public string Name { get; }

	// Value as written, without quotes; empty when the attribute has no value.
	public ReadOnlySpan<char> ValueSpan => _source.ValueSpan;

	// Value with character references decoded; null when the attribute has no value.
	public string? Value => _source.HasValue ? _value ??= _source.Value : null;

	[MemberNotNullWhen(true, nameof(Value))]
	public bool HasValue => _source.HasValue;
}
