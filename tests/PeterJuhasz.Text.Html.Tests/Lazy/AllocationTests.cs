using System.Text.Html.Lazy;

namespace PeterJuhasz.Text.Html.Tests.Lazy;

[TestClass]
public sealed class AllocationTests
{
	private const string Html =
		"<!DOCTYPE html><html><head><title>T</title><meta charset=\"utf-8\"></head>" +
		"<body><div class=\"a\" id='x'><p>text<br>more <b>bold</b></p>" +
		"<a href=\"/link?a=1&amp;b=2\" target=_blank>link</a><!-- c --><ul><li>1<li>2</ul>" +
		"<script>if (a<b) {}</script></div></body></html>";

	[TestMethod]
	public void VisitorTraversalDoesNotAllocate()
	{
		var document = new LazyHtmlDocument(Html);
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
		var document = new LazyHtmlDocument(Html);
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
		var document = new LazyHtmlDocument(Html);
		var expected = CountQuerySelector(document);

		var before = GC.GetAllocatedBytesForCurrentThread();
		var count = CountQuerySelector(document);
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

	// Touches the found elements and searches inside them too, so nested enumerators are measured as well.
	private static int CountQuerySelector(LazyHtmlDocument document)
	{
		var count = 0;
		foreach (var element in document.QuerySelectorAll("div"))
		{
			count += element.OuterSpan.Length;
			foreach (var link in element.QuerySelectorAll("a"))
			{
				if (link.TryGetAttribute("href", out var href))
					count += href.ValueSpan.Length;
			}

			foreach (var script in element.QuerySelectorAll("script"))
				count += script.InnerSpan.Length;

			if (element.TryQuerySelector("b", out var bold))
				count += bold.InnerSpan.Length;
		}

		if (document.TryQuerySelector("title", out var title))
			count += title.InnerSpan.Length;

		count += document.TryQuerySelector("missing", out _) ? 0 : 1;

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
			Count += attribute.NameSpan.Length + attribute.ValueSpan.Length + (attribute.HasValue ? 1 : 0);
		}
	}
}
