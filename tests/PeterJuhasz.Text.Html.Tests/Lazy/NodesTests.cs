using Microsoft.Extensions.Primitives;

namespace PeterJuhasz.Text.Html.Tests.Lazy;

[TestClass]
public sealed class NodesTests
{
	[TestMethod]
	public void DocumentNodesAreTextElementsAndCommentsInOrder()
	{
		var document = LazyHtmlDocument.Parse("before<div>x</div><!-- c -->after");

		var nodes = document.Nodes().ToList();

		Assert.AreSequenceEqual([LazyHtmlNodeKind.Text, LazyHtmlNodeKind.Element, LazyHtmlNodeKind.Comment, LazyHtmlNodeKind.Text], nodes.ConvertAll(n => n.Kind));
		Assert.AreSequenceEqual(["before", "<div>x</div>", "<!-- c -->", "after"], nodes.ConvertAll(n => n.OuterSpan.ToString()));
		Assert.AreEqual("before", nodes[0].Text.Text);
		Assert.AreEqual("div", nodes[1].Element.Name);
		Assert.AreEqual(" c ", nodes[2].Comment.Text);
		Assert.AreEqual("after", nodes[3].Text.Text);
	}

	[TestMethod]
	public void ElementNodesAreItsDirectContent()
	{
		var element = TestHelpers.FirstElement("<p>a<b>b<i>c</i></b>c<!--d-->e</p>tail");

		var nodes = element.Nodes().ToList();

		Assert.AreSequenceEqual([LazyHtmlNodeKind.Text, LazyHtmlNodeKind.Element, LazyHtmlNodeKind.Text, LazyHtmlNodeKind.Comment, LazyHtmlNodeKind.Text], nodes.ConvertAll(n => n.Kind));
		Assert.AreSequenceEqual(["a", "<b>b<i>c</i></b>", "c", "<!--d-->", "e"], nodes.ConvertAll(n => n.OuterSpan.ToString()));
	}

	[TestMethod]
	public void EmptyDocumentHasNoNodes()
	{
		Assert.IsEmpty(LazyHtmlDocument.Parse("").Nodes().ToList());
		Assert.IsEmpty(default(LazyHtmlDocument).Nodes().ToList());
	}

	[TestMethod]
	public void TextOnlyDocumentIsASingleTextNode()
	{
		var nodes = LazyHtmlDocument.Parse("just some text").Nodes().ToList();

		Assert.HasCount(1, nodes);
		Assert.AreEqual(LazyHtmlNodeKind.Text, nodes[0].Kind);
		Assert.AreEqual("just some text", nodes[0].Text.Text);
	}

	[TestMethod]
	public void EmptyTextBetweenElementsIsNotReported()
	{
		var document = LazyHtmlDocument.Parse("<a></a><b></b><!----><c></c>");

		Assert.AreSequenceEqual([LazyHtmlNodeKind.Element, LazyHtmlNodeKind.Element, LazyHtmlNodeKind.Comment, LazyHtmlNodeKind.Element], document.Nodes().Kinds());
	}

	[TestMethod]
	public void WhitespaceOnlyTextIsReportedAsWritten()
	{
		var document = LazyHtmlDocument.Parse("<a></a>\r\n  <b></b>");

		var nodes = document.Nodes().ToList();

		Assert.AreSequenceEqual([LazyHtmlNodeKind.Element, LazyHtmlNodeKind.Text, LazyHtmlNodeKind.Element], nodes.ConvertAll(n => n.Kind));
		Assert.AreEqual("\r\n  ", nodes[1].Text.Text);
	}

	[TestMethod]
	public void CharacterReferencesAreDecodedButTextSpanIsAsWritten()
	{
		var element = TestHelpers.FirstElement("<p>a &amp; b</p>");

		var text = element.Nodes().ToList()[0].Text;

		Assert.AreEqual("a & b", text.Text);
		Assert.AreEqual("a &amp; b", text.TextSpan.ToString());
	}

	[TestMethod]
	[DataRow("script")]
	[DataRow("style")]
	public void RawTextIsNotDecoded(string name)
	{
		var element = TestHelpers.FirstElement($"<{name}>x = \"&amp;\";</{name}>");

		var nodes = element.Nodes().ToList();

		Assert.HasCount(1, nodes);
		Assert.AreEqual("x = \"&amp;\";", nodes[0].Text.Text);
	}

	[TestMethod]
	[DataRow("textarea")]
	[DataRow("title")]
	public void EscapableRawTextIsDecoded(string name)
	{
		var element = TestHelpers.FirstElement($"<{name}>&lt;b&gt; &amp; c</{name}>");

		var nodes = element.Nodes().ToList();

		Assert.HasCount(1, nodes);
		Assert.AreEqual("<b> & c", nodes[0].Text.Text);
		Assert.AreEqual("&lt;b&gt; &amp; c", nodes[0].Text.TextSpan.ToString());
	}

