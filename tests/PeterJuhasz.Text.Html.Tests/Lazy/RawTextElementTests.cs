using System.Text.Html.Lazy;

namespace PeterJuhasz.Text.Html.Tests.Lazy;

[TestClass]
public sealed class RawTextElementTests
{
	[TestMethod]
	[DataRow("script")]
	[DataRow("style")]
	[DataRow("textarea")]
	[DataRow("title")]
	public void MarkupInsideRawTextElementIsNotParsed(string name)
	{
		var html = $"<{name}>a <b>not an element</b> c</{name}><span></span>";
		var element = TestHelpers.FirstElement(html);

		Assert.AreEqual($"<{name}>a <b>not an element</b> c</{name}>", element.OuterSpan.ToString());
		Assert.AreEqual("a <b>not an element</b> c", element.InnerSpan.ToString());
		Assert.IsFalse(element.Elements().MoveNext());
	}

	[TestMethod]
	public void ScriptContentWithComparisonOperators()
	{
		var element = TestHelpers.FirstElement("<script>if (a < b && c > d) { }</script>");

		Assert.AreEqual("if (a < b && c > d) { }", element.InnerSpan.ToString());
		Assert.AreEqual("if (a < b && c > d) { }", element.TextContent);
	}

	[TestMethod]
	public void ScriptContentWithEndTagOfOtherElement()
	{
		var element = TestHelpers.FirstElement("<script>document.write('</div>');</script>");

		Assert.AreEqual("document.write('</div>');", element.InnerSpan.ToString());
	}

	[TestMethod]
	public void ScriptContentWithSimilarEndTagPrefix()
	{
		var element = TestHelpers.FirstElement("<script>x = '</scripts>';</script>");

		Assert.AreEqual("x = '</scripts>';", element.InnerSpan.ToString());
	}

	[TestMethod]
	public void EndTagIsCaseInsensitive()
	{
		var element = TestHelpers.FirstElement("<script>x</SCRIPT>tail");

		Assert.AreEqual("<script>x</SCRIPT>", element.OuterSpan.ToString());
	}

	[TestMethod]
	public void EndTagMayContainWhitespace()
	{
		var element = TestHelpers.FirstElement("<style>a{}</style >tail");

		Assert.AreEqual("<style>a{}</style >", element.OuterSpan.ToString());
	}

	[TestMethod]
	public void RawTextElementWithAttributes()
	{
		var element = TestHelpers.FirstElement("<script type=\"module\" src=\"x.js\"></script>");

		Assert.IsTrue(element.TryGetAttribute("src", out var src));
		Assert.AreEqual("x.js", src.Value);
		Assert.IsTrue(element.InnerSpan.IsEmpty);
	}

	[TestMethod]
	public void SiblingAfterRawTextElementIsFound()
	{
		var document = LazyHtmlDocument.Parse("<script>'<a>'</script><div></div>");

		CollectionAssert.AreEqual(new[] { "script", "div" }, document.Elements().Names());
	}

	[TestMethod]
	public void RawTextElementInsideParentDoesNotBreakParent()
	{
		var element = TestHelpers.FirstElement("<head><title>a </head> b</title><meta></head>tail");

		Assert.AreEqual("<head><title>a </head> b</title><meta></head>", element.OuterSpan.ToString());
		CollectionAssert.AreEqual(new[] { "title", "meta" }, element.Elements().Names());
	}

	[TestMethod]
	public void UnclosedRawTextElementExtendsToEnd()
	{
		var element = TestHelpers.FirstElement("<script>var x = 1;<div></div>");

		Assert.AreEqual("<script>var x = 1;<div></div>", element.OuterSpan.ToString());
		Assert.AreEqual("var x = 1;<div></div>", element.InnerSpan.ToString());
	}
}
