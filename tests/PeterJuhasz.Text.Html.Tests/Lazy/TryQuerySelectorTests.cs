namespace PeterJuhasz.Text.Html.Tests.Lazy;

[TestClass]
public sealed class TryQuerySelectorTests
{
	[TestMethod]
	public void ReturnsFirstMatchInDocumentOrder()
	{
		var document = LazyHtmlDocument.Parse("<div><p><a>1</a></p><a>2</a></div><a>3</a>");

		Assert.IsTrue(document.TryQuerySelector(result: out var element, element: "a"));
		Assert.AreEqual("<a>1</a>", element.OuterSpan.ToString());
	}

	[TestMethod]
	public void ReturnsFalseWhenNothingMatches()
	{
		var document = LazyHtmlDocument.Parse("<div><p>text</p></div>");

		Assert.IsFalse(document.TryQuerySelector(out var element, element: "a"));
		Assert.AreEqual(default, element);
	}

	[TestMethod]
	public void EmptyAndDefaultDocumentsReturnFalse()
	{
		Assert.IsFalse(LazyHtmlDocument.Parse("").TryQuerySelector(out _, element: "a"));
		Assert.IsFalse(default(LazyHtmlDocument).TryQuerySelector(out _, element: "a"));
		Assert.IsFalse(LazyHtmlDocument.Parse("just text").TryQuerySelector(out _, element: "a"));
	}

	[TestMethod]
	public void ElementSearchesOnlyItsOwnContent()
	{
		var document = LazyHtmlDocument.Parse("<a>0</a><div><span><a>1</a></span></div><a>2</a>");
		var div = document.Elements().ToList()[1];

		Assert.IsTrue(div.TryQuerySelector(out var element, element: "a"));
		Assert.AreEqual("<a>1</a>", element.OuterSpan.ToString());
	}

	[TestMethod]
	public void ElementDoesNotMatchItself()
	{
		var element = TestHelpers.FirstElement("<div><span></span></div>");

		Assert.IsFalse(element.TryQuerySelector(out _, element: "div"));
	}

	[TestMethod]
	public void ElementReturnsFalseWhenNothingMatches()
	{
		var element = TestHelpers.FirstElement("<div><span></span></div><a>after</a>");

		Assert.IsFalse(element.TryQuerySelector(out var found, element: "a"));
		Assert.AreEqual(default, found);
	}

	[TestMethod]
	public void ElementComparisonIsCaseInsensitive()
	{
		var document = LazyHtmlDocument.Parse("<DIV>x</DIV>");

		Assert.IsTrue(document.TryQuerySelector(out var element, element: "div"));
		Assert.AreEqual("<DIV>x</DIV>", element.OuterSpan.ToString());
	}

	[TestMethod]
	public void DoesNotLookInsideRawTextContent()
	{
		var document = LazyHtmlDocument.Parse("<script><b>fake</b></script><b>real</b>");

		Assert.IsTrue(document.TryQuerySelector(out var element, element: "b"));
		Assert.AreEqual("<b>real</b>", element.OuterSpan.ToString());
	}

	[TestMethod]
	public void RawTextElementReturnsFalse()
	{
		var script = TestHelpers.FirstElement("<script><b>not markup</b></script>");

		Assert.IsFalse(script.TryQuerySelector(out _, element: "b"));
	}

	[TestMethod]
	public void FoundElementExposesItsContent()
	{
		var document = LazyHtmlDocument.Parse("<html><head><title>Hello</title></head><body><a href=\"/x\">link</a></body></html>");

		Assert.IsTrue(document.TryQuerySelector(out var title, element: "title"));
		Assert.AreEqual("Hello", title.TextContent);
		Assert.IsTrue(document.TryQuerySelector(out var link, element: "a"));
		Assert.IsTrue(link.TryGetAttribute("href", out var href));
		Assert.AreEqual("/x", href.Value);
	}

	[TestMethod]
	public void CanBeChained()
	{
		var document = LazyHtmlDocument.Parse("<html><body><ul><li><a>1</a></li></ul></body></html>");

		Assert.IsTrue(document.TryQuerySelector(out var body, element: "body"));
		Assert.IsTrue(body.TryQuerySelector(out var list, element: "ul"));
		Assert.IsTrue(list.TryQuerySelector(out var link, element: "a"));
		Assert.AreEqual("1", link.InnerSpan.ToString());
	}

