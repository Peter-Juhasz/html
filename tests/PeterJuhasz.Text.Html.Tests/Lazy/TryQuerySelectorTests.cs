using System.Text.Html.Lazy;

namespace PeterJuhasz.Text.Html.Tests.Lazy;

[TestClass]
public sealed class TryQuerySelectorTests
{
	[TestMethod]
	public void ReturnsFirstMatchInDocumentOrder()
	{
		var document = LazyHtmlDocument.Parse("<div><p><a>1</a></p><a>2</a></div><a>3</a>");

		Assert.IsTrue(document.TryQuerySelector(out var element, name: "a"));
		Assert.AreEqual("<a>1</a>", element.OuterSpan.ToString());
	}

	[TestMethod]
	public void ReturnsFalseWhenNothingMatches()
	{
		var document = LazyHtmlDocument.Parse("<div><p>text</p></div>");

		Assert.IsFalse(document.TryQuerySelector(out var element, name: "a"));
		Assert.AreEqual(default, element);
	}

	[TestMethod]
	public void EmptyAndDefaultDocumentsReturnFalse()
	{
		Assert.IsFalse(LazyHtmlDocument.Parse("").TryQuerySelector(out _, name: "a"));
		Assert.IsFalse(default(LazyHtmlDocument).TryQuerySelector(out _, name: "a"));
		Assert.IsFalse(LazyHtmlDocument.Parse("just text").TryQuerySelector(out _, name: "a"));
	}

	[TestMethod]
	public void ElementSearchesOnlyItsOwnContent()
	{
		var document = LazyHtmlDocument.Parse("<a>0</a><div><span><a>1</a></span></div><a>2</a>");
		var div = document.Elements().ToList()[1];

		Assert.IsTrue(div.TryQuerySelector(out var element, name: "a"));
		Assert.AreEqual("<a>1</a>", element.OuterSpan.ToString());
	}

	[TestMethod]
	public void ElementDoesNotMatchItself()
	{
		var element = TestHelpers.FirstElement("<div><span></span></div>");

		Assert.IsFalse(element.TryQuerySelector(out _, name: "div"));
	}

	[TestMethod]
	public void ElementReturnsFalseWhenNothingMatches()
	{
		var element = TestHelpers.FirstElement("<div><span></span></div><a>after</a>");

		Assert.IsFalse(element.TryQuerySelector(out var found, name: "a"));
		Assert.AreEqual(default, found);
	}

	[TestMethod]
	public void NameComparisonIsCaseInsensitive()
	{
		var document = LazyHtmlDocument.Parse("<DIV>x</DIV>");

		Assert.IsTrue(document.TryQuerySelector(out var element, name: "div"));
		Assert.AreEqual("<DIV>x</DIV>", element.OuterSpan.ToString());
	}

	[TestMethod]
	public void DoesNotLookInsideRawTextContent()
	{
		var document = LazyHtmlDocument.Parse("<script><b>fake</b></script><b>real</b>");

		Assert.IsTrue(document.TryQuerySelector(out var element, name: "b"));
		Assert.AreEqual("<b>real</b>", element.OuterSpan.ToString());
	}

	[TestMethod]
	public void RawTextElementReturnsFalse()
	{
		var script = TestHelpers.FirstElement("<script><b>not markup</b></script>");

		Assert.IsFalse(script.TryQuerySelector(out _, name: "b"));
	}

	[TestMethod]
	public void FoundElementExposesItsContent()
	{
		var document = LazyHtmlDocument.Parse("<html><head><title>Hello</title></head><body><a href=\"/x\">link</a></body></html>");

		Assert.IsTrue(document.TryQuerySelector(out var title, name: "title"));
		Assert.AreEqual("Hello", title.TextContent);
		Assert.IsTrue(document.TryQuerySelector(out var link, name: "a"));
		Assert.IsTrue(link.TryGetAttribute("href", out var href));
		Assert.AreEqual("/x", href.Value);
	}

	[TestMethod]
	public void CanBeChained()
	{
		var document = LazyHtmlDocument.Parse("<html><body><ul><li><a>1</a></li></ul></body></html>");

		Assert.IsTrue(document.TryQuerySelector(out var body, name: "body"));
		Assert.IsTrue(body.TryQuerySelector(out var list, name: "ul"));
		Assert.IsTrue(list.TryQuerySelector(out var link, name: "a"));
		Assert.AreEqual("1", link.InnerSpan.ToString());
	}

