using Microsoft.Extensions.Primitives;

namespace PeterJuhasz.Text.Html.Tests.Lazy;

[TestClass]
public sealed class QuerySelectorAllTests
{
	[TestMethod]
	public void FindsRootElementsInDocument()
	{
		var document = LazyHtmlDocument.Parse("<a>1</a><b>2</b><a>3</a>");

		Assert.AreSequenceEqual(["<a>1</a>", "<a>3</a>"], document.QuerySelectorAll(element: "a").Outers());
	}

	[TestMethod]
	public void FindsDescendantsAtAnyDepth()
	{
		var document = LazyHtmlDocument.Parse("<html><body><div><ul><li><a>1</a></li></ul><p><a>2</a></p></div><a>3</a></body></html>");

		Assert.AreSequenceEqual(["<a>1</a>", "<a>2</a>", "<a>3</a>"], document.QuerySelectorAll(element: "a").Outers());
	}

	[TestMethod]
	public void FindsMatchesNestedInsideMatches()
	{
		var document = LazyHtmlDocument.Parse("<div id=1><div id=2><div id=3></div></div></div>");

		var ids = document.QuerySelectorAll(element: "div").ToList().ConvertAll(e => e.TryGetAttribute("id", out var id) ? id.Value : "");

		Assert.AreSequenceEqual(["1", "2", "3"], ids);
	}

	[TestMethod]
	public void ReturnsElementsInDocumentOrder()
	{
		var document = LazyHtmlDocument.Parse("<x><i>1</i><y><i>2</i><z><i>3</i></z><i>4</i></y><i>5</i></x>");

		Assert.AreSequenceEqual(["1", "2", "3", "4", "5"], document.QuerySelectorAll(element: "i").ToList().ConvertAll(e => e.InnerSpan.ToString()));
	}

	[TestMethod]
	public void ElementSearchesOnlyItsOwnContent()
	{
		var document = LazyHtmlDocument.Parse("<a>0</a><div><a>1</a><p><a>2</a></p></div><a>3</a>");
		var div = document.Elements().ToList()[1];

		Assert.AreSequenceEqual(["<a>1</a>", "<a>2</a>"], div.QuerySelectorAll(element: "a").Outers());
	}

	[TestMethod]
	public void ElementDoesNotMatchItself()
	{
		var element = TestHelpers.FirstElement("<div><span></span></div>");

		Assert.IsFalse(element.QuerySelectorAll(element: "div").MoveNext());
	}

	[TestMethod]
	public void ElementComparisonIsCaseInsensitive()
	{
		var document = LazyHtmlDocument.Parse("<DIV>1</DIV><div>2</div><Div>3</Div>");

		Assert.HasCount(3, document.QuerySelectorAll(element: "div").ToList());
		Assert.HasCount(3, document.QuerySelectorAll(element: "DIV").ToList());
		Assert.HasCount(3, document.QuerySelectorAll(element: "dIv").ToList());
	}

	[TestMethod]
	public void DoesNotMatchNamesWithTheSamePrefix()
	{
		var document = LazyHtmlDocument.Parse("<b>1</b><br><a>2</a><abbr>3</abbr><li>4<link></li><p>5</p>");

		Assert.AreSequenceEqual(["<b>1</b>"], document.QuerySelectorAll(element: "b").Outers());
		Assert.AreSequenceEqual(["<a>2</a>"], document.QuerySelectorAll(element: "a").Outers());
		Assert.AreSequenceEqual(["<li>4<link></li>"], document.QuerySelectorAll(element: "li").Outers());
		Assert.AreSequenceEqual(["<br>"], document.QuerySelectorAll(element: "br").Outers());
	}

	[TestMethod]
	public void ElementFilterIsIndependentOfNameAttribute()
	{
		var document = LazyHtmlDocument.Parse("<input name=\"q\"><q name=\"input\"></q><input name=\"other\">");

		Assert.AreSequenceEqual(["<input name=\"q\">", "<input name=\"other\">"], document.QuerySelectorAll(element: "input").Outers());
		Assert.AreSequenceEqual(["<input name=\"q\">"], document.QuerySelectorAll(element: "input", attributes: [new("name", "q")]).Outers());
	}

	[TestMethod]
	public void FindsVoidAndSelfClosingElements()
	{
		var document = LazyHtmlDocument.Parse("<p>a<br>b<br/>c<br />d</p><img src=x><input/>");

		Assert.HasCount(3, document.QuerySelectorAll(element: "br").ToList());
		Assert.AreSequenceEqual(["<img src=x>"], document.QuerySelectorAll(element: "img").Outers());
		Assert.AreSequenceEqual(["<input/>"], document.QuerySelectorAll(element: "input").Outers());
	}

