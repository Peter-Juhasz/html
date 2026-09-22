using System.Buffers;
using PeterJuhasz.Text.Html.Writer;

namespace PeterJuhasz.Text.Html.Model;

// Writes the visited nodes to the writer, so a document can be written back with the writer's encoding and formatting.
public class HtmlWriterVisitor<TWriter>(HtmlWriter<TWriter> writer) : HtmlVisitor where TWriter : IBufferWriter<char>
{
	public HtmlWriter<TWriter> Writer => writer;

	public override void VisitElement(HtmlElement element)
	{
		writer.OpenElement(element.Name);
		base.VisitElement(element);
		writer.CloseElement();
	}

	public override void VisitAttribute(HtmlAttribute attribute)
	{
		if (attribute.HasValue)
		{
			writer.WriteAttribute(attribute.Name, attribute.Value);
		}
		else
		{
			writer.WriteAttribute(attribute.Name);
		}
	}

	public override void VisitText(HtmlText text) => writer.WriteText(text.Text);

	public override void VisitComment(HtmlComment comment) => writer.WriteComment(comment.TextSpan);
}
