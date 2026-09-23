namespace PeterJuhasz.Text.Html.Tests.Model;

[TestClass]
public sealed class SelectorTests
{
	private const string Html =
		"<div id=main class=\"box\">" +
			"<a class=\"btn primary\" href=\"/\" rel=next>1</a>" +
			"<A CLASS=\"btn\" title=\"a b\">2</A>" +
			"<p><a class=\"primary btn\" href='/' data-x=\"x&amp;y\">3</a></p>" +
		"</div>" +
		"<a id=\"main\" class=\"btn\" title>4</a>";

	[TestMethod]
	[DataRow("a", new[] { "1", "2", "3", "4" })]
	[DataRow("A", new[] { "1", "2", "3", "4" })]
	[DataRow("*", new[] { "123", "1", "2", "3", "3", "4" })]
	[DataRow(".btn", new[] { "1", "2", "3", "4" })]
	[DataRow("a.btn.primary", new[] { "1", "3" })]
	[DataRow("#main", new[] { "123", "4" })]
	[DataRow("div#main.box", new[] { "123" })]
	[DataRow("[href=\"/\"]", new[] { "1", "3" })]
	[DataRow("a[rel=next]", new[] { "1" })]
	[DataRow("[title='a b']", new[] { "2" })]
	[DataRow("[title=\"\"]", new[] { "4" })]
	[DataRow("[data-x=\"x&y\"]", new[] { "3" })]
	[DataRow("[CLASS=btn]", new[] { "2", "4" })]
	[DataRow(".btn[href='/'][rel=next]", new[] { "1" })]
	[DataRow("p.btn", new string[0])]
	public void FindsMatchingElementsLikeTheLazyLayer(string selector, string[] expected)
	{
		var document = HtmlDocument.Parse(Html);
		var lazy = LazyHtmlDocument.Parse(Html).QuerySelectorAll(selector).ToArray();

		Assert.AreSequenceEqual(expected, document.QuerySelectorAll(selector).Select(e => e.TextContent).ToList());
		Assert.AreSequenceEqual(Array.ConvertAll(lazy, e => e.OuterSpan.ToString()), document.QuerySelectorAll(selector).Outers());
	}

	[TestMethod]
	public void ElementSearchesOnlyItsOwnContent()
	{
		var document = HtmlDocument.Parse("<a class=x>0</a><div class=x><a class=x>1</a></div><a class=x>2</a>");
		var div = document.Elements().ElementAt(1);

		Assert.AreSequenceEqual(["<a class=x>1</a>"], div.QuerySelectorAll(".x").Outers());
		Assert.AreEqual("<a class=x>1</a>", div.QuerySelector("a.x")?.OuterSpan.ToString());
	}

	[TestMethod]
	public void QuerySelectorReturnsTheFirstMatchOrNull()
	{
		var document = HtmlDocument.Parse("<div><p class=x>1</p></div><p class=x>2</p>");

		Assert.AreSame(document.QuerySelectorAll("p.x").First(), document.QuerySelector("p.x"));
		Assert.AreEqual("1", document.QuerySelector("p.x")?.InnerSpan.ToString());
		Assert.IsNull(document.QuerySelector("p.y"));
		Assert.IsNull(document.Elements().First().QuerySelector("div"));
	}

	[TestMethod]
	public void UnsupportedSelectorThrowsBeforeEnumeration()
	{
		var document = HtmlDocument.Parse("<a></a>");
		var element = document.Elements().First();

		foreach (var selector in new[] { "", "div p", "a > b", "a,b", "a:hover", "[href]", "[href~=x]", ".1", "a[href=x" })
		{
			Assert.AreEqual("selector", Assert.ThrowsExactly<ArgumentException>(() => document.QuerySelectorAll(selector), selector).ParamName);
			Assert.AreEqual("selector", Assert.ThrowsExactly<ArgumentException>(() => element.QuerySelectorAll(selector), selector).ParamName);
			Assert.AreEqual("selector", Assert.ThrowsExactly<ArgumentException>(() => document.QuerySelector(selector), selector).ParamName);
			Assert.AreEqual("selector", Assert.ThrowsExactly<ArgumentException>(() => element.QuerySelector(selector), selector).ParamName);
		}

		Assert.ThrowsExactly<ArgumentNullException>(() => document.QuerySelectorAll((string)null!));
		Assert.ThrowsExactly<ArgumentNullException>(() => element.QuerySelector((string)null!));
	}
}