	[TestMethod]
	public void FindsImplicitlyClosedElements()
	{
		var document = LazyHtmlDocument.Parse("<ul><li>1<li>2<li>3</ul><p>a<p>b");

		Assert.AreSequenceEqual(["<li>1", "<li>2", "<li>3"], document.QuerySelectorAll(element: "li").Outers());
		Assert.AreSequenceEqual(["<p>a", "<p>b"], document.QuerySelectorAll(element: "p").Outers());
	}

	[TestMethod]
	public void ElementDoesNotFindTheStartTagThatImplicitlyClosedIt()
	{
		var paragraph = TestHelpers.FirstElement("<p>text<div>block</div>");

		Assert.IsFalse(paragraph.QuerySelectorAll(element: "div").MoveNext());
	}

	[TestMethod]
	public void SkipsMarkupInsideComments()
	{
		var document = LazyHtmlDocument.Parse("<div><!-- <a>x</a> --><a>y</a></div>");

		Assert.AreSequenceEqual(["<a>y</a>"], document.QuerySelectorAll(element: "a").Outers());
	}

	[TestMethod]
	public void SkipsDoctypeAndProcessingInstructions()
	{
		var document = LazyHtmlDocument.Parse("<!DOCTYPE html><?xml version=\"1.0\"?><html></html>");

		Assert.AreSequenceEqual(["<html></html>"], document.QuerySelectorAll(element: "html").Outers());
		Assert.IsFalse(document.QuerySelectorAll(element: "DOCTYPE").MoveNext());
		Assert.IsFalse(document.QuerySelectorAll(element: "xml").MoveNext());
	}

	[TestMethod]
	public void DoesNotTreatAngleBracketsInAttributeValuesAsTags()
	{
		var document = LazyHtmlDocument.Parse("<a title=\"x<b>y\" data='<i>'>1</a><b>2</b>");

		Assert.AreSequenceEqual(["<b>2</b>"], document.QuerySelectorAll(element: "b").Outers());
		Assert.IsFalse(document.QuerySelectorAll(element: "i").MoveNext());
	}

	[TestMethod]
	public void DoesNotLookInsideRawTextContent()
	{
		var document = LazyHtmlDocument.Parse("<script>if (a<b) { x = '<i>'; }</script><style>a>b{}</style><textarea><p></textarea><title><em></title><b>real</b>");

		Assert.AreSequenceEqual(["<b>real</b>"], document.QuerySelectorAll(element: "b").Outers());
		Assert.IsFalse(document.QuerySelectorAll(element: "i").MoveNext());
		Assert.IsFalse(document.QuerySelectorAll(element: "p").MoveNext());
		Assert.IsFalse(document.QuerySelectorAll(element: "em").MoveNext());
	}

	[TestMethod]
	public void FindsRawTextElementsThemselves()
	{
		var document = LazyHtmlDocument.Parse("<head><title>T</title><script>1</script></head><body><script>2</script></body>");

		Assert.AreSequenceEqual(["<script>1</script>", "<script>2</script>"], document.QuerySelectorAll(element: "script").Outers());
		Assert.AreSequenceEqual(["<title>T</title>"], document.QuerySelectorAll(element: "title").Outers());
	}

	[TestMethod]
	public void RawTextElementHasNoDescendants()
	{
		var script = TestHelpers.FirstElement("<script><b>not markup</b></script><b>after</b>");

		Assert.IsFalse(script.QuerySelectorAll(element: "b").MoveNext());
	}

	[TestMethod]
	public void SelfClosingRawTextElementDoesNotSwallowFollowingElements()
	{
		var document = LazyHtmlDocument.Parse("<script/><b>1</b>");

		Assert.AreSequenceEqual(["<b>1</b>"], document.QuerySelectorAll(element: "b").Outers());
	}

	[TestMethod]
	public void ReturnsEmptyWhenNothingMatches()
	{
		var document = LazyHtmlDocument.Parse("<html><body><p>text</p></body></html>");

		Assert.IsFalse(document.QuerySelectorAll(element: "a").MoveNext());
	}

	[TestMethod]
	public void EmptyAndDefaultDocumentsHaveNoMatches()
	{
		Assert.IsFalse(LazyHtmlDocument.Parse("").QuerySelectorAll(element: "a").MoveNext());
		Assert.IsFalse(default(LazyHtmlDocument).QuerySelectorAll(element: "a").MoveNext());
		Assert.IsFalse(LazyHtmlDocument.Parse("just text").QuerySelectorAll(element: "a").MoveNext());
	}

	[TestMethod]
	public void EnumeratorReturnsFalseRepeatedlyAfterEnd()
	{
		var elements = LazyHtmlDocument.Parse("<a></a>").QuerySelectorAll(element: "a");

		Assert.IsTrue(elements.MoveNext());
		Assert.IsFalse(elements.MoveNext());
		Assert.IsFalse(elements.MoveNext());
	}

