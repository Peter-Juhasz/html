using System.Text.Html.Lazy;

namespace PeterJuhasz.Text.Html.Tests.Lazy;

[TestClass]
public sealed class AttributeLookupTests
{
	[TestMethod]
	public void FindsAttributeByName()
	{
		var element = TestHelpers.FirstElement("<a href=\"x\">link</a>");

		Assert.IsTrue(element.TryGetAttribute("href", out var attribute));
		Assert.AreEqual("href", attribute.Name);
		Assert.AreEqual("x", attribute.Value);
	}

	[TestMethod]
	public void LookupIsCaseInsensitive()
	{
		var element = TestHelpers.FirstElement("<a HREF=\"x\">link</a>");

		Assert.IsTrue(element.TryGetAttribute("href", out var attribute));
		Assert.AreEqual("HREF", attribute.Name);
		Assert.IsTrue(element.HasAttribute("Href"));
	}

	[TestMethod]
	public void ReturnsFalseForMissingAttribute()
	{
		var element = TestHelpers.FirstElement("<a href=\"x\">link</a>");

		Assert.IsFalse(element.TryGetAttribute("class", out var attribute));
		Assert.IsFalse(element.HasAttribute("class"));
		Assert.AreEqual("", attribute.Name);
		Assert.IsFalse(attribute.HasValue);
	}

	[TestMethod]
	public void ReturnsFalseForElementWithoutAttributes()
	{
		var element = TestHelpers.FirstElement("<a>link</a>");

		Assert.IsFalse(element.HasAttribute("href"));
		Assert.IsFalse(element.Attributes().MoveNext());
	}

	[TestMethod]
	public void FindsAttributeAmongMany()
	{
		var element = TestHelpers.FirstElement("<input type=\"text\" name=\"q\" id=\"search\" required>");

		Assert.IsTrue(element.TryGetAttribute("id", out var id));
		Assert.AreEqual("search", id.Value);
		Assert.IsTrue(element.HasAttribute("required"));
		Assert.IsTrue(element.HasAttribute("type"));
	}

	[TestMethod]
	public void FirstDuplicateWins()
	{
		var element = TestHelpers.FirstElement("<a class=\"first\" class=\"second\"></a>");

		Assert.IsTrue(element.TryGetAttribute("class", out var attribute));
		Assert.AreEqual("first", attribute.Value);
	}

	[TestMethod]
	public void DoesNotMatchAttributeNamePrefix()
	{
		var element = TestHelpers.FirstElement("<a data-id=\"x\"></a>");

		Assert.IsFalse(element.HasAttribute("data"));
		Assert.IsFalse(element.HasAttribute("id"));
	}

	[TestMethod]
	public void DoesNotMatchAttributesInsideContent()
	{
		var element = TestHelpers.FirstElement("<div><a href=\"x\"></a></div>");

		Assert.IsFalse(element.HasAttribute("href"));
	}

	[TestMethod]
	public void EnumeratesAllAttributesInOrder()
	{
		var element = TestHelpers.FirstElement("<input type=\"text\" name='q' id=search required>");

		CollectionAssert.AreEqual(new[] { "type", "name", "id", "required" }, element.Attributes().Names());
	}

	[TestMethod]
	public void EnumeratesAttributesOfSelfClosingTag()
	{
		var element = TestHelpers.FirstElement("<img src=\"a.png\" alt=\"\"/>");

		CollectionAssert.AreEqual(new[] { "src", "alt" }, element.Attributes().Names());
	}

	[TestMethod]
	public void EnumeratesAttributesSeparatedByNewlines()
	{
		var element = TestHelpers.FirstElement("<div\n\tid=\"a\"\r\n\tclass=\"b\"\n>x</div>");

		CollectionAssert.AreEqual(new[] { "id", "class" }, element.Attributes().Names());
	}

