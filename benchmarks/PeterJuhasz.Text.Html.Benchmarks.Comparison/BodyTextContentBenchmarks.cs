using AngleSharp.Dom;
using BenchmarkDotNet.Attributes;
using PeterJuhasz.Text.Html.Lazy;
using PeterJuhasz.Text.Html.Model;

namespace PeterJuhasz.Text.Html.Benchmarks.Comparison;

// The model memoizes TextContent on the element, so to measure the first access fresh trees are parsed before each iteration.
// A single invocation per iteration would never let the JIT tier the code up, so each iteration is a batch of invocations,
// and the warmup is long enough for the tiering to finish before measuring.
[MemoryDiagnoser]
[InvocationCount(Invocations, 1)]
[MinWarmupCount(20)]
public class BodyTextContentBenchmarks : ComparisonBenchmarks
{
	private const int Invocations = 16;

	private IElement angleSharpBody = null!;
	private readonly HtmlElement[] modelBodies = new HtmlElement[Invocations];
	private int nextModelBody;
	private LazyHtmlElement lazyBody;

	protected override void OnSetup()
	{
		angleSharpBody = AngleSharpDocument.Body ?? throw new InvalidOperationException("Sample has no <body>.");
		lazyBody = LazyDocument.TryGetBody(out var body) ? body : throw new InvalidOperationException("Sample has no <body>.");
	}

	[IterationSetup(Target = nameof(Model))]
	public void ParseModel()
	{
		for (var i = 0; i < modelBodies.Length; i++)
			modelBodies[i] = HtmlDocument.Parse(Html).TryGetBody(out var body) ? body : throw new InvalidOperationException("Sample has no <body>.");
		nextModelBody = 0;
	}

	[Benchmark(Baseline = true)]
	public string AngleSharp() => angleSharpBody.TextContent;

	[Benchmark]
	public string Model() => modelBodies[nextModelBody++].TextContent;

	[Benchmark]
	public string Lazy() => lazyBody.TextContent;
}
