using System.Net;

namespace PeterJuhasz.Text.Html.Tests;

[TestClass]
public sealed class HtmlDecoderTests
{
	[TestMethod]
	[DataRow("")]
	[DataRow("plain text")]
	[DataRow("<div class=\"a\">b</div>")]
	public void TextWithoutReferencesIsCopied(string input)
	{
		Assert.AreEqual(input, Decode(input));
	}

	[TestMethod]
	[DataRow("&amp;", "&")]
	[DataRow("&lt;", "<")]
	[DataRow("&gt;", ">")]
	[DataRow("&quot;", "\"")]
	[DataRow("&apos;", "'")]
	[DataRow("&nbsp;", "\u00A0")]
	[DataRow("&copy;", "\u00A9")]
	[DataRow("&euro;", "\u20AC")]
	[DataRow("&hearts;", "\u2665")]
	[DataRow("&thetasym;", "\u03D1")]
	public void DecodesNamedReferences(string input, string expected)
	{
		Assert.AreEqual(expected, Decode(input));
	}

	[TestMethod]
	[DataRow("a &lt; b", "a < b")]
	[DataRow("&lt;b", "<b")]
	[DataRow("a&gt;", "a>")]
	[DataRow("&lt;&gt;", "<>")]
	[DataRow("a &lt; b &amp;&amp; c &gt; d", "a < b && c > d")]
	[DataRow("&amp;lt;", "&lt;")]
	[DataRow("&lt;;", "<;")]
	public void DecodesReferencesAmongText(string input, string expected)
	{
		Assert.AreEqual(expected, Decode(input));
	}

	[TestMethod]
	[DataRow("&Aacute;", "\u00C1")]
	[DataRow("&aacute;", "\u00E1")]
	[DataRow("&AMP;", "&AMP;")]
	[DataRow("&Lt;", "&Lt;")]
	public void NamesAreCaseSensitive(string input, string expected)
	{
		Assert.AreEqual(expected, Decode(input));
	}

	[TestMethod]
	[DataRow("&unknown;")]
	[DataRow("&;")]
	[DataRow("&thetasyms;")]
	[DataRow("& amp;")]
	[DataRow("&\u00E1mp;")]
	[DataRow("&\u0430mp;")]
	public void UnknownNamesAreCopied(string input)
	{
		Assert.AreEqual(input, Decode(input));
	}

	[TestMethod]
	[DataRow("&#60;", "<")]
	[DataRow("&#0060;", "<")]
	[DataRow("&#229;", "\u00E5")]
	[DataRow("&#x3C;", "<")]
	[DataRow("&#X3c;", "<")]
	[DataRow("&#xE5;", "\u00E5")]
	[DataRow("&#x00E5;", "\u00E5")]
	[DataRow("&#65535;", "\uFFFF")]
	[DataRow("&#xFFFF;", "\uFFFF")]
	[DataRow("a &#60; b &#x3E; c", "a < b > c")]
	[DataRow("&#60;&lt;&#x3C;", "<<<")]
	public void DecodesNumericReferences(string input, string expected)
	{
		Assert.AreEqual(expected, Decode(input));
	}

	[TestMethod]
	[DataRow("&#65536;", "\uD800\uDC00")]
	[DataRow("&#x10000;", "\uD800\uDC00")]
	[DataRow("&#128512;", "\uD83D\uDE00")]
	[DataRow("&#x1F600;", "\uD83D\uDE00")]
	[DataRow("&#x10FFFF;", "\uDBFF\uDFFF")]
	[DataRow("a&#x1F600;b", "a\uD83D\uDE00b")]
	public void DecodesSupplementaryNumericReferencesToSurrogatePairs(string input, string expected)
	{
		Assert.AreEqual(expected, Decode(input));
	}

	[TestMethod]
	[DataRow("&#;")]
	[DataRow("&#x;")]
	[DataRow("&#X;")]
	[DataRow("&#abc;")]
	[DataRow("&#xG;")]
	[DataRow("&#x 3C;")]
	[DataRow("&#1.5;")]
	[DataRow("&#55296;")]
	[DataRow("&#xD800;")]
	[DataRow("&#xDFFF;")]
	[DataRow("&#x110000;")]
	[DataRow("&#1114112;")]
	[DataRow("&#4294967296;")]
	[DataRow("&#x100000000;")]
	public void InvalidNumericReferencesAreCopied(string input)
	{
		Assert.AreEqual(input, Decode(input));
	}

	[TestMethod]
	[DataRow("&", "&")]
	[DataRow("a & b", "a & b")]
	[DataRow("&&", "&&")]
	[DataRow("&amp", "&amp")]
	[DataRow("&amp &lt", "&amp &lt")]
	[DataRow("&&amp;", "&&")]
	[DataRow("&amp&lt;", "&amp<")]
	[DataRow("&l&t;", "&l&t;")]
	[DataRow("&lt;&", "<&")]
	[DataRow("&#60", "&#60")]
	[DataRow("&#&#60;", "&#<")]
	public void AmpersandWithoutReferenceIsCopied(string input, string expected)
	{
		Assert.AreEqual(expected, Decode(input));
	}

