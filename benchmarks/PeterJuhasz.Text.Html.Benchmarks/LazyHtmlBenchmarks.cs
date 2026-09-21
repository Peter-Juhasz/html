using BenchmarkDotNet.Attributes;
using System.Text.Html.Lazy;

namespace PeterJuhasz.Text.Html.Benchmarks;

[MemoryDiagnoser]
public class LazyHtmlBenchmarks
{
	private string html = null!;
	private LazyHtmlDocument document;
	private LazyHtmlElement body;
	private readonly CountingVisitor visitor = new();
	private readonly FilteringVisitor filteringVisitor = new("a");

	[GlobalSetup]
	public void Setup()
	{
		html = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Samples", "sample.html"));
		document = LazyHtmlDocument.Parse(html);
		body = FindBody(document);
	}

	[Benchmark]
	public int Visit()
	{
		visitor.Count = 0;
		visitor.VisitDocument(document);
		return visitor.Count;
	}

	[Benchmark]
	public string TextContent() => body.TextContent;

	[Benchmark]
	public int QuerySelectorAll()
	{
		var count = 0;
		foreach (var element in document.QuerySelectorAll(name: "a"))
			count += element.NameSpan.Length;
		return count;
	}

	[Benchmark]
	public int QuerySelectorAllByNameAndAttribute()
	{
		var count = 0;
		foreach (var element in document.QuerySelectorAll(name: "a", attributes: [new("class", "btn")]))
			count += element.NameSpan.Length;
		return count;
	}

	[Benchmark]
	public int QuerySelectorAllByAttribute()
	{
		var count = 0;
		foreach (var element in document.QuerySelectorAll(attributes: [new("rel", "author")]))
			count += element.NameSpan.Length;
		return count;
	}

	[Benchmark]
	public int TryQuerySelectorByNameAndAttribute()
		=> document.TryQuerySelector(out var element, name: "a", attributes: [new("class", "btn"), new("rel", "author")]) ? element.OuterSpan.Length : 0;

	[Benchmark]
	public int QuerySelectorAllByClassName()
	{
		var count = 0;
		foreach (var element in document.QuerySelectorAll(className: "btn"))
			count += element.NameSpan.Length;
		return count;
	}

	// The id is near the end of the sample, so most of the document is scanned.
	[Benchmark]
	public int TryQuerySelectorById()
		=> document.TryQuerySelector(out var element, id: "article-40") ? element.OuterSpan.Length : 0;

	// Includes the cost of scanning the found element, unlike TextContent which reads an element scanned in Setup.
	[Benchmark]
	public int TryQuerySelectorByIdTextContent()
		=> document.TryQuerySelector(out var element, id: "article-40") ? element.TextContent.Length : 0;

	// Matches elements with large subtrees without needing where they end.
	[Benchmark]
	public int QuerySelectorAllArticles()
	{
		var count = 0;
		foreach (var element in document.QuerySelectorAll(name: "article"))
			count += element.NameSpan.Length;
		return count;
	}

	// Needs where each match ends.
	[Benchmark]
	public int QuerySelectorAllOuterLength()
	{
		var count = 0;
		foreach (var element in document.QuerySelectorAll(name: "a"))
			count += element.OuterSpan.Length;
		return count;
	}

	// The same search done by descending through Elements() recursively, for comparison.
	[Benchmark]
	public int QuerySelectorAllViaVisitor()
	{
		filteringVisitor.Count = 0;
		filteringVisitor.VisitDocument(document);
		return filteringVisitor.Count;
	}

	private static LazyHtmlElement FindBody(LazyHtmlDocument document)
	{
		foreach (var element in document.Elements())
		{
			if (element.NameSpan.Equals("html", StringComparison.OrdinalIgnoreCase))
			{
				foreach (var child in element.Elements())
				{
					if (child.NameSpan.Equals("body", StringComparison.OrdinalIgnoreCase))
						return child;
				}
			}
		}

		throw new InvalidOperationException("Sample has no <body>.");
	}

	// Reads every element and attribute so the whole traversal is measured.
	private sealed class CountingVisitor : LazyHtmlVisitor
	{
		public int Count;

		public override void VisitElement(LazyHtmlElement element)
		{
			Count += element.NameSpan.Length;
			base.VisitElement(element);
		}

		public override void VisitAttribute(LazyHtmlElement element, LazyHtmlAttribute attribute)
		{
			Count += attribute.NameSpan.Length + attribute.ValueSpan.Length;
		}
	}

	private sealed class FilteringVisitor(string name) : LazyHtmlVisitor
	{
		public int Count;

		public override void VisitElement(LazyHtmlElement element)
		{
			if (element.NameSpan.Equals(name, StringComparison.OrdinalIgnoreCase))
				Count += element.NameSpan.Length;

			foreach (var child in element.Elements())
				VisitElement(child);
		}
	}
}
