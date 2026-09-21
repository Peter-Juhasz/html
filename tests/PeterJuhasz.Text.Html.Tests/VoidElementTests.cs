using System.Text.Html.Lazy;

namespace PeterJuhasz.Text.Html.Tests;

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
	public void VoidElementEndsAtStartTag(string name)
	{
		var element = TestHelpers.FirstElement($"<{name}>text<span></span>");

		Assert.AreEqual($"<{name}>", element.OuterSpan.ToString());
		Assert.IsTrue(element.InnerSpan.IsEmpty);
	}

	[TestMethod]
	public void VoidElementNameIsCaseInsensitive()
	{
		var element = TestHelpers.FirstElement("<BR>text");

		Assert.AreEqual("<BR>", element.OuterSpan.ToString());
	}

	[TestMethod]
	public void VoidElementWithAttributes()
	{
		var element = TestHelpers.FirstElement("<img src=\"a.png\" alt=\"a > b\">tail");

		Assert.AreEqual("<img src=\"a.png\" alt=\"a > b\">", element.OuterSpan.ToString());
		Assert.IsTrue(element.TryGetAttribute("alt", out var alt));
		Assert.AreEqual("a > b", alt.Value);
	}

	[TestMethod]
	public void VoidElementsAreSiblingsNotDescendants()
	{
		var document = new LazyHtmlDocument("<meta charset=\"utf-8\"><link rel=\"stylesheet\"><br><hr>");

		CollectionAssert.AreEqual(new[] { "meta", "link", "br", "hr" }, document.Elements().Names());
	}

	[TestMethod]
	public void VoidElementDoesNotSwallowFollowingContent()
	{
		var element = TestHelpers.FirstElement("<p>a<br>b</p>");

		Assert.AreEqual("<p>a<br>b</p>", element.OuterSpan.ToString());
		Assert.AreEqual("ab", element.TextContent);
	}

	[TestMethod]
	public void NonVoidElementWithSimilarNameIsNotVoid()
	{
		var element = TestHelpers.FirstElement("<bro>text</bro>");

		Assert.AreEqual("<bro>text</bro>", element.OuterSpan.ToString());
	}
}