	[TestMethod]
	public void EnumeratorCanBeIteratedWithForeach()
	{
		var document = LazyHtmlDocument.Parse("<a></a><div><a></a></div>");
		var count = 0;

		foreach (var element in document.QuerySelectorAll(element: "a"))
			count++;

		Assert.AreEqual(2, count);
	}

	[TestMethod]
	public void FoundElementsExposeTheirContent()
	{
		var document = LazyHtmlDocument.Parse("<div><a href=\"/1\">one</a><span><a href='/2'>two</a></span></div>");

		var links = document.QuerySelectorAll(element: "a").ToList();

		Assert.HasCount(2, links);
		Assert.AreEqual("/1", links[0].TryGetAttribute("href", out var first) ? first.Value : null);
		Assert.AreEqual("one", links[0].TextContent);
		Assert.AreEqual("/2", links[1].TryGetAttribute("href", out var second) ? second.Value : null);
		Assert.AreEqual("two", links[1].TextContent);
	}

	[TestMethod]
	public void UnclosedMatchIsFoundAndSearchContinuesInsideIt()
	{
		var document = LazyHtmlDocument.Parse("<div><a>1<a>2");

		Assert.AreSequenceEqual(["<a>1<a>2", "<a>2"], document.QuerySelectorAll(element: "a").Outers());
	}

	[TestMethod]
	public void QuerySelectorOnStringSegmentRespectsBounds()
	{
		var html = "<a>outside</a><div><a>inside</a></div><a>outside</a>";
		var segment = new Microsoft.Extensions.Primitives.StringSegment(html, html.IndexOf("<div>", StringComparison.Ordinal), "<div><a>inside</a></div>".Length);
		var document = LazyHtmlDocument.Parse(segment);

		Assert.AreSequenceEqual(["<a>inside</a>"], document.QuerySelectorAll(element: "a").Outers());
	}

	[TestMethod]
	public void EmptyElementThrows()
	{
		var document = LazyHtmlDocument.Parse("<a></a>");
		var element = TestHelpers.FirstElement("<a></a>");

		Assert.AreEqual("element", Assert.ThrowsExactly<ArgumentException>(() => document.QuerySelectorAll(element: "")).ParamName);
		Assert.AreEqual("element", Assert.ThrowsExactly<ArgumentException>(() => element.QuerySelectorAll(element: "")).ParamName);
	}

	[TestMethod]
	public void NullOrEmptyAttributeNameThrows()
	{
		var document = LazyHtmlDocument.Parse("<a></a>");

		Assert.ThrowsExactly<ArgumentNullException>(() => document.QuerySelectorAll(element: "a", attributes: Attributes((null!, "x"))));
		Assert.ThrowsExactly<ArgumentException>(() => document.QuerySelectorAll(element: "a", attributes: Attributes(("", "x"))));
		Assert.ThrowsExactly<ArgumentException>(() => document.QuerySelectorAll(attributes: Attributes(("id", "1"), ("", "x"))));
	}

	[TestMethod]
	public void NoFiltersMatchEveryElement()
	{
		var document = LazyHtmlDocument.Parse("<html><head><title>T</title></head><body><p>a<br>b</p><script>x</script></body></html>");

		Assert.AreSequenceEqual(["html", "head", "title", "body", "p", "br", "script"], document.QuerySelectorAll().Names());
		Assert.AreSequenceEqual(document.QuerySelectorAll().Names(), document.QuerySelectorAll(element: null).Names());
	}

	[TestMethod]
	public void ElementWithNoFiltersMatchesAllDescendants()
	{
		var body = TestHelpers.FirstElement("<body><div><p><b>x</b></p></div><hr></body><footer></footer>");

		Assert.AreSequenceEqual(["div", "p", "b", "hr"], body.QuerySelectorAll().Names());
		Assert.AreSequenceEqual(body.QuerySelectorAll().Names(), body.QuerySelectorAll(element: null).Names());
	}

	[TestMethod]
	public void MatchesByAttributeWithoutElement()
	{
		var document = LazyHtmlDocument.Parse("<div id=\"a\"><p id=\"b\">1</p><span id=\"a\">2</span></div><a id=a>3</a>");

		Assert.AreSequenceEqual(["div", "span", "a"], document.QuerySelectorAll(attributes: [new("id", "a")]).Names());
	}

	[TestMethod]
	public void MatchesByElementAndAttribute()
	{
		var document = LazyHtmlDocument.Parse("<a class=\"btn\">1</a><button class=\"btn\">2</button><a class=\"link\">3</a><div><a class=\"btn\">4</a></div>");

		Assert.AreSequenceEqual(["<a class=\"btn\">1</a>", "<a class=\"btn\">4</a>"], document.QuerySelectorAll(element: "a", attributes: [new("class", "btn")]).Outers());
	}

