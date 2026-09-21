namespace PeterJuhasz.Text.Html.Model;

public abstract class HtmlVisitor
{
	public virtual void VisitDocument(HtmlDocument document)
	{
		foreach (var node in document.Nodes)
		{
			VisitNode(node);
		}
	}

	// Dispatches to the visit method of the node's type.
	public virtual void VisitNode(HtmlNode node)
	{
		switch (node)
		{
			case HtmlElement element:
				VisitElement(element);
				break;

			case HtmlText text:
				VisitText(text);
				break;

			case HtmlComment comment:
				VisitComment(comment);
				break;
		}
	}

	public virtual void VisitElement(HtmlElement element)
	{
		foreach (var attribute in element.Attributes)
		{
			VisitAttribute(attribute);
		}

		foreach (var child in element.Nodes)
		{
			VisitNode(child);
		}
	}

	public virtual void VisitAttribute(HtmlAttribute attribute)
	{
	}

	public virtual void VisitText(HtmlText text)
	{
	}

	public virtual void VisitComment(HtmlComment comment)
	{
	}
}
