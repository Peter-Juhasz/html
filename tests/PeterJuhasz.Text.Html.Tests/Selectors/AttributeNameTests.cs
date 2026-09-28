using PeterJuhasz.Text.Html.Selectors;
using static PeterJuhasz.Text.Html.Tests.Selectors.TestHelpers;

namespace PeterJuhasz.Text.Html.Tests.Selectors;

/// <summary>
/// Tests shared by the attribute selectors, for the attribute name and the brackets around the selector.
/// </summary>
public abstract class AttributeNameTests
{
	/// <summary>
	/// Writes the selector for the attribute name as written.
	/// </summary>
	private protected abstract string Write(string name);

	/// <summary>
	/// Creates the selector expected for the attribute name.
	/// </summary>
	private protected abstract SimpleSelector Create(string name);

	[TestMethod]
	[DynamicData(nameof(Identifiers), typeof(TestHelpers))]
	public void ParsesTheName(string written, string name)
		=> AssertParses(Write(written), Compound(Create(name)));

	[TestMethod]
	[DynamicData(nameof(InvalidIdentifiers), typeof(TestHelpers))]
	public void RejectsNamesThatAreNotIdentifiers(string written)
		=> AssertInvalid(Write(written));

	[TestMethod]
	public void KeepsTheCaseOfTheName()
	{
		AssertParses(Write("HREF"), Compound(Create("HREF")));
		AssertParses(Write("Data-X"), Compound(Create("Data-X")));
	}

	[TestMethod]
	public void IgnoresWhitespaceAroundTheName()
		=> AssertParses(Write(" \t a \n "), Compound(Create("a")));

	[TestMethod]
	public void RejectsNamespaces()
	{
		AssertInvalid(Write("ns|a"));
		AssertInvalid(Write("*|a"));
		AssertInvalid(Write("|a"));
	}

	[TestMethod]
	public void RejectsAnUnclosedBracket()
		=> AssertInvalid(Write("a")[..^1]);

	[TestMethod]
	public void SelectsElementsWithTheAttributeWhateverTheCaseOfItsName()
	{
		const string html = "<a id=a1 href=v></a><a id=a2 HREF=v></a><a id=a3 title=v></a>";

		AssertSelects(html, Write("href"), "a1", "a2");
		AssertSelects(html, Write("HREF"), "a1", "a2");
	}

	[TestMethod]
	public void FollowsOtherSimpleSelectors()
		=> AssertParses($"a.x{Write("b")}", Compound(ElementName("a"), Class("x"), Create("b")));
}
