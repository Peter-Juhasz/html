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
			if (!SyntaxFacts.NeedsDecoding(attribute.ValueSpan))
			{
				writer.WriteAttribute(attribute.NameSpan, attribute.ValueSpan);
			}
			else
			{
				var decodedBuffer = attribute.ValueSpan.Length < HtmlDecoder.StackAllocThreshold ? stackalloc char[attribute.ValueSpan.Length] : new char[attribute.ValueSpan.Length];
				HtmlDecoder.Decode(attribute.ValueSpan, decodedBuffer, out int charsWritten);
				var decoded = decodedBuffer[..charsWritten];
				writer.WriteAttribute(attribute.NameSpan, decoded);
			}
		}
		else
		{
			writer.WriteAttribute(attribute.NameSpan);
		}
	}

	public override void VisitText(LazyHtmlText text)
	{
		if (text.IsLiteral)
		{
			writer.WriteText(text.TextSpan);
		}
		else if (!SyntaxFacts.NeedsDecoding(text.TextSpan))
		{
			writer.WriteText(text.TextSpan);
		}
		else
		{
			var decodedBuffer = text.TextSpan.Length < HtmlDecoder.StackAllocThreshold ? stackalloc char[text.TextSpan.Length] : new char[text.TextSpan.Length];
			HtmlDecoder.Decode(text.TextSpan, decodedBuffer, out int charsWritten);
			var decoded = decodedBuffer[..charsWritten];
			writer.WriteText(decoded);
		}
	}

	public override void VisitComment(LazyHtmlComment comment) => writer.WriteComment(comment.TextSpan);
}
