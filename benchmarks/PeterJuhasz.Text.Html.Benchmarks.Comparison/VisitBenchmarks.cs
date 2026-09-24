using AngleSharp.Dom;
using BenchmarkDotNet.Attributes;
using PeterJuhasz.Text.Html.Lazy;
using PeterJuhasz.Text.Html.Model;

namespace PeterJuhasz.Text.Html.Benchmarks.Comparison;

// Visits every node of the document, reading the name of each element and the name and value of each attribute.
[MemoryDiagnoser]
public class VisitBenchmarks : ComparisonBenchmarks
{
	private readonly AngleSharpCountingVisitor angleSharpVisitor = new();
	private readonly ModelCountingVisitor modelVisitor = new();
	private readonly LazyCountingVisitor lazyVisitor = new();

	[Benchmark(Baseline = true)]
	public int AngleSharp()
	{
		angleSharpVisitor.Count = 0;
		angleSharpVisitor.Visit(AngleSharpDocument);
		return angleSharpVisitor.Count;
	}

	[Benchmark]
	public int Model()
	{
		modelVisitor.Count = 0;
		modelVisitor.VisitDocument(ModelDocument);
		return modelVisitor.Count;
	}

	[Benchmark]
	public int Lazy()
	{
		lazyVisitor.Count = 0;
		lazyVisitor.VisitDocument(LazyDocument);
		return lazyVisitor.Count;
	}

	// AngleSharp has no visitor, so the tree is walked by index, which avoids allocating enumerators for the collections.
	private sealed class AngleSharpCountingVisitor
	{
		public int Count;

		public void Visit(INode node)
		{
			if (node is IElement element)
			{
				Count += element.LocalName.Length;

				var attributes = element.Attributes;
				for (var i = 0; i < attributes.Length; i++)
				{
					var attribute = attributes[i]!;
					Count += attribute.Name.Length + attribute.Value.Length;
				}
			}

			var children = node.ChildNodes;
			for (var i = 0; i < children.Length; i++)
			{
				Visit(children[i]);
			}
		}
	}

	private sealed class ModelCountingVisitor : HtmlVisitor
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

	private sealed class LazyCountingVisitor : LazyHtmlVisitor
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
}