	[TestMethod]
	public void AllAttributesMustMatch()
	{
		var document = LazyHtmlDocument.Parse(
			"<input type=\"text\" name=\"q\">" +
			"<input type=\"text\" name=\"other\">" +
			"<input type=\"hidden\" name=\"q\">" +
			"<input name=\"q\" type=\"text\" id=\"search\">");

		var matches = document.QuerySelectorAll(element: "input", attributes: [new("type", "text"), new("name", "q")]).Outers();

		Assert.AreSequenceEqual(["<input type=\"text\" name=\"q\">", "<input name=\"q\" type=\"text\" id=\"search\">"], matches);
	}

	[TestMethod]
	public void MissingAttributeDoesNotMatch()
	{
		var document = LazyHtmlDocument.Parse("<a href=\"/\">1</a><a>2</a>");

		Assert.IsFalse(document.QuerySelectorAll(element: "a", attributes: Attributes(("target", "_blank"))).MoveNext());
	}

	[TestMethod]
	public void DifferentAttributeValueDoesNotMatch()
	{
		var document = LazyHtmlDocument.Parse("<a rel=\"author\">1</a><a rel=\"authors\">2</a><a rel=\"noauthor\">3</a>");

		Assert.AreSequenceEqual(["<a rel=\"author\">1</a>"], document.QuerySelectorAll(element: "a", attributes: Attributes(("rel", "author"))).Outers());
	}

	[TestMethod]
	public void AttributeNameIsCaseInsensitiveButValueIsCaseSensitive()
	{
		var document = LazyHtmlDocument.Parse("<a CLASS=\"Btn\">1</a><a class=\"btn\">2</a><a Class=\"Btn\">3</a>");

		Assert.AreSequenceEqual(["<a CLASS=\"Btn\">1</a>", "<a Class=\"Btn\">3</a>"], document.QuerySelectorAll(element: "a", attributes: Attributes(("class", "Btn"))).Outers());
		Assert.AreSequenceEqual(["<a class=\"btn\">2</a>"], document.QuerySelectorAll(element: "a", attributes: Attributes(("CLASS", "btn"))).Outers());
	}

	[TestMethod]
	public void QuotingStyleDoesNotAffectMatching()
	{
		var document = LazyHtmlDocument.Parse("<a class=\"x\">1</a><a class='x'>2</a><a class=x>3</a><a  class=\"x\"  id=y>4</a>");

		Assert.HasCount(4, document.QuerySelectorAll(element: "a", attributes: Attributes(("class", "x"))).ToList());
	}

	[TestMethod]
	public void BareAttributeMatchesEmptyValue()
	{
		var document = LazyHtmlDocument.Parse("<input disabled><input disabled=\"\"><input disabled=\"disabled\"><input>");

		Assert.AreSequenceEqual(["<input disabled>", "<input disabled=\"\">"], document.QuerySelectorAll(element: "input", attributes: Attributes(("disabled", ""))).Outers());
		Assert.AreSequenceEqual(["<input disabled=\"disabled\">"], document.QuerySelectorAll(element: "input", attributes: Attributes(("disabled", "disabled"))).Outers());
	}

	[TestMethod]
	public void FirstOfDuplicateAttributesWins()
	{
		var document = LazyHtmlDocument.Parse("<a class=\"x\" class=\"y\">1</a>");

		Assert.IsTrue(document.QuerySelectorAll(element: "a", attributes: Attributes(("class", "x"))).MoveNext());
		Assert.IsFalse(document.QuerySelectorAll(element: "a", attributes: Attributes(("class", "y"))).MoveNext());
	}

	[TestMethod]
	public void AttributeValueWithMarkupCharactersIsMatched()
	{
		var document = LazyHtmlDocument.Parse("<a title=\"a<b>c\">1</a><a title=\"x\">2</a>");

		Assert.AreSequenceEqual(["<a title=\"a<b>c\">1</a>"], document.QuerySelectorAll(element: "a", attributes: Attributes(("title", "a<b>c"))).Outers());
	}

	[TestMethod]
	public void AttributeValueIsMatchedAsWritten()
	{
		var document = LazyHtmlDocument.Parse("<a href=\"/x?a=1&amp;b=2\">1</a><a href=\"/x?a=1&b=2\">2</a>");

		Assert.AreSequenceEqual(["<a href=\"/x?a=1&amp;b=2\">1</a>"], document.QuerySelectorAll(element: "a", attributes: Attributes(("href", "/x?a=1&amp;b=2"))).Outers());
		Assert.AreSequenceEqual(["<a href=\"/x?a=1&b=2\">2</a>"], document.QuerySelectorAll(element: "a", attributes: Attributes(("href", "/x?a=1&b=2"))).Outers());
	}

	[TestMethod]
	public void AttributesOfDescendantsDoNotMatchTheAncestor()
	{
		var document = LazyHtmlDocument.Parse("<div><p class=\"x\">1</p></div>");

		Assert.IsFalse(document.QuerySelectorAll(element: "div", attributes: Attributes(("class", "x"))).MoveNext());
		Assert.AreSequenceEqual(["p"], document.QuerySelectorAll(attributes: Attributes(("class", "x"))).Names());
	}

