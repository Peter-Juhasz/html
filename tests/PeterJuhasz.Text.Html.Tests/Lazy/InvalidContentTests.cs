namespace PeterJuhasz.Text.Html.Tests.Lazy;

[TestClass]
public sealed class InvalidContentTests
{
	[TestMethod]
	public void UnclosedElementExtendsToEnd()
	{
		var element = TestHelpers.FirstElement("<div>text");

		Assert.AreEqual("<div>text", element.OuterSpan.ToString());
		Assert.AreEqual("text", element.InnerSpan.ToString());
		Assert.AreEqual("text", element.TextContent);
	}

	[TestMethod]
	public void UnclosedNestedElementsExtendToEnd()
	{
		var element = TestHelpers.FirstElement("<div><span>text");

		Assert.AreEqual("<div><span>text", element.OuterSpan.ToString());
		var span = element.Elements().ToList()[0];
		Assert.AreEqual("<span>text", span.OuterSpan.ToString());
	}

	[TestMethod]
	public void UnterminatedStartTagIsTheWholeElement()
	{
		var element = TestHelpers.FirstElement("<div class=\"a");

		Assert.AreEqual("div", element.Name);
		Assert.AreEqual("<div class=\"a", element.OuterSpan.ToString());
		Assert.IsTrue(element.InnerSpan.IsEmpty);
		Assert.IsTrue(element.TryGetAttribute("class", out var attribute));
		Assert.AreEqual("a", attribute.Value);
	}

	[TestMethod]
	public void UnterminatedStartTagWithoutAttributes()
	{
		var element = TestHelpers.FirstElement("<div");

		Assert.AreEqual("div", element.Name);
		Assert.AreEqual("<div", element.OuterSpan.ToString());
		Assert.IsFalse(element.Attributes().MoveNext());
	}

	[TestMethod]
	public void UnterminatedAttributeQuoteEndsAtCloseAngle()
	{
		var element = TestHelpers.FirstElement("<a href=\"foo>text</a>");

		Assert.IsTrue(element.TryGetAttribute("href", out var href));
		Assert.AreEqual("foo", href.Value);
		Assert.AreEqual("text", element.InnerSpan.ToString());
		Assert.AreEqual("<a href=\"foo>text</a>", element.OuterSpan.ToString());
	}

	[TestMethod]
	public void UnterminatedAttributeQuoteEndsAtWhitespace()
	{
		var element = TestHelpers.FirstElement("<a href='foo id=\"x\">text</a>");

		Assert.IsTrue(element.TryGetAttribute("href", out var href));
		Assert.AreEqual("foo", href.Value);
		Assert.IsTrue(element.TryGetAttribute("id", out var id));
		Assert.AreEqual("x", id.Value);
	}

	[TestMethod]
	public void UnterminatedEndTagStillClosesElement()
	{
		var element = TestHelpers.FirstElement("<div>a</div");

		Assert.AreEqual("<div>a</div", element.OuterSpan.ToString());
		Assert.AreEqual("a", element.InnerSpan.ToString());
	}

	[TestMethod]
	public void UnterminatedCommentSwallowsRest()
	{
		var element = TestHelpers.FirstElement("<div>a<!-- b</div>");

		Assert.AreEqual("<div>a<!-- b</div>", element.OuterSpan.ToString());
		Assert.AreEqual("a", element.TextContent);
	}

	[TestMethod]
	[DataRow("<")]
	[DataRow("<<<")]
	[DataRow("<>")]
	[DataRow("< div>")]
	[DataRow("<3>")]
	[DataRow("<-a>")]
	[DataRow("</>")]
	[DataRow("</div>")]
	[DataRow("</ div>")]
	[DataRow("<!>")]
	[DataRow("<!--")]
	[DataRow("<!---->")]
	[DataRow("<?>")]
	[DataRow("   ")]
	[DataRow(">>>")]
	[DataRow("a > b < c")]
	public void ContentWithoutStartTagYieldsNoElements(string html)
	{
		var document = LazyHtmlDocument.Parse(html);

		Assert.IsFalse(document.Elements().MoveNext());
	}

	[TestMethod]
	public void StrayOpenAngleIsText()
	{
		var element = TestHelpers.FirstElement("<p>a <3 b < c</p>");

		Assert.AreEqual("<p>a <3 b < c</p>", element.OuterSpan.ToString());
		Assert.AreEqual("a <3 b < c", element.TextContent);
	}

	[TestMethod]
	public void StrayEndTagBeforeElementIsSkipped()
	{
		var document = LazyHtmlDocument.Parse("</p><div></div>");

		Assert.AreSequenceEqual(["div"], document.Elements().Names());
	}

	[TestMethod]
	public void MismatchedEndTagClosesElement()
	{
		var document = LazyHtmlDocument.Parse("<a>x</b>y</a>");

		var elements = document.Elements().ToList();

		Assert.HasCount(1, elements);
		Assert.AreEqual("<a>x", elements[0].OuterSpan.ToString());
	}

	[TestMethod]
	public void OpenAngleInsideTagNameIsPartOfName()
	{
		var document = LazyHtmlDocument.Parse("<div<span>>x");

		var elements = document.Elements().ToList();

		Assert.HasCount(1, elements);
		Assert.AreEqual("div<span", elements[0].Name);
	}

	[TestMethod]
	public void AttributeWithoutNameIsSkipped()
	{
		var element = TestHelpers.FirstElement("<div =x =\"y\" id=\"a\"></div>");

		Assert.AreSequenceEqual(["id"], element.Attributes().Names());
		Assert.IsTrue(element.TryGetAttribute("id", out var id));
		Assert.AreEqual("a", id.Value);
	}

