using System.Text.Html.Lazy;

namespace PeterJuhasz.Text.Html.Tests;

internal static class TestHelpers
{
	public static LazyHtmlElement FirstElement(string html)
	{
		var elements = new LazyHtmlDocument(html).Elements();
		Assert.IsTrue(elements.MoveNext(), "Expected at least one element.");
		return elements.Current;
	}

	public static List<LazyHtmlElement> ToList(this ElementsEnumerator elements)
	{
		var list = new List<LazyHtmlElement>();
		foreach (var element in elements)
			list.Add(element);
		return list;
	}

	public static List<LazyHtmlAttribute> ToList(this AttributesEnumerator attributes)
	{
		var list = new List<LazyHtmlAttribute>();
		foreach (var attribute in attributes)
			list.Add(attribute);
		return list;
	}

	public static List<string> Names(this ElementsEnumerator elements)
		=> elements.ToList().ConvertAll(e => e.Name);

	public static List<string> Names(this AttributesEnumerator attributes)
		=> attributes.ToList().ConvertAll(a => a.Name);
}
