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
	public void StandardNameIsSharedAndDoesNotAllocate()
	{
		var elements = LazyHtmlDocument.Parse("<a href=\"x\" onclick=\"f()\" aria-label=\"l\"></a><b href=\"y\"></b>").Elements().ToList();
		Assert.IsTrue(elements[0].TryGetAttribute("href", out var first));
		Assert.IsTrue(elements[1].TryGetAttribute("href", out var second));
		_ = first.Name;

		var before = GC.GetAllocatedBytesForCurrentThread();
		var name = first.Name;
		var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

		Assert.AreEqual("href", name);
		Assert.AreEqual(0, allocated);
		Assert.AreSame(name, second.Name);
		Assert.AreSequenceEqual(["href", "onclick", "aria-label"], elements[0].Attributes().Names());
	}

	[TestMethod]
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MSTEST0025:Use 'Assert.Fail' instead of an always-failing assert", Justification = "<Pending>")]
	public void NonLowercaseOrCustomNameIsCopied()
	{
		var element = TestHelpers.FirstElement("<a HREF=\"x\" Href=\"y\" data-x=\"z\"></a>");

		Assert.AreSequenceEqual(["HREF", "Href", "data-x"], element.Attributes().Names());
		Assert.IsTrue(element.TryGetAttribute("data-x", out var attribute));
		Assert.AreNotSame(attribute.Name, attribute.Name);
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

		Assert.AreSequenceEqual(["type", "name", "id", "required"], element.Attributes().Names());
	}

	[TestMethod]
	public void EnumeratesAttributesOfSelfClosingTag()
	{
		var element = TestHelpers.FirstElement("<img src=\"a.png\" alt=\"\"/>");

		Assert.AreSequenceEqual(["src", "alt"], element.Attributes().Names());
	}

	[TestMethod]
	public void EnumeratesAttributesSeparatedByNewlines()
	{
		var element = TestHelpers.FirstElement("<div\n\tid=\"a\"\r\n\tclass=\"b\"\n>x</div>");

		Assert.AreSequenceEqual(["id", "class"], element.Attributes().Names());
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
		var document = LazyHtmlDocument.Parse("<div><a class=\"x\">1</a><a class=\"y\">2</a></div>");

		Assert.IsTrue(document.TryQuerySelector(out var element, classNames: "y"));
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
