namespace PeterJuhasz.Text.Html.Tests.Model;

[TestClass]
public sealed class NodesTests
{
	[TestMethod]
	public void DocumentNodesAreTextElementsAndCommentsInOrder()
	{
		var document = HtmlDocument.Parse("before<div>x</div><!-- c -->after");

		var nodes = document.Nodes;

		Assert.HasCount(4, nodes);
		Assert.IsInstanceOfType<HtmlText>(nodes[0]);
		Assert.IsInstanceOfType<HtmlElement>(nodes[1]);
		Assert.IsInstanceOfType<HtmlComment>(nodes[2]);
		Assert.IsInstanceOfType<HtmlText>(nodes[3]);
		Assert.AreSequenceEqual(["before", "<div>x</div>", "<!-- c -->", "after"], nodes.Select(n => n.ToString()).ToList());
	}

	[TestMethod]
	public void TopLevelNodesHaveNoParentAndReferenceTheDocument()
	{
		var document = HtmlDocument.Parse("a<b></b><!--c-->");

		foreach (var node in document.Nodes)
		{
			Assert.IsNull(node.Parent);
			Assert.AreSame(document, node.Document);
		}
	}

	[TestMethod]
	public void ElementNodesReferenceTheirParent()
	{
		var element = TestHelpers.FirstElement("<p>a<b>b</b>c<!--d-->e</p>");

		var nodes = element.Nodes;

		Assert.AreSequenceEqual(["a", "<b>b</b>", "c", "<!--d-->", "e"], nodes.Select(n => n.OuterSpan.ToString()).ToList());
		foreach (var node in nodes)
		{
			Assert.AreSame(element, node.Parent);
			Assert.AreSame(element.Document, node.Document);
		}
	}

	[TestMethod]
	public void TextExposesItsContent()
	{
		var text = (HtmlText)HtmlDocument.Parse("<p>a &amp; b</p>").Elements().First().Nodes[0];

		Assert.AreEqual("a & b", text.Text);
		Assert.AreEqual("a &amp; b", text.TextSpan.ToString());
		Assert.AreEqual("a &amp; b", text.OuterSpan.ToString());
		Assert.AreEqual("a &amp; b", text.ToString());
	}

	[TestMethod]
	public void RawTextIsNotDecoded()
	{
		var script = (HtmlText)HtmlDocument.Parse("<script>x = \"&amp;\";</script>").Elements().First().Nodes[0];
		var title = (HtmlText)HtmlDocument.Parse("<title>a &amp; b</title>").Elements().First().Nodes[0];

		Assert.AreEqual("x = \"&amp;\";", script.Text);
		Assert.AreEqual("a & b", title.Text);
	}

	[TestMethod]
	public void CommentExposesItsContentAndDelimiters()
	{
		var comment = (HtmlComment)HtmlDocument.Parse("<!-- a > b -->").Nodes[0];

		Assert.AreEqual(" a > b ", comment.Text);
		Assert.AreEqual(" a > b ", comment.TextSpan.ToString());
		Assert.AreEqual("<!-- a > b -->", comment.OuterSpan.ToString());
		Assert.AreEqual("<!-- a > b -->", comment.ToString());
	}

	[TestMethod]
	public void EmptyDocumentHasNoNodes()
	{
		Assert.IsEmpty(HtmlDocument.Parse("").Nodes);
		Assert.IsEmpty(HtmlDocument.Parse("").Elements());
	}

	[TestMethod]
	public void TextOnlyDocumentIsASingleTextNode()
	{
		var document = HtmlDocument.Parse("just some text");

		Assert.HasCount(1, document.Nodes);
		Assert.AreEqual("just some text", ((HtmlText)document.Nodes[0]).Text);
		Assert.IsEmpty(document.Elements());
	}

	[TestMethod]
	public void WhitespaceOnlyTextIsKeptAsWritten()
	{
		var document = HtmlDocument.Parse("<a></a>\r\n  <b></b>");

		Assert.HasCount(3, document.Nodes);
		Assert.AreEqual("\r\n  ", ((HtmlText)document.Nodes[1]).Text);
		Assert.AreSequenceEqual(["a", "b"], document.Elements().Names());
	}

	[TestMethod]
	public void DoctypeProcessingInstructionCDataAndStrayEndTagsAreSkipped()
	{
		var document = HtmlDocument.Parse("<!DOCTYPE html><?xml version=\"1.0\"?><![CDATA[ x ]]></x><p>x</p>");

		Assert.HasCount(1, document.Nodes);
		Assert.AreEqual("<p>x</p>", document.Nodes[0].ToString());
	}

	[TestMethod]
	public void RawTextElementContentIsASingleTextNode()
	{
		var document = HtmlDocument.Parse("<script>if (a<b) { x = '<i>'; }</script><style>a>b{}</style><textarea><p></textarea><title></title>");

		var elements = document.Elements().ToList();

		Assert.HasCount(4, elements);
		Assert.AreEqual("if (a<b) { x = '<i>'; }", ((HtmlText)elements[0].Nodes.Single()).Text);
		Assert.AreEqual("a>b{}", ((HtmlText)elements[1].Nodes.Single()).Text);
		Assert.AreEqual("<p>", ((HtmlText)elements[2].Nodes.Single()).Text);
		Assert.IsEmpty(elements[3].Nodes);
	}

	[TestMethod]
	public void VoidAndSelfClosingElementsHaveNoNodes()
	{
		var document = HtmlDocument.Parse("<br>text<span/>");

		Assert.IsEmpty(document.Elements().First().Nodes);
		Assert.IsEmpty(document.Elements().Last().Nodes);
	}

	[TestMethod]
	public void ElementsAreTheElementNodesOnly()
	{
		var element = TestHelpers.FirstElement("<div>a<b></b><!--c--><i></i>d</div>");

		Assert.AreSequenceEqual(["b", "i"], element.Elements().Names());
		Assert.AreSequenceEqual(element.Nodes.OfType<HtmlElement>().ToList(), element.Elements().ToList());
	}

	[TestMethod]
	public void ElementsCanBeEnumeratedRepeatedly()
	{
		var document = HtmlDocument.Parse("<a></a>x<b></b>");
		var elements = document.Elements();

		Assert.AreSequenceEqual(["a", "b"], elements.Names());
		Assert.AreSequenceEqual(["a", "b"], elements.Names());
	}

	[TestMethod]
	public void DescendantsAndQueriesIgnoreTextAndComments()
	{
		var document = HtmlDocument.Parse("x<a>y<b>z</b><!--<c>--></a>w");

		Assert.AreSequenceEqual(["a", "b"], document.Descendants().Names());
		Assert.AreSequenceEqual(["a", "b"], document.QuerySelectorAll().Names());
		Assert.IsNull(document.QuerySelector(name: "c"));
	}

	[TestMethod]
	public void ExcessiveNestingDropsNodesBeyondTheLimitButKeepsTheText()
	{
		var depth = 20_000;
		var html = string.Concat(Enumerable.Repeat("<div>", depth)) + "x" + string.Concat(Enumerable.Repeat("</div>", depth));
		var document = HtmlDocument.Parse(html);

		var deepest = document.Descendants().Last();

		Assert.IsEmpty(deepest.Nodes);
		Assert.AreEqual("x", deepest.TextContent);
	}
}
