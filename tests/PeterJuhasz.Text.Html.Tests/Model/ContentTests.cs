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
	public void TextContentIsDecoded()
	{
		var element = TestHelpers.FirstElement("<p>a &amp; <b>b &lt; c</b></p>");

		Assert.AreEqual("a & b < c", element.TextContent);
	}

	[TestMethod]
	public void TextContentIsCached()
	{
		var element = TestHelpers.FirstElement("<p>a &amp; <b>b &lt; c</b></p>");

		var first = element.TextContent;
		var second = element.TextContent;

		Assert.AreSame(first, second);
	}

	[TestMethod]
	public void TextContentOfASingleTextChildIsTheTextOfTheChild()
	{
		var element = TestHelpers.FirstElement("<p>a &amp; b</p>");
		var text = (HtmlText)element.Nodes.Single();

		Assert.AreEqual("a & b", element.TextContent);
		Assert.AreSame(text.Text, element.TextContent);
	}

	[TestMethod]
	public void TextContentIsTheSameWhicheverNodeIsReadFirst()
	{
		const string html = "<div>a &amp; <p>b<b>c &lt; d</b>e</p><script>f &amp; g</script>h</div>";

		var fresh = TestHelpers.FirstElement(html);
		var expected = fresh.TextContent;

		var childFirst = TestHelpers.FirstElement(html);
		foreach (var child in childFirst.Descendants().Reverse())
		{
			_ = child.TextContent;
		}

		Assert.AreEqual(expected, childFirst.TextContent);

		var textFirst = TestHelpers.FirstElement(html);
		foreach (var text in textFirst.Nodes.OfType<HtmlText>())
		{
			_ = text.Text;
		}

		Assert.AreEqual(expected, textFirst.TextContent);

		Assert.AreEqual("a & bc < def &amp; gh", expected);
	}

	[TestMethod]
	public void TextContentOfRawTextIsNotDecoded()
	{
		var element = TestHelpers.FirstElement("<div>a &amp; <script>x = \"&amp;\";</script><title>&lt;b&gt;</title></div>");

		Assert.AreEqual("a & x = \"&amp;\";<b>", element.TextContent);
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
