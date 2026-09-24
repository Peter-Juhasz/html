namespace PeterJuhasz.Text.Html.Tests.Writer;

[TestClass]
public sealed class VoidElementTests
{
	[TestMethod]
	[DataRow("area")]
	[DataRow("base")]
	[DataRow("br")]
	[DataRow("col")]
	[DataRow("embed")]
	[DataRow("hr")]
	[DataRow("img")]
	[DataRow("input")]
	[DataRow("link")]
	[DataRow("meta")]
	[DataRow("param")]
	[DataRow("source")]
	[DataRow("track")]
	[DataRow("wbr")]
	public void VoidElementHasNoEndTag(string name)
	{
		var html = TestHelpers.Write(writer =>
		{
			writer.OpenElement(name);
			writer.CloseElement();
		});

		Assert.AreEqual($"<{name}>", html);
	}

	[TestMethod]
	[DataRow(true, "<br />")]
	[DataRow(false, "<br/>")]
	public void VoidElementIsSelfClosedInXmlStyle(bool spaceBeforeSlash, string expected)
	{
		var html = TestHelpers.Write(writer =>
		{
			writer.OpenElement("br");
			writer.CloseElement();
		}, options: HtmlWriterFormattingOptions.Minimal with { XmlStyleSelfClosingTags = true, SpaceBeforeSelfClosingSlash = spaceBeforeSlash });

		Assert.AreEqual(expected, html);
	}

	[TestMethod]
	public void VoidElementNameIsCaseInsensitive()
	{
		var html = TestHelpers.Write(writer =>
		{
			writer.OpenElement("BR");
			writer.CloseElement();
		});

		Assert.AreEqual("<BR>", html);
	}

	[TestMethod]
	public void VoidElementWithAttributes()
	{
		var html = TestHelpers.Write(writer =>
		{
			writer.OpenElement("img");
			writer.WriteAttribute("src", "a.png");
			writer.WriteAttribute("alt", "a > b");
			writer.CloseElement();
		});

		Assert.AreEqual("<img src=\"a.png\" alt=\"a &gt; b\">", html);
	}

	[TestMethod]
	public void VoidElementWithBareAttribute()
	{
		var html = TestHelpers.Write(writer =>
		{
			writer.OpenElement("input");
			writer.WriteAttribute("type", "checkbox");
			writer.WriteAttribute("checked");
			writer.CloseElement();
		});

		Assert.AreEqual("<input type=checkbox checked>", html);
	}

	[TestMethod]
	public void VoidElementInsideElement()
	{
		var html = TestHelpers.Write(writer =>
		{
			writer.OpenElement("p");
			writer.WriteText("a");
			writer.OpenElement("br");
			writer.CloseElement();
			writer.WriteText("b");
			writer.CloseElement();
		});

		Assert.AreEqual("<p>a<br>b</p>", html);
	}

	[TestMethod]
	public void VoidElementsAsSiblings()
	{
		var html = TestHelpers.Write(writer =>
		{
			writer.OpenElement("head");
			writer.OpenElement("meta");
			writer.WriteAttribute("charset", "utf-8");
			writer.CloseElement();
			writer.OpenElement("link");
			writer.WriteAttribute("rel", "stylesheet");
			writer.CloseElement();
			writer.CloseElement();
		});

		Assert.AreEqual("<head><meta charset=\"utf-8\"><link rel=stylesheet></head>", html);
	}

	[TestMethod]
	public void NonVoidElementWithSimilarNameIsNotVoid()
	{
		var html = TestHelpers.Write(writer =>
		{
			writer.OpenElement("bro");
			writer.CloseElement();
		});

		Assert.AreEqual("<bro></bro>", html);
	}
}