	[TestMethod]
	public void AttributeMatchesAreFoundInsideOtherMatches()
	{
		var document = LazyHtmlDocument.Parse("<div class=\"c\" id=\"1\"><div class=\"c\" id=\"2\"><span class=\"c\" id=\"3\"></span></div></div>");

		var ids = document.QuerySelectorAll(attributes: Attributes(("class", "c"))).ToList().ConvertAll(e => e.TryGetAttribute("id", out var id) ? id.Value : "");

		Assert.AreSequenceEqual(["1", "2", "3"], ids);
	}

	[TestMethod]
	public void AttributeQueryDoesNotLookInsideRawText()
	{
		var document = LazyHtmlDocument.Parse("<script><a class=\"x\">fake</a></script><a class=\"x\">real</a>");

		Assert.AreSequenceEqual(["<a class=\"x\">real</a>"], document.QuerySelectorAll(attributes: Attributes(("class", "x"))).Outers());
	}

	[TestMethod]
	public void AttributeQueryOnElementSearchesOnlyItsContent()
	{
		var document = LazyHtmlDocument.Parse("<a class=\"x\">0</a><div><a class=\"x\">1</a><p><a class=\"y\">2</a></p></div><a class=\"x\">3</a>");
		var div = document.Elements().ToList()[1];

		Assert.AreSequenceEqual(["<a class=\"x\">1</a>"], div.QuerySelectorAll(element: "a", attributes: Attributes(("class", "x"))).Outers());
	}

	[TestMethod]
	public void MatchesByIdAttribute()
	{
		var document = LazyHtmlDocument.Parse("<div id=\"a\"><p id=\"b\">1</p><span id=\"a\">2</span></div><a id=a>3</a>");

		Assert.AreSequenceEqual(["div", "span", "a"], document.QuerySelectorAll(attributes: [new("id", "a")]).Names());
	}

	[TestMethod]
	public void MatchesByElementAndIdAttribute()
	{
		var document = LazyHtmlDocument.Parse("<div id=\"main\">1</div><section id=\"main\">2</section><div id=\"other\">3</div>");

		Assert.AreSequenceEqual(["<div id=\"main\">1</div>"], document.QuerySelectorAll(element: "div", attributes: [new("id", "main")]).Outers());
	}

	[TestMethod]
	public void IdMustMatchTheWholeValueCaseSensitively()
	{
		var document = LazyHtmlDocument.Parse("<div id=\"Main\">1</div><div id=\"main-content\">2</div><div id=\"main\">3</div><div id=\" main\">4</div>");

		Assert.AreSequenceEqual(["<div id=\"main\">3</div>"], document.QuerySelectorAll(attributes: [new("id", "main")]).Outers());
	}

	[TestMethod]
	public void MissingIdDoesNotMatch()
	{
		var document = LazyHtmlDocument.Parse("<div class=\"main\">1</div><div>2</div>");

		Assert.IsFalse(document.QuerySelectorAll(attributes: [new("id", "main")]).MoveNext());
	}

	[TestMethod]
	public void MatchesByClassName()
	{
		var document = LazyHtmlDocument.Parse(
			"<a class=\"btn\">1</a>" +
			"<a class=\"btn primary\">2</a>" +
			"<a class=\"primary btn\">3</a>" +
			"<a class=\"x btn y\">4</a>" +
			"<a class=\"btn-large\">5</a>" +
			"<a class=\"nobtn\">6</a>" +
			"<a class=\"primary\">7</a>" +
			"<a>8</a>");

		Assert.AreSequenceEqual(["1", "2", "3", "4"], document.QuerySelectorAll(classNames: "btn").ToList().ConvertAll(e => e.InnerSpan.ToString()));
	}

	[TestMethod]
	public void ClassNameMatchesAcrossAnyWhitespaceSeparators()
	{
		var document = LazyHtmlDocument.Parse("<a class=\"x\tbtn\n y\">1</a><a class=\"  btn  \">2</a><a class=\"x\r\n\fbtn\">3</a><a class='btn'>4</a><a class=btn>5</a>");

		Assert.HasCount(5, document.QuerySelectorAll(classNames: "btn").ToList());
	}

	[TestMethod]
	public void ClassNameIsCaseSensitive()
	{
		var document = LazyHtmlDocument.Parse("<a class=\"Btn\">1</a><a class=\"btn\">2</a><a class=\"BTN\">3</a>");

		Assert.AreSequenceEqual(["<a class=\"btn\">2</a>"], document.QuerySelectorAll(classNames: "btn").Outers());
	}

