using System.Text.Html.Lazy;

namespace PeterJuhasz.Text.Html.Tests.Lazy;

[TestClass]
public sealed class ChildElementTests
{
	[TestMethod]
	public void EnumeratesDirectChildrenOnly()
	{
		var element = TestHelpers.FirstElement("<ul><li><a>1</a></li><li><a>2</a></li></ul>");

		Assert.AreSequenceEqual(["li", "li"], element.Elements().Names());
	}

	[TestMethod]
	public void SkipsTextBetweenChildren()
	{
		var element = TestHelpers.FirstElement("<div>a <b>b</b> c <i>d</i> e</div>");

		Assert.AreSequenceEqual(["b", "i"], element.Elements().Names());
	}

	[TestMethod]
	public void ElementWithoutChildrenHasEmptyEnumeration()
	{
		var element = TestHelpers.FirstElement("<div>text only</div>");

		Assert.IsFalse(element.Elements().MoveNext());
	}

	[TestMethod]
	public void SelfClosingElementHasNoChildren()
	{
		var element = TestHelpers.FirstElement("<div/><span></span>");

		Assert.IsFalse(element.Elements().MoveNext());
	}

	[TestMethod]
	public void ChildrenDoNotLeakOutsideParent()
	{
		var element = TestHelpers.FirstElement("<div><a></a></div><b></b>");

		Assert.AreSequenceEqual(["a"], element.Elements().Names());
	}

	[TestMethod]
	public void GrandchildrenAreReachableThroughChildren()
	{
		var element = TestHelpers.FirstElement("<html><body><div><p>x</p></div></body></html>");

		var body = element.Elements().ToList()[0];
		var div = body.Elements().ToList()[0];
		var p = div.Elements().ToList()[0];

		Assert.AreEqual("body", body.Name);
		Assert.AreEqual("div", div.Name);
		Assert.AreEqual("p", p.Name);
		Assert.AreEqual("x", p.InnerSpan.ToString());
	}

	[TestMethod]
	public void ChildSpansAreCorrect()
	{
		var element = TestHelpers.FirstElement("<div><a href=\"1\">one</a><a href=\"2\">two</a></div>");

		var children = element.Elements().ToList();

		Assert.HasCount(2, children);
		Assert.AreEqual("<a href=\"1\">one</a>", children[0].OuterSpan.ToString());
		Assert.AreEqual("<a href=\"2\">two</a>", children[1].OuterSpan.ToString());
	}

	[TestMethod]
	public void VoidChildrenAreEnumeratedAsSiblings()
	{
		var element = TestHelpers.FirstElement("<p>a<br>b<br>c<img src=\"x\"><span>d</span></p>");

		Assert.AreSequenceEqual(["br", "br", "img", "span"], element.Elements().Names());
	}

	[TestMethod]
	public void CommentsBetweenChildrenAreSkipped()
	{
		var element = TestHelpers.FirstElement("<div><!-- <a></a> --><b></b></div>");

		Assert.AreSequenceEqual(["b"], element.Elements().Names());
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
			var children = current.Elements().ToList();
			Assert.HasCount(1, children);
			current = children[0];
		}

		Assert.AreEqual("x", current.InnerSpan.ToString());
		Assert.AreEqual(html, element.OuterSpan.ToString());
	}
}
