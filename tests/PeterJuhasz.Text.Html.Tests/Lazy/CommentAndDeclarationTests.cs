namespace PeterJuhasz.Text.Html.Tests.Lazy;

[TestClass]
public sealed class CommentAndDeclarationTests
{
	[TestMethod]
	public void CommentIsNotAnElement()
	{
		var document = LazyHtmlDocument.Parse("<!-- comment --><div></div>");

		Assert.AreSequenceEqual(["div"], document.Elements().Names());
	}

	[TestMethod]
	public void MarkupInsideCommentIsIgnored()
	{
		var document = LazyHtmlDocument.Parse("<!-- <a></a> <b> --><div></div>");

		Assert.AreSequenceEqual(["div"], document.Elements().Names());
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
		var document = LazyHtmlDocument.Parse("<!-- a > b --><div></div>");

		Assert.AreSequenceEqual(["div"], document.Elements().Names());
	}

	[TestMethod]
	public void CommentContainingDashesIsSkipped()
	{
		var document = LazyHtmlDocument.Parse("<!-- a -- b - c --><div></div>");

		Assert.AreSequenceEqual(["div"], document.Elements().Names());
	}

	[TestMethod]
	public void MultilineCommentIsSkipped()
	{
		var document = LazyHtmlDocument.Parse("<!--\r\n<a>\r\n-->\r\n<div></div>");

		Assert.AreSequenceEqual(["div"], document.Elements().Names());
	}

	[TestMethod]
	public void DoctypeIsNotAnElement()
	{
		var document = LazyHtmlDocument.Parse("<!DOCTYPE html><html></html>");

		Assert.AreSequenceEqual(["html"], document.Elements().Names());
	}

	[TestMethod]
	public void LegacyDoctypeWithQuotesIsSkipped()
	{
		var document = LazyHtmlDocument.Parse("<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Strict//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd\"><html></html>");

		Assert.AreSequenceEqual(["html"], document.Elements().Names());
	}

	[TestMethod]
	public void ProcessingInstructionIsSkipped()
	{
		var document = LazyHtmlDocument.Parse("<?xml version=\"1.0\"?><root></root>");

		Assert.AreSequenceEqual(["root"], document.Elements().Names());
	}

	[TestMethod]
	public void CDataIsSkipped()
	{
		var element = TestHelpers.FirstElement("<div><![CDATA[ <a> ]]><b></b></div>");

		Assert.AreSequenceEqual(["b"], element.Elements().Names());
	}

	[TestMethod]
	public void ConditionalCommentIsSkipped()
	{
		var document = LazyHtmlDocument.Parse("<!--[if IE]><p>ie</p><![endif]--><div></div>");

		Assert.AreSequenceEqual(["div"], document.Elements().Names());
	}
}
