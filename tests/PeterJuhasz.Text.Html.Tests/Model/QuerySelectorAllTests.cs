using Microsoft.Extensions.Primitives;
using static PeterJuhasz.Text.Html.Tests.Model.TestHelpers;

namespace PeterJuhasz.Text.Html.Tests.Model;

[TestClass]
public sealed class QuerySelectorAllTests
{
	[TestMethod]
	public void FindsRootElementsInDocument()
	{
		var document = HtmlDocument.Parse("<a>1</a><b>2</b><a>3</a>");

		Assert.AreSequenceEqual(["<a>1</a>", "<a>3</a>"], document.QuerySelectorAll(element: "a").Outers());
	}

	[TestMethod]
	public void FindsDescendantsAtAnyDepthInDocumentOrder()
	{
		var document = HtmlDocument.Parse("<html><body><div><ul><li><a>1</a></li></ul><p><a>2</a></p></div><a>3</a></body></html>");

		Assert.AreSequenceEqual(["<a>1</a>", "<a>2</a>", "<a>3</a>"], document.QuerySelectorAll(element: "a").Outers());
	}

	[TestMethod]
	public void FindsMatchesNestedInsideMatches()
	{
		var document = HtmlDocument.Parse("<div id=1><div id=2><div id=3></div></div></div>");

		Assert.AreSequenceEqual(["1", "2", "3"], document.QuerySelectorAll(element: "div").Select(e => e.GetAttribute("id")?.Value).ToList());
	}

	[TestMethod]
	public void ElementSearchesOnlyItsOwnContentAndNotItself()
	{
		var document = HtmlDocument.Parse("<a>0</a><div><a>1</a><p><a>2</a></p></div><a>3</a>");
		var div = document.Elements().ElementAt(1);

		Assert.AreSequenceEqual(["<a>1</a>", "<a>2</a>"], div.QuerySelectorAll(element: "a").Outers());
		Assert.IsEmpty(div.QuerySelectorAll(element: "div"));
	}

	[TestMethod]
	public void ElementComparisonIsCaseInsensitive()
	{
		var document = HtmlDocument.Parse("<DIV>1</DIV><div>2</div><Div>3</Div>");

		Assert.HasCount(3, document.QuerySelectorAll(element: "div").ToList());
		Assert.HasCount(3, document.QuerySelectorAll(element: "DIV").ToList());
	}

	[TestMethod]
	public void DoesNotMatchNamesWithTheSamePrefix()
	{
		var document = HtmlDocument.Parse("<b>1</b><br><a>2</a><abbr>3</abbr>");

		Assert.AreSequenceEqual(["<b>1</b>"], document.QuerySelectorAll(element: "b").Outers());
		Assert.AreSequenceEqual(["<a>2</a>"], document.QuerySelectorAll(element: "a").Outers());
	}

	[TestMethod]
	public void ElementFilterIsIndependentOfNameAttribute()
	{
		var document = HtmlDocument.Parse("<input name=\"q\"><q name=\"input\"></q><input name=\"other\">");

		Assert.AreSequenceEqual(["<input name=\"q\">", "<input name=\"other\">"], document.QuerySelectorAll(element: "input").Outers());
		Assert.AreSequenceEqual(["<input name=\"q\">"], document.QuerySelectorAll(element: "input", attributes: Attributes(("name", "q"))).Outers());
	}

	[TestMethod]
	public void FindsVoidRawTextAndImplicitlyClosedElements()
	{
		var document = HtmlDocument.Parse("<ul><li>1<li>2</ul><p>a<br>b<br/></p><script>1</script><body><script>2</script></body>");

		Assert.AreSequenceEqual(["<li>1", "<li>2"], document.QuerySelectorAll(element: "li").Outers());
		Assert.HasCount(2, document.QuerySelectorAll(element: "br").ToList());
		Assert.AreSequenceEqual(["<script>1</script>", "<script>2</script>"], document.QuerySelectorAll(element: "script").Outers());
	}

