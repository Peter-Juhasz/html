using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using BenchmarkDotNet.Attributes;
using PeterJuhasz.Text.Html.Lazy;
using PeterJuhasz.Text.Html.Model;

namespace PeterJuhasz.Text.Html.Benchmarks.Comparison;

// Each scenario compares AngleSharp with the model and the lazy API of this library on the same sample document.
public abstract class ComparisonBenchmarks
{
	protected static readonly HtmlParser AngleSharpParser = new();

	protected string Html { get; private set; } = null!;
	protected IHtmlDocument AngleSharpDocument { get; private set; } = null!;
	protected HtmlDocument ModelDocument { get; private set; } = null!;
	protected LazyHtmlDocument LazyDocument { get; private set; }

	[GlobalSetup]
	public void Setup()
	{
		Html = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Samples", "sample.html"));
		AngleSharpDocument = AngleSharpParser.ParseDocument(Html);
		ModelDocument = HtmlDocument.Parse(Html);
		LazyDocument = LazyHtmlDocument.Parse(Html);
		OnSetup();
	}

	// Runs once the documents are parsed, so a scenario can look up the nodes it operates on.
	protected virtual void OnSetup()
	{
	}
}
