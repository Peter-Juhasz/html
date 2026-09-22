using Microsoft.Extensions.Primitives;

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

		Assert.IsTrue(document.TryQuerySelector(out var element, element: "a", classNames: "btn", attributes: TestHelpers.Attributes(("id", "x"), ("href", "/"))));
		Assert.AreEqual("3", element.InnerSpan.ToString());
	}

	[TestMethod]
	public void QueriesUseOnlyTheSpecifiedAttributeMemorySlice()
	{
		var document = HtmlDocument.Parse("<div><a class=\"btn\" rel=\"author\">1</a><a class=\"btn\" rel=\"other\">2</a></div><a class=\"btn\" rel=\"author\">3</a>");
		var div = document.Elements().First();
		ReadOnlyMemory<KeyValuePair<string, string>> attributes = TestHelpers.Attributes((null!, "ignored"), ("class", "btn"), ("rel", "author"), ("", "ignored")).AsMemory(1, 2);

		Assert.AreSequenceEqual(["1", "3"], document.QuerySelectorAll(element: "a", attributes: attributes).Inners());
		Assert.AreSequenceEqual(["1"], div.QuerySelectorAll(element: "a", attributes: attributes).Inners());
		Assert.IsTrue(document.TryQuerySelector(out var first, element: "a", attributes: attributes));
		Assert.IsTrue(div.TryQuerySelector(out var child, element: "a", attributes: attributes));
		Assert.AreEqual("1", first.InnerSpan.ToString());
		Assert.AreSame(first, child);
		Assert.AreSame(first, document.QuerySelector(element: "a", attributes: attributes));
		Assert.AreSame(first, div.QuerySelector(element: "a", attributes: attributes));
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

		Assert.AreEqual("1", document.QuerySelector(classNames: "btn")?.InnerSpan.ToString());
		Assert.AreEqual("1", div.QuerySelector(element: "a", attributes: TestHelpers.Attributes(("class", "btn")))?.InnerSpan.ToString());
		Assert.AreEqual("2", document.QuerySelector(element: "a", classNames: "btn", attributes: TestHelpers.Attributes(("id", "y")))?.InnerSpan.ToString());
		Assert.AreEqual("2", div.QuerySelector(element: "a", classNames: "btn", attributes: TestHelpers.Attributes(("id", "y")))?.InnerSpan.ToString());
		Assert.AreEqual("2", document.QuerySelector(classNames: new[] { "btn", "big" })?.InnerSpan.ToString());
		Assert.AreEqual("2", div.QuerySelector(classNames: new[] { "big", "btn" })?.InnerSpan.ToString());
		Assert.IsNull(document.QuerySelector(classNames: new[] { "btn", "missing" }));
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

		Assert.IsTrue(document.TryQuerySelector(out var first, attributes: TestHelpers.Attributes(("id", ""))));
		Assert.AreEqual("<a id>1</a>", first.OuterSpan.ToString());
		Assert.IsTrue(div.TryQuerySelector(out var child, attributes: TestHelpers.Attributes(("id", ""))));
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
	public void GetElementsByNameFindsAllDescendantsWithTheNameAttribute()
	{
		var document = HtmlDocument.Parse("<input name=\"q\"><form name=\"q\"><input name=\"q\"><fieldset><input NAME=\"q\"><select name=\"q\"></select></fieldset><textarea name=\"q\"></textarea></form><button name=\"q\"></button>");
		var form = document.Elements().ElementAt(1);
		var matches = document.GetElementsByName(name: "q");
		var descendants = form.GetElementsByName(name: "q");
		var names = matches.Names();

		Assert.AreSequenceEqual(["input", "form", "input", "input", "select", "textarea", "button"], names);
		Assert.AreSequenceEqual(["input", "input", "select", "textarea"], descendants.Names());
		Assert.AreSequenceEqual(names, matches.Names());
		Assert.IsEmpty(document.GetElementsByName("missing"));
		Assert.IsEmpty(form.GetElementsByName("missing"));
	}

	[TestMethod]
	[DataRow("<input NAME='q'><input name='Q'><q></q><input id='q'><input name='q-extra'><input name=' q'><input name='q q'>", "q", "<input NAME='q'>")]
	[DataRow("<input name='x' name='q'><input name='q' name='x'>", "q", "<input name='q' name='x'>")]
	[DataRow("<input name='a&amp;b'><input name='a&amp;amp;b'><input name='a-b'>", "a&b", "<input name='a&amp;b'>")]
	[DataRow("<input name='a&amp;amp;b'><input name='a&amp;b'>", "a&amp;b", "<input name='a&amp;amp;b'>")]
	[DataRow("<input name='first last'><input name='first'><input name='last'>", "first last", "<input name='first last'>")]
	[DataRow("<script><input name='q'></script><!--<input name='q'>--><input name='q'>", "q", "<input name='q'>")]
	public void GetElementsByNameUsesAttributeMatchingRules(string html, string name, string expected)
	{
		var document = HtmlDocument.Parse("<form>" + html + "</form>");
		var form = document.Elements().First();

		Assert.AreSequenceEqual([expected], document.GetElementsByName(name).Outers());
		Assert.AreSequenceEqual([expected], form.GetElementsByName(name).Outers());
	}

	[TestMethod]
	public void GetElementsByNameRejectsNullOrEmptyNames()
	{
		var document = HtmlDocument.Parse("<form></form>");
		var form = document.Elements().First();

		Assert.AreEqual("name", Assert.ThrowsExactly<ArgumentNullException>(() => document.GetElementsByName(null!)).ParamName);
		Assert.AreEqual("name", Assert.ThrowsExactly<ArgumentNullException>(() => form.GetElementsByName(null!)).ParamName);
		Assert.AreEqual("name", Assert.ThrowsExactly<ArgumentException>(() => document.GetElementsByName("")).ParamName);
		Assert.AreEqual("name", Assert.ThrowsExactly<ArgumentException>(() => form.GetElementsByName("")).ParamName);
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
		var document = HtmlDocument.Parse("<div class=\"c\"><p class=\"a c\">1</p><span class=\"cc\">2</span><span><p class=\"c a\">3</p></span></div><p class=\"c\">4</p>");
		var div = document.Elements().First();

		Assert.AreSequenceEqual(["div", "p", "p", "p"], document.GetElementsByClassName("c").Names());
		Assert.AreSequenceEqual(["p", "p"], div.GetElementsByClassName("c").Names());
		Assert.AreSequenceEqual(["1", "3"], document.GetElementsByClassName(new[] { "c", "a" }).Inners());
		Assert.AreSequenceEqual(["1", "3"], div.GetElementsByClassName(new StringValues(["a", "c"])).Inners());
		Assert.AreSequenceEqual(["div", "p", "p", "p"], document.GetElementsByClassName(new StringValues("c")).Names());
		Assert.IsEmpty(document.GetElementsByClassName(new[] { "c", "missing" }));
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
		Assert.AreEqual("classNames", Assert.ThrowsExactly<ArgumentException>(() => document.GetElementsByClassName(StringValues.Empty)).ParamName);
		Assert.AreEqual("classNames", Assert.ThrowsExactly<ArgumentException>(() => element.GetElementsByClassName(Array.Empty<string>())).ParamName);
		Assert.AreEqual("classNames", Assert.ThrowsExactly<ArgumentException>(() => document.GetElementsByClassName(new[] { "a", "" })).ParamName);
		Assert.AreEqual("classNames", Assert.ThrowsExactly<ArgumentException>(() => element.GetElementsByClassName(new[] { "a", "b c" })).ParamName);
		Assert.AreEqual("classNames", Assert.ThrowsExactly<ArgumentException>(() => document.GetElementsByClassName(new string?[] { "a", null })).ParamName);
		Assert.ThrowsExactly<ArgumentException>(() => element.QuerySelector(classNames: ""));
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