	[TestMethod]
	public void DoesNotLookInsideRawTextOrComments()
	{
		var document = HtmlDocument.Parse("<script><b>fake</b></script><!-- <b>fake</b> --><b>real</b>");

		Assert.AreSequenceEqual(["<b>real</b>"], document.QuerySelectorAll(element: "b").Outers());
	}

	[TestMethod]
	public void NoFiltersMatchEveryDescendant()
	{
		var document = HtmlDocument.Parse("<html><head><title>T</title></head><body><p>a<br>b</p><script>x</script></body></html>");
		var body = document.Elements().First().Elements().ElementAt(1);

		Assert.AreSequenceEqual(["html", "head", "title", "body", "p", "br", "script"], document.QuerySelectorAll().Names());
		Assert.AreSequenceEqual(["p", "br", "script"], body.QuerySelectorAll().Names());
		Assert.AreSequenceEqual(document.QuerySelectorAll().Names(), document.QuerySelectorAll(element: null).Names());
		Assert.AreSequenceEqual(body.QuerySelectorAll().Names(), body.QuerySelectorAll(element: null).Names());
	}

	[TestMethod]
	public void ReturnsEmptyWhenNothingMatches()
	{
		Assert.IsEmpty(HtmlDocument.Parse("<html><body><p>text</p></body></html>").QuerySelectorAll(element: "a"));
		Assert.IsEmpty(HtmlDocument.Parse("").QuerySelectorAll(element: "a"));
		Assert.IsEmpty(HtmlDocument.Parse("just text").QuerySelectorAll());
	}

	[TestMethod]
	public void ResultCanBeEnumeratedRepeatedly()
	{
		var query = HtmlDocument.Parse("<a></a><div><a></a></div>").QuerySelectorAll(element: "a");

		Assert.HasCount(2, query.ToList());
		Assert.HasCount(2, query.ToList());
	}

	[TestMethod]
	public void MatchesByAttributeWithoutElement()
	{
		var document = HtmlDocument.Parse("<div id=\"a\"><p id=\"b\">1</p><span id=\"a\">2</span></div><a id=a>3</a>");

		Assert.AreSequenceEqual(["div", "span", "a"], document.QuerySelectorAll(attributes: Attributes(("id", "a"))).Names());
	}

	[TestMethod]
	public void MatchesByElementAndAttribute()
	{
		var document = HtmlDocument.Parse("<a class=\"btn\">1</a><button class=\"btn\">2</button><a class=\"link\">3</a><div><a class=\"btn\">4</a></div>");

		Assert.AreSequenceEqual(["<a class=\"btn\">1</a>", "<a class=\"btn\">4</a>"], document.QuerySelectorAll(element: "a", attributes: Attributes(("class", "btn"))).Outers());
	}

	[TestMethod]
	public void AllAttributesMustMatch()
	{
		var document = HtmlDocument.Parse(
			"<input type=\"text\" name=\"q\">" +
			"<input type=\"text\" name=\"other\">" +
			"<input type=\"hidden\" name=\"q\">" +
			"<input name=\"q\" type=\"text\" id=\"search\">");

		var matches = document.QuerySelectorAll(element: "input", attributes: Attributes(("type", "text"), ("name", "q"))).Outers();

		Assert.AreSequenceEqual(["<input type=\"text\" name=\"q\">", "<input name=\"q\" type=\"text\" id=\"search\">"], matches);
	}

	[TestMethod]
	public void MissingOrDifferentAttributeValueDoesNotMatch()
	{
		var document = HtmlDocument.Parse("<a rel=\"author\">1</a><a rel=\"authors\">2</a><a>3</a>");

		Assert.AreSequenceEqual(["<a rel=\"author\">1</a>"], document.QuerySelectorAll(element: "a", attributes: Attributes(("rel", "author"))).Outers());
		Assert.IsEmpty(document.QuerySelectorAll(element: "a", attributes: Attributes(("target", "_blank"))));
	}

	[TestMethod]
	public void AttributeNameIsCaseInsensitiveButValueIsCaseSensitive()
	{
		var document = HtmlDocument.Parse("<a CLASS=\"Btn\">1</a><a class=\"btn\">2</a><a Class=\"Btn\">3</a>");

		Assert.AreSequenceEqual(["1", "3"], document.QuerySelectorAll(element: "a", attributes: Attributes(("class", "Btn"))).Inners());
		Assert.AreSequenceEqual(["2"], document.QuerySelectorAll(element: "a", attributes: Attributes(("CLASS", "btn"))).Inners());
	}

