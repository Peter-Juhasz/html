namespace PeterJuhasz.Text.Html.Tests.Writer;

[TestClass]
public sealed class CommentTests
{
	[TestMethod]
	public void TopLevelComment()
	{
		var html = TestHelpers.Write(writer => writer.WriteComment(" c "));

		Assert.AreEqual("<!-- c -->", html);
	}

	[TestMethod]
	public void EmptyComment()
	{
		var html = TestHelpers.Write(writer => writer.WriteComment(""));

		Assert.AreEqual("<!---->", html);
	}

	[TestMethod]
	public void CommentInsideElement()
	{
		var html = TestHelpers.Write(writer =>
		{
			writer.OpenElement("div");
			writer.WriteText("a");
			writer.WriteComment("c");
			writer.WriteText("b");
			writer.CloseElement();
		});

		Assert.AreEqual("<div>a<!--c-->b</div>", html);
	}

	[TestMethod]
	public void CommentAfterClosedElement()
	{
		var html = TestHelpers.Write(writer =>
		{
			writer.OpenElement("div");
			writer.OpenElement("span");
			writer.CloseElement();
			writer.WriteComment("c");
			writer.CloseElement();
		});

		Assert.AreEqual("<div><span></span><!--c--></div>", html);
	}

	[TestMethod]
	public void CommentBetweenElements()
	{
		var html = TestHelpers.Write(writer =>
		{
			writer.OpenElement("a");
			writer.CloseElement();
			writer.WriteComment("c");
			writer.OpenElement("b");
			writer.CloseElement();
		});

		Assert.AreEqual("<a></a><!--c--><b></b>", html);
	}

	[TestMethod]
	public void CommentContentIsEncoded()
	{
		var html = TestHelpers.Write(writer => writer.WriteComment("a < b & \"c\""));

		Assert.AreEqual("<!--a < b & \"c\"-->", html);
	}

	[TestMethod]
	public void CommentCannotBeTerminatedEarly()
	{
		var html = TestHelpers.Write(writer => writer.WriteComment("a --> <script>"));

		Assert.AreEqual("<!--a --> <script>-->", html);
	}

	[TestMethod]
	public void MultilineCommentIsEncoded()
	{
		var html = TestHelpers.Write(writer => writer.WriteComment("\r\n a \r\n"));

		Assert.AreEqual("<!--\r\n a \r\n-->", html);
	}
}
