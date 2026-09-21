using System.Text.Html.Lazy;

namespace PeterJuhasz.Text.Html.Tests.Lazy;

[TestClass]
public sealed class TryQuerySelectorTests
{
	[TestMethod]
	public void ReturnsFirstMatchInDocumentOrder()
	{
		var document = new LazyHtmlDocument("<div><p><a>1</a></p><a>2</a></div><a>3</a>");

		Assert.IsTrue(document.TryQuerySelector("a", out var element));
		Assert.AreEqual("<a>1</a>", element.OuterSpan.ToString());
	}

	[TestMethod]
	public void ReturnsFalseWhenNothingMatches()
	{
		var document = new LazyHtmlDocument("<div><p>text</p></div>");

		Assert.IsFalse(document.TryQuerySelector("a", out var element));
		Assert.AreEqual(default, element);
	}

	[TestMethod]
	public void EmptyAndDefaultDocumentsReturnFalse()
	{
		Assert.IsFalse(new LazyHtmlDocument("").TryQuerySelector("a", out _));
		Assert.IsFalse(default(LazyHtmlDocument).TryQuerySelector("a", out _));
		Assert.IsFalse(new LazyHtmlDocument("just text").TryQuerySelector("a", out _));
	}

	[TestMethod]
	public void ElementSearchesOnlyItsOwnContent()
	{
		var document = new LazyHtmlDocument("<a>0</a><div><span><a>1</a></span></div><a>2</a>");
		var div = document.Elements().ToList()[1];

		Assert.IsTrue(div.TryQuerySelector("a", out var element));
		Assert.AreEqual("<a>1</a>", element.OuterSpan.ToString());
	}

	[TestMethod]
	public void ElementDoesNotMatchItself()
	{
		var element = TestHelpers.FirstElement("<div><span></span></div>");

		Assert.IsFalse(element.TryQuerySelector("div", out _));
	}

	[TestMethod]
	public void ElementReturnsFalseWhenNothingMatches()
	{
		var element = TestHelpers.FirstElement("<div><span></span></div><a>after</a>");

		Assert.IsFalse(element.TryQuerySelector("a", out var found));
		Assert.AreEqual(default, found);
	}

	[TestMethod]
	public void NameComparisonIsCaseInsensitive()
	{
		var document = new LazyHtmlDocument("<DIV>x</DIV>");

		Assert.IsTrue(document.TryQuerySelector("div", out var element));
		Assert.AreEqual("<DIV>x</DIV>", element.OuterSpan.ToString());
	}

	[TestMethod]
	public void DoesNotLookInsideRawTextContent()
	{
		var document = new LazyHtmlDocument("<script><b>fake</b></script><b>real</b>");

		Assert.IsTrue(document.TryQuerySelector("b", out var element));
		Assert.AreEqual("<b>real</b>", element.OuterSpan.ToString());
	}

	[TestMethod]
	public void RawTextElementReturnsFalse()
	{
		var script = TestHelpers.FirstElement("<script><b>not markup</b></script>");

		Assert.IsFalse(script.TryQuerySelector("b", out _));
	}

	[TestMethod]
	public void FoundElementExposesItsContent()
	{
		var document = new LazyHtmlDocument("<html><head><title>Hello</title></head><body><a href=\"/x\">link</a></body></html>");

		Assert.IsTrue(document.TryQuerySelector("title", out var title));
		Assert.AreEqual("Hello", title.TextContent);
		Assert.IsTrue(document.TryQuerySelector("a", out var link));
		Assert.IsTrue(link.TryGetAttribute("href", out var href));
		Assert.AreEqual("/x", href.Value);
	}

	[TestMethod]
	public void CanBeChained()
	{
		var document = new LazyHtmlDocument("<html><body><ul><li><a>1</a></li></ul></body></html>");

		Assert.IsTrue(document.TryQuerySelector("body", out var body));
		Assert.IsTrue(body.TryQuerySelector("ul", out var list));
		Assert.IsTrue(list.TryQuerySelector("a", out var link));
		Assert.AreEqual("1", link.InnerSpan.ToString());
	}

	[TestMethod]
	public void AgreesWithQuerySelectorAll()
	{
		var document = new LazyHtmlDocument("<div><p>a<br>b</p><ul><li>1<li>2</ul><script>x</script><p>c</p></div>");

		foreach (var name in new[] { "div", "p", "br", "li", "script", "ul", "zzz" })
		{
			var all = document.QuerySelectorAll(name).ToList();
			var found = document.TryQuerySelector(name, out var first);

			Assert.AreEqual(all.Count > 0, found, $"Mismatch for <{name}>.");
			if (found)
				Assert.AreEqual(all[0].OuterSpan.ToString(), first.OuterSpan.ToString(), $"Mismatch for <{name}>.");
		}
	}

	[TestMethod]
	public void NullOrEmptyNameThrows()
	{
		var document = new LazyHtmlDocument("<a></a>");
		var element = TestHelpers.FirstElement("<a></a>");

		Assert.ThrowsExactly<ArgumentNullException>(() => document.TryQuerySelector(null!, out _));
		Assert.ThrowsExactly<ArgumentException>(() => document.TryQuerySelector("", out _));
		Assert.ThrowsExactly<ArgumentNullException>(() => element.TryQuerySelector(null!, out _));
		Assert.ThrowsExactly<ArgumentException>(() => element.TryQuerySelector("", out _));
	}
}
