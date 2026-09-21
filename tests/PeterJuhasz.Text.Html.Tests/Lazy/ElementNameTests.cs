using System.Text.Html.Lazy;

namespace PeterJuhasz.Text.Html.Tests.Lazy;

[TestClass]
public sealed class ElementNameTests
{
	[TestMethod]
	public void NameIsReadFromStartTag()
	{
		var element = TestHelpers.FirstElement("<div></div>");

		Assert.AreEqual("div", element.Name);
		Assert.AreEqual("div", element.NameSpan.ToString());
	}

	[TestMethod]
	public void NamePreservesCase()
	{
		var element = TestHelpers.FirstElement("<DIV></div>");

		Assert.AreEqual("DIV", element.Name);
	}

	[TestMethod]
	public void StandardNameIsSharedAndDoesNotAllocate()
	{
		var elements = LazyHtmlDocument.Parse("<div></div><div></div>").Elements().ToList();
		_ = elements[0].Name;

		var before = GC.GetAllocatedBytesForCurrentThread();
		var name = elements[0].Name;
		var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

		Assert.AreEqual("div", name);
		Assert.AreEqual(0, allocated);
		Assert.AreSame(name, elements[1].Name);
	}

	[TestMethod]
	public void NonLowercaseOrCustomNameIsCopied()
	{
		var elements = LazyHtmlDocument.Parse("<DIV></DIV><Div></Div><my-element></my-element>").Elements().ToList();

		Assert.AreEqual("DIV", elements[0].Name);
		Assert.AreEqual("Div", elements[1].Name);
		Assert.AreEqual("my-element", elements[2].Name);
		Assert.AreNotSame(elements[0].Name, elements[0].Name);
	}

	[TestMethod]
	public void NameEndsAtWhitespace()
	{
		var element = TestHelpers.FirstElement("<div class=\"a\"></div>");

		Assert.AreEqual("div", element.Name);
	}

	[TestMethod]
	public void NameEndsAtSelfClosingSlash()
	{
		var element = TestHelpers.FirstElement("<br/>");

		Assert.AreEqual("br", element.Name);
	}

	[TestMethod]
	public void NameEndsAtNewline()
	{
		var element = TestHelpers.FirstElement("<div\n\tid=\"a\">\n</div>");

		Assert.AreEqual("div", element.Name);
	}

	[TestMethod]
	public void NameMayContainDigitsAndHyphens()
	{
		var element = TestHelpers.FirstElement("<h1></h1><my-element></my-element>");

		Assert.AreEqual("h1", element.Name);
		CollectionAssert.AreEqual(new[] { "h1", "my-element" }, LazyHtmlDocument.Parse("<h1></h1><my-element></my-element>").Elements().Names());
	}

	[TestMethod]
	public void EndTagMatchingIsCaseInsensitive()
	{
		var element = TestHelpers.FirstElement("<div>a</DIV>b");

		Assert.AreEqual("<div>a</DIV>", element.OuterSpan.ToString());
	}

	[TestMethod]
	public void DefaultElementHasEmptyName()
	{
		var element = default(LazyHtmlElement);

		Assert.AreEqual("", element.Name);
		Assert.IsTrue(element.NameSpan.IsEmpty);
		Assert.IsTrue(element.OuterSpan.IsEmpty);
		Assert.IsTrue(element.InnerSpan.IsEmpty);
		Assert.AreEqual("", element.TextContent);
		Assert.IsFalse(element.Elements().MoveNext());
		Assert.IsFalse(element.Attributes().MoveNext());
	}
}
