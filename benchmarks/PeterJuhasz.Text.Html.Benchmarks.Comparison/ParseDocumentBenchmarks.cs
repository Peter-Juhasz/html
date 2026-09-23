using AngleSharp.Html.Dom;
using BenchmarkDotNet.Attributes;
using PeterJuhasz.Text.Html.Lazy;
using PeterJuhasz.Text.Html.Model;

namespace PeterJuhasz.Text.Html.Benchmarks.Comparison;

[MemoryDiagnoser]
public class ParseDocumentBenchmarks : ComparisonBenchmarks
{
	[Benchmark(Baseline = true)]
	public IHtmlDocument AngleSharp() => AngleSharpParser.ParseDocument(Html);

	[Benchmark]
	public HtmlDocument Model() => HtmlDocument.Parse(Html);

	// Only wraps the text; the document is scanned when it is read.
	[Benchmark]
	public LazyHtmlDocument Lazy() => LazyHtmlDocument.Parse(Html);
}
