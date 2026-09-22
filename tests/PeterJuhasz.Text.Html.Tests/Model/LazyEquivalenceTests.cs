namespace PeterJuhasz.Text.Html.Tests.Model;

// The model is built on the lazy layer, so the two must always agree.
[TestClass]
public sealed class LazyEquivalenceTests
{
	private const string Html =
		"<!DOCTYPE html><html lang=\"en\"><head><title>T &amp; U</title><meta charset=\"utf-8\"><link rel=\"icon\" href=\"/i\"></head>" +
		"<body class=\"page\"><div class=\"a\" id='x'><p class=\"a\">text &amp; <br class=\"a\">more &lt;<b>bold</b></p>" +
		"<a href=\"/1?a=1&amp;b=2\" rel=\"author\" class=\"btn\">1</a><a href=\"/2\" class=\"btn\">2</a><a href=\"/3\" rel=\"author\">3</a>" +
		"<!-- <a class=\"btn\"> --><ul><li class=a>1<li>2</ul><script>if (a<b &amp;&amp; c) {}</script><table><tr><td>1<td>2<tr><td>3</table>" +
		"<select><option value=1>a<optgroup label=g><option>b</select><div><p><a class=\"btn\" rel=\"author\" disabled>nested</a></p></div></div></body></html>";

	[TestMethod]
	public void TreesAreTheSame()
	{
		var lazy = LazyHtmlDocument.Parse(Html);
		var document = HtmlDocument.Parse(Html);

		var lazyElements = new List<LazyHtmlElement>();
		foreach (var element in lazy.QuerySelectorAll())
			lazyElements.Add(element);
		var elements = document.Descendants().ToList();

		Assert.HasCount(lazyElements.Count, elements);
		Assert.IsGreaterThan(20, elements.Count);
		for (var i = 0; i < elements.Count; i++)
		{
			Assert.AreEqual(lazyElements[i].Name, elements[i].Name);
			Assert.AreEqual(lazyElements[i].OuterSpan.ToString(), elements[i].OuterSpan.ToString());
			Assert.AreEqual(lazyElements[i].InnerSpan.ToString(), elements[i].InnerSpan.ToString());
			Assert.AreEqual(lazyElements[i].TextContent, elements[i].TextContent);

			var lazyAttributes = new List<(string, string?)>();
			foreach (var attribute in lazyElements[i].Attributes())
				lazyAttributes.Add((attribute.Name, attribute.Value));
			Assert.AreSequenceEqual(lazyAttributes, elements[i].Attributes.Select(a => (a.Name, a.Value)).ToList());
		}
	}

	[TestMethod]
	public void NodesAreTheSame()
	{
		var lazy = LazyHtmlDocument.Parse(Html);
		var document = HtmlDocument.Parse(Html);

		var lazyNodes = new List<string>();
		CollectLazy(lazy.Nodes(), lazyNodes);
		var nodes = new List<string>();
		CollectModel(document.Nodes, nodes);

		Assert.IsGreaterThan(30, nodes.Count);
		Assert.IsTrue(nodes.Any(n => n.StartsWith("text:", StringComparison.Ordinal)));
		Assert.IsTrue(nodes.Any(n => n.StartsWith("comment:", StringComparison.Ordinal)));
		Assert.AreSequenceEqual(lazyNodes, nodes);

		static void CollectLazy(NodesEnumerator source, List<string> nodes)
		{
			foreach (var node in source)
			{
				switch (node.Kind)
				{
					case LazyHtmlNodeKind.Element:
						nodes.Add($"element:{node.Element.Name}:{node.OuterSpan}");
						CollectLazy(node.Element.Nodes(), nodes);
						break;

					case LazyHtmlNodeKind.Text:
						nodes.Add($"text:{node.Text.Text}:{node.OuterSpan}");
						break;

					case LazyHtmlNodeKind.Comment:
						nodes.Add($"comment:{node.Comment.Text}:{node.OuterSpan}");
						break;
				}
			}
		}

		static void CollectModel(IEnumerable<HtmlNode> source, List<string> nodes)
		{
			foreach (var node in source)
			{
				switch (node)
				{
					case HtmlElement element:
						nodes.Add($"element:{element.Name}:{element.OuterSpan}");
						CollectModel(element.Nodes, nodes);
						break;

					case HtmlText text:
						nodes.Add($"text:{text.Text}:{text.OuterSpan}");
						break;

					case HtmlComment comment:
						nodes.Add($"comment:{comment.Text}:{comment.OuterSpan}");
						break;
				}
			}
		}
	}

	[TestMethod]
	public void QueriesGiveTheSameResults()
	{
		var lazy = LazyHtmlDocument.Parse(Html);
		var document = HtmlDocument.Parse(Html);

		var queries = new (string? Element, string? ClassName, (string, string)[] Attributes)[]
		{
			(null, null, []),
			("a", null, []),
			("A", null, []),
			("li", null, []),
			("td", null, []),
			("option", null, []),
			(null, null, [("id", "x")]),
			("div", null, [("id", "x")]),
			("p", null, [("id", "x")]),
			("div", "a", [("id", "x")]),
			(null, null, [("ID", "x")]),
			(null, null, [("id", "X")]),
			(null, null, [("id", "")]),
			(null, "a", []),
			(null, "btn", []),
			("a", "btn", []),
			(null, null, [("class", "a")]),
			("a", null, [("class", "btn")]),
			("a", null, [("rel", "author")]),
			("a", null, [("class", "btn"), ("rel", "author")]),
			("a", "btn", [("rel", "author"), ("disabled", "")]),
			(null, null, [("href", "/2")]),
			(null, null, [("href", "/1?a=1&amp;b=2")]),
			(null, null, [("href", "/1?a=1&b=2")]),
			(null, null, [("class", "zzz")]),
			("zzz", null, [("class", "a")]),
		};

		foreach (var (element, className, attributes) in queries)
		{
			var required = Array.ConvertAll(attributes, a => KeyValuePair.Create(a.Item1, a.Item2));
			var expected = new List<string>();
			foreach (var match in lazy.QuerySelectorAll(element: element, className: className, attributes: required))
				expected.Add(match.OuterSpan.ToString());

			var actual = document.QuerySelectorAll(element: element, className: className, attributes: required).Outers();

			Assert.AreSequenceEqual(expected, actual, $"Mismatch for element={element}, class={className}, attributes={string.Join(",", attributes)}.");
			Assert.AreEqual(lazy.TryQuerySelector(out _, element: element, className: className, attributes: required), document.TryQuerySelector(out _, element: element, className: className, attributes: required));
		}
	}

	[TestMethod]
	public void ElementQueriesGiveTheSameResults()
	{
		var lazy = LazyHtmlDocument.Parse(Html);
		var document = HtmlDocument.Parse(Html);

		Assert.IsTrue(lazy.TryQuerySelector(out var lazyBody, element: "body"));
		Assert.IsTrue(document.TryQuerySelector(out var body, element: "body"));

		foreach (var name in new[] { "a", "p", "td", "li", "script", "html", "zzz" })
		{
			var expected = new List<string>();
			foreach (var element in lazyBody.QuerySelectorAll(element: name))
				expected.Add(element.OuterSpan.ToString());

			Assert.AreSequenceEqual(expected, body.QuerySelectorAll(element: name).Outers(), $"Mismatch for <{name}>.");
		}
	}
}
