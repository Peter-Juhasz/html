using PeterJuhasz.Text.Html.Selectors;
using static PeterJuhasz.Text.Html.Tests.Selectors.TestHelpers;

namespace PeterJuhasz.Text.Html.Tests.Selectors;

/// <summary>
/// Tests shared by the selectors written as a prefix followed by an identifier, like <c>.name</c> and <c>#name</c>.
/// </summary>
public abstract class IdentifierSelectorTests
{
	/// <summary>
	/// The character that starts the selector.
	/// </summary>
	private protected abstract char Prefix { get; }

	/// <summary>
	/// Creates the selector expected for the identifier.
	/// </summary>
	private protected abstract SimpleSelector Create(string identifier);

	[TestMethod]
	[DynamicData(nameof(Identifiers), typeof(TestHelpers))]
	public void ParsesTheIdentifier(string written, string identifier)
		=> AssertParses($"{Prefix}{written}", Compound(Create(identifier)));

	[TestMethod]
	public void EscapeAtTheEndStandsForTheReplacementCharacter()
		=> AssertParses($"{Prefix}x\\", Compound(Create("x�")));

	[TestMethod]
	[DynamicData(nameof(InvalidIdentifiers), typeof(TestHelpers))]
	public void RejectsTextThatIsNotAnIdentifier(string written)
		=> AssertInvalid($"{Prefix}{written}");

	[TestMethod]
	public void RejectsWhitespaceAfterThePrefix()
		=> AssertInvalid($"{Prefix} x", "expected an identifier");

	[TestMethod]
	public void FollowsOtherSimpleSelectors()
	{
		AssertParses($"div{Prefix}x", Compound(ElementName("div"), Create("x")));
		AssertParses($"*{Prefix}x", Compound(UniversalSelector.Instance, Create("x")));
		AssertParses($"{Prefix}x{Prefix}y", Compound(Create("x"), Create("y")));
		AssertParses($"[a]{Prefix}x", Compound(new AttributeSelector(Name: "a"), Create("x")));
	}

	[TestMethod]
	public void IsWrittenBackWithThePrefix()
		=> AssertPrints($"div{Prefix}main-nav", $"div{Prefix}main-nav");
}
