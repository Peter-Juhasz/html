using System.Buffers;
using PeterJuhasz.Text.Html.Tests.Lazy;

namespace PeterJuhasz.Text.Html.Tests.Writer;

[TestClass]
public sealed class DocumentTests
{
	private const string ExpectedDocument =
		"<!DOCTYPE html><html lang=\"en\"><head><meta charset=\"utf-8\" /><title>Tom &amp; Jerry</title></head>" +
		"<body><h1 class=\"title\">Hello</h1><p>a <a href=\"/x?a=1&amp;b=2\" target=\"_blank\">link</a><br /><!-- c --></p>" +
		"<ul><li>1</li><li>2</li></ul><input type=\"checkbox\" checked /></body></html>";

	private static void WriteDocument(HtmlWriter<ArrayBufferWriter<char>> writer)
	{
		writer.WriteHtml("<!DOCTYPE html>");
		writer.OpenElement("html");
		writer.WriteAttribute("lang", "en");
		writer.OpenElement("head");
		writer.OpenElement("meta");
		writer.WriteAttribute("charset", "utf-8");
		writer.CloseElement();
		writer.OpenElement("title");
		writer.WriteText("Tom & Jerry");
		writer.CloseElement();
		writer.CloseElement();
		writer.OpenElement("body");
		writer.OpenElement("h1");
		writer.WriteAttribute("class", "title");
		writer.WriteText("Hello");
		writer.CloseElement();
		writer.OpenElement("p");
		writer.WriteText("a ");
		writer.OpenElement("a");
		writer.WriteAttribute("href", "/x?a=1&b=2");
		writer.WriteAttribute("target", "_blank");
		writer.WriteText("link");
		writer.CloseElement();
		writer.OpenElement("br");
		writer.CloseElement();
		writer.WriteComment(" c ");
		writer.CloseElement();
		writer.OpenElement("ul");
		writer.OpenElement("li");
		writer.WriteText("1");
		writer.CloseElement();
		writer.OpenElement("li");
		writer.WriteText("2");
		writer.CloseElement();
		writer.CloseElement();
		writer.OpenElement("input");
		writer.WriteAttribute("type", "checkbox");
		writer.WriteAttribute("checked");
		writer.CloseElement();
		writer.CloseElement();
		writer.CloseElement();
	}

	[TestMethod]
	public void WritesCompleteDocument()
	{
		var html = TestHelpers.Write(WriteDocument);

		Assert.AreEqual(ExpectedDocument, html);
	}

	[TestMethod]
	public void OutputCanBeParsedByLazyDocument()
	{
		var document = LazyHtmlDocument.Parse(TestHelpers.Write(WriteDocument));

		var roots = document.Elements().ToList();
		Assert.HasCount(1, roots);

		var root = roots[0];
		Assert.AreEqual("html", root.Name);
		Assert.IsTrue(root.TryGetAttribute("lang", out var lang));
		Assert.AreEqual("en", lang.Value);
		Assert.AreSequenceEqual(["head", "body"], root.Elements().Names());

		var head = root.Elements().ToList()[0];
		Assert.AreSequenceEqual(["meta", "title"], head.Elements().Names());
		Assert.AreEqual("Tom &amp; Jerry", head.Elements().ToList()[1].TextContent);

		var body = root.Elements().ToList()[1];
		Assert.AreSequenceEqual(["h1", "p", "ul", "input"], body.Elements().Names());

		var paragraph = body.Elements().ToList()[1];
		Assert.AreSequenceEqual(["a", "br"], paragraph.Elements().Names());

		var anchor = paragraph.Elements().ToList()[0];
		Assert.IsTrue(anchor.TryGetAttribute("href", out var href));
		Assert.AreEqual("/x?a=1&amp;b=2", href.Value);
		Assert.AreEqual("link", anchor.TextContent);

		var input = body.Elements().ToList()[3];
		Assert.IsTrue(input.TryGetAttribute("checked", out var checkedAttribute));
		Assert.IsFalse(checkedAttribute.HasValue);
	}

	[TestMethod]
	public void EncodedTextIsNotParsedAsMarkup()
	{
		var html = TestHelpers.Write(writer =>
		{
			writer.OpenElement("div");
			writer.WriteText("<span>x</span>");
			writer.CloseElement();
		});

		var element = Lazy.TestHelpers.FirstElement(html);

		Assert.AreEqual("div", element.Name);
		Assert.IsFalse(element.Elements().MoveNext());
		Assert.AreEqual("&lt;span&gt;x&lt;/span&gt;", element.TextContent);
	}

	[TestMethod]
	public void EncodedAttributeValueDoesNotBreakOutOfTag()
	{
		var html = TestHelpers.Write(writer =>
		{
			writer.OpenElement("div");
			writer.WriteAttribute("title", "\" onclick=\"evil()");
			writer.WriteText("x");
			writer.CloseElement();
		});

		var element = Lazy.TestHelpers.FirstElement(html);

		Assert.AreSequenceEqual(["title"], element.Attributes().Names());
		Assert.AreEqual("x", element.TextContent);
	}
}