	[TestMethod]
	public void ClassNameMatchesTheDecodedAttributeValue()
	{
		var document = LazyHtmlDocument.Parse("<a class=\"a&amp;b\">1</a><a class=\"a&b\">2</a><a class=\"&#98;tn&#32;x\">3</a><a class=\"btn\">4</a><a class=\"x&nbsp;btn\">5</a>");

		Assert.AreSequenceEqual(["1", "2"], document.QuerySelectorAll(classNames: "a&b").Inners());
		Assert.AreSequenceEqual(["3", "4"], document.QuerySelectorAll(classNames: "btn").Inners());
		Assert.AreSequenceEqual(["3"], document.QuerySelectorAll(classNames: new[] { "x", "btn" }).Inners());
		Assert.AreSequenceEqual(["5"], document.QuerySelectorAll(classNames: "x\u00a0btn").Inners());
		Assert.IsFalse(document.QuerySelectorAll(classNames: "a&amp;b").MoveNext());
	}

	[TestMethod]
	public void MissingOrEmptyClassAttributeDoesNotMatch()
	{
		var document = LazyHtmlDocument.Parse("<a>1</a><a class>2</a><a class=\"\">3</a><a class=\"  \">4</a><a id=\"btn\">5</a>");

		Assert.IsFalse(document.QuerySelectorAll(classNames: "btn").MoveNext());
	}

	[TestMethod]
	public void MatchesByElementAndClassName()
	{
		var document = LazyHtmlDocument.Parse("<a class=\"btn\">1</a><button class=\"btn\">2</button><div><a class=\"big btn\">3</a></div>");

		Assert.AreSequenceEqual(["<a class=\"btn\">1</a>", "<a class=\"big btn\">3</a>"], document.QuerySelectorAll(element: "a", classNames: "btn").Outers());
	}

	[TestMethod]
	public void CombinesAllFilters()
	{
		var document = LazyHtmlDocument.Parse(
			"<a id=\"x\" class=\"btn\" href=\"/\">1</a>" +
			"<a id=\"x\" class=\"btn\" href=\"/other\">2</a>" +
			"<a id=\"y\" class=\"btn\" href=\"/\">3</a>" +
			"<a id=\"x\" class=\"link\" href=\"/\">4</a>" +
			"<span id=\"x\" class=\"btn\" href=\"/\">5</span>" +
			"<div><a href=\"/\" class=\"big btn\" id=\"x\">6</a></div>");

		var matches = document.QuerySelectorAll(element: "a", classNames: "btn", attributes: [new("id", "x"), new("href", "/")]).ToList().ConvertAll(e => e.InnerSpan.ToString());

		Assert.AreSequenceEqual(["1", "6"], matches);
	}

	[TestMethod]
	public void IdAttributeAndClassNameOfDescendantsDoNotMatchTheAncestor()
	{
		var document = LazyHtmlDocument.Parse("<div><p id=\"x\" class=\"c\">1</p></div>");

		Assert.IsFalse(document.QuerySelectorAll(element: "div", attributes: [new("id", "x")]).MoveNext());
		Assert.IsFalse(document.QuerySelectorAll(element: "div", classNames: "c").MoveNext());
	}

	[TestMethod]
	public void IdAttributeAndClassNameQueriesDoNotLookInsideRawText()
	{
		var document = LazyHtmlDocument.Parse("<script><a id=\"x\" class=\"c\">fake</a></script><a id=\"x\" class=\"c\">real</a>");

		Assert.AreSequenceEqual(["<a id=\"x\" class=\"c\">real</a>"], document.QuerySelectorAll(attributes: [new("id", "x")]).Outers());
		Assert.AreSequenceEqual(["<a id=\"x\" class=\"c\">real</a>"], document.QuerySelectorAll(classNames: "c").Outers());
	}

	[TestMethod]
	public void EmptyIdAttributeMatchesOnlyPresentEmptyIds()
	{
		var document = LazyHtmlDocument.Parse("<div><a id>1</a><a id=\"\">2</a><a>3</a><a id=\"x\">4</a></div>");
		var div = document.Elements().ToList()[0];

		Assert.AreSequenceEqual(["<a id>1</a>", "<a id=\"\">2</a>"], document.QuerySelectorAll(attributes: [new("id", "")]).Outers());
		Assert.AreSequenceEqual(["<a id>1</a>", "<a id=\"\">2</a>"], div.QuerySelectorAll(attributes: [new("id", "")]).Outers());
	}

	[TestMethod]
	public void MatchesByMultipleClassNames()
	{
		var document = LazyHtmlDocument.Parse(
			"<a class=\"btn primary\">1</a>" +
			"<a class=\"primary btn\">2</a>" +
			"<a class=\"x btn y primary z\">3</a>" +
			"<a class=\"btn\">4</a>" +
			"<a class=\"primary\">5</a>" +
			"<a class=\"btn-primary\">6</a>" +
			"<a class=\"btn Primary\">7</a>" +
			"<a>8</a>");

		Assert.AreSequenceEqual(["1", "2", "3"], document.QuerySelectorAll(classNames: new[] { "btn", "primary" }).Inners());
		Assert.AreSequenceEqual(["1", "2", "3"], document.QuerySelectorAll(classNames: new[] { "primary", "btn" }).Inners());
		Assert.AreSequenceEqual(["1", "2", "3", "4", "7"], document.QuerySelectorAll(classNames: new[] { "btn", "btn" }).Inners());
		Assert.AreSequenceEqual(["3"], document.QuerySelectorAll(classNames: new[] { "z", "btn", "x" }).Inners());
		Assert.IsFalse(document.QuerySelectorAll(classNames: new[] { "btn", "primary", "missing" }).MoveNext());
	}

