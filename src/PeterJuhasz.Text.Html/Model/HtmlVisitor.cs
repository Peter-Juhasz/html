namespace System.Text.Html.Model;

public abstract class HtmlVisitor
{
	public virtual void VisitDocument(HtmlDocument document)
	{
		foreach (var element in document.Elements)
		{
			VisitElement(element);
		}
	}

	public virtual void VisitElement(HtmlElement element)
	{
		foreach (var attribute in element.Attributes)
		{
			VisitAttribute(attribute);
		}

		foreach (var child in element.Children)
		{
			VisitElement(child);
		}
	}

	public virtual void VisitAttribute(HtmlAttribute attribute)
	{
	}
}
