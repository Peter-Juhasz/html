using System.Buffers;
using System.Text.Encodings.Web;

namespace PeterJuhasz.Text.Html.Tests.Writer;

internal static class TestHelpers
{
	public static string Write(Action<HtmlWriter<ArrayBufferWriter<char>>> write, HtmlEncoder? encoder = null, int initialCapacity = 256, HtmlWriterFormattingOptions? options = null)
	{
		var buffer = new ArrayBufferWriter<char>(initialCapacity);
		var writer = new HtmlWriter<ArrayBufferWriter<char>>(buffer, encoder ?? HtmlEncoder.Default, options ?? HtmlWriterFormattingOptions.Minimal);
		write(writer);
		return buffer.WrittenSpan.ToString();
	}
}