	[TestMethod]
	public void AttributeValueIsMatchedAsWritten()
	{
		var document = HtmlDocument.Parse("<a href=\"/x?a=1&amp;b=2\">1</a><a href=\"/x?a=1&b=2\">2</a>");

		Assert.AreSequenceEqual(["1"], document.QuerySelectorAll(element: "a", attributes: Attributes(("href", "/x?a=1&amp;b=2"))).Inners());
		Assert.AreSequenceEqual(["2"], document.QuerySelectorAll(element: "a", attributes: Attributes(("href", "/x?a=1&b=2"))).Inners());
	}

	[TestMethod]
	public void BareAttributeMatchesEmptyValue()
	{
		var document = HtmlDocument.Parse("<input disabled><input disabled=\"\"><input disabled=\"disabled\"><input>");

		Assert.AreSequenceEqual(["<input disabled>", "<input disabled=\"\">"], document.QuerySelectorAll(element: "input", attributes: Attributes(("disabled", ""))).Outers());
		Assert.AreSequenceEqual(["<input disabled=\"disabled\">"], document.QuerySelectorAll(element: "input", attributes: Attributes(("disabled", "disabled"))).Outers());
	}

	[TestMethod]
	public void FirstOfDuplicateAttributesWins()
	{
		var document = HtmlDocument.Parse("<a class=\"x\" class=\"y\">1</a>");

		Assert.HasCount(1, document.QuerySelectorAll(element: "a", attributes: Attributes(("class", "x"))).ToList());
		Assert.IsEmpty(document.QuerySelectorAll(element: "a", attributes: Attributes(("class", "y"))));
	}

	[TestMethod]
	public void AttributesOfDescendantsDoNotMatchTheAncestor()
	{
		var document = HtmlDocument.Parse("<div><p class=\"x\" id=\"y\">1</p></div>");

		Assert.IsEmpty(document.QuerySelectorAll(element: "div", attributes: Attributes(("class", "x"))));
		Assert.IsEmpty(document.QuerySelectorAll(element: "div", attributes: Attributes(("id", "y"))));
		Assert.IsEmpty(document.QuerySelectorAll(element: "div", classNames: "x"));
		Assert.AreSequenceEqual(["p"], document.QuerySelectorAll(attributes: Attributes(("class", "x"))).Names());
	}

	[TestMethod]
	public void MatchesByIdAttribute()
	{
		var document = HtmlDocument.Parse("<div id=\"a\"><p id=\"b\">1</p><span id=\"a\">2</span></div><a id=a>3</a>");

		Assert.AreSequenceEqual(["div", "span", "a"], document.QuerySelectorAll(attributes: Attributes(("id", "a"))).Names());
		Assert.AreSequenceEqual(["span"], document.QuerySelectorAll(element: "span", attributes: Attributes(("id", "a"))).Names());
	}

	[TestMethod]
	public void IdMustMatchTheWholeValueCaseSensitively()
	{
		var document = HtmlDocument.Parse("<div id=\"Main\">1</div><div id=\"main-content\">2</div><div id=\"main\">3</div><div id=\" main\">4</div><div id>5</div><div class=\"main\">6</div>");

		Assert.AreSequenceEqual(["<div id=\"main\">3</div>"], document.QuerySelectorAll(attributes: Attributes(("id", "main"))).Outers());
	}

	[TestMethod]
	public void EmptyIdAttributeMatchesOnlyPresentEmptyIds()
	{
		var document = HtmlDocument.Parse("<div><a id>1</a><a id=\"\">2</a><a>3</a><a id=\"x\">4</a></div>");
		var div = document.Elements().First();

		Assert.AreSequenceEqual(["<a id>1</a>", "<a id=\"\">2</a>"], document.QuerySelectorAll(attributes: Attributes(("id", ""))).Outers());
		Assert.AreSequenceEqual(["<a id>1</a>", "<a id=\"\">2</a>"], div.QuerySelectorAll(attributes: Attributes(("id", ""))).Outers());
	}

