namespace PeterJuhasz.Text.Html.Tests.Lazy;

[TestClass]
public sealed class SelectorTests
{
	[TestMethod]
	public void ElementNameIsComparedCaseInsensitively()
	{
		var document = LazyHtmlDocument.Parse("<a>1</a><b>2</b><A>3</A>");

		Assert.AreSequenceEqual(["<a>1</a>", "<A>3</A>"], document.QuerySelectorAll("a").Outers());
		Assert.AreSequenceEqual(["<a>1</a>", "<A>3</A>"], document.QuerySelectorAll("A").Outers());
	}

	[TestMethod]
	public void UniversalSelectorMatchesEveryElement()
	{
		var document = LazyHtmlDocument.Parse("<div><p>a<br>b</p></div><span></span>");

		Assert.AreSequenceEqual(["div", "p", "br", "span"], document.QuerySelectorAll("*").Names());
	}

	[TestMethod]
	public void ClassesMustAllBePresent()
	{
		var document = LazyHtmlDocument.Parse("<a class=\"x\">1</a><a class=\"y x\">2</a><b class=\"x y\">3</b>");

		Assert.AreSequenceEqual(["1", "2", "3"], document.QuerySelectorAll(".x").Inners());
		Assert.AreSequenceEqual(["2", "3"], document.QuerySelectorAll(".x.y").Inners());
		Assert.AreSequenceEqual(["2", "3"], document.QuerySelectorAll("*.y.x").Inners());
		Assert.AreSequenceEqual(["2"], document.QuerySelectorAll("a.y.x").Inners());
	}

	[TestMethod]
	public void IdMatchesTheIdAttributeExactly()
	{
		var document = LazyHtmlDocument.Parse("<div id=\"main\">1</div><p id=main>2</p><div id=\"Main\">3</div><div id=\"main x\">4</div>");

		Assert.AreSequenceEqual(["1", "2"], document.QuerySelectorAll("#main").Inners());
		Assert.AreSequenceEqual(["2"], document.QuerySelectorAll("p#main").Inners());
	}

	[TestMethod]
	public void AttributeValueCanBeAnIdentifierOrAQuotedString()
	{
		var document = LazyHtmlDocument.Parse("<a rel=next>1</a><a rel=\"prev\">2</a><a REL='next'>3</a><a rel=\"Next\">4</a>");

		foreach (var selector in new[] { "[rel=next]", "[rel=\"next\"]", "[rel='next']", "a[ rel = next ]", "[REL=next]" })
			Assert.AreSequenceEqual(["1", "3"], document.QuerySelectorAll(selector).Inners(), selector);
	}

	[TestMethod]
	public void QuotedAttributeValueIsTakenLiterally()
	{
		var document = LazyHtmlDocument.Parse("<a title=\"a b\">1</a><a title=\"a]b\">2</a><a title='say \"hi\"'>3</a><a title=\"x&amp;y\">4</a><a title>5</a><a title=\"\">6</a>");

		Assert.AreSequenceEqual(["1"], document.QuerySelectorAll("[title=\"a b\"]").Inners());
		Assert.AreSequenceEqual(["2"], document.QuerySelectorAll("[title=\"a]b\"]").Inners());
		Assert.AreSequenceEqual(["3"], document.QuerySelectorAll("[title='say \"hi\"']").Inners());
		Assert.AreSequenceEqual(["4"], document.QuerySelectorAll("[title=\"x&y\"]").Inners());
		Assert.AreSequenceEqual(["5", "6"], document.QuerySelectorAll("[title=\"\"]").Inners());
	}

	[TestMethod]
	public void ClassAttributeSelectorMatchesTheWholeValue()
	{
		var document = LazyHtmlDocument.Parse("<a class=\"x y\">1</a><a class=\"y x\">2</a>");

		Assert.AreSequenceEqual(["1"], document.QuerySelectorAll("[class=\"x y\"]").Inners());
		Assert.AreSequenceEqual(["1", "2"], document.QuerySelectorAll(".x.y").Inners());
	}

	[TestMethod]
	public void CombinesAllFilters()
	{
		var document = LazyHtmlDocument.Parse(
			"<article class=\"article\" data-tags=\"html,dotnet\" id=a>1</article>" +
			"<article class=\"article\" data-tags=\"html\">2</article>" +
			"<div class=\"article\" data-tags=\"html,dotnet\">3</div>" +
			"<article class=\"big article\" data-tags=\"html,dotnet\" id=b>4</article>");

		Assert.AreSequenceEqual(["1", "4"], document.QuerySelectorAll("article.article[data-tags=\"html,dotnet\"]").Inners());
		Assert.AreSequenceEqual(["4"], document.QuerySelectorAll("article#b.article[data-tags='html,dotnet'].big").Inners());
	}

