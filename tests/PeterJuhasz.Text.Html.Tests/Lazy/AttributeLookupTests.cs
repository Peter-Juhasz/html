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
		var attribute = new LazyHtmlAttribute(html, html.IndexOf("href", StringComparison.Ordinal));

		Assert.AreEqual("href", attribute.Name);
		Assert.AreEqual("x", attribute.Value);
	}

	[TestMethod]
	public void AttributeConstructorRejectsIndexOutOfRange()
	{
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new LazyHtmlAttribute("<a href=\"x\">", -1));
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new LazyHtmlAttribute("<a href=\"x\">", 12));
	}
}
