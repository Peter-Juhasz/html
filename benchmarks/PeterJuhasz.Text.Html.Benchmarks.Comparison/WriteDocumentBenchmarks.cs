using AngleSharp;
using BenchmarkDotNet.Attributes;
using PeterJuhasz.Text.Html.Lazy;
using PeterJuhasz.Text.Html.Model;
using PeterJuhasz.Text.Html.Writer;
using System.Buffers;
using System.Text.Encodings.Web;
using System.Text.Unicode;

namespace PeterJuhasz.Text.Html.Benchmarks.Comparison;

// Writes the whole document back to a string. The writer is configured to produce output equivalent to AngleSharp's:
// a doctype, quoted attributes, no self-closing slashes and only the markup characters encoded.
[MemoryDiagnoser]
public class WriteDocumentBenchmarks : ComparisonBenchmarks
{
	private static readonly HtmlEncoder Encoder = HtmlEncoder.Create(UnicodeRanges.All);
	private static readonly HtmlWriterFormattingOptions Options = new(XmlStyleSelfClosingTags: false);

	[Benchmark(Baseline = true)]
	public string AngleSharp() => AngleSharpDocument.ToHtml();

	[Benchmark]
	public string Model()
	{
		var buffer = new ArrayBufferWriter<char>();
		var writer = new HtmlWriter<ArrayBufferWriter<char>>(buffer, Encoder, Options);
		writer.WriteHtml5Doctype();
		new HtmlWriterVisitor<ArrayBufferWriter<char>>(writer).VisitDocument(ModelDocument);
		return buffer.WrittenSpan.ToString();
	}

	[Benchmark]
	public string Lazy()
	{
		var buffer = new ArrayBufferWriter<char>();
		var writer = new HtmlWriter<ArrayBufferWriter<char>>(buffer, Encoder, Options);
		writer.WriteHtml5Doctype();
		new LazyHtmlWriterVisitor<ArrayBufferWriter<char>>(writer).VisitDocument(LazyDocument);
		return buffer.WrittenSpan.ToString();
	}
}