	[TestMethod]
	public void StrayOpenAngleIsPartOfText()
	{
		var element = TestHelpers.FirstElement("<p>a < b <= c</p>");

		var nodes = element.Nodes().ToList();

		Assert.HasCount(1, nodes);
		Assert.AreEqual("a < b <= c", nodes[0].Text.Text);
	}

	[TestMethod]
	public void DoctypeProcessingInstructionCDataAndStrayEndTagsAreSkipped()
	{
		var document = LazyHtmlDocument.Parse("<!DOCTYPE html><?xml version=\"1.0\"?><![CDATA[ x ]]></x><p>x</p>");

		Assert.AreSequenceEqual(["<p>x</p>"], document.Nodes().Outers());
	}

	[TestMethod]
	public void SkippedMarkupEndsAtTheFirstCloseAngleLikeABogusComment()
	{
		// the same as browsers do for CDATA in HTML content: the rest is text
		var document = LazyHtmlDocument.Parse("<![CDATA[ <a> ]]><p></p>");

		Assert.AreSequenceEqual([" ]]>", "<p></p>"], document.Nodes().Outers());
	}

	[TestMethod]
	public void TextAroundSkippedMarkupIsReportedSeparately()
	{
		var element = TestHelpers.FirstElement("<div>a<![CDATA[x]]>b</x>c</div>");

		// the stray end tag also implicitly closes the div, so "c" is outside it
		Assert.AreSequenceEqual(["a", "b"], element.Nodes().Outers());
		Assert.AreSequenceEqual(["<div>a<![CDATA[x]]>b", "c"], LazyHtmlDocument.Parse("<div>a<![CDATA[x]]>b</x>c</div>").Nodes().Outers());
	}

	[TestMethod]
	public void ConditionalCommentIsASingleComment()
	{
		var document = LazyHtmlDocument.Parse("<!--[if IE]><p>ie</p><![endif]--><div></div>");

		var nodes = document.Nodes().ToList();

		Assert.AreSequenceEqual([LazyHtmlNodeKind.Comment, LazyHtmlNodeKind.Element], nodes.ConvertAll(n => n.Kind));
		Assert.AreEqual("[if IE]><p>ie</p><![endif]", nodes[0].Comment.Text);
	}

	[TestMethod]
	public void CommentSpansExcludeAndIncludeTheDelimiters()
	{
		var comment = LazyHtmlDocument.Parse("<!-- a -- b > c -->").Nodes().ToList()[0].Comment;

		Assert.AreEqual("<!-- a -- b > c -->", comment.OuterSpan.ToString());
		Assert.AreEqual(" a -- b > c ", comment.TextSpan.ToString());
		Assert.AreEqual(" a -- b > c ", comment.Text);
	}

	[TestMethod]
	public void EmptyCommentHasEmptyText()
	{
		var comment = LazyHtmlDocument.Parse("<!---->").Nodes().ToList()[0].Comment;

		Assert.AreEqual("<!---->", comment.OuterSpan.ToString());
		Assert.IsTrue(comment.TextSpan.IsEmpty);
	}

	[TestMethod]
	public void UnterminatedCommentRunsToTheEnd()
	{
		var nodes = LazyHtmlDocument.Parse("<p>a</p><!-- b <i>c</i>").Nodes().ToList();

		Assert.AreSequenceEqual([LazyHtmlNodeKind.Element, LazyHtmlNodeKind.Comment], nodes.ConvertAll(n => n.Kind));
		Assert.AreEqual("<!-- b <i>c</i>", nodes[1].OuterSpan.ToString());
		Assert.AreEqual(" b <i>c</i>", nodes[1].Comment.Text);
	}

	[TestMethod]
	public void RawTextElementContentIsASingleTextNode()
	{
		var document = LazyHtmlDocument.Parse("<script>if (a<b) { x = '<i>'; } <!-- not a comment --></script><style>a>b{}</style>");

		foreach (var element in document.Elements())
		{
			var nodes = element.Nodes().ToList();
			Assert.HasCount(1, nodes, element.Name);
			Assert.AreEqual(LazyHtmlNodeKind.Text, nodes[0].Kind);
			Assert.AreEqual(element.InnerSpan.ToString(), nodes[0].Text.Text);
		}
	}

	[TestMethod]
	public void EmptyRawTextElementHasNoNodes()
	{
		Assert.IsEmpty(TestHelpers.FirstElement("<script></script>").Nodes().ToList());
		Assert.IsEmpty(TestHelpers.FirstElement("<title></title>after").Nodes().ToList());
	}

	[TestMethod]
	public void VoidAndSelfClosingElementsHaveNoNodes()
	{
		Assert.IsEmpty(TestHelpers.FirstElement("<br>text").Nodes().ToList());
		Assert.IsEmpty(TestHelpers.FirstElement("<span/>text").Nodes().ToList());
	}

