using System.Text.Html.Model;

namespace PeterJuhasz.Text.Html.Tests.Model;

[TestClass]
public sealed class VisitorTests
{
	[TestMethod]
	public void VisitsElementsInDocumentOrderWithTheirAttributes()
	{
		var document = HtmlDocument.Parse("<html lang=\"en\"><body class=\"a\"><p id=\"x\" hidden>t<br>u</p></body></html><footer></footer>");
		var visitor = new RecordingVisitor();

		visitor.VisitDocument(document);

		CollectionAssert.AreEqual(new[] { "html", "lang", "body", "class", "p", "id", "hidden", "br", "footer" }, visitor.Visited);
	}

	[TestMethod]
	public void DefaultVisitorVisitsEverything()
	{
		var document = HtmlDocument.Parse("<a><b><c></c></b></a>");
		var visitor = new EmptyVisitor();

		visitor.VisitDocument(document);
	}

	[TestMethod]
	public void OverridingVisitElementWithoutCallingBaseStopsDescending()
	{
		var document = HtmlDocument.Parse("<a><b><c></c></b></a><d></d>");
		var visitor = new ShallowVisitor();

		visitor.VisitDocument(document);

		CollectionAssert.AreEqual(new[] { "a", "d" }, visitor.Visited);
	}

	private sealed class EmptyVisitor : HtmlVisitor
	{
	}

	private sealed class RecordingVisitor : HtmlVisitor
	{
		public List<string> Visited { get; } = [];

		public override void VisitElement(HtmlElement element)
		{
			Visited.Add(element.Name);
			base.VisitElement(element);
		}

		public override void VisitAttribute(HtmlAttribute attribute)
		{
			Visited.Add(attribute.Name);
		}
	}

	private sealed class ShallowVisitor : HtmlVisitor
	{
		public List<string> Visited { get; } = [];

		public override void VisitElement(HtmlElement element) => Visited.Add(element.Name);
	}
}