	[TestMethod]
	public void AgreesWithQuerySelectorAll()
	{
		var document = LazyHtmlDocument.Parse("<div><p>a<br>b</p><ul><li>1<li>2</ul><script>x</script><p>c</p></div>");

		foreach (var name in new[] { "div", "p", "br", "li", "script", "ul", "zzz" })
		{
			var all = document.QuerySelectorAll(element: name).ToList();
			var found = document.TryQuerySelector(out var first, element: name);

			Assert.AreEqual(all.Count > 0, found, $"Mismatch for <{name}>.");
			if (found)
				Assert.AreEqual(all[0].OuterSpan.ToString(), first.OuterSpan.ToString(), $"Mismatch for <{name}>.");
		}
	}

	[TestMethod]
	public void OmittedOrNullElementMatchesTheFirstElementOfAnyName()
	{
		var document = LazyHtmlDocument.Parse("text<!-- c --><html><body></body></html>");
		var body = TestHelpers.FirstElement("<body><p><b>x</b></p></body>");

		Assert.IsTrue(document.TryQuerySelector(out var root));
		Assert.AreEqual("html", root.Name);
		Assert.IsTrue(body.TryQuerySelector(out var first, element: null));
		Assert.AreEqual("p", first.Name);
	}

	[TestMethod]
	public void MatchesByElementAndAttribute()
	{
		var document = LazyHtmlDocument.Parse("<a class=\"link\">1</a><div><a class=\"btn\">2</a></div><a class=\"btn\">3</a>");

		Assert.IsTrue(document.TryQuerySelector(out var element, element: "a", attributes: [new("class", "btn")]));
		Assert.AreEqual("<a class=\"btn\">2</a>", element.OuterSpan.ToString());
	}

	[TestMethod]
	public void MatchesByAttributeOnly()
	{
		var document = LazyHtmlDocument.Parse("<div id=\"x\"><p id=\"main\">1</p></div><span id=\"main\">2</span>");

		Assert.IsTrue(document.TryQuerySelector(out var element, attributes: [new("id", "main")]));
		Assert.AreEqual("<p id=\"main\">1</p>", element.OuterSpan.ToString());
	}

	[TestMethod]
	public void ReturnsFalseWhenAttributesDoNotMatch()
	{
		var document = LazyHtmlDocument.Parse("<a class=\"btn\" rel=\"nofollow\">1</a>");

		Assert.IsFalse(document.TryQuerySelector(out var element, element: "a", attributes: [new("class", "btn"), new("rel", "author")]));
		Assert.AreEqual(default, element);
	}

	[TestMethod]
	public void MatchesByIdAttribute()
	{
		var document = LazyHtmlDocument.Parse("<div id=\"x\"><p id=\"main\">1</p></div><span id=\"main\">2</span>");

		Assert.IsTrue(document.TryQuerySelector(out var element, attributes: [new("id", "main")]));
		Assert.AreEqual("<p id=\"main\">1</p>", element.OuterSpan.ToString());
		Assert.IsFalse(document.TryQuerySelector(out _, attributes: [new("id", "Main")]));
		Assert.IsFalse(document.TryQuerySelector(out _, attributes: [new("id", "missing")]));
	}

	[TestMethod]
	public void EmptyIdAttributeMatchesTheFirstPresentEmptyId()
	{
		var document = LazyHtmlDocument.Parse("<div><a>0</a><a id>1</a><a id=\"\">2</a></div>");
		var div = document.Elements().ToList()[0];

		Assert.IsTrue(document.TryQuerySelector(out var first, attributes: [new("id", "")]));
		Assert.AreEqual("<a id>1</a>", first.OuterSpan.ToString());
		Assert.IsTrue(div.TryQuerySelector(out var child, attributes: [new("id", "")]));
		Assert.AreEqual(first, child);
	}

	[TestMethod]
	public void MatchesByClassName()
	{
		var document = LazyHtmlDocument.Parse("<a class=\"button\">1</a><div><a class=\"big btn\">2</a></div><a class=\"btn\">3</a>");

		Assert.IsTrue(document.TryQuerySelector(out var element, className: "btn"));
		Assert.AreEqual("<a class=\"big btn\">2</a>", element.OuterSpan.ToString());
		Assert.IsFalse(document.TryQuerySelector(out _, className: "Btn"));
		Assert.IsFalse(document.TryQuerySelector(out _, className: "bt"));
	}

