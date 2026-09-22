namespace PeterJuhasz.Text.Html.Tests.Lazy;

[TestClass]
public sealed class AttributeValueTests
{
	[TestMethod]
	public void DoubleQuotedValue()
	{
		var element = TestHelpers.FirstElement("<a href=\"http://x/?a=1&b=2\"></a>");

		Assert.IsTrue(element.TryGetAttribute("href", out var attribute));
		Assert.IsTrue(attribute.HasValue);
		Assert.AreEqual("http://x/?a=1&b=2", attribute.Value);
		Assert.AreEqual("http://x/?a=1&b=2", attribute.ValueSpan.ToString());
	}

	[TestMethod]
	public void SingleQuotedValue()
	{
		var element = TestHelpers.FirstElement("<a title='say \"hi\"'></a>");

		Assert.IsTrue(element.TryGetAttribute("title", out var attribute));
		Assert.AreEqual("say \"hi\"", attribute.Value);
	}

	[TestMethod]
	public void DoubleQuotedValueMayContainSingleQuotes()
	{
		var element = TestHelpers.FirstElement("<a title=\"it's\"></a>");

		Assert.IsTrue(element.TryGetAttribute("title", out var attribute));
		Assert.AreEqual("it's", attribute.Value);
	}

	[TestMethod]
	public void UnquotedValue()
	{
		var element = TestHelpers.FirstElement("<input type=text name=q>");

		Assert.IsTrue(element.TryGetAttribute("type", out var type));
		Assert.AreEqual("text", type.Value);
		Assert.IsTrue(element.TryGetAttribute("name", out var name));
		Assert.AreEqual("q", name.Value);
	}

	[TestMethod]
	public void UnquotedValueEndsAtCloseAngle()
	{
		var element = TestHelpers.FirstElement("<input value=abc>tail");

		Assert.IsTrue(element.TryGetAttribute("value", out var attribute));
		Assert.AreEqual("abc", attribute.Value);
		Assert.AreEqual("<input value=abc>", element.OuterSpan.ToString());
	}

	[TestMethod]
	public void EmptyQuotedValue()
	{
		var element = TestHelpers.FirstElement("<input value=\"\" alt=''>");

		Assert.IsTrue(element.TryGetAttribute("value", out var value));
		Assert.IsTrue(value.HasValue);
		Assert.AreEqual("", value.Value);
		Assert.IsTrue(element.TryGetAttribute("alt", out var alt));
		Assert.AreEqual("", alt.Value);
	}

	[TestMethod]
	public void ValueWithWhitespaceAndSpecialCharacters()
	{
		var element = TestHelpers.FirstElement("<div class=\"a b  c\" data-json='{\"k\": [1, 2]}'></div>");

		Assert.IsTrue(element.TryGetAttribute("class", out var @class));
		Assert.AreEqual("a b  c", @class.Value);
		Assert.IsTrue(element.TryGetAttribute("data-json", out var json));
		Assert.AreEqual("{\"k\": [1, 2]}", json.Value);
	}

	[TestMethod]
	public void ValueIsDecodedButValueSpanIsAsWritten()
	{
		var element = TestHelpers.FirstElement("<a title=\"a &amp; b &lt; &#x63; &quot;d&quot;\"></a>");

		Assert.IsTrue(element.TryGetAttribute("title", out var attribute));
		Assert.AreEqual("a & b < c \"d\"", attribute.Value);
		Assert.AreEqual("a &amp; b &lt; &#x63; &quot;d&quot;", attribute.ValueSpan.ToString());
	}

	[TestMethod]
	public void BareAttributeHasNoValue()
	{
		var element = TestHelpers.FirstElement("<input disabled>");

		Assert.IsTrue(element.TryGetAttribute("disabled", out var attribute));
		Assert.IsFalse(attribute.HasValue);
		Assert.IsNull(attribute.Value);
		Assert.IsTrue(attribute.ValueSpan.IsEmpty);
		Assert.IsFalse(attribute.TryGetValue(out var span));
		Assert.IsTrue(span.IsEmpty);
	}

	[TestMethod]
	public void BareAttributeBeforeSelfClosingSlash()
	{
		var element = TestHelpers.FirstElement("<input disabled/>");

		Assert.IsTrue(element.TryGetAttribute("disabled", out var attribute));
		Assert.IsFalse(attribute.HasValue);
	}

	[TestMethod]
	public void BareAttributeFollowedByValuedAttribute()
	{
		var element = TestHelpers.FirstElement("<input disabled value=\"x\">");

		Assert.IsTrue(element.TryGetAttribute("disabled", out var disabled));
		Assert.IsFalse(disabled.HasValue);
		Assert.IsTrue(element.TryGetAttribute("value", out var value));
		Assert.AreEqual("x", value.Value);
	}

	[TestMethod]
	public void TryGetValueReturnsValue()
	{
		var element = TestHelpers.FirstElement("<a href=\"x\"></a>");

		Assert.IsTrue(element.TryGetAttribute("href", out var attribute));
		Assert.IsTrue(attribute.TryGetValue(out var span));
		Assert.AreEqual("x", span.ToString());
	}

	[TestMethod]
	public void DefaultAttributeHasNoNameOrValue()
	{
		var attribute = default(LazyHtmlAttribute);

		Assert.AreEqual("", attribute.Name);
		Assert.IsFalse(attribute.HasValue);
		Assert.IsNull(attribute.Value);
	}
}
