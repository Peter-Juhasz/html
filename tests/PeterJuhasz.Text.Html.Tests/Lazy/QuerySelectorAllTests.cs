using System.Text.Html.Lazy;

namespace PeterJuhasz.Text.Html.Tests.Lazy;

[TestClass]
public sealed class QuerySelectorAllTests
{
	[TestMethod]
	public void FindsRootElementsInDocument()
	{
		var document = new LazyHtmlDocument("<a>1</a><b>2</b><a>3</a>");

		CollectionAssert.AreEqual(new[] { "<a>1</a>", "<a>3</a>" }, document.QuerySelectorAll("a").Outers());
	}

	[TestMethod]
	public void FindsDescendantsAtAnyDepth()
	{
		var document = new LazyHtmlDocument("<html><body><div><ul><li><a>1</a></li></ul><p><a>2</a></p></div><a>3</a></body></html>");

		CollectionAssert.AreEqual(new[] { "<a>1</a>", "<a>2</a>", "<a>3</a>" }, document.QuerySelectorAll("a").Outers());
	}

	[TestMethod]
	public void FindsMatchesNestedInsideMatches()
	{
		var document = new LazyHtmlDocument("<div id=1><div id=2><div id=3></div></div></div>");

		var ids = document.QuerySelectorAll("div").ToList().ConvertAll(e => e.TryGetAttribute("id", out var id) ? id.Value : "");

		CollectionAssert.AreEqual(new[] { "1", "2", "3" }, ids);
	}

	[TestMethod]
	public void ReturnsElementsInDocumentOrder()
	{
		var document = new LazyHtmlDocument("<x><i>1</i><y><i>2</i><z><i>3</i></z><i>4</i></y><i>5</i></x>");

		CollectionAssert.AreEqual(new[] { "1", "2", "3", "4", "5" }, document.QuerySelectorAll("i").ToList().ConvertAll(e => e.InnerSpan.ToString()));
	}

	[TestMethod]
	public void ElementSearchesOnlyItsOwnContent()
	{
		var document = new LazyHtmlDocument("<a>0</a><div><a>1</a><p><a>2</a></p></div><a>3</a>");
		var div = document.Elements().ToList()[1];

		CollectionAssert.AreEqual(new[] { "<a>1</a>", "<a>2</a>" }, div.QuerySelectorAll("a").Outers());
	}

	[TestMethod]
	public void ElementDoesNotMatchItself()
	{
		var element = TestHelpers.FirstElement("<div><span></span></div>");

		Assert.IsFalse(element.QuerySelectorAll("div").MoveNext());
	}

	[TestMethod]
	public void NameComparisonIsCaseInsensitive()
	{
		var document = new LazyHtmlDocument("<DIV>1</DIV><div>2</div><Div>3</Div>");

		Assert.HasCount(3, document.QuerySelectorAll("div").ToList());
		Assert.HasCount(3, document.QuerySelectorAll("DIV").ToList());
		Assert.HasCount(3, document.QuerySelectorAll("dIv").ToList());
	}

	[TestMethod]
	public void DoesNotMatchNamesWithTheSamePrefix()
	{
		var document = new LazyHtmlDocument("<b>1</b><br><a>2</a><abbr>3</abbr><li>4<link></li><p>5</p>");

		CollectionAssert.AreEqual(new[] { "<b>1</b>" }, document.QuerySelectorAll("b").Outers());
		CollectionAssert.AreEqual(new[] { "<a>2</a>" }, document.QuerySelectorAll("a").Outers());
		CollectionAssert.AreEqual(new[] { "<li>4<link></li>" }, document.QuerySelectorAll("li").Outers());
		CollectionAssert.AreEqual(new[] { "<br>" }, document.QuerySelectorAll("br").Outers());
	}

	[TestMethod]
	public void FindsVoidAndSelfClosingElements()
	{
		var document = new LazyHtmlDocument("<p>a<br>b<br/>c<br />d</p><img src=x><input/>");

		Assert.HasCount(3, document.QuerySelectorAll("br").ToList());
		CollectionAssert.AreEqual(new[] { "<img src=x>" }, document.QuerySelectorAll("img").Outers());
		CollectionAssert.AreEqual(new[] { "<input/>" }, document.QuerySelectorAll("input").Outers());
	}

