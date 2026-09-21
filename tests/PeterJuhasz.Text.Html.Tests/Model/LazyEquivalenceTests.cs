using System.Text.Html.Lazy;
using System.Text.Html.Model;

namespace PeterJuhasz.Text.Html.Tests.Model;

// The model is built on the lazy layer, so the two must always agree.
[TestClass]
public sealed class LazyEquivalenceTests
{
	private const string Html =
		"<!DOCTYPE html><html lang=\"en\"><head><title>T</title><meta charset=\"utf-8\"><link rel=\"icon\" href=\"/i\"></head>" +
		"<body class=\"page\"><div class=\"a\" id='x'><p class=\"a\">text<br class=\"a\">more <b>bold</b></p>" +
		"<a href=\"/1\" rel=\"author\" class=\"btn\">1</a><a href=\"/2\" class=\"btn\">2</a><a href=\"/3\" rel=\"author\">3</a>" +
		"<!-- <a class=\"btn\"> --><ul><li class=a>1<li>2</ul><script>if (a<b) {}</script><table><tr><td>1<td>2<tr><td>3</table>" +
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
			CollectionAssert.AreEqual(lazyAttributes, elements[i].Attributes.Select(a => (a.Name, a.Value)).ToList());
		}
	}

	[TestMethod]
	public void QueriesGiveTheSameResults()
	{
		var lazy = LazyHtmlDocument.Parse(Html);
		var document = HtmlDocument.Parse(Html);

		var queries = new (string? Name, string? Id, string? ClassName, (string, string)[] Attributes)[]
		{
			(null, null, null, []),
			("a", null, null, []),
			("A", null, null, []),
			("li", null, null, []),
			("td", null, null, []),
			("option", null, null, []),
			(null, "x", null, []),
			("div", "x", null, []),
			("p", "x", null, []),
			(null, null, "a", []),
			(null, null, "btn", []),
			("a", null, "btn", []),
			(null, null, null, [("class", "a")]),
			("a", null, null, [("class", "btn")]),
			("a", null, null, [("rel", "author")]),
			("a", null, null, [("class", "btn"), ("rel", "author")]),
			("a", null, "btn", [("rel", "author"), ("disabled", "")]),
			(null, null, null, [("href", "/2")]),
			(null, null, null, [("class", "zzz")]),
			("zzz", null, null, [("class", "a")]),
		};

		foreach (var (name, id, className, attributes) in queries)
		{
			var required = Array.ConvertAll(attributes, a => KeyValuePair.Create(a.Item1, a.Item2));
			var expected = new List<string>();
			foreach (var element in lazy.QuerySelectorAll(name: name, id: id, className: className, attributes: required))
				expected.Add(element.OuterSpan.ToString());

			var actual = document.QuerySelectorAll(name: name, id: id, className: className, attributes: required).Outers();

			CollectionAssert.AreEqual(expected, actual, $"Mismatch for name={name}, id={id}, class={className}, attributes={string.Join(",", attributes)}.");
			Assert.AreEqual(lazy.TryQuerySelector(out _, name: name, id: id, className: className, attributes: required), document.TryQuerySelector(out _, name: name, id: id, className: className, attributes: required));
		}
	}

	[TestMethod]
	public void ElementQueriesGiveTheSameResults()
	{
		var lazy = LazyHtmlDocument.Parse(Html);
		var document = HtmlDocument.Parse(Html);

		Assert.IsTrue(lazy.TryQuerySelector(out var lazyBody, name: "body"));
		Assert.IsTrue(document.TryQuerySelector(out var body, name: "body"));

		foreach (var name in new[] { "a", "p", "td", "li", "script", "html", "zzz" })
		{
			var expected = new List<string>();
			foreach (var element in lazyBody.QuerySelectorAll(name: name))
				expected.Add(element.OuterSpan.ToString());

			CollectionAssert.AreEqual(expected, body.QuerySelectorAll(name: name).Outers(), $"Mismatch for <{name}>.");
		}
	}
}
