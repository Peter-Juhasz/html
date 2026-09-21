namespace PeterJuhasz.Text.Html.Tests.Writer;

[TestClass]
public sealed class InvalidUsageTests
{
	[TestMethod]
	public void CloseWithoutOpenThrows()
	{
		TestHelpers.Write(writer =>
		{
			Assert.ThrowsExactly<InvalidOperationException>(writer.CloseElement);
		});
	}

	[TestMethod]
	public void CloseMoreThanOpenedThrows()
	{
		var html = TestHelpers.Write(writer =>
		{
			writer.OpenElement("div");
			writer.CloseElement();

			Assert.ThrowsExactly<InvalidOperationException>(writer.CloseElement);
		});

		Assert.AreEqual("<div></div>", html);
	}

	[TestMethod]
	public void AttributeBeforeAnyElementThrows()
	{
		TestHelpers.Write(writer =>
		{
			Assert.ThrowsExactly<InvalidOperationException>(() => writer.WriteAttribute("id", "x"));
			Assert.ThrowsExactly<InvalidOperationException>(() => writer.WriteAttribute("disabled"));
		});
	}

	[TestMethod]
	public void AttributeAfterTextThrows()
	{
		var html = TestHelpers.Write(writer =>
		{
			writer.OpenElement("p");
			writer.WriteText("x");

			Assert.ThrowsExactly<InvalidOperationException>(() => writer.WriteAttribute("id", "x"));
			Assert.ThrowsExactly<InvalidOperationException>(() => writer.WriteAttribute("disabled"));

			writer.CloseElement();
		});

		Assert.AreEqual("<p>x</p>", html);
	}

	[TestMethod]
	public void AttributeAfterCloseElementThrows()
	{
		var html = TestHelpers.Write(writer =>
		{
			writer.OpenElement("div");
			writer.OpenElement("span");
			writer.CloseElement();

			Assert.ThrowsExactly<InvalidOperationException>(() => writer.WriteAttribute("id", "x"));

			writer.CloseElement();
		});

		Assert.AreEqual("<div><span></span></div>", html);
	}

	[TestMethod]
	public void AttributeAfterCommentThrows()
	{
		TestHelpers.Write(writer =>
		{
			writer.OpenElement("div");
			writer.WriteText("");
			writer.WriteComment("c");

			Assert.ThrowsExactly<InvalidOperationException>(() => writer.WriteAttribute("id", "x"));
		});
	}

	[TestMethod]
	public void CommentInsideStartTagThrows()
	{
		var html = TestHelpers.Write(writer =>
		{
			writer.OpenElement("div");

			Assert.ThrowsExactly<InvalidOperationException>(() => writer.WriteComment("c"));
		});

		Assert.AreEqual("<div", html);
	}

	[TestMethod]
	public void CommentAfterAttributeThrows()
	{
		TestHelpers.Write(writer =>
		{
			writer.OpenElement("div");
			writer.WriteAttribute("id", "x");

			Assert.ThrowsExactly<InvalidOperationException>(() => writer.WriteComment("c"));
		});
	}

	[TestMethod]
	public void FailedOperationDoesNotWriteAnything()
	{
		var html = TestHelpers.Write(writer =>
		{
			Assert.ThrowsExactly<InvalidOperationException>(() => writer.WriteAttribute("id", "x"));
			Assert.ThrowsExactly<InvalidOperationException>(writer.CloseElement);

			writer.OpenElement("p");

			Assert.ThrowsExactly<InvalidOperationException>(() => writer.WriteComment("c"));

			writer.WriteText("x");

			Assert.ThrowsExactly<InvalidOperationException>(() => writer.WriteAttribute("id", "x"));

			writer.CloseElement();
		});

		Assert.AreEqual("<p>x</p>", html);
	}
}
