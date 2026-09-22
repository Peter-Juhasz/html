using Microsoft.Extensions.Primitives;

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

		Assert.AreSequenceEqual(["html"], document.Elements().Names());
	}

	[TestMethod]
	public void ParsesMultipleRootElements()
	{
		var document = HtmlDocument.Parse("<a></a><b></b><c></c>");

		Assert.AreSequenceEqual(["a", "b", "c"], document.Elements().Names());
	}

	[TestMethod]
	public void SkipsTextBetweenRootElements()
	{
		var document = HtmlDocument.Parse("before <a>x</a> between <b>y</b> after");

		Assert.AreSequenceEqual(["a", "b"], document.Elements().Names());
	}

	[TestMethod]
	public void SkipsDoctypeCommentsAndProcessingInstructions()
	{
		var document = HtmlDocument.Parse("<!DOCTYPE html><!-- <a></a> --><?xml version=\"1.0\"?>\r\n<html>\r\n</html>\r\n");

		Assert.AreSequenceEqual(["html"], document.Elements().Names());
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

		Assert.AreSequenceEqual(["div"], document.Elements().Names());
		Assert.AreSequenceEqual(["<a>inside</a>"], document.Descendants().Where(e => e.Name == "a").Outers());
	}

	[TestMethod]
	public void ParsesLazyDocument()
	{
		var lazy = LazyHtmlDocument.Parse("<ul><li>1</li><li>2</li></ul>");
		var document = HtmlDocument.Parse(lazy);

		Assert.HasCount(1, document.Elements());
		Assert.AreEqual("ul", document.Elements().First().Name);
		Assert.AreSequenceEqual(["1", "2"], document.Elements().First().Elements().Inners());
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

		Assert.AreSequenceEqual(["x", "i", "y", "i", "z", "i", "i", "i", "w"], document.Descendants().Names());
	}

	[TestMethod]
	public void DescendantsCanBeEnumeratedRepeatedly()
	{
		var document = HtmlDocument.Parse("<a><b></b></a>");
		var descendants = document.Descendants();

		Assert.HasCount(2, descendants.ToList());
		Assert.HasCount(2, descendants.ToList());
	}

	[TestMethod]
	[DataRow("html", "head", "body")]
	[DataRow("HTML", "HEAD", "BODY")]
	[DataRow("HtMl", "HeAd", "BoDy")]
	public void TryGetHeadAndBodyFindChildrenOfHtml(string htmlTag, string headTag, string bodyTag)
	{
		var headHtml = $"<{headTag}><title>Title</title></{headTag}>";
		var bodyHtml = $"<{bodyTag}><p>Content</p></{bodyTag}>";
		var document = HtmlDocument.Parse($"<!DOCTYPE html>before<!-- root --><div></div><{htmlTag}>between<!-- child --><meta charset=utf-8>{headHtml}<link>{bodyHtml}</{htmlTag}>");

		Assert.IsTrue(document.TryGetHead(out var head));
		Assert.IsTrue(document.TryGetBody(out var body));
		Assert.AreEqual(headHtml, head.OuterSpan.ToString());
		Assert.AreEqual(bodyHtml, body.OuterSpan.ToString());
		Assert.AreEqual("Title", head.TextContent);
		Assert.AreEqual("Content", body.TextContent);
		Assert.AreSame(document.QuerySelector(element: "head"), head);
		Assert.AreSame(document.QuerySelector(element: "body"), body);
	}

	[TestMethod]
	[DataRow("<head>H</head><body>B</body>")]
	[DataRow("<section><div><head>H</head><body>B</body></div></section>")]
	[DataRow("<html><div><head>H</head><body>B</body></div></html>")]
	[DataRow("<html></html><html><head>H</head><body>B</body></html>")]
	public void TryGetHeadAndBodyFindSectionsOutsideTheUsualLayout(string html)
	{
		var document = HtmlDocument.Parse(html);

		Assert.IsTrue(document.TryGetHead(out var head));
		Assert.IsTrue(document.TryGetBody(out var body));
		Assert.AreEqual("<head>H</head>", head.OuterSpan.ToString());
		Assert.AreEqual("<body>B</body>", body.OuterSpan.ToString());
		Assert.AreSame(document.QuerySelector(element: "head"), head);
		Assert.AreSame(document.QuerySelector(element: "body"), body);
	}

	[TestMethod]
	[DataRow("<head>H1</head><head>H2</head><body>B1</body><body>B2</body>")]
	[DataRow("<html><head>H1</head><head>H2</head><body>B1</body><body>B2</body></html>")]
	public void TryGetHeadAndBodyReturnTheFirstMatchingSections(string html)
	{
		var document = HtmlDocument.Parse(html);

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
	public void TryGetHeadAndBodyReturnFalseAndNullForMissingSections(string html, bool hasHead, bool hasBody)
	{
		var document = HtmlDocument.Parse(html);

		Assert.AreEqual(hasHead, document.TryGetHead(out var head));
		Assert.AreEqual(hasBody, document.TryGetBody(out var body));
		Assert.AreEqual(hasHead ? "<head>H</head>" : null, head?.OuterSpan.ToString());
		Assert.AreEqual(hasBody ? "<body>B</body>" : null, body?.OuterSpan.ToString());
	}

	[TestMethod]
	[DataRow("<html><head>H</head><body>B</body></html>", true)]
	[DataRow("<div>no sections</div>", false)]
	public void TryGetHeadAndBodyRespectStringSegmentBounds(string content, bool hasSections)
	{
		const string outside = "<head>outside</head><body>outside</body>";
		var html = outside + content + outside;
		var document = HtmlDocument.Parse(new StringSegment(html, outside.Length, content.Length));

		Assert.AreEqual(hasSections, document.TryGetHead(out var head));
		Assert.AreEqual(hasSections, document.TryGetBody(out var body));
		Assert.AreEqual(hasSections ? "<head>H</head>" : null, head?.OuterSpan.ToString());
		Assert.AreEqual(hasSections ? "<body>B</body>" : null, body?.OuterSpan.ToString());
	}
}