	[TestMethod]
	public void MatchesByElementClassNameAndAttributes()
	{
		var document = LazyHtmlDocument.Parse("<a id=\"x\" class=\"btn\">1</a><span id=\"x\" class=\"btn\" href=\"/\">2</span><a id=\"x\" class=\"a btn\" href=\"/\">3</a>");

		Assert.IsTrue(document.TryQuerySelector(out var element, element: "a", className: "btn", attributes: [new("id", "x"), new("href", "/")]));
		Assert.AreEqual("3", element.InnerSpan.ToString());
	}

	[TestMethod]
	public void ElementMatchesByIdAttributeAndClassNameOnlyInsideItsContent()
	{
		var document = LazyHtmlDocument.Parse("<a id=\"x\" class=\"c\">0</a><div><a id=\"x\" class=\"c\">1</a></div>");
		var div = document.Elements().ToList()[1];

		Assert.IsTrue(div.TryQuerySelector(out var byId, attributes: [new("id", "x")]));
		Assert.AreEqual("1", byId.InnerSpan.ToString());
		Assert.IsTrue(div.TryQuerySelector(out var byClass, className: "c"));
		Assert.AreEqual("1", byClass.InnerSpan.ToString());
	}

	[TestMethod]
	public void QuerySelectorReturnsFirstMatchOrNull()
	{
		var document = LazyHtmlDocument.Parse("<div><a class=\"btn\" id=\"x\">1</a><a class=\"big btn\" id=\"y\">2</a></div><a class=\"btn\" id=\"y\">3</a>");
		var div = document.Elements().ToList()[0];

		Assert.AreEqual("2", document.QuerySelector(element: "a", className: "btn", attributes: [new("id", "y")])?.InnerSpan.ToString());
		Assert.AreEqual("2", div.QuerySelector(element: "a", className: "btn", attributes: [new("id", "y")])?.InnerSpan.ToString());
		Assert.AreEqual("div", document.QuerySelector()?.Name);
		Assert.AreEqual("a", div.QuerySelector(element: null)?.Name);
		Assert.IsNull(document.QuerySelector(element: "span"));
		Assert.IsNull(div.QuerySelector(element: "div"));
	}

	[TestMethod]
	public void GetElementByIdFindsTheFirstElementWithTheId()
	{
		var document = LazyHtmlDocument.Parse("<div id=\"x\"><p ID=\"main\">1</p></div><span id=\"main\">2</span>");
		var div = document.Elements().ToList()[0];

		Assert.AreEqual("<p ID=\"main\">1</p>", document.GetElementById("main")?.OuterSpan.ToString());
		Assert.AreEqual("<p ID=\"main\">1</p>", div.GetElementById("main")?.OuterSpan.ToString());
		Assert.IsNull(document.GetElementById("Main"));
		Assert.IsNull(div.GetElementById("Main"));
		Assert.IsNull(document.GetElementById("missing"));
		Assert.IsNull(div.GetElementById("missing"));
		Assert.IsNull(div.GetElementById("x"));
	}

	[TestMethod]
	public void GetElementsByNameFindsAllDescendantsWithTheNameAttribute()
	{
		var document = LazyHtmlDocument.Parse("<input name=\"q\"><form name=\"q\"><input name=\"q\"><fieldset><input NAME=\"q\"><select name=\"q\"></select></fieldset><textarea name=\"q\"></textarea></form><button name=\"q\"></button>");
		var form = document.Elements().ToList()[1];
		var matches = document.GetElementsByName(name: "q");
		var descendants = form.GetElementsByName(name: "q");
		var names = matches.Names();

		Assert.AreSequenceEqual(["input", "form", "input", "input", "select", "textarea", "button"], names);
		Assert.AreSequenceEqual(["input", "input", "select", "textarea"], descendants.Names());
		Assert.AreSequenceEqual(names, matches.Names());
		Assert.IsFalse(document.GetElementsByName("missing").MoveNext());
		Assert.IsFalse(form.GetElementsByName("missing").MoveNext());
	}

