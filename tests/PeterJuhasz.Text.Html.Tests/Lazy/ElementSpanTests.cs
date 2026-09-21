namespace PeterJuhasz.Text.Html.Tests.Lazy;

[TestClass]
public sealed class ElementSpanTests
{
	[TestMethod]
	public void OuterSpanCoversStartTagContentAndEndTag()
	{
		var element = TestHelpers.FirstElement("x<div class=\"a\">content</div>y");

		Assert.AreEqual("<div class=\"a\">content</div>", element.OuterSpan.ToString());
	}

	[TestMethod]
	public void InnerSpanCoversContentOnly()
	{
		var element = TestHelpers.FirstElement("<div class=\"a\">content</div>");

		Assert.AreEqual("content", element.InnerSpan.ToString());
	}

	[TestMethod]
	public void InnerSpanIncludesNestedMarkup()
	{
		var element = TestHelpers.FirstElement("<div>a<b>c</b>d</div>");

		Assert.AreEqual("a<b>c</b>d", element.InnerSpan.ToString());
	}

	[TestMethod]
	public void EmptyElementHasEmptyInnerSpan()
	{
		var element = TestHelpers.FirstElement("<div></div>");

		Assert.IsTrue(element.InnerSpan.IsEmpty);
		Assert.AreEqual("<div></div>", element.OuterSpan.ToString());
	}

	[TestMethod]
	public void SelfClosingElementHasEmptyInnerSpan()
	{
		var element = TestHelpers.FirstElement("<div/>after");

		Assert.IsTrue(element.InnerSpan.IsEmpty);
		Assert.AreEqual("<div/>", element.OuterSpan.ToString());
	}

	[TestMethod]
	public void SelfClosingElementWithAttributes()
	{
		var element = TestHelpers.FirstElement("<img src=\"a.png\" alt=\"a\" />after");

		Assert.AreEqual("<img src=\"a.png\" alt=\"a\" />", element.OuterSpan.ToString());
	}

	[TestMethod]
	public void NestedSameNameElementsAreMatchedByDepth()
	{
		var element = TestHelpers.FirstElement("<div><div>inner</div></div>tail");

		Assert.AreEqual("<div><div>inner</div></div>", element.OuterSpan.ToString());
		Assert.AreEqual("<div>inner</div>", element.InnerSpan.ToString());
	}

	[TestMethod]
	public void EndTagWithSimilarPrefixIsNotMatched()
	{
		var element = TestHelpers.FirstElement("<a>x</abbr>");

		// the mismatched end tag closes the element but is not consumed by it
		Assert.AreEqual("<a>x", element.OuterSpan.ToString());
		Assert.AreEqual("x", element.InnerSpan.ToString());
	}

	[TestMethod]
	public void EndTagWithWhitespaceIsMatched()
	{
		var element = TestHelpers.FirstElement("<div>x</div >tail");

		Assert.AreEqual("<div>x</div >", element.OuterSpan.ToString());
	}

	[TestMethod]
	public void QuotedAttributeValueMayContainCloseAngle()
	{
		var element = TestHelpers.FirstElement("<div title=\"a > b\">x</div>");

		Assert.AreEqual("x", element.InnerSpan.ToString());
		Assert.AreEqual("a > b", element.TryGetAttribute("title", out var title) ? title.Value : null);
	}
}
