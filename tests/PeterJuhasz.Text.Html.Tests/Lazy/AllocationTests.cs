namespace PeterJuhasz.Text.Html.Tests.Lazy;

[TestClass]
public sealed class AllocationTests
{
	private const string Html =
		"<!DOCTYPE html><html><head><title>T</title><meta charset=\"utf-8\"></head>" +
		"<body><div class=\"a\" id='x'><p>text<br>more <b>bold</b></p>" +
		"<a href=\"/link?a=1&amp;b=2\" target=_blank>link</a><!-- c --><ul><li>1<li>2</ul>" +
		"<script>if (a<b) {}</script></div></body></html>";

	private static readonly KeyValuePair<string, string>[] LinkQuery = [KeyValuePair.Create("href", "/link?a=1&amp;b=2"), KeyValuePair.Create("target", "_blank")];
	private static readonly KeyValuePair<string, string>[] ClassQuery = [KeyValuePair.Create("class", "a")];

	[TestMethod]
	public void VisitorTraversalDoesNotAllocate()
	{
		var document = LazyHtmlDocument.Parse(Html);
		var visitor = new CountingVisitor();

		visitor.VisitDocument(document);
		var expected = visitor.Count;
		visitor.Count = 0;

		var before = GC.GetAllocatedBytesForCurrentThread();
		visitor.VisitDocument(document);
		var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

		Assert.AreEqual(expected, visitor.Count);
		Assert.AreEqual(0, allocated);
	}

	[TestMethod]
	public void DefaultVisitorDoesNotAllocate()
	{
		var document = LazyHtmlDocument.Parse(Html);
		var visitor = new EmptyVisitor();
		visitor.VisitDocument(document);

		var before = GC.GetAllocatedBytesForCurrentThread();
		visitor.VisitDocument(document);
		var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

		Assert.AreEqual(0, allocated);
	}

	[TestMethod]
	public void QuerySelectorDoesNotAllocate()
	{
		var document = LazyHtmlDocument.Parse(Html);
		var expected = CountQuerySelector(document);

		var before = GC.GetAllocatedBytesForCurrentThread();
		var count = CountQuerySelector(document);
		var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

		Assert.AreEqual(expected, count);
		Assert.IsGreaterThan(0, count);
		Assert.AreEqual(0, allocated);
	}

	[TestMethod]
	public void NodesEnumerationDoesNotAllocate()
	{
		var document = LazyHtmlDocument.Parse(Html);
		var expected = CountNodes(document.Nodes());

		var before = GC.GetAllocatedBytesForCurrentThread();
		var count = CountNodes(document.Nodes());
		var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

		Assert.AreEqual(expected, count);
		Assert.IsGreaterThan(0, count);
		Assert.AreEqual(0, allocated);
	}

	[TestMethod]
	public void TextContentWithoutMarkupAllocatesOnlyTheResult()
	{
		var element = TestHelpers.FirstElement("<p>hello world</p>");
		_ = element.TextContent;

		var before = GC.GetAllocatedBytesForCurrentThread();
		var text = element.TextContent;
		var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

		Assert.AreEqual("hello world", text);
		Assert.IsLessThanOrEqualTo(64, allocated, $"Allocated {allocated} bytes.");
	}

	private sealed class EmptyVisitor : LazyHtmlVisitor
	{
	}

	// Touches every span-based member of every kind of node, descending through the typed views.
	private static int CountNodes(NodesEnumerator nodes)
	{
		var count = 0;
		foreach (var node in nodes)
		{
			count += node.OuterSpan.Length;
			switch (node.Kind)
			{
				case LazyHtmlNodeKind.Element when node.TryGetElement(out var element):
					count += element.NameSpan.Length + element.InnerSpan.Length + CountNodes(element.Nodes());
					break;

				case LazyHtmlNodeKind.Text when node.TryGetText(out var text):
					count += text.TextSpan.Length;
					break;

				case LazyHtmlNodeKind.Comment when node.TryGetComment(out var comment):
					count += comment.TextSpan.Length + comment.OuterSpan.Length;
					break;
			}
		}

		return count;
	}

	// Touches the found elements and searches inside them too, so nested enumerators are measured as well.
	private static int CountQuerySelector(LazyHtmlDocument document)
	{
		var count = 0;
		foreach (var element in document.QuerySelectorAll(name: "div"))
		{
			count += element.OuterSpan.Length;
			foreach (var link in element.QuerySelectorAll(name: "a"))
			{
				if (link.TryGetAttribute("href", out var href))
					count += href.ValueSpan.Length;
			}

			foreach (var script in element.QuerySelectorAll(name: "script"))
				count += script.InnerSpan.Length;

			if (element.TryQuerySelector(out var bold, name: "b"))
				count += bold.InnerSpan.Length;
		}

		if (document.TryQuerySelector(out var title, name: "title"))
			count += title.InnerSpan.Length;

		count += document.TryQuerySelector(out _, name: "missing") ? 0 : 1;

		foreach (var element in document.QuerySelectorAll(attributes: ClassQuery))
			count += element.NameSpan.Length;

		foreach (var element in document.QuerySelectorAll())
			count++;

		if (document.TryQuerySelector(out var anchor, name: "a", attributes: LinkQuery))
			count += anchor.InnerSpan.Length;

		count += document.TryQuerySelector(out _, attributes: LinkQuery) ? 1 : 0;

		// inline attributes are stack-allocated by the compiler, so these must not allocate either
		foreach (var element in document.QuerySelectorAll(name: "a", attributes: [new("target", "_blank")]))
			count += element.NameSpan.Length;

		foreach (var element in document.QuerySelectorAll(attributes: [new("class", "a"), new("id", "x")]))
			count += element.NameSpan.Length;

		if (document.TryQuerySelector(out var blank, name: "a", attributes: [new("href", "/link?a=1&amp;b=2"), new("target", "_blank")]))
			count += blank.InnerSpan.Length;

		count += document.TryQuerySelector(out _, name: "meta", attributes: [new("charset", "utf-8")]) ? 1 : 0;

		// id and class matching, including the class list split, must not allocate either
		foreach (var element in document.QuerySelectorAll(id: "x"))
			count += element.NameSpan.Length;

		foreach (var element in document.QuerySelectorAll(className: "a"))
			count += element.NameSpan.Length;

		if (document.TryQuerySelector(out var box, name: "div", id: "x", className: "a"))
			count += box.OuterSpan.Length;

		count += document.TryQuerySelector(out _, className: "missing") ? 1 : 0;

		return count;
	}

	// Touches every span-based member so the whole read path is measured.
	private sealed class CountingVisitor : LazyHtmlVisitor
	{
		public int Count;

		public override void VisitElement(LazyHtmlElement element)
		{
			Count += 1 + element.NameSpan.Length + element.OuterSpan.Length + element.InnerSpan.Length;

			if (element.TryGetAttribute("href", out var href))
				Count += href.ValueSpan.Length;

			if (element.HasAttribute("class"))
				Count++;

			base.VisitElement(element);
		}

		public override void VisitAttribute(LazyHtmlElement element, LazyHtmlAttribute attribute)
		{
			Count += attribute.NameSpan.Length + attribute.ValueSpan.Length + (attribute.HasValue ? 1 : 0) + attribute.Element.NameSpan.Length;
		}

		public override void VisitText(LazyHtmlText text)
		{
			Count += 1 + text.TextSpan.Length;
		}

		public override void VisitComment(LazyHtmlComment comment)
		{
			Count += 1 + comment.TextSpan.Length + comment.OuterSpan.Length;
		}
	}
}
