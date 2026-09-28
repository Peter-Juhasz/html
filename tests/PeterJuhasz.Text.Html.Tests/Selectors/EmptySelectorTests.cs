using PeterJuhasz.Text.Html.Selectors;
using static PeterJuhasz.Text.Html.Tests.Selectors.TestHelpers;

namespace PeterJuhasz.Text.Html.Tests.Selectors;

[TestClass]
public sealed class EmptySelectorTests : KeywordPseudoClassTests
{
	private protected override string Keyword => "empty";

	private protected override SimpleSelector Instance => EmptySelector.Instance;

	private protected override string Document =>
		"<div id=d1></div><div id=d2><!-- comment --></div><div id=d3> </div><div id=d4>text</div><div id=d5><br id=b1></div>" +
		"<div id=d6></span></div><img id=i1><script id=s1></script><script id=s2> </script>";

	private protected override string[] Selected => ["d1", "d2", "b1", "d6", "i1", "s1"];

	[TestMethod]
	public void ElementBelowTheDepthLimitIsEmptyOnlyWithoutContent()
	{
		AssertSelects(BelowTheDepthLimit("<div id=deep></div>"), "#deep:empty", "deep");
		AssertSelects(BelowTheDepthLimit("<div id=deep><i></i></div>"), "#deep:empty");
	}

	/// <summary>
	/// Wraps the markup in as many elements as the parser descends into, so the content of its elements is not parsed into nodes.
	/// </summary>
	private static string BelowTheDepthLimit(string html)
		=> string.Concat(Enumerable.Repeat("<div>", HtmlScanner.MaxDepth)) + html + string.Concat(Enumerable.Repeat("</div>", HtmlScanner.MaxDepth));
}