	[TestMethod]
	public void FindsImplicitlyClosedElements()
	{
		var document = new LazyHtmlDocument("<ul><li>1<li>2<li>3</ul><p>a<p>b");

		CollectionAssert.AreEqual(new[] { "<li>1", "<li>2", "<li>3" }, document.QuerySelectorAll("li").Outers());
		CollectionAssert.AreEqual(new[] { "<p>a", "<p>b" }, document.QuerySelectorAll("p").Outers());
	}

	[TestMethod]
	public void ElementDoesNotFindTheStartTagThatImplicitlyClosedIt()
	{
		var paragraph = TestHelpers.FirstElement("<p>text<div>block</div>");

		Assert.IsFalse(paragraph.QuerySelectorAll("div").MoveNext());
	}

	[TestMethod]
	public void SkipsMarkupInsideComments()
	{
		var document = new LazyHtmlDocument("<div><!-- <a>x</a> --><a>y</a></div>");

		CollectionAssert.AreEqual(new[] { "<a>y</a>" }, document.QuerySelectorAll("a").Outers());
	}

	[TestMethod]
	public void SkipsDoctypeAndProcessingInstructions()
	{
		var document = new LazyHtmlDocument("<!DOCTYPE html><?xml version=\"1.0\"?><html></html>");

		CollectionAssert.AreEqual(new[] { "<html></html>" }, document.QuerySelectorAll("html").Outers());
		Assert.IsFalse(document.QuerySelectorAll("DOCTYPE").MoveNext());
		Assert.IsFalse(document.QuerySelectorAll("xml").MoveNext());
	}

	[TestMethod]
	public void DoesNotTreatAngleBracketsInAttributeValuesAsTags()
	{
		var document = new LazyHtmlDocument("<a title=\"x<b>y\" data='<i>'>1</a><b>2</b>");

		CollectionAssert.AreEqual(new[] { "<b>2</b>" }, document.QuerySelectorAll("b").Outers());
		Assert.IsFalse(document.QuerySelectorAll("i").MoveNext());
	}

	[TestMethod]
	public void DoesNotLookInsideRawTextContent()
	{
		var document = new LazyHtmlDocument("<script>if (a<b) { x = '<i>'; }</script><style>a>b{}</style><textarea><p></textarea><title><em></title><b>real</b>");

		CollectionAssert.AreEqual(new[] { "<b>real</b>" }, document.QuerySelectorAll("b").Outers());
		Assert.IsFalse(document.QuerySelectorAll("i").MoveNext());
		Assert.IsFalse(document.QuerySelectorAll("p").MoveNext());
		Assert.IsFalse(document.QuerySelectorAll("em").MoveNext());
	}

	[TestMethod]
	public void FindsRawTextElementsThemselves()
	{
		var document = new LazyHtmlDocument("<head><title>T</title><script>1</script></head><body><script>2</script></body>");

		CollectionAssert.AreEqual(new[] { "<script>1</script>", "<script>2</script>" }, document.QuerySelectorAll("script").Outers());
		CollectionAssert.AreEqual(new[] { "<title>T</title>" }, document.QuerySelectorAll("title").Outers());
	}

	[TestMethod]
	public void RawTextElementHasNoDescendants()
	{
		var script = TestHelpers.FirstElement("<script><b>not markup</b></script><b>after</b>");

		Assert.IsFalse(script.QuerySelectorAll("b").MoveNext());
	}

	[TestMethod]
	public void SelfClosingRawTextElementDoesNotSwallowFollowingElements()
	{
		var document = new LazyHtmlDocument("<script/><b>1</b>");

		CollectionAssert.AreEqual(new[] { "<b>1</b>" }, document.QuerySelectorAll("b").Outers());
	}

	[TestMethod]
	public void ReturnsEmptyWhenNothingMatches()
	{
		var document = new LazyHtmlDocument("<html><body><p>text</p></body></html>");

		Assert.IsFalse(document.QuerySelectorAll("a").MoveNext());
	}

	[TestMethod]
	public void EmptyAndDefaultDocumentsHaveNoMatches()
	{
		Assert.IsFalse(new LazyHtmlDocument("").QuerySelectorAll("a").MoveNext());
		Assert.IsFalse(default(LazyHtmlDocument).QuerySelectorAll("a").MoveNext());
		Assert.IsFalse(new LazyHtmlDocument("just text").QuerySelectorAll("a").MoveNext());
	}

