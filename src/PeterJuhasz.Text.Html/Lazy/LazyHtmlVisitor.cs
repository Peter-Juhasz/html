namespace System.Text.Html.Lazy;

public abstract class LazyHtmlVisitor
{
	public virtual void VisitDocument(LazyHtmlDocument document)
	{
		foreach (var node in document.Nodes())
		{
			VisitNode(node);
		}
	}

	// Dispatches to the visit method of the node's kind.
	public virtual void VisitNode(LazyHtmlNode node)
	{
		switch (node.Kind)
		{
			case LazyHtmlNodeKind.Element:
				VisitElement(node.Element);
				break;

			case LazyHtmlNodeKind.Text:
				VisitText(node.Text);
				break;

			case LazyHtmlNodeKind.Comment:
				VisitComment(node.Comment);
				break;
		}
	}

	public virtual void VisitElement(LazyHtmlElement element)
	{
		foreach (var attribute in element.Attributes())
		{
			VisitAttribute(element, attribute);
		}

		foreach (var child in element.Nodes())
		{
			VisitNode(child);
		}
	}

	public virtual void VisitAttribute(LazyHtmlElement element, LazyHtmlAttribute attribute)
	{
	}

	public virtual void VisitText(LazyHtmlText text)
	{
	}

	public virtual void VisitComment(LazyHtmlComment comment)
	{
	}
}
