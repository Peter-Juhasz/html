using Microsoft.Extensions.Primitives;
using System.Text.Html.Lazy;
using System.Text.Html.Model;

namespace PeterJuhasz.Text.Html.Tests.Model;

[TestClass]
public sealed class DocumentTests
{
	[TestMethod]
	public void EmptyDocumentHasNoElements()
	{
		var document = HtmlDocument.Parse("");

		Assert.IsEmpty(document.Elements());
		Assert.IsEmpty(document.Descendants());
	}

	[TestMethod]
	public void TextOnlyDocumentHasNoElements()
	{
		var document = HtmlDocument.Parse("just some text");

		Assert.IsEmpty(document.Elements());
	}

	[TestMethod]
	public void ParsesSingleRootElement()
	{
		var document = HtmlDocument.Parse("<html><body></body></html>");

		CollectionAssert.AreEqual(new[] { "html" }, document.Elements().Names());
	}

	[TestMethod]
	public void ParsesMultipleRootElements()
	{
		var document = HtmlDocument.Parse("<a></a><b></b><c></c>");

		CollectionAssert.AreEqual(new[] { "a", "b", "c" }, document.Elements().Names());
	}

	[TestMethod]
	public void SkipsTextBetweenRootElements()
	{
		var document = HtmlDocument.Parse("before <a>x</a> between <b>y</b> after");

		CollectionAssert.AreEqual(new[] { "a", "b" }, document.Elements().Names());
	}

	[TestMethod]
	public void SkipsDoctypeCommentsAndProcessingInstructions()
	{
		var document = HtmlDocument.Parse("<!DOCTYPE html><!-- <a></a> --><?xml version=\"1.0\"?>\r\n<html>\r\n</html>\r\n");

		CollectionAssert.AreEqual(new[] { "html" }, document.Elements().Names());
	}

	[TestMethod]
	public void RootElementsHaveNoParentAndReferenceTheDocument()
	{
		var document = HtmlDocument.Parse("<a></a><b></b>");

		foreach (var element in document.Elements())
		{
			Assert.IsNull(element.Parent);
			Assert.AreSame(document, element.Document);
		}
	}

	[TestMethod]
	public void ParsesStringSegmentWithinItsBounds()
	{
		var html = "<a>outside</a><div><a>inside</a></div><a>outside</a>";
		var segment = new StringSegment(html, html.IndexOf("<div>", StringComparison.Ordinal), "<div><a>inside</a></div>".Length);
		var document = HtmlDocument.Parse(segment);

		CollectionAssert.AreEqual(new[] { "div" }, document.Elements().Names());
		CollectionAssert.AreEqual(new[] { "<a>inside</a>" }, document.Descendants().Where(e => e.Name == "a").Outers());
	}

	[TestMethod]
	public void ParsesLazyDocument()
	{
		var lazy = LazyHtmlDocument.Parse("<ul><li>1</li><li>2</li></ul>");
		var document = HtmlDocument.Parse(lazy);

		Assert.HasCount(1, document.Elements());
		Assert.AreEqual("ul", document.Elements().First().Name);
		CollectionAssert.AreEqual(new[] { "1", "2" }, document.Elements().First().Elements().Inners());
	}

	[TestMethod]
	public void ParsingDefaultLazyDocumentYieldsNoElements()
	{
		var document = HtmlDocument.Parse(default(LazyHtmlDocument));

		Assert.IsEmpty(document.Elements());
	}

	[TestMethod]
	public void DescendantsAreInDocumentOrder()
	{
		var document = HtmlDocument.Parse("<x><i>1</i><y><i>2</i><z><i>3</i></z><i>4</i></y><i>5</i></x><w></w>");

		CollectionAssert.AreEqual(new[] { "x", "i", "y", "i", "z", "i", "i", "i", "w" }, document.Descendants().Names());
	}

	[TestMethod]
	public void DescendantsCanBeEnumeratedRepeatedly()
	{
		var document = HtmlDocument.Parse("<a><b></b></a>");
		var descendants = document.Descendants();

		Assert.HasCount(2, descendants.ToList());
		Assert.HasCount(2, descendants.ToList());
	}
}
