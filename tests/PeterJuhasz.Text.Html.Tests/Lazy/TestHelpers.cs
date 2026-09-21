using System.Text.Html.Lazy;

namespace PeterJuhasz.Text.Html.Tests.Lazy;

internal static class TestHelpers
{
	public static LazyHtmlElement FirstElement(string html)
	{
		var elements = LazyHtmlDocument.Parse(html).Elements();
		Assert.IsTrue(elements.MoveNext(), "Expected at least one element.");
		return elements.Current;
	}

	// The first element found by a query, which is not scanned as a whole up front, unlike an enumerated one.
	public static LazyHtmlElement FirstQueriedElement(string html)
	{
		Assert.IsTrue(LazyHtmlDocument.Parse(html).TryQuerySelector(out var element), "Expected at least one element.");
		return element;
	}

	public static List<LazyHtmlElement> ToList(this ElementsEnumerator elements)
	{
		var list = new List<LazyHtmlElement>();
		foreach (var element in elements)
			list.Add(element);
		return list;
	}

	public static List<LazyHtmlElement> ToList(this ElementsQueryEnumerator elements)
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

	public static List<string> Names(this ElementsQueryEnumerator elements)
		=> elements.ToList().ConvertAll(e => e.Name);

	public static List<string> Outers(this ElementsQueryEnumerator elements)
		=> elements.ToList().ConvertAll(e => e.OuterSpan.ToString());

	public static List<string> Names(this AttributesEnumerator attributes)
		=> attributes.ToList().ConvertAll(a => a.Name);
}
