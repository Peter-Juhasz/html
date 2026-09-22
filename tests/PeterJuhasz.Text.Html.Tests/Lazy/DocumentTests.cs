using Microsoft.Extensions.Primitives;

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

		Assert.AreSequenceEqual(["html"], document.Elements().Names());
	}

	[TestMethod]
	public void EnumeratesMultipleRootElements()
	{
		var document = LazyHtmlDocument.Parse("<a></a><b></b><c></c>");

		Assert.AreSequenceEqual(["a", "b", "c"], document.Elements().Names());
	}

	[TestMethod]
	public void SkipsTextBetweenRootElements()
	{
		var document = LazyHtmlDocument.Parse("before <a>x</a> between <b>y</b> after");

		Assert.AreSequenceEqual(["a", "b"], document.Elements().Names());
	}

	[TestMethod]
	public void SkipsDoctypeAndWhitespace()
	{
		var document = LazyHtmlDocument.Parse("<!DOCTYPE html>\r\n<html>\r\n</html>\r\n");

		Assert.AreSequenceEqual(["html"], document.Elements().Names());
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

	[TestMethod]
	[DataRow("html", "head", "body")]
	[DataRow("HTML", "HEAD", "BODY")]
	[DataRow("HtMl", "HeAd", "BoDy")]
	public void TryGetHeadAndBodyFindChildrenOfHtml(string htmlTag, string headTag, string bodyTag)
	{
		var headHtml = $"<{headTag}><title>Title</title></{headTag}>";
		var bodyHtml = $"<{bodyTag}><p>Content</p></{bodyTag}>";
		var document = LazyHtmlDocument.Parse($"<!DOCTYPE html>before<!-- root --><div></div><{htmlTag}>between<!-- child --><meta charset=utf-8>{headHtml}<link>{bodyHtml}</{htmlTag}>");

		Assert.IsTrue(document.TryGetHead(out var head));
		Assert.IsTrue(document.TryGetBody(out var body));
		Assert.AreEqual(headHtml, head.OuterSpan.ToString());
		Assert.AreEqual(bodyHtml, body.OuterSpan.ToString());
		Assert.AreEqual("Title", head.TextContent);
		Assert.AreEqual("Content", body.TextContent);
	}

	[TestMethod]
	[DataRow("<head>H</head><body>B</body>")]
	[DataRow("<section><div><head>H</head><body>B</body></div></section>")]
	[DataRow("<html><div><head>H</head><body>B</body></div></html>")]
	[DataRow("<html></html><html><head>H</head><body>B</body></html>")]
	public void TryGetHeadAndBodyFindSectionsOutsideTheUsualLayout(string html)
	{
		var document = LazyHtmlDocument.Parse(html);

		Assert.IsTrue(document.TryGetHead(out var head));
		Assert.IsTrue(document.TryGetBody(out var body));
		Assert.AreEqual("<head>H</head>", head.OuterSpan.ToString());
		Assert.AreEqual("<body>B</body>", body.OuterSpan.ToString());
	}

	[TestMethod]
	[DataRow("<head>H1</head><head>H2</head><body>B1</body><body>B2</body>")]
	[DataRow("<html><head>H1</head><head>H2</head><body>B1</body><body>B2</body></html>")]
	public void TryGetHeadAndBodyReturnTheFirstMatchingSections(string html)
	{
		var document = LazyHtmlDocument.Parse(html);

		Assert.IsTrue(document.TryGetHead(out var head));
		Assert.IsTrue(document.TryGetBody(out var body));
		Assert.AreEqual("<head>H1</head>", head.OuterSpan.ToString());
		Assert.AreEqual("<body>B1</body>", body.OuterSpan.ToString());
	}

	[TestMethod]
	[DataRow("", false, false)]
	[DataRow("just text", false, false)]
	[DataRow("<html><!-- no sections --></html>", false, false)]
	[DataRow("<html><head>H</head></html>", true, false)]
	[DataRow("<html><body>B</body></html>", false, true)]
	[DataRow("<header></header><bodyguard></bodyguard><input name=head><div id=body title='<head></head><body></body>'></div>", false, false)]
	[DataRow("<!--<head></head><body></body>--><script><head></head><body></body></script><style><head></head><body></body></style><textarea><head></head><body></body></textarea><title><head></head><body></body></title>", false, false)]
	public void TryGetHeadAndBodyReturnFalseAndDefaultForMissingSections(string html, bool hasHead, bool hasBody)
	{
		var document = LazyHtmlDocument.Parse(html);

		Assert.AreEqual(hasHead, document.TryGetHead(out var head));
		Assert.AreEqual(hasBody, document.TryGetBody(out var body));
		if (hasHead)
			Assert.AreEqual("<head>H</head>", head.OuterSpan.ToString());
		else
			Assert.AreEqual(default, head);
		if (hasBody)
			Assert.AreEqual("<body>B</body>", body.OuterSpan.ToString());
		else
			Assert.AreEqual(default, body);
	}

	[TestMethod]
	public void DefaultDocumentHasNoHeadOrBody()
	{
		var document = default(LazyHtmlDocument);

		Assert.IsFalse(document.TryGetHead(out var head));
		Assert.AreEqual(default, head);
		Assert.IsFalse(document.TryGetBody(out var body));
		Assert.AreEqual(default, body);
	}

	[TestMethod]
	[DataRow("<html><head>H</head><body>B</body></html>", true)]
	[DataRow("<div>no sections</div>", false)]
	public void TryGetHeadAndBodyRespectStringSegmentBounds(string content, bool hasSections)
	{
		const string outside = "<head>outside</head><body>outside</body>";
		var html = outside + content + outside;
		var document = LazyHtmlDocument.Parse(new StringSegment(html, outside.Length, content.Length));

		Assert.AreEqual(hasSections, document.TryGetHead(out var head));
		Assert.AreEqual(hasSections, document.TryGetBody(out var body));
		if (hasSections)
		{
			Assert.AreEqual("<head>H</head>", head.OuterSpan.ToString());
			Assert.AreEqual("<body>B</body>", body.OuterSpan.ToString());
		}
		else
		{
			Assert.AreEqual(default, head);
			Assert.AreEqual(default, body);
		}
	}
}
