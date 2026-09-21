using System.Text.Html.Model;
using static PeterJuhasz.Text.Html.Tests.Model.TestHelpers;

namespace PeterJuhasz.Text.Html.Tests.Model;

[TestClass]
public sealed class QuerySelectorAllTests
{
	[TestMethod]
	public void FindsRootElementsInDocument()
	{
		var document = HtmlDocument.Parse("<a>1</a><b>2</b><a>3</a>");

		CollectionAssert.AreEqual(new[] { "<a>1</a>", "<a>3</a>" }, document.QuerySelectorAll(name: "a").Outers());
	}

	[TestMethod]
	public void FindsDescendantsAtAnyDepthInDocumentOrder()
	{
		var document = HtmlDocument.Parse("<html><body><div><ul><li><a>1</a></li></ul><p><a>2</a></p></div><a>3</a></body></html>");

		CollectionAssert.AreEqual(new[] { "<a>1</a>", "<a>2</a>", "<a>3</a>" }, document.QuerySelectorAll(name: "a").Outers());
	}

	[TestMethod]
	public void FindsMatchesNestedInsideMatches()
	{
		var document = HtmlDocument.Parse("<div id=1><div id=2><div id=3></div></div></div>");

		CollectionAssert.AreEqual(new[] { "1", "2", "3" }, document.QuerySelectorAll(name: "div").Select(e => e.GetAttribute("id")?.Value).ToList());
	}

	[TestMethod]
	public void ElementSearchesOnlyItsOwnContentAndNotItself()
	{
		var document = HtmlDocument.Parse("<a>0</a><div><a>1</a><p><a>2</a></p></div><a>3</a>");
		var div = document.Elements().ElementAt(1);

		CollectionAssert.AreEqual(new[] { "<a>1</a>", "<a>2</a>" }, div.QuerySelectorAll(name: "a").Outers());
		Assert.IsEmpty(div.QuerySelectorAll(name: "div"));
	}

	[TestMethod]
	public void NameComparisonIsCaseInsensitive()
	{
		var document = HtmlDocument.Parse("<DIV>1</DIV><div>2</div><Div>3</Div>");

		Assert.HasCount(3, document.QuerySelectorAll(name: "div").ToList());
		Assert.HasCount(3, document.QuerySelectorAll(name: "DIV").ToList());
	}

	[TestMethod]
	public void DoesNotMatchNamesWithTheSamePrefix()
	{
		var document = HtmlDocument.Parse("<b>1</b><br><a>2</a><abbr>3</abbr>");

		CollectionAssert.AreEqual(new[] { "<b>1</b>" }, document.QuerySelectorAll(name: "b").Outers());
		CollectionAssert.AreEqual(new[] { "<a>2</a>" }, document.QuerySelectorAll(name: "a").Outers());
	}

	[TestMethod]
	public void FindsVoidRawTextAndImplicitlyClosedElements()
	{
		var document = HtmlDocument.Parse("<ul><li>1<li>2</ul><p>a<br>b<br/></p><script>1</script><body><script>2</script></body>");

		CollectionAssert.AreEqual(new[] { "<li>1", "<li>2" }, document.QuerySelectorAll(name: "li").Outers());
		Assert.HasCount(2, document.QuerySelectorAll(name: "br").ToList());
		CollectionAssert.AreEqual(new[] { "<script>1</script>", "<script>2</script>" }, document.QuerySelectorAll(name: "script").Outers());
	}

	[TestMethod]
	public void DoesNotLookInsideRawTextOrComments()
	{
		var document = HtmlDocument.Parse("<script><b>fake</b></script><!-- <b>fake</b> --><b>real</b>");

		CollectionAssert.AreEqual(new[] { "<b>real</b>" }, document.QuerySelectorAll(name: "b").Outers());
	}

	[TestMethod]
	public void NoFiltersMatchEveryDescendant()
	{
		var document = HtmlDocument.Parse("<html><head><title>T</title></head><body><p>a<br>b</p><script>x</script></body></html>");
		var body = document.Elements().First().Elements().ElementAt(1);

		CollectionAssert.AreEqual(new[] { "html", "head", "title", "body", "p", "br", "script" }, document.QuerySelectorAll().Names());
		CollectionAssert.AreEqual(new[] { "p", "br", "script" }, body.QuerySelectorAll().Names());
	}

