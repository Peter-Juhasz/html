using System.Text.Html.Lazy;

namespace PeterJuhasz.Text.Html.Tests.Lazy;

[TestClass]
public sealed class ImplicitlyClosedElementTests
{
	[TestMethod]
	public void ListItemIsClosedByNextListItem()
	{
		var element = TestHelpers.FirstElement("<ul><li>one<li>two<li>three</ul>");

		var items = element.Elements().ToList();

		Assert.HasCount(3, items);
		Assert.AreEqual("<li>one", items[0].OuterSpan.ToString());
		Assert.AreEqual("<li>two", items[1].OuterSpan.ToString());
		Assert.AreEqual("<li>three", items[2].OuterSpan.ToString());
		Assert.AreEqual("<ul><li>one<li>two<li>three</ul>", element.OuterSpan.ToString());
	}

	[TestMethod]
	public void ParagraphIsClosedByNextParagraph()
	{
		var document = new LazyHtmlDocument("<p>a<p>b<p>c");

		var paragraphs = document.Elements().ToList();

		Assert.HasCount(3, paragraphs);
		Assert.AreEqual("a", paragraphs[0].TextContent);
		Assert.AreEqual("b", paragraphs[1].TextContent);
		Assert.AreEqual("c", paragraphs[2].TextContent);
	}

	[TestMethod]
	public void TableCellsAndRowsAreClosedBySiblings()
	{
		var element = TestHelpers.FirstElement("<table><tr><td>1<td>2<tr><td>3<td>4</table>");

		var rows = element.Elements().ToList();

		Assert.HasCount(2, rows);
		CollectionAssert.AreEqual(new[] { "1", "2" }, rows[0].Elements().ToList().ConvertAll(c => c.TextContent));
		CollectionAssert.AreEqual(new[] { "3", "4" }, rows[1].Elements().ToList().ConvertAll(c => c.TextContent));
	}

	[TestMethod]
	public void OptionsAreClosedBySiblings()
	{
		var element = TestHelpers.FirstElement("<select><option value=\"1\">One<option value=\"2\">Two</select>");

		CollectionAssert.AreEqual(new[] { "One", "Two" }, element.Elements().ToList().ConvertAll(c => c.TextContent));
	}

	[TestMethod]
	public void DefinitionTermsAndDescriptionsAreClosedByEachOther()
	{
		var element = TestHelpers.FirstElement("<dl><dt>a<dt>b<dd>c<dd>d</dl>");

		CollectionAssert.AreEqual(new[] { "dt", "dt", "dd", "dd" }, element.Elements().Names());
	}

	[TestMethod]
	public void ParagraphIsClosedByBlockLevelStartTag()
	{
		var document = new LazyHtmlDocument("<p>a<div>b</div><p>c<ul><li>d</ul><p>e<hr><p>f<h1>g</h1>");

		var elements = document.Elements().ToList();

		CollectionAssert.AreEqual(new[] { "p", "div", "p", "ul", "p", "hr", "p", "h1" }, elements.ConvertAll(e => e.Name));
		Assert.AreEqual("a", elements[0].TextContent);
		Assert.AreEqual("c", elements[2].TextContent);
		Assert.AreEqual("e", elements[4].TextContent);
	}

	[TestMethod]
	public void ParagraphIsNotClosedByInlineStartTag()
	{
		var element = TestHelpers.FirstElement("<p>a<span>b</span><b>c</b><img><a href=\"x\">d</a>");

		Assert.AreEqual("abcd", element.TextContent);
		CollectionAssert.AreEqual(new[] { "span", "b", "img", "a" }, element.Elements().Names());
	}

	[TestMethod]
	public void TableSectionsAreClosedByEachOther()
	{
		var element = TestHelpers.FirstElement("<table><thead><tr><th>h<tbody><tr><td>1<tr><td>2<tfoot><tr><td>f</table>");

		var sections = element.Elements().ToList();

		CollectionAssert.AreEqual(new[] { "thead", "tbody", "tfoot" }, sections.ConvertAll(s => s.Name));
		Assert.HasCount(1, sections[0].Elements().ToList());
		Assert.HasCount(2, sections[1].Elements().ToList());
		Assert.HasCount(1, sections[2].Elements().ToList());
	}

	[TestMethod]
	public void OptionIsClosedByOptionGroup()
	{
		var element = TestHelpers.FirstElement("<select><option>a<optgroup label=\"g\"><option>b<option>c<optgroup label=\"h\"><option>d</select>");

		var children = element.Elements().ToList();

		CollectionAssert.AreEqual(new[] { "option", "optgroup", "optgroup" }, children.ConvertAll(c => c.Name));
		CollectionAssert.AreEqual(new[] { "b", "c" }, children[1].Elements().ToList().ConvertAll(o => o.TextContent));
		CollectionAssert.AreEqual(new[] { "d" }, children[2].Elements().ToList().ConvertAll(o => o.TextContent));
	}

	[TestMethod]
	public void SiblingClosingIsCaseInsensitive()
	{
		var element = TestHelpers.FirstElement("<ul><li>one<LI>two</ul>");

		Assert.HasCount(2, element.Elements().ToList());
	}

	[TestMethod]
	public void ExplicitlyClosedListItemsStillWork()
	{
		var element = TestHelpers.FirstElement("<ul><li>one</li><li>two</li></ul>");

		var items = element.Elements().ToList();

		Assert.HasCount(2, items);
		Assert.AreEqual("<li>one</li>", items[0].OuterSpan.ToString());
	}

	[TestMethod]
	public void NestedListsAreNotClosedByInnerItems()
	{
		var element = TestHelpers.FirstElement("<ul><li>a<ul><li>a1</li><li>a2</li></ul></li><li>b</li></ul>");

		var items = element.Elements().ToList();

		Assert.HasCount(2, items);
		Assert.AreEqual("<li>a<ul><li>a1</li><li>a2</li></ul></li>", items[0].OuterSpan.ToString());
		Assert.AreEqual("<li>b</li>", items[1].OuterSpan.ToString());
	}

	[TestMethod]
	public void ElementIsClosedByParentEndTag()
	{
		var element = TestHelpers.FirstElement("<div><p>unclosed</div>tail");

		Assert.AreEqual("<div><p>unclosed</div>", element.OuterSpan.ToString());
		var paragraph = element.Elements().ToList()[0];
		Assert.AreEqual("<p>unclosed", paragraph.OuterSpan.ToString());
		Assert.AreEqual("unclosed", paragraph.InnerSpan.ToString());
	}

	[TestMethod]
	public void NonSiblingClosedElementsNestWhenUnclosed()
	{
		var element = TestHelpers.FirstElement("<div><span>a<span>b</div>");

		var outer = element.Elements().ToList();
		Assert.HasCount(1, outer);
		var inner = outer[0].Elements().ToList();
		Assert.HasCount(1, inner);
		Assert.AreEqual("<span>b", inner[0].OuterSpan.ToString());
	}
}
