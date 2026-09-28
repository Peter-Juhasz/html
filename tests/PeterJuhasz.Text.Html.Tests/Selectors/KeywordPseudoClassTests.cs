using PeterJuhasz.Text.Html.Selectors;
using static PeterJuhasz.Text.Html.Tests.Selectors.TestHelpers;

namespace PeterJuhasz.Text.Html.Tests.Selectors;

/// <summary>
/// Tests shared by the pseudo-classes without arguments, like <c>:empty</c>, which parse to the shared instance of their selector.
/// </summary>
public abstract class KeywordPseudoClassTests
{
	/// <summary>
	/// The name of the pseudo-class, without the colon.
	/// </summary>
	private protected abstract string Keyword { get; }

	/// <summary>
	/// The shared instance that the pseudo-class parses to.
	/// </summary>
	private protected abstract SimpleSelector Instance { get; }

	/// <summary>
	/// A document where every element has an ID, and the pseudo-class matches some of them.
	/// </summary>
	private protected abstract string Document { get; }

	/// <summary>
	/// The IDs of the elements in <see cref="Document"/> that the pseudo-class matches, in document order.
	/// </summary>
	private protected abstract string[] Selected { get; }

	[TestMethod]
	public void SelectsTheMatchingElements()
		=> AssertSelects(Document, $":{Keyword}", Selected);

	[TestMethod]
	public void NegationSelectsEveryOtherElement()
	{
		var others = HtmlDocument.Parse(Document).Descendants().Select(Describe).Where(id => !Selected.Contains(id)).ToArray();

		AssertSelects(Document, $":not(:{Keyword})", others);
	}

	[TestMethod]
	public void ParsesToTheSharedInstance()
		=> Assert.AreSame(Instance, ParseSimple($":{Keyword}"));

	[TestMethod]
	public void NameIsCaseInsensitive()
	{
		Assert.AreSame(Instance, ParseSimple($":{Keyword.ToUpperInvariant()}"));
		Assert.AreSame(Instance, ParseSimple($":{char.ToUpperInvariant(Keyword[0])}{Keyword[1..]}"));
	}

	[TestMethod]
	public void FollowsOtherSimpleSelectors()
		=> AssertParses($"p.x:{Keyword}", Compound(ElementName("p"), Class("x"), Instance));

	[TestMethod]
	public void ParsesInsideOtherPseudoClasses()
		=> AssertParses($":not(:{Keyword})", Compound(!Compound(Instance)));

	[TestMethod]
	public void RejectsArguments()
	{
		AssertInvalid($":{Keyword}()", "unsupported pseudo-class");
		AssertInvalid($":{Keyword}(a)", "unsupported pseudo-class");
	}

	[TestMethod]
	public void RejectsWhitespaceAfterTheColon()
		=> AssertInvalid($": {Keyword}", "expected an identifier");

	[TestMethod]
	public void IsWrittenBackInLowercase()
		=> AssertPrints($":{Keyword.ToUpperInvariant()}", $":{Keyword}");
}
