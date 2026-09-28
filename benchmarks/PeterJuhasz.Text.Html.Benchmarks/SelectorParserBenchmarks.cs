using BenchmarkDotNet.Attributes;
using PeterJuhasz.Text.Html.Selectors;

namespace PeterJuhasz.Text.Html.Benchmarks;

/// <summary>
/// Measures parsing CSS selectors into selector trees, from a single element name up to lists with combinators, pseudo-classes and escapes.
/// </summary>
[MemoryDiagnoser]
public class SelectorParserBenchmarks
{
	/// <summary>
	/// The selectors to parse: an element name, a compound selector that the structured query also supports, then ones that only the selector parser does.
	/// </summary>
	[Params(
		"div",
		"a.btn.primary[rel=next]",
		"ul > li:nth-child(odd) a[href^='https:']",
		"article:has(> img, + figure) p:not(.lead, [hidden])",
		"li:nth-last-child(-n + 3 of .item:not(:empty))",
		".md\\:flex.w-1\\/2 > #\\31 0")]
	public string Selector { get; set; } = null!;

	/// <summary>
	/// Parses the selector into a selector tree; returned as an object, as the selector types are internal.
	/// </summary>
	[Benchmark(Baseline = true)]
	public object Parse() => ElementSelector.Parse(Selector);

	/// <summary>
	/// Parses the selector for the structured query, which the model layer tries first for every selector: this is the fast path for the simplest selectors,
	/// and for the others it is the cost of finding that they need the selector parser.
	/// </summary>
	[Benchmark]
	public bool TryParseForTheStructuredQuery() => SelectorParser.TryParseSelector(Selector, out _, out _, out _);
}