	[TestMethod]
	public void MultipleClassNamesCombineWithElementAndAttributes()
	{
		var document = LazyHtmlDocument.Parse("<a id=\"x\" class=\"a b\">1</a><span id=\"x\" class=\"a b\">2</span><a id=\"y\" class=\"a b\">3</a><a id=\"x\" class=\"a\">4</a><div><a class=\"b a\" id=\"x\">5</a></div>");

		Assert.AreSequenceEqual(["1", "5"], document.QuerySelectorAll(element: "a", classNames: new[] { "a", "b" }, attributes: [new("id", "x")]).Inners());
	}

	[TestMethod]
	public void EmptyClassNamesDoNotFilter()
	{
		var document = LazyHtmlDocument.Parse("<a class=\"x\">1</a><a>2</a>");

		Assert.AreSequenceEqual(["1", "2"], document.QuerySelectorAll(classNames: StringValues.Empty).Inners());
		Assert.AreSequenceEqual(["1", "2"], document.QuerySelectorAll(classNames: default).Inners());
	}

	[TestMethod]
	public void EmptyOrMultipleClassNamesThrow()
	{
		var document = LazyHtmlDocument.Parse("<a></a>");

		Assert.ThrowsExactly<ArgumentException>(() => document.QuerySelectorAll(classNames: ""));
		Assert.ThrowsExactly<ArgumentException>(() => document.QuerySelectorAll(classNames: "a b"));
		Assert.ThrowsExactly<ArgumentException>(() => document.QuerySelectorAll(classNames: " a"));
		Assert.ThrowsExactly<ArgumentException>(() => document.QuerySelectorAll(classNames: "a\t"));
		Assert.AreEqual("classNames", Assert.ThrowsExactly<ArgumentException>(() => document.QuerySelectorAll(classNames: new[] { "a", "" })).ParamName);
		Assert.AreEqual("classNames", Assert.ThrowsExactly<ArgumentException>(() => document.QuerySelectorAll(classNames: new[] { "a", "b c" })).ParamName);
		Assert.AreEqual("classNames", Assert.ThrowsExactly<ArgumentException>(() => document.QuerySelectorAll(classNames: new string?[] { "a", null })).ParamName);
	}

	[TestMethod]
	public void AttributeQueryMatchesVisitorBasedSearch()
	{
		var html =
			"<!DOCTYPE html><html lang=\"en\"><head><title>T</title><meta charset=\"utf-8\"><link rel=\"icon\" href=\"/i\"></head>" +
			"<body class=\"page\"><div class=\"a\" id='x'><p class=\"a\">text<br class=\"a\">more <b>bold</b></p>" +
			"<a href=\"/1\" rel=\"author\" class=\"btn\">1</a><a href=\"/2\" class=\"btn\">2</a><a href=\"/3\" rel=\"author\">3</a>" +
			"<!-- <a class=\"btn\"> --><ul><li class=a>1<li>2</ul><script>if (a<b) {}</script><div><p><a class=\"btn\" rel=\"author\">nested</a></p></div></div></body></html>";
		var document = LazyHtmlDocument.Parse(html);

		var queries = new (string? Element, (string, string)[] Attributes)[]
		{
			(null, new[] { ("class", "a") }),
			("a", new[] { ("class", "btn") }),
			("a", new[] { ("rel", "author") }),
			("a", new[] { ("class", "btn"), ("rel", "author") }),
			(null, new[] { ("rel", "author"), ("class", "btn") }),
			("p", new[] { ("class", "a") }),
			(null, new[] { ("href", "/2") }),
			("li", new[] { ("class", "a") }),
			(null, new[] { ("class", "zzz") }),
			("zzz", new[] { ("class", "a") }),
		};

		foreach (var (element, attributes) in queries)
		{
			var visitor = new CollectingVisitor(element, attributes: attributes);
			visitor.VisitDocument(document);

			Assert.AreSequenceEqual(visitor.Outers, document.QuerySelectorAll(element: element, attributes: Attributes(attributes)).Outers(), $"Mismatch for <{element}> {string.Join(' ', attributes)}.");
		}
	}