	[TestMethod]
	public void ReturnsEmptyWhenNothingMatches()
	{
		Assert.IsEmpty(HtmlDocument.Parse("<html><body><p>text</p></body></html>").QuerySelectorAll(name: "a"));
		Assert.IsEmpty(HtmlDocument.Parse("").QuerySelectorAll(name: "a"));
		Assert.IsEmpty(HtmlDocument.Parse("just text").QuerySelectorAll());
	}

	[TestMethod]
	public void ResultCanBeEnumeratedRepeatedly()
	{
		var query = HtmlDocument.Parse("<a></a><div><a></a></div>").QuerySelectorAll(name: "a");

		Assert.HasCount(2, query.ToList());
		Assert.HasCount(2, query.ToList());
	}

	[TestMethod]
	public void MatchesByAttributeWithoutName()
	{
		var document = HtmlDocument.Parse("<div id=\"a\"><p id=\"b\">1</p><span id=\"a\">2</span></div><a id=a>3</a>");

		CollectionAssert.AreEqual(new[] { "div", "span", "a" }, document.QuerySelectorAll(attributes: [new("id", "a")]).Names());
	}

	[TestMethod]
	public void MatchesByNameAndAttribute()
	{
		var document = HtmlDocument.Parse("<a class=\"btn\">1</a><button class=\"btn\">2</button><a class=\"link\">3</a><div><a class=\"btn\">4</a></div>");

		CollectionAssert.AreEqual(new[] { "<a class=\"btn\">1</a>", "<a class=\"btn\">4</a>" }, document.QuerySelectorAll(name: "a", attributes: [new("class", "btn")]).Outers());
	}

	[TestMethod]
	public void AllAttributesMustMatch()
	{
		var document = HtmlDocument.Parse(
			"<input type=\"text\" name=\"q\">" +
			"<input type=\"text\" name=\"other\">" +
			"<input type=\"hidden\" name=\"q\">" +
			"<input name=\"q\" type=\"text\" id=\"search\">");

		var matches = document.QuerySelectorAll(name: "input", attributes: [new("type", "text"), new("name", "q")]).Outers();

		CollectionAssert.AreEqual(new[] { "<input type=\"text\" name=\"q\">", "<input name=\"q\" type=\"text\" id=\"search\">" }, matches);
	}

	[TestMethod]
	public void MissingOrDifferentAttributeValueDoesNotMatch()
	{
		var document = HtmlDocument.Parse("<a rel=\"author\">1</a><a rel=\"authors\">2</a><a>3</a>");

		CollectionAssert.AreEqual(new[] { "<a rel=\"author\">1</a>" }, document.QuerySelectorAll(name: "a", attributes: Attributes(("rel", "author"))).Outers());
		Assert.IsEmpty(document.QuerySelectorAll(name: "a", attributes: Attributes(("target", "_blank"))));
	}

	[TestMethod]
	public void AttributeNameIsCaseInsensitiveButValueIsCaseSensitive()
	{
		var document = HtmlDocument.Parse("<a CLASS=\"Btn\">1</a><a class=\"btn\">2</a><a Class=\"Btn\">3</a>");

		CollectionAssert.AreEqual(new[] { "1", "3" }, document.QuerySelectorAll(name: "a", attributes: Attributes(("class", "Btn"))).Inners());
		CollectionAssert.AreEqual(new[] { "2" }, document.QuerySelectorAll(name: "a", attributes: Attributes(("CLASS", "btn"))).Inners());
	}

	[TestMethod]
	public void BareAttributeMatchesEmptyValue()
	{
		var document = HtmlDocument.Parse("<input disabled><input disabled=\"\"><input disabled=\"disabled\"><input>");

		CollectionAssert.AreEqual(new[] { "<input disabled>", "<input disabled=\"\">" }, document.QuerySelectorAll(name: "input", attributes: Attributes(("disabled", ""))).Outers());
		CollectionAssert.AreEqual(new[] { "<input disabled=\"disabled\">" }, document.QuerySelectorAll(name: "input", attributes: Attributes(("disabled", "disabled"))).Outers());
	}

	[TestMethod]
	public void FirstOfDuplicateAttributesWins()
	{
		var document = HtmlDocument.Parse("<a class=\"x\" class=\"y\">1</a>");

		Assert.HasCount(1, document.QuerySelectorAll(name: "a", attributes: Attributes(("class", "x"))).ToList());
		Assert.IsEmpty(document.QuerySelectorAll(name: "a", attributes: Attributes(("class", "y"))));
	}