	[TestMethod]
	public void AgreesWithQuerySelectorAll()
	{
		var document = LazyHtmlDocument.Parse("<div><p>a<br>b</p><ul><li>1<li>2</ul><script>x</script><p>c</p></div>");

		foreach (var name in new[] { "div", "p", "br", "li", "script", "ul", "zzz" })
		{
			var all = document.QuerySelectorAll(name: name).ToList();
			var found = document.TryQuerySelector(out var first, name: name);

			Assert.AreEqual(all.Count > 0, found, $"Mismatch for <{name}>.");
			if (found)
				Assert.AreEqual(all[0].OuterSpan.ToString(), first.OuterSpan.ToString(), $"Mismatch for <{name}>.");
		}
	}

	[TestMethod]
	public void OmittedOrNullNameMatchesTheFirstElementOfAnyName()
	{
		var document = LazyHtmlDocument.Parse("text<!-- c --><html><body></body></html>");
		var body = TestHelpers.FirstElement("<body><p><b>x</b></p></body>");

		Assert.IsTrue(document.TryQuerySelector(out var root));
		Assert.AreEqual("html", root.Name);
		Assert.IsTrue(body.TryQuerySelector(out var first, name: null));
		Assert.AreEqual("p", first.Name);
	}

	[TestMethod]
	public void MatchesByNameAndAttribute()
	{
		var document = LazyHtmlDocument.Parse("<a class=\"link\">1</a><div><a class=\"btn\">2</a></div><a class=\"btn\">3</a>");

		Assert.IsTrue(document.TryQuerySelector(out var element, name: "a", attributes: [new("class", "btn")]));
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

		Assert.IsFalse(document.TryQuerySelector(out var element, name: "a", attributes: [new("class", "btn"), new("rel", "author")]));
		Assert.AreEqual(default, element);
	}

	[TestMethod]
	public void MatchesById()
	{
		var document = LazyHtmlDocument.Parse("<div id=\"x\"><p id=\"main\">1</p></div><span id=\"main\">2</span>");

		Assert.IsTrue(document.TryQuerySelector(out var element, id: "main"));
		Assert.AreEqual("<p id=\"main\">1</p>", element.OuterSpan.ToString());
		Assert.IsFalse(document.TryQuerySelector(out _, id: "Main"));
		Assert.IsFalse(document.TryQuerySelector(out _, id: "missing"));
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
	public void MatchesByNameIdClassNameAndAttributes()
	{
		var document = LazyHtmlDocument.Parse("<a id=\"x\" class=\"btn\">1</a><span id=\"x\" class=\"btn\" href=\"/\">2</span><a id=\"x\" class=\"a btn\" href=\"/\">3</a>");

		Assert.IsTrue(document.TryQuerySelector(out var element, name: "a", id: "x", className: "btn", attributes: [new("href", "/")]));
		Assert.AreEqual("3", element.InnerSpan.ToString());
	}

	[TestMethod]
	public void ElementMatchesByIdAndClassNameOnlyInsideItsContent()
	{
		var document = LazyHtmlDocument.Parse("<a id=\"x\" class=\"c\">0</a><div><a id=\"x\" class=\"c\">1</a></div>");
		var div = document.Elements().ToList()[1];

		Assert.IsTrue(div.TryQuerySelector(out var byId, id: "x"));
		Assert.AreEqual("1", byId.InnerSpan.ToString());
		Assert.IsTrue(div.TryQuerySelector(out var byClass, className: "c"));
		Assert.AreEqual("1", byClass.InnerSpan.ToString());
	}

	[TestMethod]
	public void EmptyNameThrows()
	{
		var document = LazyHtmlDocument.Parse("<a></a>");
		var element = TestHelpers.FirstElement("<a></a>");

		Assert.ThrowsExactly<ArgumentException>(() => document.TryQuerySelector(out _, name: ""));
		Assert.ThrowsExactly<ArgumentException>(() => element.TryQuerySelector(out _, name: ""));
		Assert.ThrowsExactly<ArgumentException>(() => document.TryQuerySelector(out _, name: "", attributes: default));
	}

	[TestMethod]
	public void EmptyIdOrInvalidClassNameThrows()
	{
		var document = LazyHtmlDocument.Parse("<a></a>");
		var element = TestHelpers.FirstElement("<a></a>");

		Assert.ThrowsExactly<ArgumentException>(() => document.TryQuerySelector(out _, id: ""));
		Assert.ThrowsExactly<ArgumentException>(() => element.TryQuerySelector(out _, className: ""));
		Assert.ThrowsExactly<ArgumentException>(() => element.TryQuerySelector(out _, className: "a b"));
	}

	private static KeyValuePair<string, string>[] Attributes(params (string Name, string Value)[] attributes)
		=> Array.ConvertAll(attributes, a => KeyValuePair.Create(a.Name, a.Value));
}
