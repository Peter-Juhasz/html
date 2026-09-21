using System.Text.Html.Lazy;

namespace PeterJuhasz.Text.Html.Tests.Lazy;

[TestClass]
public sealed class DocumentTests
{
	[TestMethod]
	public void EmptyDocumentHasNoElements()
	{
		var document = LazyHtmlDocument.Parse("");

		Assert.IsFalse(document.Elements().MoveNext());
	}

	[TestMethod]
	public void DefaultDocumentHasNoElements()
	{
		var document = default(LazyHtmlDocument);

		Assert.IsFalse(document.Elements().MoveNext());
	}

	[TestMethod]
	public void TextOnlyDocumentHasNoElements()
	{
		var document = LazyHtmlDocument.Parse("just some text");

		Assert.IsFalse(document.Elements().MoveNext());
	}

	[TestMethod]
	public void EnumeratesSingleRootElement()
	{
		var document = LazyHtmlDocument.Parse("<html><body></body></html>");

		CollectionAssert.AreEqual(new[] { "html" }, document.Elements().Names());
	}

	[TestMethod]
	public void EnumeratesMultipleRootElements()
	{
		var document = LazyHtmlDocument.Parse("<a></a><b></b><c></c>");

		CollectionAssert.AreEqual(new[] { "a", "b", "c" }, document.Elements().Names());
	}

	[TestMethod]
	public void SkipsTextBetweenRootElements()
	{
		var document = LazyHtmlDocument.Parse("before <a>x</a> between <b>y</b> after");

		CollectionAssert.AreEqual(new[] { "a", "b" }, document.Elements().Names());
	}

	[TestMethod]
	public void SkipsDoctypeAndWhitespace()
	{
		var document = LazyHtmlDocument.Parse("<!DOCTYPE html>\r\n<html>\r\n</html>\r\n");

		CollectionAssert.AreEqual(new[] { "html" }, document.Elements().Names());
	}

	[TestMethod]
	public void EnumeratorCanBeIteratedWithForeach()
	{
		var document = LazyHtmlDocument.Parse("<a></a><b></b>");
		var count = 0;

		foreach (var element in document.Elements())
			count++;

		Assert.AreEqual(2, count);
	}

	[TestMethod]
	public void EnumeratorReturnsFalseRepeatedlyAfterEnd()
	{
		var elements = LazyHtmlDocument.Parse("<a></a>").Elements();

		Assert.IsTrue(elements.MoveNext());
		Assert.IsFalse(elements.MoveNext());
		Assert.IsFalse(elements.MoveNext());
	}
}