	[TestMethod]
	public void NodesDoNotLeakOutsideTheElement()
	{
		var element = TestHelpers.FirstElement("<div>a<b></b></div>outside<!-- c -->");

		Assert.AreSequenceEqual(["a", "<b></b>"], element.Nodes().Outers());
	}

	[TestMethod]
	public void UnclosedElementNodesExtendToTheEnd()
	{
		var element = TestHelpers.FirstElement("<div>a<b>b");

		Assert.AreSequenceEqual(["a", "<b>b"], element.Nodes().Outers());
		Assert.AreSequenceEqual(["b"], element.Nodes().ToList()[1].Element.Nodes().Outers());
	}

	[TestMethod]
	public void ImplicitlyClosedElementsAreSiblingNodes()
	{
		var element = TestHelpers.FirstElement("<ul><li>one<li>two</ul>");

		Assert.AreSequenceEqual(["<li>one", "<li>two"], element.Nodes().Outers());
		Assert.AreSequenceEqual(["one"], element.Nodes().ToList()[0].Element.Nodes().Outers());
	}

	[TestMethod]
	public void ElementNodeIsTheSameAsTheEnumeratedElement()
	{
		var document = LazyHtmlDocument.Parse("x<div class=\"a\" id='1'>text<b>bold</b></div>y<br>");

		var elements = document.Elements().ToList();
		var nodes = document.Nodes().ToList().FindAll(n => n.Kind == LazyHtmlNodeKind.Element).ConvertAll(n => n.Element);

		Assert.HasCount(elements.Count, nodes);
		for (var i = 0; i < elements.Count; i++)
		{
			Assert.AreEqual(elements[i].Name, nodes[i].Name);
			Assert.AreEqual(elements[i].OuterSpan.ToString(), nodes[i].OuterSpan.ToString());
			Assert.AreEqual(elements[i].InnerSpan.ToString(), nodes[i].InnerSpan.ToString());
			Assert.AreEqual(elements[i].TextContent, nodes[i].TextContent);
			Assert.AreSequenceEqual(elements[i].Attributes().Names(), nodes[i].Attributes().Names());
			Assert.AreSequenceEqual(elements[i].Elements().Names(), nodes[i].Elements().Names());
		}
	}

	[TestMethod]
	public void TypedViewsMatchTheKind()
	{
		var nodes = LazyHtmlDocument.Parse("a<b></b><!--c-->").Nodes().ToList();

		Assert.IsTrue(nodes[0].TryGetText(out var text));
		Assert.IsFalse(nodes[0].TryGetElement(out _));
		Assert.IsFalse(nodes[0].TryGetComment(out _));
		Assert.AreEqual("a", text.Text);

		Assert.IsTrue(nodes[1].TryGetElement(out var element));
		Assert.IsFalse(nodes[1].TryGetText(out _));
		Assert.IsFalse(nodes[1].TryGetComment(out _));
		Assert.AreEqual("b", element.Name);

		Assert.IsTrue(nodes[2].TryGetComment(out var comment));
		Assert.IsFalse(nodes[2].TryGetElement(out _));
		Assert.IsFalse(nodes[2].TryGetText(out _));
		Assert.AreEqual("c", comment.Text);
	}

	[TestMethod]
	public void MismatchedTypedViewThrows()
	{
		var nodes = LazyHtmlDocument.Parse("a<b></b><!--c-->").Nodes().ToList();

		Assert.ThrowsExactly<InvalidOperationException>(() => nodes[0].Element);
		Assert.ThrowsExactly<InvalidOperationException>(() => nodes[0].Comment);
		Assert.ThrowsExactly<InvalidOperationException>(() => nodes[1].Text);
		Assert.ThrowsExactly<InvalidOperationException>(() => nodes[1].Comment);
		Assert.ThrowsExactly<InvalidOperationException>(() => nodes[2].Element);
		Assert.ThrowsExactly<InvalidOperationException>(() => nodes[2].Text);
	}

	[TestMethod]
	public void NodesStayWithinTheStringSegment()
	{
		var html = "<a>outside</a>before<div>inside</div>after<b>outside</b>";
		var start = html.IndexOf("before", StringComparison.Ordinal);
		var segment = new StringSegment(html, start, "before<div>inside</div>after".Length);

		Assert.AreSequenceEqual(["before", "<div>inside</div>", "after"], LazyHtmlDocument.Parse(segment).Nodes().Outers());
	}

	[TestMethod]
	public void EnumeratorCanBeRestartedFromTheSource()
	{
		var element = TestHelpers.FirstElement("<p>a<b></b></p>");

		Assert.AreSequenceEqual(["a", "<b></b>"], element.Nodes().Outers());
		Assert.AreSequenceEqual(["a", "<b></b>"], element.Nodes().Outers());
	}
}