	[TestMethod]
	public void MatchesByClassName()
	{
		var document = HtmlDocument.Parse(
			"<a class=\"btn\">1</a>" +
			"<a class=\"btn primary\">2</a>" +
			"<a class=\"primary btn\">3</a>" +
			"<a class=\"x\tbtn\n y\">4</a>" +
			"<a class=\"btn-large\">5</a>" +
			"<a class=\"nobtn\">6</a>" +
			"<a class=\"Btn\">7</a>" +
			"<a class>8</a>" +
			"<a class=\"\">9</a>" +
			"<a id=\"btn\">10</a>" +
			"<a>11</a>");

		Assert.AreSequenceEqual(["1", "2", "3", "4"], document.QuerySelectorAll(classNames: "btn").Inners());
	}

	[TestMethod]
	public void MatchesByMultipleClassNames()
	{
		var document = HtmlDocument.Parse(
			"<a class=\"btn primary\">1</a>" +
			"<a class=\"primary btn\">2</a>" +
			"<a class=\"x btn y primary z\">3</a>" +
			"<a class=\"btn\">4</a>" +
			"<a class=\"primary\">5</a>" +
			"<a class=\"btn-primary\">6</a>" +
			"<a class=\"btn Primary\">7</a>" +
			"<a>8</a>");

		Assert.AreSequenceEqual(["1", "2", "3"], document.QuerySelectorAll(classNames: new[] { "btn", "primary" }).Inners());
		Assert.AreSequenceEqual(["1", "2", "3"], document.QuerySelectorAll(classNames: new StringValues(["primary", "btn"])).Inners());
		Assert.AreSequenceEqual(["1", "2", "3", "4", "7"], document.QuerySelectorAll(classNames: new[] { "btn", "btn" }).Inners());
		Assert.AreSequenceEqual(["3"], document.QuerySelectorAll(classNames: new[] { "z", "btn", "x" }).Inners());
		Assert.IsEmpty(document.QuerySelectorAll(classNames: new[] { "btn", "primary", "missing" }));
	}

	[TestMethod]
	public void MultipleClassNamesCombineWithElementAndAttributes()
	{
		var document = HtmlDocument.Parse("<a id=\"x\" class=\"a b\">1</a><span id=\"x\" class=\"a b\">2</span><a id=\"y\" class=\"a b\">3</a><a id=\"x\" class=\"a\">4</a><div><a class=\"b a\" id=\"x\">5</a></div>");

		Assert.AreSequenceEqual(["1", "5"], document.QuerySelectorAll(element: "a", classNames: new[] { "a", "b" }, attributes: Attributes(("id", "x"))).Inners());
	}

	[TestMethod]
	public void EmptyClassNamesDoNotFilter()
	{
		var document = HtmlDocument.Parse("<a class=\"x\">1</a><a>2</a>");

		Assert.AreSequenceEqual(["1", "2"], document.QuerySelectorAll(classNames: default).Inners());
		Assert.AreSequenceEqual(["1", "2"], document.QuerySelectorAll(classNames: StringValues.Empty).Inners());
		Assert.AreSequenceEqual(["1", "2"], document.QuerySelectorAll(classNames: (string?)null).Inners());
		Assert.AreSequenceEqual(["1", "2"], document.QuerySelectorAll(classNames: Array.Empty<string>()).Inners());
	}

	[TestMethod]
	public void CombinesAllFilters()
	{
		var document = HtmlDocument.Parse(
			"<a id=\"x\" class=\"btn\" href=\"/\">1</a>" +
			"<a id=\"x\" class=\"btn\" href=\"/other\">2</a>" +
			"<a id=\"y\" class=\"btn\" href=\"/\">3</a>" +
			"<a id=\"x\" class=\"link\" href=\"/\">4</a>" +
			"<span id=\"x\" class=\"btn\" href=\"/\">5</span>" +
			"<div><a href=\"/\" class=\"big btn\" id=\"x\">6</a></div>");

		var matches = document.QuerySelectorAll(element: "a", classNames: "btn", attributes: Attributes(("id", "x"), ("href", "/"))).Inners();

		Assert.AreSequenceEqual(["1", "6"], matches);
	}

