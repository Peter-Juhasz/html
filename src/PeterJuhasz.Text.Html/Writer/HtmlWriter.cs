using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Text.Encodings.Web;

namespace PeterJuhasz.Text.Html.Writer;

[PerformanceCritical]
public class HtmlWriter<TWriter>(TWriter writer, HtmlEncoder htmlEncoder) where TWriter : IBufferWriter<char>
{
	private readonly Stack<string> openElements = new();
	private bool inTag = false;

	public void OpenElement(string name)
	{
		CloseStartTag();
		writer.Write("<");
		writer.Write(name);
		openElements.Push(name);
		inTag = true;
	}

	public void WriteAttribute(ReadOnlySpan<char> name, ReadOnlySpan<char> value)
	{
		if (!inTag)
		{
			throw new InvalidOperationException("Cannot write an attribute outside of an open tag.");
		}

		writer.Write(" ");
		writer.Write(name);
		writer.Write("=\"");
		WriteEncoded(value);
		writer.Write("\"");
	}

	public void WriteAttribute(ReadOnlySpan<char> name)
	{
		if (!inTag)
		{
			throw new InvalidOperationException("Cannot write an attribute outside of an open tag.");
		}

		writer.Write(" ");
		writer.Write(name);
	}

	public void WriteComment(ReadOnlySpan<char> comment)
	{
		if (inTag)
		{
			throw new InvalidOperationException("Cannot write a comment inside of an open tag.");
		}

		writer.Write("<!--");
		WriteEncoded(comment);
		writer.Write("-->");
	}

	public void CloseElement()
	{
		if (openElements.Count == 0)
		{
			throw new InvalidOperationException("No open elements to close.");
		}

		var name = openElements.Pop();

		if (SyntaxFacts.IsVoidElement(name.AsSpan()))
		{
			writer.Write(" />");
		}
		else
		{
			CloseStartTag();
			writer.Write("</");
			writer.Write(name);
			writer.Write(">");
		}

		inTag = false;
	}

	public void WriteText(ReadOnlySpan<char> text)
	{
		CloseStartTag();
		WriteEncoded(text);
	}

	private void CloseStartTag()
	{
		if (inTag)
		{
			writer.Write(">");
			inTag = false;
		}
	}

	private void WriteEncoded(ReadOnlySpan<char> text)
	{
		var maxEncodedLength = htmlEncoder.MaxOutputCharactersPerInputCharacter * text.Length;
		var output = writer.GetSpan(maxEncodedLength);
		htmlEncoder.Encode(text, output, out _, out int written);
		writer.Advance(written);
	}

	public void WriteHtml(ReadOnlySpan<char> html)
	{
		writer.Write(html);
	}
}

public static partial class Extensions
{
	extension<TWriter>(HtmlWriter<TWriter> writer) where TWriter : IBufferWriter<char>
	{
		public void WriteHtml5Doctype()
		{
			writer.WriteHtml("<!DOCTYPE html>");
		}
	}
}