using System.Buffers;
using PeterJuhasz.Text.Html.Writer;

namespace PeterJuhasz.Text.Html.Lazy;

// Writes the visited nodes to the writer, so a document can be written back with the writer's encoding and formatting.
public class LazyHtmlWriterVisitor<TWriter>(HtmlWriter<TWriter> writer) : LazyHtmlVisitor where TWriter : IBufferWriter<char>
{
	public HtmlWriter<TWriter> Writer => writer;

	public override void VisitElement(LazyHtmlElement element)
	{
		writer.OpenElement(element.Name);
		base.VisitElement(element);
		writer.CloseElement();
	}

	public override void VisitAttribute(LazyHtmlElement element, LazyHtmlAttribute attribute)
	{
		if (attribute.HasValue)
		{
			var value = attribute.ValueSpan;
			if (!SyntaxFacts.NeedsDecoding(value, out var referenceStart))
			{
				writer.WriteAttribute(attribute.NameSpan, value);
			}
			else if (value.Length <= HtmlDecoder.StackAllocThreshold)
			{
				Span<char> buffer = stackalloc char[value.Length];
				HtmlDecoder.Decode(value, referenceStart, buffer, out int charsWritten);
				writer.WriteAttribute(attribute.NameSpan, buffer[..charsWritten]);
			}
			else
			{
				using var array = ArrayPool<char>.Shared.GetPooledArray(value.Length);
				HtmlDecoder.Decode(value, referenceStart, array, out int charsWritten);
				writer.WriteAttribute(attribute.NameSpan, array.Array.AsSpan(0, charsWritten));
			}
		}
		else
		{
			writer.WriteAttribute(attribute.NameSpan);
		}
	}

	public override void VisitText(LazyHtmlText text)
	{
		var span = text.TextSpan;
		if (text.IsLiteral || !SyntaxFacts.NeedsDecoding(span, out var referenceStart))
		{
			writer.WriteText(span);
		}
		else if (span.Length <= HtmlDecoder.StackAllocThreshold)
		{
			Span<char> buffer = stackalloc char[span.Length];
			HtmlDecoder.Decode(span, referenceStart, buffer, out int charsWritten);
			writer.WriteText(buffer[..charsWritten]);
		}
		else
		{
			using var array = ArrayPool<char>.Shared.GetPooledArray(span.Length);
			HtmlDecoder.Decode(span, referenceStart, array, out int charsWritten);
			writer.WriteText(array.Array.AsSpan(0, charsWritten));
		}
	}

	public override void VisitComment(LazyHtmlComment comment) => writer.WriteComment(comment.TextSpan);
}
