using Microsoft.Extensions.Primitives;

namespace PeterJuhasz.Text.Html.Lazy;

// Visits the nodes of a document in a single pass: the elements are visited before their content is scanned, and their end is found by
// visiting their content, so the content is not scanned again to move past them. Reading the end of an element while visiting it
// (e.g. its OuterSpan or InnerSpan) scans its content on each access. An instance must not be used by multiple threads at the same time.
public abstract class LazyHtmlVisitor
{
	// The element whose content was visited last, and the index right after it, so the enumerator of its parent can move past it without scanning it.
	private StringSegment _visitedDocument;
	private int _visitedStart = -1;
	private int _visitedEnd;

	public virtual void VisitDocument(LazyHtmlDocument document)
	{
		var nodes = document.NodesToVisit();
		VisitNodes(ref nodes);
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

		var nodes = new NodesEnumerator(element, isLazy: true);
		VisitNodes(ref nodes);

		_visitedDocument = element.Document;
		_visitedStart = element.Start;
		_visitedEnd = nodes.ParentEnd;
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

	private void VisitNodes(ref NodesEnumerator nodes)
	{
		while (nodes.MoveNext())
		{
			var node = nodes.Current;
			VisitNode(node);

			// when the content of the element has been visited, its end is known; otherwise (e.g. an override did not descend) it is scanned
			if (node.Kind == LazyHtmlNodeKind.Element && node.Start == _visitedStart && IsSameDocument(node.Document, _visitedDocument))
			{
				nodes.SkipElement(_visitedEnd);
			}
		}
	}

	private static bool IsSameDocument(StringSegment a, StringSegment b)
		=> ReferenceEquals(a.Buffer, b.Buffer) && a.Offset == b.Offset && a.Length == b.Length;
}