	[TestMethod]
	public void EnumeratorReturnsFalseRepeatedlyAfterEnd()
	{
		var elements = new LazyHtmlDocument("<a></a>").QuerySelectorAll("a");

		Assert.IsTrue(elements.MoveNext());
		Assert.IsFalse(elements.MoveNext());
		Assert.IsFalse(elements.MoveNext());
	}

	[TestMethod]
	public void EnumeratorCanBeIteratedWithForeach()
	{
		var document = new LazyHtmlDocument("<a></a><div><a></a></div>");
		var count = 0;

		foreach (var element in document.QuerySelectorAll("a"))
			count++;

		Assert.AreEqual(2, count);
	}

	[TestMethod]
	public void FoundElementsExposeTheirContent()
	{
		var document = new LazyHtmlDocument("<div><a href=\"/1\">one</a><span><a href='/2'>two</a></span></div>");

		var links = document.QuerySelectorAll("a").ToList();

		Assert.HasCount(2, links);
		Assert.AreEqual("/1", links[0].TryGetAttribute("href", out var first) ? first.Value : null);
		Assert.AreEqual("one", links[0].TextContent);
		Assert.AreEqual("/2", links[1].TryGetAttribute("href", out var second) ? second.Value : null);
		Assert.AreEqual("two", links[1].TextContent);
	}

	[TestMethod]
	public void UnclosedMatchIsFoundAndSearchContinuesInsideIt()
	{
		var document = new LazyHtmlDocument("<div><a>1<a>2");

		CollectionAssert.AreEqual(new[] { "<a>1<a>2", "<a>2" }, document.QuerySelectorAll("a").Outers());
	}

	[TestMethod]
	public void QuerySelectorOnStringSegmentRespectsBounds()
	{
		var html = "<a>outside</a><div><a>inside</a></div><a>outside</a>";
		var segment = new Microsoft.Extensions.Primitives.StringSegment(html, html.IndexOf("<div>", StringComparison.Ordinal), "<div><a>inside</a></div>".Length);
		var document = new LazyHtmlDocument(segment);

		CollectionAssert.AreEqual(new[] { "<a>inside</a>" }, document.QuerySelectorAll("a").Outers());
	}

	[TestMethod]
	public void NullOrEmptyNameThrows()
	{
		var document = new LazyHtmlDocument("<a></a>");
		var element = TestHelpers.FirstElement("<a></a>");

		Assert.ThrowsExactly<ArgumentNullException>(() => document.QuerySelectorAll(null!));
		Assert.ThrowsExactly<ArgumentException>(() => document.QuerySelectorAll(""));
		Assert.ThrowsExactly<ArgumentNullException>(() => element.QuerySelectorAll(null!));
		Assert.ThrowsExactly<ArgumentException>(() => element.QuerySelectorAll(""));
	}

	[TestMethod]
	public void MatchesVisitorBasedSearch()
	{
		var html =
			"<!DOCTYPE html><html><head><title>T</title><meta charset=\"utf-8\"></head>" +
			"<body><div class=\"a\" id='x'><p>text<br>more <b>bold</b></p>" +
			"<a href=\"/link?a=1&amp;b=2\" target=_blank>link</a><!-- c --><ul><li>1<li>2</ul>" +
			"<script>if (a<b) {}</script><div><p><a>nested</a></p></div></div></body></html>";
		var document = new LazyHtmlDocument(html);

		foreach (var name in new[] { "a", "b", "p", "div", "li", "br", "meta", "script", "title", "html", "body", "zzz" })
		{
			var visitor = new CollectingVisitor(name);
			visitor.VisitDocument(document);

			CollectionAssert.AreEqual(visitor.Outers, document.QuerySelectorAll(name).Outers(), $"Mismatch for <{name}>.");
		}
	}

	private sealed class CollectingVisitor(string name) : LazyHtmlVisitor
	{
		public List<string> Outers { get; } = new();

		public override void VisitElement(LazyHtmlElement element)
		{
			if (element.NameSpan.Equals(name, StringComparison.OrdinalIgnoreCase))
				Outers.Add(element.OuterSpan.ToString());

			base.VisitElement(element);
		}
	}
}
