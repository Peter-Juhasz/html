using System.Text.Html.Lazy;

namespace PeterJuhasz.Text.Html.Tests;

[TestClass]
public sealed class AllocationTests
{
	private const string Html =
		"<!DOCTYPE html><html><head><title>T</title><meta charset=\"utf-8\"></head>" +
		"<body><div class=\"a\" id='x'><p>text<br>more <b>bold</b></p>" +
		"<a href=\"/link?a=1&amp;b=2\" target=_blank>link</a><!-- c --><ul><li>1<li>2</ul>" +
		"<script>if (a<b) {}</script></div></body></html>";

	[TestMethod]
	public void TraversalDoesNotAllocate()
	{
		var document = new LazyHtmlDocument(Html);
		var expected = Traverse(document);

		var before = GC.GetAllocatedBytesForCurrentThread();
		var actual = Traverse(document);
		var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

		Assert.AreEqual(expected, actual);
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

	private static int Traverse(LazyHtmlDocument document)
	{
		var count = 0;
		foreach (var element in document.Elements())
			count += Visit(element);
		return count;
	}

	private static int Visit(LazyHtmlElement element)
	{
		var count = 1 + element.NameSpan.Length + element.OuterSpan.Length + element.InnerSpan.Length;

		foreach (var attribute in element.Attributes())
			count += attribute.NameSpan.Length + attribute.ValueSpan.Length + (attribute.HasValue ? 1 : 0);

		if (element.TryGetAttribute("href", out var href))
			count += href.ValueSpan.Length;

		if (element.HasAttribute("class"))
			count++;

		foreach (var child in element.Elements())
			count += Visit(child);

		return count;
	}
}