	[TestMethod]
	public void AttributesOfDescendantsDoNotMatchTheAncestor()
	{
		var document = HtmlDocument.Parse("<div><p class=\"x\" id=\"y\">1</p></div>");

		Assert.IsEmpty(document.QuerySelectorAll(name: "div", attributes: Attributes(("class", "x"))));
		Assert.IsEmpty(document.QuerySelectorAll(name: "div", id: "y"));
		Assert.IsEmpty(document.QuerySelectorAll(name: "div", className: "x"));
		CollectionAssert.AreEqual(new[] { "p" }, document.QuerySelectorAll(attributes: Attributes(("class", "x"))).Names());
	}

	[TestMethod]
	public void MatchesById()
	{
		var document = HtmlDocument.Parse("<div id=\"a\"><p id=\"b\">1</p><span id=\"a\">2</span></div><a id=a>3</a>");

		CollectionAssert.AreEqual(new[] { "div", "span", "a" }, document.QuerySelectorAll(id: "a").Names());
		CollectionAssert.AreEqual(new[] { "span" }, document.QuerySelectorAll(name: "span", id: "a").Names());
	}

	[TestMethod]
	public void IdMustMatchTheWholeValueCaseSensitively()
	{
		var document = HtmlDocument.Parse("<div id=\"Main\">1</div><div id=\"main-content\">2</div><div id=\"main\">3</div><div id=\" main\">4</div><div id>5</div><div class=\"main\">6</div>");

		CollectionAssert.AreEqual(new[] { "<div id=\"main\">3</div>" }, document.QuerySelectorAll(id: "main").Outers());
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

		CollectionAssert.AreEqual(new[] { "1", "2", "3", "4" }, document.QuerySelectorAll(className: "btn").Inners());
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

		var matches = document.QuerySelectorAll(name: "a", id: "x", className: "btn", attributes: [new("href", "/")]).Inners();

		CollectionAssert.AreEqual(new[] { "1", "6" }, matches);
	}

	[TestMethod]
	public void ElementQueriesSearchOnlyItsContent()
	{
		var document = HtmlDocument.Parse("<a id=\"x\" class=\"c\">0</a><div><a id=\"x\" class=\"c\">1</a><p><a class=\"y\">2</a></p></div><a id=\"x\" class=\"c\">3</a>");
		var div = document.Elements().ElementAt(1);

		CollectionAssert.AreEqual(new[] { "1" }, div.QuerySelectorAll(id: "x").Inners());
		CollectionAssert.AreEqual(new[] { "1" }, div.QuerySelectorAll(className: "c").Inners());
		CollectionAssert.AreEqual(new[] { "1" }, div.QuerySelectorAll(name: "a", attributes: Attributes(("class", "c"))).Inners());
	}

	[TestMethod]
	public void InvalidArgumentsThrowEagerly()
	{
		var document = HtmlDocument.Parse("<a></a>");
		var element = document.Elements().First();

		Assert.ThrowsExactly<ArgumentException>(() => document.QuerySelectorAll(name: ""));
		Assert.ThrowsExactly<ArgumentException>(() => element.QuerySelectorAll(name: ""));
		Assert.ThrowsExactly<ArgumentException>(() => document.QuerySelectorAll(id: ""));
		Assert.ThrowsExactly<ArgumentException>(() => document.QuerySelectorAll(className: ""));
		Assert.ThrowsExactly<ArgumentException>(() => document.QuerySelectorAll(className: "a b"));
		Assert.ThrowsExactly<ArgumentException>(() => element.QuerySelectorAll(className: " a"));
		Assert.ThrowsExactly<ArgumentNullException>(() => document.QuerySelectorAll(attributes: Attributes((null!, "x"))));
		Assert.ThrowsExactly<ArgumentException>(() => document.QuerySelectorAll(attributes: Attributes(("id", "1"), ("", "x"))));
	}

	[TestMethod]
	public void ResultIsNotAffectedByLaterChangesToTheAttributes()
	{
		var document = HtmlDocument.Parse("<a class=\"x\">1</a><a class=\"y\">2</a>");
		var attributes = Attributes(("class", "x"));

		var query = document.QuerySelectorAll(attributes: attributes);
		attributes[0] = KeyValuePair.Create("class", "y");

		CollectionAssert.AreEqual(new[] { "1" }, query.Inners());
	}
}
