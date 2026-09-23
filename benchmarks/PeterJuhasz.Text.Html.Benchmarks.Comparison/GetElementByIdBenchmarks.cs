using AngleSharp.Dom;
using BenchmarkDotNet.Attributes;
using PeterJuhasz.Text.Html.Lazy;
using PeterJuhasz.Text.Html.Model;

namespace PeterJuhasz.Text.Html.Benchmarks.Comparison;

[MemoryDiagnoser]
public class GetElementByIdBenchmarks : ComparisonBenchmarks
{
	// The id is near the end of the sample, so most of the document is searched.
	private const string Id = "article-40";

	[Benchmark(Baseline = true)]
	public IElement? AngleSharp() => AngleSharpDocument.GetElementById(Id);

	[Benchmark]
	public HtmlElement? Model() => ModelDocument.GetElementById(Id);

	[Benchmark]
	public LazyHtmlElement? Lazy() => LazyDocument.GetElementById(Id);
}
