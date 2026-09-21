using System.Text.Encodings.Web;
using System.Text.Unicode;

namespace PeterJuhasz.Text.Html.Tests.Writer;

[TestClass]
public sealed class TextTests
{
	[TestMethod]
	public void TextClosesStartTag()
	{
		var html = TestHelpers.Write(writer =>
		{
			writer.OpenElement("p");
			writer.WriteText("x");
		});

		Assert.AreEqual("<p>x", html);
	}

	[TestMethod]
	public void EmptyTextClosesStartTag()
	{
		var html = TestHelpers.Write(writer =>
		{
			writer.OpenElement("p");
			writer.WriteText("");
			writer.CloseElement();
		});

		Assert.AreEqual("<p></p>", html);
	}

	[TestMethod]
	public void TopLevelText()
	{
		var html = TestHelpers.Write(writer => writer.WriteText("hello"));

		Assert.AreEqual("hello", html);
	}

	[TestMethod]
	public void ConsecutiveTextIsConcatenated()
	{
		var html = TestHelpers.Write(writer =>
		{
			writer.OpenElement("p");
			writer.WriteText("a");
			writer.WriteText("b");
			writer.WriteText("c");
			writer.CloseElement();
		});

		Assert.AreEqual("<p>abc</p>", html);
	}

	[TestMethod]
	public void MarkupCharactersAreEncoded()
	{
		var html = TestHelpers.Write(writer =>
		{
			writer.OpenElement("p");
			writer.WriteText("a < b & c > d");
			writer.CloseElement();
		});

		Assert.AreEqual("<p>a &lt; b &amp; c &gt; d</p>", html);
	}

	[TestMethod]
	public void TagsInTextAreNotInterpreted()
	{
		var html = TestHelpers.Write(writer =>
		{
			writer.OpenElement("p");
			writer.WriteText("<b>bold</b></p>");
			writer.CloseElement();
		});

		Assert.AreEqual("<p>&lt;b&gt;bold&lt;/b&gt;&lt;/p&gt;</p>", html);
	}

	[TestMethod]
	public void QuotesAreEncoded()
	{
		var html = TestHelpers.Write(writer =>
		{
			writer.OpenElement("p");
			writer.WriteText("\"it's\"");
			writer.CloseElement();
		});

		Assert.AreEqual("<p>&quot;it&#x27;s&quot;</p>", html);
	}

	[TestMethod]
	public void AlreadyEncodedTextIsEncodedAgain()
	{
		var html = TestHelpers.Write(writer =>
		{
			writer.OpenElement("p");
			writer.WriteText("&lt;");
			writer.CloseElement();
		});

		Assert.AreEqual("<p>&amp;lt;</p>", html);
	}

	[TestMethod]
	public void SpacesArePreserved()
	{
		var html = TestHelpers.Write(writer =>
		{
			writer.OpenElement("pre");
			writer.WriteText("  a   b  ");
			writer.CloseElement();
		});

		Assert.AreEqual("<pre>  a   b  </pre>", html);
	}

	[TestMethod]
	public void LineBreaksAndTabsAreEncodedByDefault()
	{
		var html = TestHelpers.Write(writer =>
		{
			writer.OpenElement("pre");
			writer.WriteText("a\r\n\tb");
			writer.CloseElement();
		});

		Assert.AreEqual("<pre>a&#xD;&#xA;&#x9;b</pre>", html);
	}

	[TestMethod]
	public void NonAsciiTextIsEncodedByDefault()
	{
		var html = TestHelpers.Write(writer =>
		{
			writer.OpenElement("p");
			writer.WriteText("héllo 😀");
			writer.CloseElement();
		});

		Assert.AreEqual("<p>h&#xE9;llo &#x1F600;</p>", html);
	}

	[TestMethod]
	public void NonAsciiTextIsPreservedWithRelaxedEncoder()
	{
		var html = TestHelpers.Write(writer =>
		{
			writer.OpenElement("p");
			writer.WriteText("héllo 世界 <b>");
			writer.CloseElement();
		}, HtmlEncoder.Create(UnicodeRanges.All));

		Assert.AreEqual("<p>héllo 世界 &lt;b&gt;</p>", html);
	}

	[TestMethod]
	public void TextWithoutSpecialCharactersIsUnchanged()
	{
		const string text = "The quick brown fox jumps over the lazy dog. 0123456789 -_.,;:!?()[]{}";

		var html = TestHelpers.Write(writer => writer.WriteText(text));

		Assert.AreEqual(text, html);
	}

	[TestMethod]
	public void LongTextIsEncodedWithSmallInitialBuffer()
	{
		var text = string.Concat(Enumerable.Repeat("x<", 10_000));

		var html = TestHelpers.Write(writer =>
		{
			writer.OpenElement("p");
			writer.WriteText(text);
			writer.CloseElement();
		}, initialCapacity: 1);

		Assert.AreEqual("<p>" + string.Concat(Enumerable.Repeat("x&lt;", 10_000)) + "</p>", html);
	}

	[TestMethod]
	public void TextIsWrittenInChunks()
	{
		var html = TestHelpers.Write(writer =>
		{
			writer.OpenElement("p");
			for (var i = 0; i < 1000; i++)
				writer.WriteText("<");
			writer.CloseElement();
		}, initialCapacity: 1);

		Assert.AreEqual("<p>" + string.Concat(Enumerable.Repeat("&lt;", 1000)) + "</p>", html);
	}
}