	[TestMethod]
	[DataRow("")]
	[DataRow("plain text")]
	[DataRow("a &lt; b &amp;&amp; c &gt; d")]
	[DataRow("&Aacute;&aacute;&AMP;")]
	[DataRow("&unknown; &; &thetasyms; & amp;")]
	[DataRow("& && &amp &&amp; &amp&lt; &l&t; &lt;&")]
	[DataRow("&nbsp;&iexcl;&cent;&pound;&curren;&yen;&brvbar;&sect;&uml;&copy;&ordf;&laquo;&not;&shy;&reg;&macr;")]
	[DataRow("&alpha;&beta;&gamma;&Omega;&thetasym;&upsih;&piv;&bull;&hellip;&prime;&oline;&frasl;&weierp;&image;&real;&trade;")]
	[DataRow("&larr;&uarr;&rarr;&darr;&harr;&crarr;&lArr;&forall;&part;&exist;&empty;&nabla;&isin;&notin;&ni;&prod;&sum;")]
	[DataRow("&lceil;&rceil;&lfloor;&rfloor;&lang;&rang;&loz;&spades;&clubs;&hearts;&diams;&OElig;&oelig;&Scaron;&euro;")]
	[DataRow("&#60;&#0060;&#229;&#x3C;&#X3c;&#xE5;&#x00E5;&#65535;&#65536;&#128512;&#x1F600;&#x10FFFF;&#0;")]
	[DataRow("&#; &#x; &#abc; &#xG; &#1.5; &#55296; &#xD800; &#xDFFF; &#x110000; &#4294967296; &#60 &#&#60;")]
	[DataRow("&# 60; &#60 ; &#+60; &#-0; &#-60; &#x 3C; &#x+3C; &#x-3C;")]
	public void MatchesWebUtility(string input)
	{
		Assert.AreEqual(WebUtility.HtmlDecode(input), Decode(input));
	}

	[TestMethod]
	public void DecodesLongText()
	{
		var input = string.Concat(Enumerable.Repeat("Some text with &lt;markup&gt;, a &amp; b, &#60;&#x1F600;, an & alone and &unknown; names. ", 100));

		Assert.AreEqual(WebUtility.HtmlDecode(input), Decode(input));
	}

	[TestMethod]
	public void DecodesSliceOfLargerText()
	{
		var input = "&lt;a &amp; b&gt;".AsSpan(2, 13);
		var output = new char[input.Length];

		HtmlDecoder.Decode(input, output, out int charsWritten);

		Assert.AreEqual("t;a & b&g", new string(output, 0, charsWritten));
	}

	[TestMethod]
	public void OutputCanBeExactlyAsLongAsInput()
	{
		var output = new char[8];

		HtmlDecoder.Decode("a &lt; b", output, out int charsWritten);

		Assert.AreEqual("a < b", new string(output, 0, charsWritten));
	}

	[TestMethod]
	public void OutputBeyondDecodedTextIsNotModified()
	{
		var output = "..........".ToCharArray();

		HtmlDecoder.Decode("a &lt; b", output, out int charsWritten);

		Assert.AreEqual(5, charsWritten);
		Assert.AreEqual("a < b.....", new string(output));
	}

	[TestMethod]
	public void DecodesInPlace()
	{
		var buffer = "&lt;p&gt;a &amp; b&lt;/p&gt; &#60;&#x1F600;&#128512; &unknown; &#xD800; & c".ToCharArray();

		HtmlDecoder.Decode(buffer, buffer, out int charsWritten);

		Assert.AreEqual("<p>a & b</p> <\uD83D\uDE00\uD83D\uDE00 &unknown; &#xD800; & c", new string(buffer, 0, charsWritten));
	}

	[TestMethod]
	[DataRow("abc", 2)]
	[DataRow("a &lt; b", 5)]
	[DataRow("a &lt; b", 7)]
	[DataRow("&lt;", 0)]
	[DataRow("a &unknown;", 10)]
	[DataRow("&#x1F600;", 2)]
	public void ThrowsWhenOutputIsShorterThanInput(string input, int outputLength)
	{
		var output = new char[outputLength];
		Array.Fill(output, '.');

		var exception = Assert.ThrowsExactly<ArgumentException>(() => HtmlDecoder.Decode(input, output, out _));

		Assert.AreEqual("output", exception.ParamName);
		Assert.AreEqual(new string('.', outputLength), new string(output));
	}

	private static string Decode(string input)
	{
		var output = new char[input.Length];
		HtmlDecoder.Decode(input, output, out int charsWritten);
		return new string(output, 0, charsWritten);
	}
}
