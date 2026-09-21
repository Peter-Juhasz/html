using System.Text.Html.Lazy;
using Microsoft.Extensions.Primitives;

namespace PeterJuhasz.Text.Html.Tests;

[TestClass]
public sealed class StringSegmentTests
{
	[TestMethod]
	public void DocumentCanBeASegmentOfALargerString()
	{
		var segment = new StringSegment("xx<div>a</div>yy", 2, 12);
		var document = new LazyHtmlDocument(segment);

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

		Assert.AreEqual("<div>a", new LazyHtmlDocument(segment).Elements().ToList()[0].OuterSpan.ToString());
		Assert.AreEqual("<div>a</div>", TestHelpers.FirstElement(html).OuterSpan.ToString());
	}

	[TestMethod]
	public void IndicesAreRelativeToTheSegment()
	{
		var segment = new StringSegment("skip<a href=\"x\"></a>", 4, 16);

		var element = new LazyHtmlElement(segment, 0);
		var attribute = new LazyHtmlAttribute(segment, 3);

		Assert.AreEqual("a", element.Name);
		Assert.AreEqual("href", attribute.Name);
		Assert.AreEqual("x", attribute.Value);
	}

	[TestMethod]
	public void StringIsImplicitlyConvertedToSegment()
	{
		var document = new LazyHtmlDocument("<a></a>");

		Assert.IsTrue(document.Elements().MoveNext());
	}
}
