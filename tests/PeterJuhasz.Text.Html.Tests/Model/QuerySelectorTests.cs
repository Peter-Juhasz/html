namespace PeterJuhasz.Text.Html.Tests.Model;

[TestClass]
public sealed class QuerySelectorTests
{
	[TestMethod]
	public void TryQuerySelectorReturnsFirstMatchInDocumentOrder()
	{
		var document = HtmlDocument.Parse("<div><p><a>1</a></p><a>2</a></div><a>3</a>");

		Assert.IsTrue(document.TryQuerySelector(result: out var element, element: "a"));
		Assert.AreEqual("<a>1</a>", element.OuterSpan.ToString());
	}

	[TestMethod]
	public void TryQuerySelectorReturnsFalseWhenNothingMatches()
	{
		var document = HtmlDocument.Parse("<div><p>text</p></div>");

		Assert.IsFalse(document.TryQuerySelector(out var element, element: "a"));
		Assert.IsNull(element);
		Assert.IsFalse(document.Elements().First().TryQuerySelector(out var child, element: "div"));
		Assert.IsNull(child);
	}

	[TestMethod]
	public void TryQuerySelectorCombinesFilters()
	{
		var document = HtmlDocument.Parse("<a id=\"x\" class=\"btn\">1</a><span id=\"x\" class=\"btn\" href=\"/\">2</span><a id=\"x\" class=\"a btn\" href=\"/\">3</a>");

		Assert.IsTrue(document.TryQuerySelector(out var element, element: "a", className: "btn", attributes: [new("id", "x"), new("href", "/")]));
		Assert.AreEqual("3", element.InnerSpan.ToString());
	}

	[TestMethod]
	public void TryQuerySelectorCanBeChained()
	{
		var document = HtmlDocument.Parse("<html><body><ul><li><a>1</a></li></ul></body></html>");

		Assert.IsTrue(document.TryQuerySelector(out var body, element: "body"));
		Assert.IsTrue(body.TryQuerySelector(out var list, element: "ul"));
		Assert.IsTrue(list.TryQuerySelector(out var link, element: "a"));
		Assert.AreEqual("1", link.InnerSpan.ToString());
	}

	[TestMethod]
	public void TryQuerySelectorAgreesWithQuerySelectorAll()
	{
		var document = HtmlDocument.Parse("<div><p>a<br>b</p><ul><li>1<li>2</ul><script>x</script><p>c</p></div>");

		foreach (var name in new[] { "div", "p", "br", "li", "script", "ul", "zzz" })
		{
			var all = document.QuerySelectorAll(element: name).ToList();
			var found = document.TryQuerySelector(out var first, element: name);

			Assert.AreEqual(all.Count > 0, found, $"Mismatch for <{name}>.");
			if (found)
				Assert.AreSame(all[0], first, $"Mismatch for <{name}>.");
		}
	}

	[TestMethod]
	public void QuerySelectorReturnsFirstMatchOrNull()
	{
		var document = HtmlDocument.Parse("<div><a class=\"btn\" id=\"x\">1</a><a class=\"big btn\" id=\"y\">2</a></div><a class=\"btn\" id=\"y\">3</a>");
		var div = document.Elements().First();

		Assert.AreEqual("1", document.QuerySelector(className: "btn")?.InnerSpan.ToString());
		Assert.AreEqual("1", div.QuerySelector(element: "a", attributes: [new("class", "btn")])?.InnerSpan.ToString());
		Assert.AreEqual("2", document.QuerySelector(element: "a", className: "btn", attributes: [new("id", "y")])?.InnerSpan.ToString());
		Assert.AreEqual("2", div.QuerySelector(element: "a", className: "btn", attributes: [new("id", "y")])?.InnerSpan.ToString());
		Assert.AreSame(div, document.QuerySelector());
		Assert.AreSame(div.Elements().First(), div.QuerySelector(element: null));
		Assert.IsNull(document.QuerySelector(element: "span"));
		Assert.IsNull(div.QuerySelector(element: "div"));
	}

	[TestMethod]
	public void TryQuerySelectorWithNullElementMatchesAnyTag()
	{
		var document = HtmlDocument.Parse("<div><p>1</p><a>2</a></div>");
		var div = document.Elements().First();

		Assert.IsTrue(document.TryQuerySelector(out var first));
		Assert.AreSame(div, first);
		Assert.IsTrue(div.TryQuerySelector(out var child, element: null));
		Assert.AreSame(div.Elements().First(), child);
	}