	[TestMethod]
	[DataRow("<input NAME='q'><input name='Q'><q></q><input id='q'><input name='q-extra'><input name=' q'><input name='q q'>", "q", "<input NAME='q'>")]
	[DataRow("<input name='x' name='q'><input name='q' name='x'>", "q", "<input name='q' name='x'>")]
	[DataRow("<input name='a&amp;b'><input name='a&b'>", "a&amp;b", "<input name='a&amp;b'>")]
	[DataRow("<input name='first last'><input name='first'><input name='last'>", "first last", "<input name='first last'>")]
	[DataRow("<script><input name='q'></script><!--<input name='q'>--><input name='q'>", "q", "<input name='q'>")]
	public void GetElementsByNameUsesAttributeMatchingRules(string html, string name, string expected)
	{
		var document = LazyHtmlDocument.Parse("<form>" + html + "</form>");
		var form = document.Elements().ToList()[0];

		Assert.AreSequenceEqual([expected], document.GetElementsByName(name).Outers());
		Assert.AreSequenceEqual([expected], form.GetElementsByName(name).Outers());
	}

	[TestMethod]
	public void GetElementsByNameRejectsNullOrEmptyNames()
	{
		var document = LazyHtmlDocument.Parse("<form></form>");
		var form = document.Elements().ToList()[0];

		Assert.AreEqual("name", Assert.ThrowsExactly<ArgumentNullException>(() => document.GetElementsByName(null!)).ParamName);
		Assert.AreEqual("name", Assert.ThrowsExactly<ArgumentNullException>(() => form.GetElementsByName(null!)).ParamName);
		Assert.AreEqual("name", Assert.ThrowsExactly<ArgumentException>(() => document.GetElementsByName("")).ParamName);
		Assert.AreEqual("name", Assert.ThrowsExactly<ArgumentException>(() => form.GetElementsByName("")).ParamName);
	}

	[TestMethod]
	public void GetElementsByTagNameFindsAllDescendantsWithTheName()
	{
		var document = LazyHtmlDocument.Parse("<div><p>1</p><span><p>2</p></span></div><p>3</p>");
		var div = document.Elements().ToList()[0];

		Assert.AreSequenceEqual(["<p>1</p>", "<p>2</p>", "<p>3</p>"], document.GetElementsByTagName("p").Outers());
		Assert.AreSequenceEqual(["<p>1</p>", "<p>2</p>"], div.GetElementsByTagName("P").Outers());
	}

	[TestMethod]
	public void ShortcutsRejectNullOrEmptyArguments()
	{
		var document = LazyHtmlDocument.Parse("<a></a>");
		var element = TestHelpers.FirstElement("<a></a>");

		Assert.ThrowsExactly<ArgumentNullException>(() => document.GetElementById(null!));
		Assert.ThrowsExactly<ArgumentNullException>(() => element.GetElementById(null!));
		Assert.ThrowsExactly<ArgumentException>(() => document.GetElementById(""));
		Assert.ThrowsExactly<ArgumentException>(() => element.GetElementById(""));
		Assert.ThrowsExactly<ArgumentNullException>(() => document.GetElementsByTagName(null!));
		Assert.ThrowsExactly<ArgumentException>(() => element.GetElementsByTagName(""));
	}

	[TestMethod]
	public void EmptyElementThrows()
	{
		var document = LazyHtmlDocument.Parse("<a></a>");
		var element = TestHelpers.FirstElement("<a></a>");

		Assert.AreEqual("element", Assert.ThrowsExactly<ArgumentException>(() => document.TryQuerySelector(out _, element: "")).ParamName);
		Assert.AreEqual("element", Assert.ThrowsExactly<ArgumentException>(() => element.TryQuerySelector(out _, element: "")).ParamName);
		Assert.AreEqual("element", Assert.ThrowsExactly<ArgumentException>(() => document.QuerySelector(element: "")).ParamName);
		Assert.AreEqual("element", Assert.ThrowsExactly<ArgumentException>(() => element.QuerySelector(element: "")).ParamName);
	}

	[TestMethod]
	public void InvalidClassNameThrows()
	{
		var document = LazyHtmlDocument.Parse("<a></a>");
		var element = TestHelpers.FirstElement("<a></a>");

		Assert.ThrowsExactly<ArgumentException>(() => document.TryQuerySelector(out _, className: ""));
		Assert.ThrowsExactly<ArgumentException>(() => element.TryQuerySelector(out _, className: ""));
		Assert.ThrowsExactly<ArgumentException>(() => element.TryQuerySelector(out _, className: "a b"));
	}

	private static KeyValuePair<string, string>[] Attributes(params (string Name, string Value)[] attributes)
		=> Array.ConvertAll(attributes, a => KeyValuePair.Create(a.Name, a.Value));
}
