namespace PeterJuhasz.Text.Html.Tests.Writer;

[TestClass]
public sealed class RawHtmlTests
{
	[TestMethod]
	public void RawHtmlIsNotEncoded()
	{
		var html = TestHelpers.Write(writer => writer.WriteHtml("<b>a &amp; \"b\"</b>"));

		Assert.AreEqual("<b>a &amp; \"b\"</b>", html);
	}

	[TestMethod]
	public void EmptyRawHtml()
	{
		var html = TestHelpers.Write(writer => writer.WriteHtml(""));

		Assert.AreEqual("", html);
	}

	[TestMethod]
	public void RawHtmlAfterText()
	{
		var html = TestHelpers.Write(writer =>
		{
			writer.OpenElement("div");
			writer.WriteText("a");
			writer.WriteHtml("<b>x</b>");
			writer.CloseElement();
		});

		Assert.AreEqual("<div>a<b>x</b></div>", html);
	}

	[TestMethod]
	public void RawHtmlDoctype()
	{
		var html = TestHelpers.Write(writer =>
		{
			writer.WriteHtml("<!DOCTYPE html>");
			writer.OpenElement("html");
			writer.CloseElement();
		});

		Assert.AreEqual("<!DOCTYPE html><html></html>", html);
	}

	[TestMethod]
	public void RawHtmlDoesNotOpenElements()
	{
		var html = TestHelpers.Write(writer =>
		{
			writer.WriteHtml("<b>");

			Assert.ThrowsExactly<InvalidOperationException>(writer.CloseElement);
		});

		Assert.AreEqual("<b>", html);
	}

	[TestMethod]
	public void RawHtmlDoesNotCloseElements()
	{
		var html = TestHelpers.Write(writer =>
		{
			writer.OpenElement("div");
			writer.WriteText("");
			writer.WriteHtml("</div>");
			writer.CloseElement();
		});

		Assert.AreEqual("<div></div></div>", html);
	}
}
