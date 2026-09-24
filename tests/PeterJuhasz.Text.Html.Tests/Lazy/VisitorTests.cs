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

	[TestMethod]
	[DataRow("<html><head><title>a<b></title></head><body><p>1<p>2<div>3</div><ul><li>x<li>y</ul></body></html>")]
	[DataRow("<table><tr><td>1<td>2<tr><td>3</table><dl><dt>a<dd>b<dt>c</dl><select><option>1<optgroup><option>2</select>")]
	[DataRow("<div><span>unclosed<b>bold</div>after</span>tail")]
	[DataRow("<div><p>a</x>b</p></div></stray><br><img src=x/><input/><div/>text")]
	[DataRow("<a>1<a>2<!-- <b> --><script>if (a<b) {}</script><style>p>q{}</style><textarea>&lt;</textarea>")]
	[DataRow("<p><b>x<div>y</div></b>z<script>no end")]
	[DataRow("<!DOCTYPE html><?pi?>x<DIV Class=\"a\"><P>1</p></div ><li>1<p>2<li>3")]
	[DataRow("<ul><li><ul><li>nested<li>n2</ul><li>outer</ul><p>a<p>b")]
	public void VisitsTheSameNodesAsTheNodesEnumerator(string html)
	{
		var document = LazyHtmlDocument.Parse(html);
		var expected = new List<string>();
		var expectedElements = new List<LazyHtmlElement>();
		Collect(document.Nodes(), expected, expectedElements);

		var visitor = new SpanVisitor();
		visitor.VisitDocument(document);

		Assert.AreSequenceEqual(expected, visitor.Visited);

		// query results are not scanned until their end is needed, which must give the same ends and the same content
		var found = document.QuerySelectorAll().ToList();
		Assert.AreSequenceEqual(expectedElements.ConvertAll(Describe), found.ConvertAll(Describe));
		for (var i = 0; i < found.Count; i++)
		{
			Assert.AreSequenceEqual(Nodes(expectedElements[i]), Nodes(found[i]));
			Assert.AreSequenceEqual(expectedElements[i].Elements().ToList().ConvertAll(Describe), found[i].Elements().ToList().ConvertAll(Describe));
			Assert.AreEqual(expectedElements[i].TextContent, found[i].TextContent);
		}

		static List<string> Nodes(LazyHtmlElement element)
		{
			var visited = new List<string>();
			Collect(element.Nodes(), visited, []);
			return visited;
		}

		static void Collect(NodesEnumerator nodes, List<string> visited, List<LazyHtmlElement> elements)
		{
			foreach (var node in nodes)
			{
				if (node.TryGetElement(out var element))
				{
					visited.Add(Describe(element));
					elements.Add(element);
					Collect(element.Nodes(), visited, elements);
				}
				else
				{
					visited.Add(node.OuterSpan.ToString());
				}
			}
		}
	}

	[TestMethod]
	public void VisitsTheContentOfAnElementGivenDirectly()
	{
		var document = LazyHtmlDocument.Parse("<div><p>1<p>2</div><b>after</b>");
		var visitor = new SpanVisitor();

		visitor.VisitElement(document.Elements().ToList()[0]);

		Assert.AreSequenceEqual(["<div><p>1<p>2</div>|<p>1<p>2", "<p>1|1", "1", "<p>2|2", "2"], visitor.Visited);
	}

	[TestMethod]
	public void ElementsNotDescendedIntoAreSkippedCorrectly()
	{
		var document = LazyHtmlDocument.Parse("<a><b>1<c>2</c></b><d>3</d></a><e>4</e>");
		var visitor = new SelectiveVisitor("b");

		visitor.VisitDocument(document);

		Assert.AreSequenceEqual(["a", "b", "d", "3", "e", "4"], visitor.Visited);
	}

	[TestMethod]
	public void ReusedVisitorDoesNotApplyTheEndOfAnElementOfAnotherDocument()
	{
		var visitor = new SelectiveVisitor();
		visitor.VisitDocument(LazyHtmlDocument.Parse("<a><b></b></a>"));
		visitor.Visited.Clear();
		visitor.Skipped = "a";

		// an element at the same index in another document, whose content is not visited, is scanned to find its end
		visitor.VisitDocument(LazyHtmlDocument.Parse("<a>x</a><c>y</c>"));

		Assert.AreSequenceEqual(["a", "c", "y"], visitor.Visited);
	}

	private static string Describe(LazyHtmlElement element) => $"{element.OuterSpan}|{element.InnerSpan}";

	private sealed class EmptyVisitor : LazyHtmlVisitor
	{
	}

	private sealed class SpanVisitor : LazyHtmlVisitor
	{
		public List<string> Visited { get; } = [];

		public override void VisitElement(LazyHtmlElement element)
		{
			Visited.Add(Describe(element));
			base.VisitElement(element);
		}

		public override void VisitText(LazyHtmlText text) => Visited.Add(text.TextSpan.ToString());

		public override void VisitComment(LazyHtmlComment comment) => Visited.Add(comment.OuterSpan.ToString());
	}

	// Does not descend into the elements with the given name.
	private sealed class SelectiveVisitor(string? skipped = null) : LazyHtmlVisitor
	{
		public List<string> Visited { get; } = [];

		public string? Skipped { get; set; } = skipped;

		public override void VisitElement(LazyHtmlElement element)
		{
			Visited.Add(element.Name);
			if (!element.NameSpan.Equals(Skipped, StringComparison.OrdinalIgnoreCase))
			{
				base.VisitElement(element);
			}
		}

		public override void VisitText(LazyHtmlText text) => Visited.Add(text.TextSpan.ToString());
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
