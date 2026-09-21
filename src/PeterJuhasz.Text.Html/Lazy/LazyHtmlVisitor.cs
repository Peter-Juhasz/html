namespace System.Text.Html.Lazy;

public abstract class LazyHtmlVisitor
{
	public virtual void VisitDocument(LazyHtmlDocument document)
	{
		foreach (var element in document.Elements())
		{
			VisitElement(element);
		}
	}

	public virtual void VisitElement(LazyHtmlElement element)
	{
		foreach (var attribute in element.Attributes())
		{
			VisitAttribute(element, attribute);
		}

		foreach (var child in element.Elements())
		{
			VisitElement(child);
		}
	}

	public virtual void VisitAttribute(LazyHtmlElement element, LazyHtmlAttribute attribute)
	{
	}
}
