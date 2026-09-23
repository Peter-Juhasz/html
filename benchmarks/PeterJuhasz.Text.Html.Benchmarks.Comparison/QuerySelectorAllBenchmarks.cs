using BenchmarkDotNet.Attributes;

namespace PeterJuhasz.Text.Html.Benchmarks.Comparison;

// Finds the articles with a given tag list by element name, class and attribute value; 3 of the 40 articles match.
[MemoryDiagnoser]
public class QuerySelectorAllBenchmarks : ComparisonBenchmarks
{
	private const string Selector = "article.article[data-tags=\"html,dotnet\"]";
	private static readonly KeyValuePair<string, string>[] Attributes = [new("data-tags", "html,dotnet")];

	[Benchmark(Baseline = true)]
	public int AngleSharp()
	{
		var count = 0;
		var elements = AngleSharpDocument.QuerySelectorAll(Selector);
		for (var i = 0; i < elements.Length; i++)
			count += elements[i].LocalName.Length;
		return count;
	}

	[Benchmark]
	public int Model()
	{
		var count = 0;
		foreach (var element in ModelDocument.QuerySelectorAll(element: "article", classNames: "article", attributes: Attributes))
			count += element.Name.Length;
		return count;
	}

	[Benchmark]
	public int Lazy()
	{
		var count = 0;
		foreach (var element in LazyDocument.QuerySelectorAll(element: "article", classNames: "article", attributes: Attributes))
			count += element.NameSpan.Length;
		return count;
	}
}
