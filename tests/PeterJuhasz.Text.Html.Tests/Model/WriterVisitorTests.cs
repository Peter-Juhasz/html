using System.Buffers;
using System.Text.Encodings.Web;

namespace PeterJuhasz.Text.Html.Tests.Model;

[TestClass]
public sealed class WriterVisitorTests
{
	private const string Sample =
		"<html lang=\"en\">" +
		"<head><meta charset=\"utf-8\"><title>Tom &amp; Jerry</title></head>" +
		"<body>" +
		"<!-- header -->" +
		"<h1 class=\"title\">Hello, World!</h1>" +
		"<p>Read the <a href=\"/docs?a=1&amp;b=2\" target=_blank>docs</a><br>or not.</p>" +
		"<ul><li>One</li><li>Two</li></ul>" +
		"<input type=\"checkbox\" name=\"agree\" checked>" +
		"<script>if (a < b) { go(); }</script>" +
		"</body>" +
		"</html>";

	private static readonly string ExpectedIndented = string.Join("\n",
	[
		"<html lang=\"en\">",
		"\t<head>",
		"\t\t<meta charset=\"utf-8\" />",
		"\t\t<title>Tom &amp; Jerry</title>",
		"\t</head>",
		"\t<body>",
		"\t\t<!-- header -->",
		"\t\t<h1 class=\"title\">Hello, World!</h1>",
		"\t\t<p>Read the <a href=\"/docs?a=1&amp;b=2\" target=\"_blank\">docs</a><br />or not.</p>",
		"\t\t<ul>",
		"\t\t\t<li>One</li>",
		"\t\t\t<li>Two</li>",
		"\t\t</ul>",
		"\t\t<input type=\"checkbox\" name=\"agree\" checked />",
		"\t\t<script>if (a < b) { go(); }</script>",
		"\t</body>",
		"</html>",
	]);

	private const string ExpectedMinimal =
		"<html lang=en><head><meta charset=\"utf-8\"><title>Tom &amp; Jerry</title></head>" +
		"<body><!-- header --><h1 class=title>Hello, World!</h1>" +
		"<p>Read the <a href=\"/docs?a=1&amp;b=2\" target=_blank>docs</a><br>or not.</p>" +
		"<ul><li>One</li><li>Two</li></ul><input type=checkbox name=agree checked>" +
		"<script>if (a < b) { go(); }</script></body></html>";

	[TestMethod]
	public void WritesDocumentIndented()
	{
		var document = HtmlDocument.Parse(Sample);

		var html = Write(document, HtmlWriterFormattingOptions.Indented);

		Assert.AreEqual(ExpectedIndented, html);
	}

	[TestMethod]
	public void WritesDocumentMinimal()
	{
		var document = HtmlDocument.Parse(Sample);

		var html = Write(document, HtmlWriterFormattingOptions.Minimal);

		Assert.AreEqual(ExpectedMinimal, html);
	}

	private static string Write(HtmlDocument document, HtmlWriterFormattingOptions options)
	{
		var buffer = new ArrayBufferWriter<char>();
		var writer = new HtmlWriter<ArrayBufferWriter<char>>(buffer, HtmlEncoder.Default, options);
		var visitor = new HtmlWriterVisitor<ArrayBufferWriter<char>>(writer);

		visitor.VisitDocument(document);

		return buffer.WrittenSpan.ToString();
	}
}
