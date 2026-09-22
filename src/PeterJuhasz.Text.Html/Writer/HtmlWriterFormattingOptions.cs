namespace PeterJuhasz.Text.Html.Writer;

public sealed record class HtmlWriterFormattingOptions(
	bool OmitQuotesIfNotNecessary = false,
	bool XmlStyleSelfClosingTags = true,
	bool SpaceBeforeSelfClosingSlash = true,
	string? Indent = null,
	string? NewLine = null
)
{
	public static readonly HtmlWriterFormattingOptions Default = new();

	public static readonly HtmlWriterFormattingOptions Indented = new(
		OmitQuotesIfNotNecessary: false,
		XmlStyleSelfClosingTags: true,
		SpaceBeforeSelfClosingSlash: false,
		Indent: "\t",
		NewLine: "\n"
	);

	public static readonly HtmlWriterFormattingOptions Minimal = new(
		OmitQuotesIfNotNecessary: true,
		XmlStyleSelfClosingTags: false,
		SpaceBeforeSelfClosingSlash: false,
		Indent: null,
		NewLine: null
	);
}