	[TestMethod]
	public void AttributeWithEqualsButNoValueHasEmptyValue()
	{
		var element = TestHelpers.FirstElement("<div a= b=></div>");

		Assert.IsTrue(element.TryGetAttribute("a", out var a));
		Assert.IsTrue(a.HasValue);
		Assert.AreEqual("", a.Value);
		Assert.IsTrue(element.TryGetAttribute("b", out var b));
		Assert.AreEqual("", b.Value);
	}

	[TestMethod]
	public void DoubleEqualsIsPartOfUnquotedValue()
	{
		var element = TestHelpers.FirstElement("<a href==x></a>");

		Assert.IsTrue(element.TryGetAttribute("href", out var href));
		Assert.AreEqual("=x", href.Value);
	}

	[TestMethod]
	public void QuoteInsideUnquotedValueIsKept()
	{
		var element = TestHelpers.FirstElement("<a href=x\"y></a>");

		Assert.IsTrue(element.TryGetAttribute("href", out var href));
		Assert.AreEqual("x\"y", href.Value);
	}

	[TestMethod]
	public void StraySlashesInsideStartTagAreIgnored()
	{
		var element = TestHelpers.FirstElement("<a / href=\"x\" / >text</a>");

		Assert.IsTrue(element.TryGetAttribute("href", out var href));
		Assert.AreEqual("x", href.Value);
		Assert.AreEqual("<a / href=\"x\" / >text</a>", element.OuterSpan.ToString());
	}

	[TestMethod]
	public void SlashSeparatedFromCloseAngleIsNotSelfClosing()
	{
		var element = TestHelpers.FirstElement("<div/ >text</div>");

		Assert.AreEqual("<div/ >text</div>", element.OuterSpan.ToString());
	}

	[TestMethod]
	public void EndTagWithAttributesIsMatched()
	{
		var element = TestHelpers.FirstElement("<div>a</div class=\"x\">tail");

		Assert.AreEqual("<div>a</div class=\"x\">", element.OuterSpan.ToString());
	}

	[TestMethod]
	public void ExcessiveNestingDoesNotOverflowTheStack()
	{
		var depth = 100_000;
		var html = string.Concat(Enumerable.Repeat("<div>", depth)) + "x" + string.Concat(Enumerable.Repeat("</div>", depth));
		var document = LazyHtmlDocument.Parse(html);

		var elements = document.Elements().ToList();

		Assert.HasCount(1, elements);
		Assert.AreEqual("div", elements[0].Name);
		Assert.IsGreaterThan(0, elements[0].TextContent.Length);
	}

	[TestMethod]
	public void ExcessiveNestingOfUnclosedElementsDoesNotOverflowTheStack()
	{
		var html = string.Concat(Enumerable.Repeat("<p>", 100_000));
		var document = LazyHtmlDocument.Parse(html);

		Assert.HasCount(100_000, document.Elements().ToList());
	}

	[TestMethod]
	public void NonAsciiContentIsHandled()
	{
		var element = TestHelpers.FirstElement("<p título=\"ñ\">héllo 😀 世界</p>");

		Assert.AreEqual("héllo 😀 世界", element.TextContent);
		Assert.IsTrue(element.TryGetAttribute("título", out var title));
		Assert.AreEqual("ñ", title.Value);
	}

	[TestMethod]
	public void RandomGarbageDoesNotThrow()
	{
		const string alphabet = "<>/=\"' \n\tabpdiv!-?&;";
		var random = new Random(12345);
		var buffer = new char[64];

		for (var iteration = 0; iteration < 2000; iteration++)
		{
			for (var i = 0; i < buffer.Length; i++)
				buffer[i] = alphabet[random.Next(alphabet.Length)];

			var html = new string(buffer);
			foreach (var element in LazyHtmlDocument.Parse(html).Elements())
				Visit(element);

			foreach (var node in LazyHtmlDocument.Parse(html).Nodes())
				Visit(node);
		}
	}

	// The nodes must cover the same elements as Elements(), with text and comments in between.
	private static void Visit(LazyHtmlNode node)
	{
		_ = node.OuterSpan;
		switch (node.Kind)
		{
			case LazyHtmlNodeKind.Element:
				Visit(node.Element);
				break;

			case LazyHtmlNodeKind.Text:
				Assert.IsFalse(node.Text.TextSpan.IsEmpty);
				Assert.IsTrue(node.OuterSpan.SequenceEqual(node.Text.TextSpan));
				break;

			case LazyHtmlNodeKind.Comment:
				Assert.IsTrue(node.OuterSpan.StartsWith("<!--"));
				Assert.IsLessThanOrEqualTo(node.OuterSpan.Length - 4, node.Comment.TextSpan.Length);
				break;
		}
	}

	private static void Visit(LazyHtmlElement element)
	{
		_ = element.Name;
		_ = element.OuterSpan;
		_ = element.InnerSpan;
		_ = element.TextContent;
		_ = element.HasAttribute("a");

		foreach (var attribute in element.Attributes())
		{
			_ = attribute.Name;
			_ = attribute.Value;
			_ = attribute.Element;
		}

		var elements = element.Elements().Names();
		var elementNodes = new List<string>();
		foreach (var child in element.Nodes())
		{
			Visit(child);
			if (child.TryGetElement(out var childElement))
				elementNodes.Add(childElement.Name);
		}

		Assert.AreSequenceEqual(elements, elementNodes);
	}
}
