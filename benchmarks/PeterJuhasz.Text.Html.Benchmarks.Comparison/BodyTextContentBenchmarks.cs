using AngleSharp.Dom;
using BenchmarkDotNet.Attributes;
using PeterJuhasz.Text.Html.Lazy;
using PeterJuhasz.Text.Html.Model;

namespace PeterJuhasz.Text.Html.Benchmarks.Comparison;

// The model memoizes TextContent on the element, so to measure the first access a fresh tree is parsed before each iteration
// and each iteration is a single invocation.
[MemoryDiagnoser]
[InvocationCount(1, 1)]
public class BodyTextContentBenchmarks : ComparisonBenchmarks
{
	private IElement angleSharpBody = null!;
	private HtmlElement modelBody = null!;
	private LazyHtmlElement lazyBody;

	protected override void OnSetup()
	{
		angleSharpBody = AngleSharpDocument.Body ?? throw new InvalidOperationException("Sample has no <body>.");
		lazyBody = LazyDocument.TryGetBody(out var body) ? body : throw new InvalidOperationException("Sample has no <body>.");
	}

	[IterationSetup(Target = nameof(Model))]
	public void ParseModel()
	{
		modelBody = HtmlDocument.Parse(Html).TryGetBody(out var body) ? body : throw new InvalidOperationException("Sample has no <body>.");
	}

	[Benchmark(Baseline = true)]
	public string AngleSharp() => angleSharpBody.TextContent;

	[Benchmark]
	public string Model() => modelBody.TextContent;

	[Benchmark]
	public string Lazy() => lazyBody.TextContent;
}