	[TestMethod]
	public void ElementQueriesSearchOnlyItsContent()
	{
		var document = HtmlDocument.Parse("<a id=\"x\" class=\"c\">0</a><div><a id=\"x\" class=\"c\">1</a><p><a class=\"y\">2</a></p></div><a id=\"x\" class=\"c\">3</a>");
		var div = document.Elements().ElementAt(1);

		Assert.AreSequenceEqual(["1"], div.QuerySelectorAll(attributes: Attributes(("id", "x"))).Inners());
		Assert.AreSequenceEqual(["1"], div.QuerySelectorAll(classNames: "c").Inners());
		Assert.AreSequenceEqual(["1"], div.QuerySelectorAll(element: "a", attributes: Attributes(("class", "c"))).Inners());
	}

	[TestMethod]
	public void InvalidArgumentsThrowEagerly()
	{
		var document = HtmlDocument.Parse("<a></a>");
		var element = document.Elements().First();

		Assert.AreEqual("element", Assert.ThrowsExactly<ArgumentException>(() => document.QuerySelectorAll(element: "")).ParamName);
		Assert.AreEqual("element", Assert.ThrowsExactly<ArgumentException>(() => element.QuerySelectorAll(element: "")).ParamName);
		Assert.ThrowsExactly<ArgumentException>(() => document.QuerySelectorAll(classNames: ""));
		Assert.ThrowsExactly<ArgumentException>(() => document.QuerySelectorAll(classNames: "a b"));
		Assert.ThrowsExactly<ArgumentException>(() => element.QuerySelectorAll(classNames: " a"));
		Assert.AreEqual("classNames", Assert.ThrowsExactly<ArgumentException>(() => document.QuerySelectorAll(classNames: new[] { "a", "" })).ParamName);
		Assert.AreEqual("classNames", Assert.ThrowsExactly<ArgumentException>(() => document.QuerySelectorAll(classNames: new[] { "a", "b c" })).ParamName);
		Assert.AreEqual("classNames", Assert.ThrowsExactly<ArgumentException>(() => element.QuerySelectorAll(classNames: new string?[] { "a", null })).ParamName);
		Assert.ThrowsExactly<ArgumentNullException>(() => document.QuerySelectorAll(attributes: Attributes((null!, "x"))));
		Assert.ThrowsExactly<ArgumentException>(() => document.QuerySelectorAll(attributes: Attributes(("id", "1"), ("", "x"))));
	}

	[TestMethod]
	public void EmptyAttributeMemoryDoesNotApplyFilters()
	{
		var document = HtmlDocument.Parse("<div><a>1</a><b>2</b></div>");
		var div = document.Elements().First();
		ReadOnlyMemory<KeyValuePair<string, string>> attributes = Attributes(("", "ignored")).AsMemory(1, 0);

		Assert.AreSequenceEqual(["div", "a", "b"], document.QuerySelectorAll(attributes: attributes).Names());
		Assert.AreSequenceEqual(["a", "b"], div.QuerySelectorAll(attributes: ReadOnlyMemory<KeyValuePair<string, string>>.Empty).Names());
	}

	[TestMethod]
	public void ResultsUseTheRetainedAttributeMemory()
	{
		var document = HtmlDocument.Parse("<div><a class=\"x\">1</a><a class=\"y\">2</a></div>");
		var div = document.Elements().First();
		var attributes = Attributes(("class", "x"));
		ReadOnlyMemory<KeyValuePair<string, string>> memory = attributes;

		var query = document.QuerySelectorAll(attributes: memory);
		var descendants = div.QuerySelectorAll(attributes: memory);

		Assert.AreSequenceEqual(["1"], query.Inners());
		Assert.AreSequenceEqual(["1"], descendants.Inners());

		attributes[0] = KeyValuePair.Create("class", "y");

		Assert.AreSequenceEqual(["2"], query.Inners());
		Assert.AreSequenceEqual(["2"], descendants.Inners());
	}
}
