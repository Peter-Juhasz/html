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

		Assert.AreSequenceEqual(["html", "lang", "body", "class", "p", "id", "hidden", "\"t\"", "br", "\"u\"", "footer"], visitor.Visited);
	}

	[TestMethod]
	public void VisitsTextAndCommentsWhereTheyAre()
	{
		var document = HtmlDocument.Parse("<!DOCTYPE html>x<div><!-- c -->y<script>a<b</script></div>z");
		var visitor = new RecordingVisitor();

		visitor.VisitDocument(document);

		Assert.AreSequenceEqual(["\"x\"", "div", "<!-- c -->", "\"y\"", "script", "\"a<b\"", "\"z\""], visitor.Visited);
	}

	[TestMethod]
	public void DefaultVisitorVisitsEverything()
	{
		var document = HtmlDocument.Parse("a<b><!--c--><d>e</d></b>");
		var visitor = new EmptyVisitor();

		visitor.VisitDocument(document);
	}

	[TestMethod]
	public void OverridingVisitElementWithoutCallingBaseStopsDescending()
	{
		var document = HtmlDocument.Parse("<a><b><c></c></b></a>x<d></d>");
		var visitor = new ShallowVisitor();

		visitor.VisitDocument(document);

		Assert.AreSequenceEqual(["a", "\"x\"", "d"], visitor.Visited);
	}

	[TestMethod]
	public void OverridingVisitNodeInterceptsEveryNode()
	{
		var document = HtmlDocument.Parse("a<b>c<!--d--></b>");
		var visitor = new TypeRecordingVisitor();

		visitor.VisitDocument(document);

		Assert.AreSequenceEqual([typeof(HtmlText), typeof(HtmlElement), typeof(HtmlText), typeof(HtmlComment)], visitor.Types);
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

		public override void VisitText(HtmlText text) => Visited.Add($"\"{text.Text}\"");

		public override void VisitComment(HtmlComment comment) => Visited.Add(comment.ToString());
	}

	private sealed class ShallowVisitor : HtmlVisitor
	{
		public List<string> Visited { get; } = [];

		public override void VisitElement(HtmlElement element) => Visited.Add(element.Name);

		public override void VisitText(HtmlText text) => Visited.Add($"\"{text.Text}\"");
	}

	private sealed class TypeRecordingVisitor : HtmlVisitor
	{
		public List<Type> Types { get; } = [];

		public override void VisitNode(HtmlNode node)
		{
			Types.Add(node.GetType());
			base.VisitNode(node);
		}
	}
}
