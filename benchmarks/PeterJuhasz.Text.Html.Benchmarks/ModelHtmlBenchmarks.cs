using BenchmarkDotNet.Attributes;
using PeterJuhasz.Text.Html.Model;

namespace PeterJuhasz.Text.Html.Benchmarks;

[MemoryDiagnoser]
public class ModelHtmlBenchmarks
{
	private string html = null!;
	private HtmlDocument document = null!;
	private readonly HtmlElement[] bodies = new HtmlElement[TextContentInvocations];
	private int nextBody;
	private readonly CountingVisitor visitor = new();

	// The element memoizes TextContent, so the first access is measured on fresh trees parsed before each iteration.
	// A single invocation per iteration would never let the JIT tier the code up, so a batch of them is measured.
	private const int TextContentInvocations = 16;

	[GlobalSetup]
	public void Setup()
	{
		html = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Samples", "sample.html"));
		document = HtmlDocument.Parse(html);
	}

	[Benchmark]
	public HtmlDocument Parse() => HtmlDocument.Parse(html);

	[Benchmark]
	public int Visit()
	{
		visitor.Count = 0;
		visitor.VisitDocument(document);
		return visitor.Count;
	}

	[IterationSetup(Target = nameof(TextContent))]
	public void ParseBodies()
	{
		for (var i = 0; i < bodies.Length; i++)
		{
			bodies[i] = HtmlDocument.Parse(html).TryGetBody(out var body) ? body : throw new InvalidOperationException("Sample has no <body>.");
		}

		nextBody = 0;
	}

	[Benchmark]
	[InvocationCount(TextContentInvocations, 1)]
	public string TextContent() => bodies[nextBody++].TextContent;

	[Benchmark]
	public int QuerySelectorAll()
	{
		var count = 0;
		foreach (var element in document.QuerySelectorAll(element: "a"))
		{
			count += element.Name.Length;
		}

		return count;
	}

	[Benchmark]
	public int QuerySelectorAllByElementAndAttribute()
	{
		KeyValuePair<string, string>[] attributes = [new("class", "btn")];
		var count = 0;
		foreach (var element in document.QuerySelectorAll(element: "a", attributes: attributes))
		{
			count += element.Name.Length;
		}

		return count;
	}

	[Benchmark]
	public int QuerySelectorAllByAttribute()
	{
		KeyValuePair<string, string>[] attributes = [new("rel", "author")];
		var count = 0;
		foreach (var element in document.QuerySelectorAll(attributes: attributes))
		{
			count += element.Name.Length;
		}

		return count;
	}

	[Benchmark]
	public int TryQuerySelectorByElementAndAttribute()
	{
		KeyValuePair<string, string>[] attributes = [new("class", "btn"), new("rel", "author")];
		return document.TryQuerySelector(out var element, element: "a", attributes: attributes) ? element.OuterSpan.Length : 0;
	}

	[Benchmark]
	public int QuerySelectorAllByClassName()
	{
		var count = 0;
		foreach (var element in document.QuerySelectorAll(classNames: "btn"))
		{
			count += element.Name.Length;
		}

		return count;
	}

	// The id is near the end of the sample, so most of the tree is walked.
	[Benchmark]
	public int TryQuerySelectorByIdAttribute()
	{
		KeyValuePair<string, string>[] attributes = [new("id", "article-40")];
		return document.TryQuerySelector(out var element, attributes: attributes) ? element.OuterSpan.Length : 0;
	}

	// Reads every element and attribute so the whole traversal is measured.
	private sealed class CountingVisitor : HtmlVisitor
	{
		public int Count;

		public override void VisitElement(HtmlElement element)
		{
			Count += element.Name.Length;
			base.VisitElement(element);
		}

		public override void VisitAttribute(HtmlAttribute attribute)
		{
			Count += attribute.Name.Length + (attribute.Value?.Length ?? 0);
		}
	}
}
