using System.Text.Html.Lazy;

namespace PeterJuhasz.Text.Html.Tests.Lazy;

[TestClass]
public sealed class TextContentTests
{
	[TestMethod]
	public void ReturnsPlainText()
	{
		var element = TestHelpers.FirstElement("<p>hello world</p>");

		Assert.AreEqual("hello world", element.TextContent);
	}

	[TestMethod]
	public void ReturnsEmptyForEmptyElement()
	{
		var element = TestHelpers.FirstElement("<p></p>");

		Assert.AreEqual("", element.TextContent);
	}

	[TestMethod]
	public void ReturnsEmptyForVoidElement()
	{
		var element = TestHelpers.FirstElement("<br>text");

		Assert.AreEqual("", element.TextContent);
	}

	[TestMethod]
	public void ConcatenatesTextOfDescendants()
	{
		var element = TestHelpers.FirstElement("<p>a<b>b<i>c</i>d</b>e</p>");

		Assert.AreEqual("abcde", element.TextContent);
	}

	[TestMethod]
	public void PreservesWhitespace()
	{
		var element = TestHelpers.FirstElement("<p>\n  a <b> b </b>\n</p>");

		Assert.AreEqual("\n  a  b \n", element.TextContent);
	}

	[TestMethod]
	public void ExcludesComments()
	{
		var element = TestHelpers.FirstElement("<p>a<!-- hidden -->b</p>");

		Assert.AreEqual("ab", element.TextContent);
	}

	[TestMethod]
	public void ExcludesVoidElementTags()
	{
		var element = TestHelpers.FirstElement("<p>a<br>b<img src=\"x\">c</p>");

		Assert.AreEqual("abc", element.TextContent);
	}

	[TestMethod]
	public void IgnoresCloseAngleInsideAttributeValues()
	{
		var element = TestHelpers.FirstElement("<p><span title=\"x > y\">a</span></p>");

		Assert.AreEqual("a", element.TextContent);
	}

	[TestMethod]
	public void IncludesRawTextOfDescendantScript()
	{
		var element = TestHelpers.FirstElement("<div>a<script>if (x < y) { b(); }</script>c</div>");

		Assert.AreEqual("aif (x < y) { b(); }c", element.TextContent);
	}

	[TestMethod]
	public void ReturnsRawTextOfScriptElement()
	{
		var element = TestHelpers.FirstElement("<script>var s = '<div>';</script>");

		Assert.AreEqual("var s = '<div>';", element.TextContent);
	}

	[TestMethod]
	public void TreatsStrayOpenAngleAsText()
	{
		var element = TestHelpers.FirstElement("<p>a < b</p>");

		Assert.AreEqual("a < b", element.TextContent);
	}

	[TestMethod]
	public void DoesNotDecodeCharacterReferences()
	{
		var element = TestHelpers.FirstElement("<p>a &amp; b</p>");

		Assert.AreEqual("a &amp; b", element.TextContent);
	}

	[TestMethod]
	public void ExcludesTextOutsideElement()
	{
		var element = TestHelpers.FirstElement("before<p>inside</p>after");

		Assert.AreEqual("inside", element.TextContent);
	}
}