	[TestMethod]
	public void AgreesWithTheStructuredQuery()
	{
		var document = LazyHtmlDocument.Parse("<a class=\"x y\" href=\"/\">1</a><a class=\"x\" href=\"/\">2</a><div><a class=\"y x\" href=\"/\" id=z>3</a></div>");

		Assert.AreSequenceEqual(
			document.QuerySelectorAll(element: "a", classNames: new[] { "x", "y" }, attributes: [new("href", "/"), new("id", "z")]).Outers(),
			document.QuerySelectorAll("a.x.y[href='/']#z").Outers());
	}

	[TestMethod]
	public void IgnoresSurroundingWhitespace()
	{
		var document = LazyHtmlDocument.Parse("<a>1</a><b></b>");

		Assert.AreSequenceEqual(["<a>1</a>"], document.QuerySelectorAll(" \t a\r\n").Outers());
	}

	[TestMethod]
	public void IdentifiersCanContainNonAsciiCharacters()
	{
		var document = LazyHtmlDocument.Parse("<p class=\"café\">1</p><p class=\"cafe\">2</p><my-élément data-x_y=1>3</my-élément>");

		Assert.AreSequenceEqual(["1"], document.QuerySelectorAll(".café").Inners());
		Assert.AreSequenceEqual(["3"], document.QuerySelectorAll("my-élément[data-x_y='1']").Inners());
	}

	[TestMethod]
	public void ElementSearchesOnlyItsOwnContent()
	{
		var document = LazyHtmlDocument.Parse("<a class=x>0</a><div class=x><a class=x>1</a></div><a class=x>2</a>");
		var div = document.Elements().ToList()[1];

		Assert.AreSequenceEqual(["<a class=x>1</a>"], div.QuerySelectorAll(".x").Outers());

		var found = div.QuerySelector("a.x");
		Assert.IsNotNull(found);
		Assert.AreEqual("<a class=x>1</a>", found.Value.OuterSpan.ToString());
	}

	[TestMethod]
	public void QuerySelectorReturnsTheFirstMatchOrNull()
	{
		var document = LazyHtmlDocument.Parse("<div><p class=x>1</p></div><p class=x>2</p>");

		var first = document.QuerySelector("p.x");
		Assert.IsNotNull(first);
		Assert.AreEqual("1", first.Value.InnerSpan.ToString());
		Assert.IsNull(document.QuerySelector("p.y"));
		Assert.IsNull(document.Elements().ToList()[0].QuerySelector("div"));
	}

	[TestMethod]
	[DataRow("")]
	[DataRow(" ")]
	[DataRow("div p")]
	[DataRow("div>p")]
	[DataRow("div + p")]
	[DataRow("div ~ p")]
	[DataRow("a, b")]
	[DataRow("a:hover")]
	[DataRow("a::before")]
	[DataRow("[href]")]
	[DataRow("[href^=x]")]
	[DataRow("[href|=x]")]
	[DataRow("[href=x i]")]
	[DataRow("[href=1]")]
	[DataRow("[href=\"x]")]
	[DataRow("[href=\"x\\\"y\"]")]
	[DataRow("[href=x")]
	[DataRow("[=x]")]
	[DataRow("svg|a")]
	[DataRow("*|a")]
	[DataRow("**")]
	[DataRow("a*")]
	[DataRow(".")]
	[DataRow("#")]
	[DataRow(".a..b")]
	[DataRow(".1a")]
	[DataRow("#-1")]
	[DataRow(".-")]
	[DataRow("1a")]
	[DataRow("a\\.b")]
	public void UnsupportedSelectorThrows(string selector)
	{
		var document = LazyHtmlDocument.Parse("<a></a>");
		var element = TestHelpers.FirstElement("<a></a>");

		Assert.AreEqual("selector", Assert.ThrowsExactly<ArgumentException>(() => document.QuerySelectorAll(selector)).ParamName);
		Assert.AreEqual("selector", Assert.ThrowsExactly<ArgumentException>(() => element.QuerySelectorAll(selector)).ParamName);
		Assert.AreEqual("selector", Assert.ThrowsExactly<ArgumentException>(() => document.QuerySelector(selector)).ParamName);
		Assert.AreEqual("selector", Assert.ThrowsExactly<ArgumentException>(() => element.QuerySelector(selector)).ParamName);
	}

	[TestMethod]
	public void NullSelectorThrows()
	{
		var document = LazyHtmlDocument.Parse("<a></a>");
		var element = TestHelpers.FirstElement("<a></a>");

		Assert.ThrowsExactly<ArgumentNullException>(() => document.QuerySelectorAll((string)null!));
		Assert.ThrowsExactly<ArgumentNullException>(() => element.QuerySelectorAll((string)null!));
		Assert.ThrowsExactly<ArgumentNullException>(() => document.QuerySelector((string)null!));
		Assert.ThrowsExactly<ArgumentNullException>(() => element.QuerySelector((string)null!));
	}
}