	[TestMethod]
	public void EmptyIdAttributeMatchesTheFirstPresentEmptyId()
	{
		var document = HtmlDocument.Parse("<div><a>0</a><a id>1</a><a id=\"\">2</a></div>");
		var div = document.Elements().First();

		Assert.IsTrue(document.TryQuerySelector(out var first, attributes: [new("id", "")]));
		Assert.AreEqual("<a id>1</a>", first.OuterSpan.ToString());
		Assert.IsTrue(div.TryQuerySelector(out var child, attributes: [new("id", "")]));
		Assert.AreSame(first, child);
	}

	[TestMethod]
	public void GetElementByIdFindsTheFirstElementWithTheId()
	{
		var document = HtmlDocument.Parse("<div id=\"x\"><p ID=\"main\">1</p></div><span id=\"main\">2</span>");
		var div = document.Elements().First();

		Assert.AreEqual("<p ID=\"main\">1</p>", document.GetElementById("main")?.ToString());
		Assert.AreEqual("<p ID=\"main\">1</p>", div.GetElementById("main")?.ToString());
		Assert.IsNull(document.GetElementById("Main"));
		Assert.IsNull(div.GetElementById("Main"));
		Assert.IsNull(document.GetElementById("missing"));
		Assert.IsNull(div.GetElementById("missing"));
		Assert.IsNull(div.GetElementById("x"));
	}

	[TestMethod]
	public void GetElementsByTagNameFindsAllDescendantsWithTheName()
	{
		var document = HtmlDocument.Parse("<div><p>1</p><span><p>2</p></span></div><p>3</p>");
		var div = document.Elements().First();

		Assert.AreSequenceEqual(["1", "2", "3"], document.GetElementsByTagName("p").Inners());
		Assert.AreSequenceEqual(["1", "2"], div.GetElementsByTagName("P").Inners());
	}

	[TestMethod]
	public void GetElementsByClassNameFindsAllDescendantsWithTheClass()
	{
		var document = HtmlDocument.Parse("<div class=\"c\"><p class=\"a c\">1</p><span class=\"cc\">2</span></div><p class=\"c\">3</p>");
		var div = document.Elements().First();

		Assert.AreSequenceEqual(["div", "p", "p"], document.GetElementsByClassName("c").Names());
		Assert.AreSequenceEqual(["p"], div.GetElementsByClassName("c").Names());
	}

	[TestMethod]
	public void ShortcutsRejectNullOrEmptyArguments()
	{
		var document = HtmlDocument.Parse("<a></a>");
		var element = document.Elements().First();

		Assert.ThrowsExactly<ArgumentNullException>(() => document.GetElementById(null!));
		Assert.ThrowsExactly<ArgumentNullException>(() => element.GetElementById(null!));
		Assert.ThrowsExactly<ArgumentException>(() => document.GetElementById(""));
		Assert.ThrowsExactly<ArgumentException>(() => element.GetElementById(""));
		Assert.ThrowsExactly<ArgumentException>(() => element.GetElementsByTagName(""));
		Assert.ThrowsExactly<ArgumentException>(() => element.GetElementsByClassName(""));
		Assert.ThrowsExactly<ArgumentException>(() => document.GetElementsByClassName("a b"));
		Assert.ThrowsExactly<ArgumentException>(() => element.QuerySelector(className: ""));
	}

	[TestMethod]
	public void EmptyElementThrows()
	{
		var document = HtmlDocument.Parse("<a></a>");
		var element = document.Elements().First();

		Assert.AreEqual("element", Assert.ThrowsExactly<ArgumentException>(() => document.TryQuerySelector(out _, element: "")).ParamName);
		Assert.AreEqual("element", Assert.ThrowsExactly<ArgumentException>(() => element.TryQuerySelector(out _, element: "")).ParamName);
		Assert.AreEqual("element", Assert.ThrowsExactly<ArgumentException>(() => document.QuerySelector(element: "")).ParamName);
		Assert.AreEqual("element", Assert.ThrowsExactly<ArgumentException>(() => element.QuerySelector(element: "")).ParamName);
	}
}
