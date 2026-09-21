using System.Text.Html.Model;

namespace PeterJuhasz.Text.Html.Tests.Model;

[TestClass]
public sealed class TreeTests
{
	[TestMethod]
	public void ChildrenAreDirectChildrenOnly()
	{
		var element = TestHelpers.FirstElement("<ul><li><a>1</a></li><li><a>2</a></li></ul>");

		CollectionAssert.AreEqual(new[] { "li", "li" }, element.Children.Names());
	}

	[TestMethod]
	public void ChildrenReferenceTheirParentAndDocument()
	{
		var document = HtmlDocument.Parse("<html><body><div><p>x</p></div></body></html>");

		var html = document.Elements[0];
		var body = html.Children[0];
		var div = body.Children[0];
		var p = div.Children[0];

		Assert.IsNull(html.Parent);
		Assert.AreSame(html, body.Parent);
		Assert.AreSame(body, div.Parent);
		Assert.AreSame(div, p.Parent);
		Assert.AreSame(document, p.Document);
		Assert.AreEqual("x", p.InnerSpan.ToString());
	}

	[TestMethod]
	public void SiblingsShareTheSameParent()
	{
		var element = TestHelpers.FirstElement("<div><a></a>text<b></b><!-- c --><i></i></div>");

		CollectionAssert.AreEqual(new[] { "a", "b", "i" }, element.Children.Names());
		foreach (var child in element.Children)
			Assert.AreSame(element, child.Parent);
	}

	[TestMethod]
	public void ElementWithoutChildrenHasEmptyChildren()
	{
		var element = TestHelpers.FirstElement("<div>text only</div>");

		Assert.IsEmpty(element.Children);
		Assert.IsEmpty(element.Descendants());
	}

	[TestMethod]
	public void VoidAndSelfClosingElementsHaveNoChildren()
	{
		var document = HtmlDocument.Parse("<p>a<br>b<img src=\"x\"><span/><span>d</span></p>");

		var paragraph = document.Elements[0];

		CollectionAssert.AreEqual(new[] { "br", "img", "span", "span" }, paragraph.Children.Names());
		Assert.IsEmpty(paragraph.Children[0].Children);
		Assert.IsEmpty(paragraph.Children[1].Children);
		Assert.IsEmpty(paragraph.Children[2].Children);
		Assert.AreEqual("d", paragraph.Children[3].InnerSpan.ToString());
	}

	[TestMethod]
	public void RawTextElementsHaveNoChildren()
	{
		var document = HtmlDocument.Parse("<script>if (a<b) { x = '<i>'; }</script><style>a>b{}</style><textarea><p></textarea><title><em></title>");

		CollectionAssert.AreEqual(new[] { "script", "style", "textarea", "title" }, document.Elements.Names());
		foreach (var element in document.Elements)
			Assert.IsEmpty(element.Children);
	}

	[TestMethod]
	public void ImplicitlyClosedElementsAreSiblings()
	{
		var element = TestHelpers.FirstElement("<ul><li>one<li>two<li>three</ul>");

		CollectionAssert.AreEqual(new[] { "<li>one", "<li>two", "<li>three" }, element.Children.Outers());
		Assert.AreEqual("<ul><li>one<li>two<li>three</ul>", element.OuterSpan.ToString());
	}

	[TestMethod]
	public void TableStructureIsPreserved()
	{
		var element = TestHelpers.FirstElement("<table><tr><td>1<td>2<tr><td>3<td>4</table>");

		var rows = element.Children;

		Assert.HasCount(2, rows);
		CollectionAssert.AreEqual(new[] { "1", "2" }, rows[0].Children.Select(c => c.TextContent).ToList());
		CollectionAssert.AreEqual(new[] { "3", "4" }, rows[1].Children.Select(c => c.TextContent).ToList());
	}

	[TestMethod]
	public void ParagraphClosedByBlockElementDoesNotContainIt()
	{
		var document = HtmlDocument.Parse("<p>a<div>b</div>");

		CollectionAssert.AreEqual(new[] { "p", "div" }, document.Elements.Names());
		Assert.IsEmpty(document.Elements[0].Children);
	}

	[TestMethod]
	public void UnclosedElementsNest()
	{
		var document = HtmlDocument.Parse("<div><span>a<span>b");

		var div = document.Elements[0];
		var outer = div.Children[0];
		var inner = outer.Children[0];

		Assert.HasCount(1, document.Elements);
		Assert.AreEqual("<span>b", inner.OuterSpan.ToString());
		Assert.AreSame(outer, inner.Parent);
	}

	[TestMethod]
	public void ChildrenDoNotLeakOutsideParent()
	{
		var document = HtmlDocument.Parse("<div><a></a></div><b></b>");

		CollectionAssert.AreEqual(new[] { "a" }, document.Elements[0].Children.Names());
		CollectionAssert.AreEqual(new[] { "div", "b" }, document.Elements.Names());
	}

	[TestMethod]
	public void NameIsAsWritten()
	{
		var document = HtmlDocument.Parse("<DIV><Span></Span></DIV>");

		Assert.AreEqual("DIV", document.Elements[0].Name);
		Assert.AreEqual("Span", document.Elements[0].Children[0].Name);
	}

	[TestMethod]
	public void DeeplyNestedStructureIsHandled()
	{
		var depth = 100;
		var html = string.Concat(Enumerable.Repeat("<div>", depth)) + "x" + string.Concat(Enumerable.Repeat("</div>", depth));
		var element = TestHelpers.FirstElement(html);

		var current = element;
		for (var i = 1; i < depth; i++)
		{
			Assert.HasCount(1, current.Children);
			current = current.Children[0];
		}

		Assert.IsEmpty(current.Children);
		Assert.AreEqual("x", current.InnerSpan.ToString());
		Assert.AreEqual(html, element.OuterSpan.ToString());
	}

	[TestMethod]
	public void ExcessiveNestingDoesNotOverflowTheStack()
	{
		var depth = 20_000;
		var html = string.Concat(Enumerable.Repeat("<div>", depth)) + "x" + string.Concat(Enumerable.Repeat("</div>", depth));
		var document = HtmlDocument.Parse(html);

		Assert.HasCount(1, document.Elements);
		Assert.AreEqual("div", document.Elements[0].Name);
		Assert.AreEqual("x", document.Elements[0].TextContent);
		Assert.IsGreaterThan(100, document.Descendants().Count());
		Assert.IsLessThan(depth, document.Descendants().Count());
	}

	[TestMethod]
	public void ExcessiveNestingOfUnclosedElementsDoesNotOverflowTheStack()
	{
		var html = string.Concat(Enumerable.Repeat("<p>", 100_000));
		var document = HtmlDocument.Parse(html);

		Assert.HasCount(100_000, document.Elements);
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

			var document = HtmlDocument.Parse(new string(buffer));
			foreach (var element in document.Descendants())
			{
				_ = element.Name;
				_ = element.OuterSpan;
				_ = element.InnerSpan;
				_ = element.TextContent;
				_ = element.HasAttribute("a");
				Assert.AreSame(document, element.Document);

				foreach (var attribute in element.Attributes)
				{
					_ = attribute.Name;
					_ = attribute.Value;
					Assert.AreSame(element, attribute.Element);
				}

				foreach (var child in element.Children)
					Assert.AreSame(element, child.Parent);
			}
		}
	}
}
