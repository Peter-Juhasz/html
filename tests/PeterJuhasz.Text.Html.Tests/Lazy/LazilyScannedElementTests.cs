using System.Text.Html.Lazy;

namespace PeterJuhasz.Text.Html.Tests.Lazy;

// An element found by a query is not scanned as a whole up front, so its end is found later: when a member needs it,
// or along the way while enumerating its children. These check that both give the same results as an enumerated element.
[TestClass]
public sealed class LazilyScannedElementTests
{
	[TestMethod]
	[DataRow("<div><a>x</a><b>y</b></div><i>z</i>")]
	[DataRow("<div>text only</div><i>z</i>")]
	[DataRow("<div/><i>z</i>")]
	[DataRow("<br><i>z</i>")]
	[DataRow("<script>if (a < b) {}</script><i>z</i>")]
	[DataRow("<p>a<b>b</b><div>c</div>")]
	[DataRow("<div><a>x</a></span><b>y</b>")]
	[DataRow("<div><a>x</a>")]
	public void EndIsTheSameAsWhenScannedUpFront(string html)
	{
		var enumerated = TestHelpers.FirstElement(html);
		var queried = TestHelpers.FirstQueriedElement(html);

		Assert.AreEqual(enumerated.OuterSpan.ToString(), queried.OuterSpan.ToString());
		Assert.AreEqual(enumerated.InnerSpan.ToString(), queried.InnerSpan.ToString());
		Assert.AreEqual(enumerated.TextContent, queried.TextContent);
		CollectionAssert.AreEqual(enumerated.Elements().Names(), queried.Elements().Names());
		CollectionAssert.AreEqual(enumerated.QuerySelectorAll().Names(), queried.QuerySelectorAll().Names());
	}

	[TestMethod]
	public void ChildrenEndAtOwnEndTag()
	{
		var element = TestHelpers.FirstQueriedElement("<div><a>x</a><b>y</b></div><i>z</i>");

		CollectionAssert.AreEqual(new[] { "a", "b" }, element.Elements().Names());
	}

	[TestMethod]
	public void ChildrenEndAtMismatchedEndTag()
	{
		var element = TestHelpers.FirstQueriedElement("<div><a>x</a></span><b>y</b>");

		CollectionAssert.AreEqual(new[] { "a" }, element.Elements().Names());
	}

	[TestMethod]
	public void ChildrenEndAtImplicitlyClosingStartTag()
	{
		var element = TestHelpers.FirstQueriedElement("<p><b>x</b><div>y</div>");

		CollectionAssert.AreEqual(new[] { "b" }, element.Elements().Names());
	}

	[TestMethod]
	public void ChildrenEndAtEndOfDocument()
	{
		var element = TestHelpers.FirstQueriedElement("<div><a>x</a><b>y</b>");

		CollectionAssert.AreEqual(new[] { "a", "b" }, element.Elements().Names());
	}

	[TestMethod]
	public void VoidElementHasNoChildren()
	{
		var element = TestHelpers.FirstQueriedElement("<br><span></span>");

		Assert.IsFalse(element.Elements().MoveNext());
	}

	[TestMethod]
	public void RawTextElementHasNoChildren()
	{
		var element = TestHelpers.FirstQueriedElement("<script><a>x</a></script><span></span>");

		Assert.IsFalse(element.Elements().MoveNext());
	}

	[TestMethod]
	public void ChildrenOfChildrenAreEnumeratedWithinTheirParent()
	{
		var element = TestHelpers.FirstQueriedElement("<ul><li><a>1</a></li><li><a>2</a></li></ul><li><a>3</a></li>");

		var items = element.Elements().ToList();
		Assert.HasCount(2, items);
		CollectionAssert.AreEqual(new[] { "a" }, items[0].Elements().Names());
		CollectionAssert.AreEqual(new[] { "a" }, items[1].Elements().Names());
	}

	[TestMethod]
	public void AttributeElementIsTheSameAsWhenScannedUpFront()
	{
		var html = "<div id=\"a\"><b>x</b></div><i>y</i>";
		var enumerated = TestHelpers.FirstElement(html);
		Assert.IsTrue(enumerated.TryGetAttribute("id", out var attribute));

		Assert.AreEqual(enumerated.OuterSpan.ToString(), attribute.Element.OuterSpan.ToString());
		CollectionAssert.AreEqual(new[] { "b" }, attribute.Element.Elements().Names());
	}
}