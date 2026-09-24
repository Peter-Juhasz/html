using System.Text.Encodings.Web;
using System.Text.Unicode;

namespace PeterJuhasz.Text.Html.Tests.Writer;

[TestClass]
public sealed class AttributeTests
{
	[TestMethod]
	public void ValuedAttribute()
	{
		var html = TestHelpers.Write(writer =>
		{
			writer.OpenElement("a");
			writer.WriteAttribute("href", "/x");
			writer.CloseElement();
		});

		Assert.AreEqual("<a href=\"/x\"></a>", html);
	}

	[TestMethod]
	public void MultipleAttributesPreserveOrder()
	{
		var html = TestHelpers.Write(writer =>
		{
			writer.OpenElement("div");
			writer.WriteAttribute("id", "x");
			writer.WriteAttribute("class", "a b");
			writer.WriteAttribute("data-value", "1");
			writer.CloseElement();
		});

		Assert.AreEqual("<div id=x class=\"a b\" data-value=1></div>", html);
	}

	[TestMethod]
	public void BareAttribute()
	{
		var html = TestHelpers.Write(writer =>
		{
			writer.OpenElement("button");
			writer.WriteAttribute("disabled");
			writer.CloseElement();
		});

		Assert.AreEqual("<button disabled></button>", html);
	}

	[TestMethod]
	public void BareAttributeFollowedByValuedAttribute()
	{
		var html = TestHelpers.Write(writer =>
		{
			writer.OpenElement("option");
			writer.WriteAttribute("selected");
			writer.WriteAttribute("value", "1");
			writer.CloseElement();
		});

		Assert.AreEqual("<option selected value=1>", html);
	}

	[TestMethod]
	public void EmptyValue()
	{
		var html = TestHelpers.Write(writer =>
		{
			writer.OpenElement("input");
			writer.WriteAttribute("value", "");
			writer.CloseElement();
		});

		Assert.AreEqual("<input value=\"\">", html);
	}

	[TestMethod]
	public void AttributeFollowedByText()
	{
		var html = TestHelpers.Write(writer =>
		{
			writer.OpenElement("a");
			writer.WriteAttribute("href", "/x");
			writer.WriteText("link");
			writer.CloseElement();
		});

		Assert.AreEqual("<a href=\"/x\">link</a>", html);
	}

	[TestMethod]
	public void AttributeFollowedByChildElement()
	{
		var html = TestHelpers.Write(writer =>
		{
			writer.OpenElement("div");
			writer.WriteAttribute("id", "outer");
			writer.OpenElement("span");
			writer.WriteAttribute("id", "inner");
			writer.CloseElement();
			writer.CloseElement();
		});

		Assert.AreEqual("<div id=outer><span id=inner></span></div>", html);
	}

	[TestMethod]
	public void DoubleQuoteIsEncoded()
	{
		var html = TestHelpers.Write(writer =>
		{
			writer.OpenElement("a");
			writer.WriteAttribute("title", "say \"hi\"");
			writer.CloseElement();
		});

		Assert.AreEqual("<a title=\"say &quot;hi&quot;\"></a>", html);
	}

	[TestMethod]
	public void SingleQuoteIsEncoded()
	{
		var html = TestHelpers.Write(writer =>
		{
			writer.OpenElement("a");
			writer.WriteAttribute("title", "it's");
			writer.CloseElement();
		});

		Assert.AreEqual("<a title=\"it&#x27;s\"></a>", html);
	}

	[TestMethod]
	public void AmpersandAndAngleBracketsAreEncoded()
	{
		var html = TestHelpers.Write(writer =>
		{
			writer.OpenElement("a");
			writer.WriteAttribute("href", "/x?a=1&b=<2>");
			writer.CloseElement();
		});

		Assert.AreEqual("<a href=\"/x?a=1&amp;b=&lt;2&gt;\"></a>", html);
	}

	[TestMethod]
	public void AlreadyEncodedValueIsEncodedAgain()
	{
		var html = TestHelpers.Write(writer =>
		{
			writer.OpenElement("a");
			writer.WriteAttribute("title", "a &amp; b");
			writer.CloseElement();
		});

		Assert.AreEqual("<a title=\"a &amp;amp; b\"></a>", html);
	}

	[TestMethod]
	public void NonAsciiValueIsEncodedByDefault()
	{
		var html = TestHelpers.Write(writer =>
		{
			writer.OpenElement("p");
			writer.WriteAttribute("title", "ñ");
			writer.CloseElement();
		});

		Assert.AreEqual("<p title=\"&#xF1;\"></p>", html);
	}

	[TestMethod]
	public void NonAsciiValueIsPreservedWithRelaxedEncoder()
	{
		var html = TestHelpers.Write(writer =>
		{
			writer.OpenElement("p");
			writer.WriteAttribute("title", "ñ \"世界\"");
			writer.CloseElement();
		}, HtmlEncoder.Create(UnicodeRanges.All));

		Assert.AreEqual("<p title=\"ñ &quot;世界&quot;\"></p>", html);
	}

	[TestMethod]
	public void AttributeNameIsWrittenAsGiven()
	{
		var html = TestHelpers.Write(writer =>
		{
			writer.OpenElement("div");
			writer.WriteAttribute("Data-Foo", "1");
			writer.WriteAttribute("xlink:href", "#a");
			writer.CloseElement();
		});

		Assert.AreEqual("<div Data-Foo=1 xlink:href=\"#a\"></div>", html);
	}

	[TestMethod]
	public void ValueWithSpacesIsPreserved()
	{
		var html = TestHelpers.Write(writer =>
		{
			writer.OpenElement("div");
			writer.WriteAttribute("class", " a  b ");
			writer.CloseElement();
		});

		Assert.AreEqual("<div class=\" a  b \"></div>", html);
	}

	[TestMethod]
	public void LineBreaksAndTabsInValueAreEncodedByDefault()
	{
		var html = TestHelpers.Write(writer =>
		{
			writer.OpenElement("div");
			writer.WriteAttribute("class", "a\tb\nc");
			writer.CloseElement();
		});

		Assert.AreEqual("<div class=\"a&#x9;b&#xA;c\"></div>", html);
	}

	[TestMethod]
	public void LongValueIsEncodedCompletely()
	{
		var value = string.Concat(Enumerable.Repeat("a&", 5000));

		var html = TestHelpers.Write(writer =>
		{
			writer.OpenElement("div");
			writer.WriteAttribute("title", value);
			writer.CloseElement();
		}, initialCapacity: 1);

		Assert.AreEqual("<div title=\"" + string.Concat(Enumerable.Repeat("a&amp;", 5000)) + "\"></div>", html);
	}
}
