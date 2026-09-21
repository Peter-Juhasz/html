namespace PeterJuhasz.Text.Html.Model;

public abstract class HtmlNode
{
	// The document and parent are created before their nodes, so they are passed in; the children are set by the parser afterwards.
	internal HtmlNode(HtmlDocument document, HtmlElement? parent)
	{
		Document = document;
		Parent = parent;
	}

	public HtmlDocument Document { get; }

	// Null for the top-level nodes of the document.
	public HtmlElement? Parent { get; }

	// The whole node as written: the element with its tags, the text, or the comment with its delimiters.
	public abstract ReadOnlySpan<char> OuterSpan { get; }

	public override string ToString() => OuterSpan.ToString();
}
