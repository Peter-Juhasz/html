namespace PeterJuhasz.Text.Html.Tests.Lazy;

[TestClass]
public sealed class VisitorTests
{
	[TestMethod]
	public void VisitsNodesInDocumentOrderWithTheirAttributes()
	{
		var document = LazyHtmlDocument.Parse("<!DOCTYPE html>x<html lang=\"en\"><body class=\"a\"><!-- c --><p id=\"x\" hidden>t<br>u</p></body></html><footer></footer>y");
		var visitor = new RecordingVisitor();

		visitor.VisitDocument(document);

		Assert.AreSequenceEqual(["\"x\"", "<html>", "@lang", "<body>", "@class", "<!-- c -->", "<p>", "@id", "@hidden", "\"t\"", "<br>", "\"u\"", "<footer>", "\"y\""], visitor.Visited);
	}

	[TestMethod]
	public void DefaultVisitorVisitsEverything()
	{
		var document = LazyHtmlDocument.Parse("a<b><!--c--><d>e</d></b>");
		var visitor = new EmptyVisitor();

		visitor.VisitDocument(document);
	}

	[TestMethod]
	public void OverridingVisitElementWithoutCallingBaseStopsDescending()
	{
		var document = LazyHtmlDocument.Parse("<a><b>t<c></c></b></a>x<d></d>");
		var visitor = new ShallowVisitor();

		visitor.VisitDocument(document);

		Assert.AreSequenceEqual(["<a>", "\"x\"", "<d>"], visitor.Visited);
	}

	[TestMethod]
	public void OverridingVisitNodeInterceptsEveryNode()
	{
		var document = LazyHtmlDocument.Parse("a<b>c<!--d--></b>");
		var visitor = new KindCountingVisitor();

		visitor.VisitDocument(document);

		Assert.AreSequenceEqual([LazyHtmlNodeKind.Text, LazyHtmlNodeKind.Element, LazyHtmlNodeKind.Text, LazyHtmlNodeKind.Comment], visitor.Kinds);
	}

	[TestMethod]
	public void RawTextIsVisitedAsText()
	{
		var document = LazyHtmlDocument.Parse("<script>if (a<b) {}</script>");
		var visitor = new RecordingVisitor();

		visitor.VisitDocument(document);

		Assert.AreSequenceEqual(["<script>", "\"if (a<b) {}\""], visitor.Visited);
	}

	private sealed class EmptyVisitor : LazyHtmlVisitor
	{
	}

	private sealed class RecordingVisitor : LazyHtmlVisitor
	{
		public List<string> Visited { get; } = [];

		public override void VisitElement(LazyHtmlElement element)
		{
			Visited.Add($"<{element.Name}>");
			base.VisitElement(element);
		}

		public override void VisitAttribute(LazyHtmlElement element, LazyHtmlAttribute attribute) => Visited.Add($"@{attribute.Name}");

		public override void VisitText(LazyHtmlText text) => Visited.Add($"\"{text.Text}\"");

		public override void VisitComment(LazyHtmlComment comment) => Visited.Add(comment.OuterSpan.ToString());
	}

	private sealed class ShallowVisitor : LazyHtmlVisitor
	{
		public List<string> Visited { get; } = [];

		public override void VisitElement(LazyHtmlElement element) => Visited.Add($"<{element.Name}>");

		public override void VisitText(LazyHtmlText text) => Visited.Add($"\"{text.Text}\"");
	}

	private sealed class KindCountingVisitor : LazyHtmlVisitor
	{
		public List<LazyHtmlNodeKind> Kinds { get; } = [];

		public override void VisitNode(LazyHtmlNode node)
		{
			Kinds.Add(node.Kind);
			base.VisitNode(node);
		}
	}
}
