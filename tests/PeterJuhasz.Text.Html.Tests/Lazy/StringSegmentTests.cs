using Microsoft.Extensions.Primitives;

namespace PeterJuhasz.Text.Html.Tests.Lazy;

[TestClass]
public sealed class StringSegmentTests
{
	[TestMethod]
	public void DocumentCanBeASegmentOfALargerString()
	{
		var segment = new StringSegment("xx<div>a</div>yy", 2, 12);
		var document = LazyHtmlDocument.Parse(segment);

		var elements = document.Elements().ToList();

		Assert.HasCount(1, elements);
		Assert.AreEqual("<div>a</div>", elements[0].OuterSpan.ToString());
		Assert.AreEqual("a", elements[0].TextContent);
	}

	[TestMethod]
	public void ContentOutsideSegmentIsNotVisible()
	{
		var html = "<div>a</div>";
		var segment = new StringSegment(html, 0, 6);

		Assert.AreEqual("<div>a", LazyHtmlDocument.Parse(segment).Elements().ToList()[0].OuterSpan.ToString());
		Assert.AreEqual("<div>a</div>", TestHelpers.FirstElement(html).OuterSpan.ToString());
	}

	[TestMethod]
	public void IndicesAreRelativeToTheSegment()
	{
		var segment = new StringSegment("skip<a href=\"x\"></a>", 4, 16);

		var element = LazyHtmlDocument.Parse(segment).Elements().ToList()[0];
		Assert.IsTrue(element.TryGetAttribute("href", out var attribute));

		Assert.AreEqual("a", element.Name);
		Assert.AreEqual("href", attribute.Name);
		Assert.AreEqual("x", attribute.Value);
		Assert.AreEqual("<a href=\"x\"></a>", attribute.Element.OuterSpan.ToString());
	}

	[TestMethod]
	public void StringIsImplicitlyConvertedToSegment()
	{
		var document = LazyHtmlDocument.Parse("<a></a>");

		Assert.IsTrue(document.Elements().MoveNext());
	}
}
