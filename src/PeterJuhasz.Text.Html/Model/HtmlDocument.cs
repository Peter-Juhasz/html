using System.Collections.Immutable;

namespace System.Text.Html.Model;

// ignore this file

public record class HtmlDocument(
	ImmutableArray<HtmlElement> Elements
);

public record class HtmlElement(
	HtmlElement Parent,
	string Name, 
	ImmutableArray<HtmlAttribute> Attributes,
	ImmutableArray<HtmlElement> Children,
	string? TextContent
);

public record class HtmlAttribute(
	HtmlElement Element,
	string Name, 
	string? Value
);
