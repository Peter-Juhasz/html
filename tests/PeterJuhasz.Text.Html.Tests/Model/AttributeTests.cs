using System.Text.Html.Model;

namespace PeterJuhasz.Text.Html.Tests.Model;

[TestClass]
public sealed class AttributeTests
{
	[TestMethod]
	public void AttributesAreInSourceOrder()
	{
		var element = TestHelpers.FirstElement("<a href=\"/\" class=\"x\" id=y data-z></a>");

		Assert.AreSequenceEqual(["href", "class", "id", "data-z"], element.Attributes.Names());
	}

	[TestMethod]
	public void ElementWithoutAttributesHasEmptyAttributes()
	{
		var element = TestHelpers.FirstElement("<div></div>");

		Assert.IsEmpty(element.Attributes);
	}

	[TestMethod]
	public void AttributesReferenceTheirElement()
	{
		var element = TestHelpers.FirstElement("<a href=\"/\" target=_blank></a>");

		foreach (var attribute in element.Attributes)
			Assert.AreSame(element, attribute.Element);
	}

	[TestMethod]
	public void QuotedAndUnquotedValuesAreReadWithoutQuotes()
	{
		var element = TestHelpers.FirstElement("<input type=text title='say \"hi\"' value=\"it's\" data-json='{\"k\": [1, 2]}'>");

		Assert.AreEqual("text", element.GetAttribute("type")?.Value);
		Assert.AreEqual("say \"hi\"", element.GetAttribute("title")?.Value);
		Assert.AreEqual("it's", element.GetAttribute("value")?.Value);
		Assert.AreEqual("{\"k\": [1, 2]}", element.GetAttribute("data-json")?.Value);
	}

	[TestMethod]
	public void EmptyQuotedValueIsEmptyString()
	{
		var element = TestHelpers.FirstElement("<input value=\"\" alt=''>");

		Assert.IsTrue(element.TryGetAttribute("value", out var value));
		Assert.IsTrue(value.HasValue);
		Assert.AreEqual("", value.Value);
		Assert.AreEqual("", element.GetAttribute("alt")?.Value);
	}

	[TestMethod]
	public void BareAttributeHasNoValue()
	{
		var element = TestHelpers.FirstElement("<input disabled value=\"x\">");

		Assert.IsTrue(element.TryGetAttribute("disabled", out var disabled));
		Assert.IsFalse(disabled.HasValue);
		Assert.IsNull(disabled.Value);
		Assert.AreEqual("x", element.GetAttribute("value")?.Value);
	}

	[TestMethod]
	public void ValueIsNotDecoded()
	{
		var element = TestHelpers.FirstElement("<a title=\"a &amp; b\"></a>");

		Assert.AreEqual("a &amp; b", element.GetAttribute("title")?.Value);
	}

	[TestMethod]
	public void LookupIsCaseInsensitiveAndPreservesTheWrittenName()
	{
		var element = TestHelpers.FirstElement("<a HREF=\"/\"></a>");

		Assert.IsTrue(element.TryGetAttribute("href", out var attribute));
		Assert.AreEqual("HREF", attribute.Name);
		Assert.IsTrue(element.HasAttribute("Href"));
	}

	[TestMethod]
	public void LookupDoesNotMatchPrefixes()
	{
		var element = TestHelpers.FirstElement("<a data-id=\"1\"></a>");

		Assert.IsFalse(element.HasAttribute("data"));
		Assert.IsFalse(element.HasAttribute("data-id-x"));
		Assert.IsTrue(element.HasAttribute("data-id"));
	}

	[TestMethod]
	public void FirstOfDuplicateAttributesWins()
	{
		var element = TestHelpers.FirstElement("<a class=\"x\" class=\"y\"></a>");

		Assert.AreEqual("x", element.GetAttribute("class")?.Value);
		Assert.HasCount(2, element.Attributes);
	}

	[TestMethod]
	public void MissingAttributeIsNotFound()
	{
		var element = TestHelpers.FirstElement("<a href=\"/\"></a>");

		Assert.IsFalse(element.TryGetAttribute("target", out var attribute));
		Assert.IsNull(attribute);
		Assert.IsFalse(element.HasAttribute("target"));
		Assert.IsNull(element.GetAttribute("target"));
	}

	[TestMethod]
	public void AttributesOfChildrenAreNotOnTheParent()
	{
		var element = TestHelpers.FirstElement("<div><a href=\"/\"></a></div>");

		Assert.IsEmpty(element.Attributes);
		Assert.IsFalse(element.HasAttribute("href"));
	}

	[TestMethod]
	public void AttributeWithoutNameIsSkipped()
	{
		var element = TestHelpers.FirstElement("<div =x =\"y\" id=\"a\"></div>");

		Assert.AreSequenceEqual(["id"], element.Attributes.Names());
	}

	[TestMethod]
	public void AttributesOfUnterminatedStartTagAreRead()
	{
		var element = TestHelpers.FirstElement("<div class=\"a");

		Assert.AreEqual("a", element.GetAttribute("class")?.Value);
	}

	[TestMethod]
	public void NonAsciiAttributesAreRead()
	{
		var element = TestHelpers.FirstElement("<p título=\"ñ\"></p>");

		Assert.AreEqual("ñ", element.GetAttribute("título")?.Value);
	}
}
