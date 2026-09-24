using System.Buffers;
using System.Text.Encodings.Web;

namespace PeterJuhasz.Text.Html.Tests.Writer;

[TestClass]
public sealed class AllocationTests
{
	[TestMethod]
	public void WritingDoesNotAllocateAfterWarmUp()
	{
		var buffer = new ArrayBufferWriter<char>();
		var writer = new HtmlWriter<ArrayBufferWriter<char>>(buffer, HtmlEncoder.Default, HtmlWriterFormattingOptions.Minimal);

		WriteDocument(writer);
		var expected = buffer.WrittenSpan.ToString();
		buffer.ResetWrittenCount();

		var before = GC.GetAllocatedBytesForCurrentThread();
		WriteDocument(writer);
		var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

		Assert.AreEqual(expected, buffer.WrittenSpan.ToString());
		Assert.AreEqual(0, allocated);
	}

	// Exercises every write path with constant inputs so no allocation is attributable to the caller.
	private static void WriteDocument(HtmlWriter<ArrayBufferWriter<char>> writer)
	{
		writer.WriteHtml("<!DOCTYPE html>");
		writer.OpenElement("html");
		writer.WriteAttribute("lang", "en");
		writer.OpenElement("body");
		writer.OpenElement("div");
		writer.WriteAttribute("class", "a b");
		writer.WriteAttribute("hidden");
		writer.WriteText("Tom & Jerry <3");
		writer.OpenElement("br");
		writer.CloseElement();
		writer.WriteComment(" c ");
		writer.CloseElement();
		writer.CloseElement();
		writer.CloseElement();
	}
}
