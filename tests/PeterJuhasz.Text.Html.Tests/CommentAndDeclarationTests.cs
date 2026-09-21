using System.Text.Html.Lazy;

namespace PeterJuhasz.Text.Html.Tests;

[TestClass]
public sealed class CommentAndDeclarationTests
{
	[TestMethod]
	public void CommentIsNotAnElement()
	{
		var document = new LazyHtmlDocument("<!-- comment --><div></div>");

		CollectionAssert.AreEqual(new[] { "div" }, document.Elements().Names());
	}

	[TestMethod]
	public void MarkupInsideCommentIsIgnored()
	{
		var document = new LazyHtmlDocument("<!-- <a></a> <b> --><div></div>");

		CollectionAssert.AreEqual(new[] { "div" }, document.Elements().Names());
	}

	[TestMethod]
	public void EndTagInsideCommentDoesNotCloseElement()
	{
		var element = TestHelpers.FirstElement("<div>a<!-- </div> -->b</div>tail");

		Assert.AreEqual("<div>a<!-- </div> -->b</div>", element.OuterSpan.ToString());
		Assert.AreEqual("ab", element.TextContent);
	}

	[TestMethod]
	public void CommentContainingCloseAngleIsSkippedEntirely()
	{
		var document = new LazyHtmlDocument("<!-- a > b --><div></div>");

		CollectionAssert.AreEqual(new[] { "div" }, document.Elements().Names());
	}

	[TestMethod]
	public void CommentContainingDashesIsSkipped()
	{
		var document = new LazyHtmlDocument("<!-- a -- b - c --><div></div>");

		CollectionAssert.AreEqual(new[] { "div" }, document.Elements().Names());
	}

	[TestMethod]
	public void MultilineCommentIsSkipped()
	{
		var document = new LazyHtmlDocument("<!--\r\n<a>\r\n-->\r\n<div></div>");

		CollectionAssert.AreEqual(new[] { "div" }, document.Elements().Names());
	}

	[TestMethod]
	public void DoctypeIsNotAnElement()
	{
		var document = new LazyHtmlDocument("<!DOCTYPE html><html></html>");

		CollectionAssert.AreEqual(new[] { "html" }, document.Elements().Names());
	}

	[TestMethod]
	public void LegacyDoctypeWithQuotesIsSkipped()
	{
		var document = new LazyHtmlDocument("<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Strict//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd\"><html></html>");

		CollectionAssert.AreEqual(new[] { "html" }, document.Elements().Names());
	}

	[TestMethod]
	public void ProcessingInstructionIsSkipped()
	{
		var document = new LazyHtmlDocument("<?xml version=\"1.0\"?><root></root>");

		CollectionAssert.AreEqual(new[] { "root" }, document.Elements().Names());
	}

	[TestMethod]
	public void CDataIsSkipped()
	{
		var element = TestHelpers.FirstElement("<div><![CDATA[ <a> ]]><b></b></div>");

		CollectionAssert.AreEqual(new[] { "b" }, element.Elements().Names());
	}

	[TestMethod]
	public void ConditionalCommentIsSkipped()
	{
		var document = new LazyHtmlDocument("<!--[if IE]><p>ie</p><![endif]--><div></div>");

		CollectionAssert.AreEqual(new[] { "div" }, document.Elements().Names());
	}
}
