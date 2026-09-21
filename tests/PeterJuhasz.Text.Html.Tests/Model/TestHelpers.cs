using System.Text.Html.Model;

namespace PeterJuhasz.Text.Html.Tests.Model;

internal static class TestHelpers
{
	public static HtmlElement FirstElement(string html)
	{
		var document = HtmlDocument.Parse(html);
		Assert.IsNotEmpty(document.Elements(), "Expected at least one element.");
		return document.Elements().First();
	}

	public static List<string> Names(this IEnumerable<HtmlElement> elements)
		=> elements.Select(e => e.Name).ToList();

	public static List<string> Outers(this IEnumerable<HtmlElement> elements)
		=> elements.Select(e => e.OuterSpan.ToString()).ToList();

	public static List<string> Inners(this IEnumerable<HtmlElement> elements)
		=> elements.Select(e => e.InnerSpan.ToString()).ToList();

	public static List<string> Names(this IEnumerable<HtmlAttribute> attributes)
		=> attributes.Select(a => a.Name).ToList();

	public static KeyValuePair<string, string>[] Attributes(params (string Name, string Value)[] attributes)
		=> Array.ConvertAll(attributes, a => KeyValuePair.Create(a.Name, a.Value));
}
