using System.Text.Html.Model;

namespace PeterJuhasz.Text.Html.Tests.Model;

[TestClass]
public sealed class ContentTests
{
	[TestMethod]
	public void OuterSpanIsTheWholeElement()
	{
		var document = HtmlDocument.Parse("before<div class=\"a\">text<b>bold</b></div>after");

		Assert.AreEqual("<div class=\"a\">text<b>bold</b></div>", document.Elements().First().OuterSpan.ToString());
		Assert.AreEqual("<b>bold</b>", document.Elements().First().Elements().First().OuterSpan.ToString());
	}

	[TestMethod]
	public void InnerSpanIsTheContent()
	{
		var element = TestHelpers.FirstElement("<div class=\"a\">text<b>bold</b></div>");

		Assert.AreEqual("text<b>bold</b>", element.InnerSpan.ToString());
		Assert.AreEqual("bold", element.Elements().First().InnerSpan.ToString());
	}

	[TestMethod]
	public void VoidElementHasEmptyInnerSpan()
	{
		var element = TestHelpers.FirstElement("<br>text");

		Assert.AreEqual("<br>", element.OuterSpan.ToString());
		Assert.IsTrue(element.InnerSpan.IsEmpty);
		Assert.AreEqual("", element.TextContent);
	}

	[TestMethod]
	public void TextContentConcatenatesTextOfDescendants()
	{
		var element = TestHelpers.FirstElement("<p>a<b>b<i>c</i>d</b>e<!-- x --><br>f</p>");

		Assert.AreEqual("abcdef", element.TextContent);
	}

	[TestMethod]
	public void TextContentIncludesRawText()
	{
		var element = TestHelpers.FirstElement("<div>a<script>if (x < y) { b(); }</script>c</div>");

		Assert.AreEqual("aif (x < y) { b(); }c", element.TextContent);
		Assert.AreEqual("if (x < y) { b(); }", element.Elements().First().TextContent);
	}

	[TestMethod]
	public void TextContentIsNotDecoded()
	{
		var element = TestHelpers.FirstElement("<p>a &amp; b</p>");

		Assert.AreEqual("a &amp; b", element.TextContent);
	}

	[TestMethod]
	public void UnclosedElementExtendsToEnd()
	{
		var element = TestHelpers.FirstElement("<div>text");

		Assert.AreEqual("<div>text", element.OuterSpan.ToString());
		Assert.AreEqual("text", element.TextContent);
	}

	[TestMethod]
	public void ToStringIsTheOuterHtml()
	{
		var element = TestHelpers.FirstElement("<a href=\"/\">x</a>");

		Assert.AreEqual("<a href=\"/\">x</a>", element.ToString());
	}
}