	[TestMethod]
	public void IdAttributeAndClassNameQueryMatchesVisitorBasedSearch()
	{
		var html =
			"<!DOCTYPE html><html><head><title>T</title></head>" +
			"<body class=\"page dark\" id=\"top\"><div class=\"a\" id='x'><p class=\"a b\">text<br class=\"b\">more <b class=\"a\">bold</b></p>" +
			"<a href=\"/1\" id=\"first\" class=\"btn a\">1</a><a href=\"/2\" class=\"btn\tb\">2</a><a href=\"/3\" class=\"a-b\" id=\"x\">3</a>" +
			"<!-- <a class=\"a\" id=\"x\"> --><ul><li class=\"a\" id=\"x\">1<li>2</ul><script><b class=\"a\" id=\"x\"></script>" +
			"<div><p><a class=\"a b btn\" id=\"nested\" href=\"/1\">nested</a></p></div></div></body></html>";
		var document = LazyHtmlDocument.Parse(html);

		var queries = new (string? Element, string[] ClassNames, (string, string)[] Attributes)[]
		{
			(null, [], [("id", "x")]),
			(null, ["a"], []),
			(null, ["b"], []),
			(null, ["btn"], []),
			("a", ["btn"], []),
			("a", [], [("id", "x")]),
			(null, ["a"], [("id", "x")]),
			(null, ["a"], [("href", "/1")]),
			("a", ["btn"], [("id", "nested"), ("href", "/1")]),
			("li", ["a"], [("id", "x")]),
			(null, ["a-b"], []),
			(null, ["dark"], [("id", "top")]),
			(null, [], [("id", "missing")]),
			(null, ["missing"], []),
			("p", [], [("id", "x")]),
			(null, ["a", "b"], []),
			(null, ["b", "a"], []),
			(null, ["btn", "a"], []),
			("a", ["a", "b", "btn"], []),
			("a", ["btn", "b"], [("href", "/2")]),
			(null, ["page", "dark"], [("id", "top")]),
			(null, ["a", "missing"], []),
			(null, ["a", "a"], [("id", "x")]),
		};

		foreach (var (element, classNames, attributes) in queries)
		{
			var visitor = new CollectingVisitor(element, classNames, attributes);
			visitor.VisitDocument(document);

			Assert.AreSequenceEqual(visitor.Outers, document.QuerySelectorAll(element: element, classNames: classNames, attributes: Attributes(attributes)).Outers(), $"Mismatch for <{element}> .{string.Join('.', classNames)} {string.Join(' ', attributes)}.");
		}
	}

	[TestMethod]
	public void MatchesVisitorBasedSearch()
	{
		var html =
			"<!DOCTYPE html><html><head><title>T</title><meta charset=\"utf-8\"></head>" +
			"<body><div class=\"a\" id='x'><p>text<br>more <b>bold</b></p>" +
			"<a href=\"/link?a=1&amp;b=2\" target=_blank>link</a><!-- c --><ul><li>1<li>2</ul>" +
			"<script>if (a<b) {}</script><div><p><a>nested</a></p></div></div></body></html>";
		var document = LazyHtmlDocument.Parse(html);

		foreach (var name in new[] { "a", "b", "p", "div", "li", "br", "meta", "script", "title", "html", "body", "zzz" })
		{
			var visitor = new CollectingVisitor(name);
			visitor.VisitDocument(document);

			Assert.AreSequenceEqual(visitor.Outers, document.QuerySelectorAll(element: name).Outers(), $"Mismatch for <{name}>.");
		}
	}

	private static KeyValuePair<string, string>[] Attributes(params (string Name, string Value)[] attributes)
		=> Array.ConvertAll(attributes, a => KeyValuePair.Create(a.Name, a.Value));

	private sealed class CollectingVisitor(string? element, string[]? classNames = null, params (string Name, string Value)[] attributes) : LazyHtmlVisitor
	{
		public List<string> Outers { get; } = new();

		public override void VisitElement(LazyHtmlElement candidate)
		{
			if ((element is null || candidate.NameSpan.Equals(element, StringComparison.OrdinalIgnoreCase)) && HasClasses(candidate) && HasAttributes(candidate))
				Outers.Add(candidate.OuterSpan.ToString());

			base.VisitElement(candidate);
		}

		// independent implementation on purpose: string-based splitting rather than the span split used by the query
		private bool HasClasses(LazyHtmlElement element)
		{
			if (classNames is null or [])
				return true;

			if (!element.TryGetAttribute("class", out var attribute))
				return false;

			var classes = (attribute.Value ?? "").Split(['\t', '\n', '\f', '\r', ' '], StringSplitOptions.RemoveEmptyEntries);
			return Array.TrueForAll(classNames, className => classes.Contains(className, StringComparer.Ordinal));
		}

		private bool HasAttributes(LazyHtmlElement element)
		{
			foreach (var (attributeName, value) in attributes)
			{
				if (!element.TryGetAttribute(attributeName, out var attribute) || !attribute.ValueSpan.SequenceEqual(value))
					return false;
			}

			return true;
		}
	}
}
