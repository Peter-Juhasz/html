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
			writer.WriteAttribute(attribute.NameSpan, SyntaxFacts.DecodeIfNeeded(attribute.ValueSpan));
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
			var decoded = HtmlDecoder.HtmlDecode(text.TextSpan);
			writer.WriteText(decoded);
		}
	}

	public override void VisitComment(LazyHtmlComment comment) => writer.WriteComment(comment.TextSpan);
}