	[TestMethod]
	public void AttributeCanBeConstructedAtIndex()
	{
		var html = "<a href=\"x\">";
		var attribute = new LazyHtmlAttribute(html, 0, html.IndexOf("href", StringComparison.Ordinal));

		Assert.AreEqual("href", attribute.Name);
		Assert.AreEqual("x", attribute.Value);
		Assert.AreEqual("a", attribute.Element.Name);
	}

	[TestMethod]
	public void AttributeConstructorRejectsIndexOutOfRange()
	{
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new LazyHtmlAttribute("<a href=\"x\">", 0, -1));
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new LazyHtmlAttribute("<a href=\"x\">", 0, 12));
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new LazyHtmlAttribute("<a href=\"x\">", -1, 3));
	}

	[TestMethod]
	public void AttributeConstructorRejectsElementNotBeforeAttribute()
	{
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new LazyHtmlAttribute("<a href=\"x\">", 3, 3));
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new LazyHtmlAttribute("<a href=\"x\">", 4, 3));
	}

	[TestMethod]
	public void AttributeConstructorRejectsElementIndexNotAtStartTag()
	{
		Assert.ThrowsExactly<ArgumentException>(() => new LazyHtmlAttribute("x<a href=\"y\">", 0, 4));
		Assert.ThrowsExactly<ArgumentException>(() => new LazyHtmlAttribute("<!--x--><a href=\"y\">", 0, 11));
		Assert.ThrowsExactly<ArgumentException>(() => new LazyHtmlAttribute("</a><a href=\"y\">", 0, 7));
	}

	[TestMethod]
	public void AttributePointsToItsElement()
	{
		var element = TestHelpers.FirstElement("<a href=\"x\" class=\"y\">link</a>");

		Assert.IsTrue(element.TryGetAttribute("class", out var attribute));
		Assert.AreEqual(element.OuterSpan.ToString(), attribute.Element.OuterSpan.ToString());
		Assert.AreEqual("a", attribute.Element.Name);
		Assert.AreEqual("link", attribute.Element.TextContent);
	}

	[TestMethod]
	public void EnumeratedAttributesPointToTheirElement()
	{
		var element = TestHelpers.FirstElement("<input type=\"text\" name=\"q\" required>");

		foreach (var attribute in element.Attributes())
			Assert.AreEqual(element.OuterSpan.ToString(), attribute.Element.OuterSpan.ToString(), $"Attribute {attribute.Name}.");
	}

	[TestMethod]
	public void AttributeOfNestedElementPointsToTheNestedElement()
	{
		var outer = TestHelpers.FirstElement("<div id=\"o\"><p id=\"i\">x</p></div>");
		var inner = outer.Elements().ToList()[0];

		Assert.IsTrue(outer.TryGetAttribute("id", out var outerId));
		Assert.IsTrue(inner.TryGetAttribute("id", out var innerId));
		Assert.AreEqual("<div id=\"o\"><p id=\"i\">x</p></div>", outerId.Element.OuterSpan.ToString());
		Assert.AreEqual("<p id=\"i\">x</p>", innerId.Element.OuterSpan.ToString());
	}

	[TestMethod]
	public void AttributeOfQueriedElementPointsToTheElement()
	{
		var document = new LazyHtmlDocument("<div><a class=\"x\">1</a><a class=\"y\">2</a></div>");

		Assert.IsTrue(document.TryQuerySelector(out var element, className: "y"));
		Assert.IsTrue(element.TryGetAttribute("class", out var attribute));
		Assert.AreEqual("<a class=\"y\">2</a>", attribute.Element.OuterSpan.ToString());
	}

	[TestMethod]
	public void AttributeElementCanBeRoundTripped()
	{
		var element = TestHelpers.FirstElement("<a href=\"x\" title=\"t\">link</a>");

		Assert.IsTrue(element.TryGetAttribute("title", out var attribute));
		Assert.IsTrue(attribute.Element.TryGetAttribute("href", out var href));
		Assert.AreEqual("x", href.Value);
	}
}
