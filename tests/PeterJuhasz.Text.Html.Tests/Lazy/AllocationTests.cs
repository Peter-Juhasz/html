using Microsoft.Extensions.Primitives;

namespace PeterJuhasz.Text.Html.Tests.Lazy;

[TestClass]
public sealed class AllocationTests
{
	private const string Html =
		"<!DOCTYPE html><html><head><title>T &amp; U</title><meta charset=\"utf-8\"></head>" +
		"<body><div class=\"a\" id='x'><p>text<br>more <b class=\"b a\">bold</b></p>" +
		"<a href=\"/link?a=1\" target=_blank>link</a><!-- c --><ul><li>1<li>2</ul>" +
		"<script>if (a<b) {}</script></div></body></html>";

	private static readonly KeyValuePair<string, string>[] LinkQuery = [KeyValuePair.Create("href", "/link?a=1"), KeyValuePair.Create("target", "_blank")];
	private static readonly KeyValuePair<string, string>[] ClassQuery = [KeyValuePair.Create("class", "a")];
	// Multiple class names are backed by an array, which is allocated once here rather than per query.
	private static readonly StringValues TwoClasses = new(["a", "b"]);
	private static readonly StringValues TwoClassesReversed = new(["b", "a"]);
	private static readonly StringValues MissingClasses = new(["a", "missing"]);

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
	[DataRow("<html><head><title>Title</title></head><body>Content</body></html>")]
	[DataRow("<div><head>Title</head><body>Content</body></div>")]
	[DataRow("<html><div>no sections</div></html>")]
	public void HeadAndBodyLookupDoesNotAllocate(string html)
	{
		var document = LazyHtmlDocument.Parse(html);
		var hasHead = document.TryGetHead(out var expectedHead);
		var hasBody = document.TryGetBody(out var expectedBody);

		var before = GC.GetAllocatedBytesForCurrentThread();
		var foundHead = document.TryGetHead(out var head);
		var foundBody = document.TryGetBody(out var body);
		var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

		Assert.AreEqual(hasHead, foundHead);
		Assert.AreEqual(hasBody, foundBody);
		Assert.AreEqual(expectedHead, head);
		Assert.AreEqual(expectedBody, body);
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

	// Values with character references have to be decoded to be compared, which allocates the decoded copy;
	// values without any are compared in place, and a value that is too long to match is not decoded at all.
	[TestMethod]
	public void QueryOnAttributeWithCharacterReferencesAllocatesOnlyTheDecodedValue()
	{
		var document = LazyHtmlDocument.Parse("<a href=\"/link?a=1&amp;b=2\" class=\"x&amp;y\" title=\"plain\">link</a>");
		KeyValuePair<string, string>[] href = [new("href", "/link?a=1&b=2")];
		KeyValuePair<string, string>[] longer = [new("href", "/link?a=1&amp;amp;b=2")];
		KeyValuePair<string, string>[] plain = [new("title", "plain")];
		_ = document.TryQuerySelector(out _, attributes: href);
		_ = document.TryQuerySelector(out _, classNames: "x&y");

		var before = GC.GetAllocatedBytesForCurrentThread();
		var found = document.TryQuerySelector(out _, attributes: href) && document.TryQuerySelector(out _, classNames: "x&y");
		var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

		before = GC.GetAllocatedBytesForCurrentThread();
		var skipped = !document.TryQuerySelector(out _, attributes: longer) && document.TryQuerySelector(out _, attributes: plain);
		var allocatedWithoutDecoding = GC.GetAllocatedBytesForCurrentThread() - before;

		Assert.IsTrue(found);
		Assert.IsGreaterThan(0, allocated);
		Assert.IsLessThanOrEqualTo(256, allocated, $"Allocated {allocated} bytes.");
		Assert.IsTrue(skipped);
		Assert.AreEqual(0, allocatedWithoutDecoding);
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
		foreach (var element in document.QuerySelectorAll(element: "div"))
		{
			count += element.OuterSpan.Length;
			foreach (var link in element.QuerySelectorAll(element: "a"))
			{
				if (link.TryGetAttribute("href", out var href))
				{
					count += href.ValueSpan.Length;
				}
			}

			foreach (var script in element.QuerySelectorAll(element: "script"))
			{
				count += script.InnerSpan.Length;
			}

			if (element.TryQuerySelector(out var bold, element: "b"))
			{
				count += bold.InnerSpan.Length;
			}
		}

		if (document.TryQuerySelector(out var title, element: "title"))
		{
			count += title.InnerSpan.Length;
		}

		count += document.TryQuerySelector(out _, element: "missing") ? 0 : 1;

		foreach (var element in document.QuerySelectorAll(attributes: ClassQuery))
		{
			count += element.NameSpan.Length;
		}

		foreach (var element in document.QuerySelectorAll())
		{
			count++;
		}

		if (document.TryQuerySelector(out var anchor, element: "a", attributes: LinkQuery))
		{
			count += anchor.InnerSpan.Length;
		}

		count += document.TryQuerySelector(out _, attributes: LinkQuery) ? 1 : 0;

		// inline attributes are stack-allocated by the compiler, so these must not allocate either
		foreach (var element in document.QuerySelectorAll(element: "a", attributes: [new("target", "_blank")]))
		{
			count += element.NameSpan.Length;
		}

		foreach (var element in document.QuerySelectorAll(attributes: [new("class", "a"), new("id", "x")]))
		{
			count += element.NameSpan.Length;
		}

		if (document.TryQuerySelector(out var blank, element: "a", attributes: [new("href", "/link?a=1"), new("target", "_blank")]))
		{
			count += blank.InnerSpan.Length;
		}

		count += document.TryQuerySelector(out _, element: "meta", attributes: [new("charset", "utf-8")]) ? 1 : 0;

		// ID attribute and class matching, including the class list split, must not allocate either
		foreach (var element in document.QuerySelectorAll(attributes: [new("id", "x")]))
		{
			count += element.NameSpan.Length;
		}

		foreach (var element in document.QuerySelectorAll(classNames: "a"))
		{
			count += element.NameSpan.Length;
		}

		foreach (var element in document.QuerySelectorAll(classNames: TwoClasses))
		{
			count += element.NameSpan.Length;
		}

		if (document.TryQuerySelector(out var box, element: "div", classNames: "a", attributes: [new("id", "x")]))
		{
			count += box.OuterSpan.Length;
		}

		if (document.TryQuerySelector(out var multiClassBox, element: "div", classNames: TwoClassesReversed, attributes: [new("id", "x")]))
		{
			count += multiClassBox.OuterSpan.Length;
		}

		count += document.TryQuerySelector(out _, classNames: "missing") ? 1 : 0;
		count += document.TryQuerySelector(out _, classNames: MissingClasses) ? 1 : 0;

		// a single class name is stored in the StringValues without an array, so this must not allocate either
		foreach (var element in document.GetElementsByClassName("a"))
		{
			count += element.NameSpan.Length;
		}

		foreach (var element in document.GetElementsByClassName(TwoClasses))
		{
			count += element.NameSpan.Length;
		}

		if (document.GetElementById("x") is { } byId)
		{
			count += byId.OuterSpan.Length;
		}

		if (document.TryQuerySelector(out var body, element: "body") && body.GetElementById("x") is { } childById)
		{
			count += childById.OuterSpan.Length;
		}

		count += document.GetElementById("missing") is null ? 1 : 0;

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
			{
				Count += href.ValueSpan.Length;
			}

			if (element.HasAttribute("class"))
			{
				Count++;
			}

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
