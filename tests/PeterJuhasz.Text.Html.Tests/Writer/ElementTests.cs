namespace PeterJuhasz.Text.Html.Tests.Writer;

[TestClass]
public sealed class ElementTests
{
	[TestMethod]
	public void EmptyElement()
	{
		var html = TestHelpers.Write(writer =>
		{
			writer.OpenElement("div");
			writer.CloseElement();
		});

		Assert.AreEqual("<div></div>", html);
	}

	[TestMethod]
	public void ElementWithText()
	{
		var html = TestHelpers.Write(writer =>
		{
			writer.OpenElement("p");
			writer.WriteText("hello");
			writer.CloseElement();
		});

		Assert.AreEqual("<p>hello</p>", html);
	}

	[TestMethod]
	public void NestedElements()
	{
		var html = TestHelpers.Write(writer =>
		{
			writer.OpenElement("div");
			writer.OpenElement("span");
			writer.WriteText("x");
			writer.CloseElement();
			writer.CloseElement();
		});

		Assert.AreEqual("<div><span>x</span></div>", html);
	}

	[TestMethod]
	public void NestedEmptyElements()
	{
		var html = TestHelpers.Write(writer =>
		{
			writer.OpenElement("div");
			writer.OpenElement("span");
			writer.CloseElement();
			writer.CloseElement();
		});

		Assert.AreEqual("<div><span></span></div>", html);
	}

	[TestMethod]
	public void SiblingElements()
	{
		var html = TestHelpers.Write(writer =>
		{
			writer.OpenElement("ul");
			writer.OpenElement("li");
			writer.WriteText("1");
			writer.CloseElement();
			writer.OpenElement("li");
			writer.WriteText("2");
			writer.CloseElement();
			writer.CloseElement();
		});

		Assert.AreEqual("<ul><li>1</li><li>2</li></ul>", html);
	}

	[TestMethod]
	public void MultipleRootElements()
	{
		var html = TestHelpers.Write(writer =>
		{
			writer.OpenElement("a");
			writer.CloseElement();
			writer.OpenElement("b");
			writer.CloseElement();
		});

		Assert.AreEqual("<a></a><b></b>", html);
	}

	[TestMethod]
	public void MixedContent()
	{
		var html = TestHelpers.Write(writer =>
		{
			writer.OpenElement("p");
			writer.WriteText("a");
			writer.OpenElement("b");
			writer.WriteText("b");
			writer.CloseElement();
			writer.WriteText("c");
			writer.CloseElement();
		});

		Assert.AreEqual("<p>a<b>b</b>c</p>", html);
	}

	[TestMethod]
	[DataRow("div")]
	[DataRow("DIV")]
	[DataRow("Div")]
	[DataRow("my-element")]
	[DataRow("svg:rect")]
	[DataRow("h1")]
	public void ElementNameIsWrittenAsGiven(string name)
	{
		var html = TestHelpers.Write(writer =>
		{
			writer.OpenElement(name);
			writer.WriteText("x");
			writer.CloseElement();
		});

		Assert.AreEqual($"<{name}>x</{name}>", html);
	}

	[TestMethod]
	public void EndTagMatchesInnermostOpenElement()
	{
		var html = TestHelpers.Write(writer =>
		{
			writer.OpenElement("table");
			writer.OpenElement("tr");
			writer.OpenElement("td");
			writer.WriteText("x");
			writer.CloseElement();
			writer.CloseElement();
			writer.CloseElement();
		});

		Assert.AreEqual("<table><tr><td>x</td></tr></table>", html);
	}

	[TestMethod]
	public void DeeplyNestedElementsCloseInOrder()
	{
		const int depth = 1000;

		var html = TestHelpers.Write(writer =>
		{
			for (var i = 0; i < depth; i++)
			{
				writer.OpenElement("div");
			}

			writer.WriteText("x");

			for (var i = 0; i < depth; i++)
			{
				writer.CloseElement();
			}
		});

		var expected = string.Concat(Enumerable.Repeat("<div>", depth)) + "x" + string.Concat(Enumerable.Repeat("</div>", depth));
		Assert.AreEqual(expected, html);
	}
}
