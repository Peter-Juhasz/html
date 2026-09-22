using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Text.Encodings.Web;

namespace PeterJuhasz.Text.Html.Writer;

[PerformanceCritical]
public sealed class HtmlWriter<TWriter>(TWriter writer, HtmlEncoder htmlEncoder, HtmlWriterFormattingOptions? options = null) where TWriter : IBufferWriter<char>
{
	private readonly HtmlWriterFormattingOptions options = options ?? HtmlWriterFormattingOptions.Default;
	private readonly Stack<string> openElements = new();
	private bool inTag = false;
	private int indentLevel = 0;

	public HtmlWriterFormattingOptions Options => options;

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
		if (value.IsEmpty)
		{
			writer.Write("=\"\"");
		}
		else if (options.OmitQuotesIfNotNecessary && !SyntaxFacts.AttributeValueNeedsQuotes(value))
		{
			writer.Write("=");
			WriteEncoded(value);
		}
		else
		{
			writer.Write("=\"");
			WriteEncoded(value);
			writer.Write("\"");
		}
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
		WriteHtml(comment);
		writer.Write("-->");
	}

	public void CloseElement()
	{
		if (!openElements.TryPop(out var name))
		{
			throw new InvalidOperationException("No open elements to close.");
		}

		if (SyntaxFacts.IsVoidElement(name.AsSpan()))
		{
			if (options.XmlStyleSelfClosingTags)
			{
				if (options.SpaceBeforeSelfClosingSlash)
				{
					writer.Write(" ");
				}

				writer.Write("/>");
			}
			else
			{
				writer.Write(">");
			}
			WriteLine();
		}
		else
		{
			CloseStartTag();
			writer.Write("</");
			writer.Write(name);
			writer.Write(">");
		}
		inTag = false;
		DecreaseIndent();
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

			WriteLine();
			IncreaseIndent();
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


	private void WriteLine()
	{
		if (options.NewLine == null)
		{
			return;
		}

		writer.Write(options.NewLine);
		WriteIndent();
	}

	private void IncreaseIndent()
	{
		if (options.Indent == null)
		{
			return;
		}

		indentLevel++;
	}

	private void DecreaseIndent()
	{
		if (options.Indent == null)
		{
			return;
		}

		if (indentLevel > 0)
		{
			indentLevel--;
		}
	}

	private void WriteIndent()
	{
		if (options.Indent == null)
		{
			return;
		}

		if (indentLevel == 0)
		{
			return;
		}

		var length = options.Indent.Length * indentLevel;
		var span = writer.GetSpan(length);
		var startIndex = 0;
		for (int i = 0; i < indentLevel; i++)
		{
			options.Indent.CopyTo(span[startIndex..]);
			startIndex += length;
		}
		writer.Advance(length);
	}
}

public static partial class Extensions
{
	extension<TWriter>(HtmlWriter<TWriter> writer) where TWriter : IBufferWriter<char>
	{
		public void WriteHtml5Doctype()
		{
			writer.WriteHtml("<!DOCTYPE html>");

			if (writer.Options.NewLine != null)
			{
				writer.WriteHtml(writer.Options.NewLine);
			}
		}
	}
}