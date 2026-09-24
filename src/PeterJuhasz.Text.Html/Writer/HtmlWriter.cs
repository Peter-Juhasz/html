using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Text.Encodings.Web;

namespace PeterJuhasz.Text.Html.Writer;

[PerformanceCritical]
public class HtmlWriter<TWriter>(TWriter writer, HtmlEncoder htmlEncoder, HtmlWriterFormattingOptions? options = null) where TWriter : IBufferWriter<char>
{
	private readonly HtmlWriterFormattingOptions options = options ?? HtmlWriterFormattingOptions.Default;
	private readonly Stack<ElementScope> openElements = new();
	private bool inTag = false;

	// Whether the innermost open element (or the document, when there is none) has child elements or comments, and whether it has text.
	// When formatting, an element with children but no text gets its children and end tag on their own lines; one with text stays on one line.
	private bool hasChildren = false;
	private bool hasText = false;

	public HtmlWriterFormattingOptions Options => options;

	public void OpenElement(string name)
	{
		CloseStartTag();
		StartChildNode();
		writer.Write("<");
		writer.Write(name);
		var isLiteral = SyntaxFacts.IsRawTextElement(name) && !SyntaxFacts.IsEscapableRawTextElement(name);
		openElements.Push(new(name, isLiteral, hasChildren, hasText));
		hasChildren = false;
		hasText = false;
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
		CloseStartTag();
		StartChildNode();
		writer.Write("<!--");
		WriteHtml(comment);
		writer.Write("-->");
	}

	public void CloseElement()
	{
		if (!openElements.TryPop(out var element))
		{
			throw new InvalidOperationException("No open elements to close.");
		}

		var name = element.Name;
		if (SyntaxFacts.IsVoidElement(name))
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
		}
		else if (options.OmitOptionalEndTags && SyntaxFacts.HasOptionalEndTag(name))
		{
			// the next sibling or the end of the parent implies the end tag
			CloseStartTag();
		}
		else
		{
			CloseStartTag();
			if (hasChildren && !hasText)
			{
				WriteLine();
			}

			writer.Write("</");
			writer.Write(name);
			writer.Write(">");
		}
		inTag = false;
		hasChildren = element.ParentHasChildren;
		hasText = element.ParentHasText;
	}

	public void WriteText(ReadOnlySpan<char> text)
	{
		CloseStartTag();
		hasText = true;

		// parsers take the content of script and style literally, so encoding it would change it
		if (openElements.TryPeek(out var element) && element.IsLiteral)
		{
			writer.Write(text);
		}
		else
		{
			WriteEncoded(text);
		}
	}

	private void CloseStartTag()
	{
		if (inTag)
		{
			writer.Write(">");
			inTag = false;
		}
	}

	// Starts an element or comment on its own line, unless the enclosing scope has text or this is the first node of the document.
	private void StartChildNode()
	{
		if (!hasText && (openElements.Count > 0 || hasChildren))
		{
			WriteLine();
		}

		hasChildren = true;
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

	// Indents by the number of open elements, which is the depth of the node being written.
	private void WriteIndent()
	{
		var indent = options.Indent;
		var indentLevel = openElements.Count;
		if (indent is null or "" || indentLevel == 0)
		{
			return;
		}

		var length = indent.Length * indentLevel;
		var span = writer.GetSpan(length);
		for (var startIndex = 0; startIndex < length; startIndex += indent.Length)
		{
			indent.CopyTo(span[startIndex..]);
		}
		writer.Advance(length);
	}

	// An open element with the state of its enclosing scope, which is restored when the element is closed.
	private readonly record struct ElementScope(string Name, bool IsLiteral, bool ParentHasChildren, bool ParentHasText);
}

public class HtmlWriter(IBufferWriter<char> writer, HtmlEncoder? htmlEncoder = null, HtmlWriterFormattingOptions? options = null)
	: HtmlWriter<IBufferWriter<char>>(writer, htmlEncoder ?? HtmlEncoder.Default, options)
{
	public static HtmlWriter<TWriter> Create<TWriter>(TWriter writer, HtmlEncoder? htmlEncoder = null, HtmlWriterFormattingOptions? options = null)
		where TWriter : IBufferWriter<char> => 
		new(writer, htmlEncoder ?? HtmlEncoder.Default, options);
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