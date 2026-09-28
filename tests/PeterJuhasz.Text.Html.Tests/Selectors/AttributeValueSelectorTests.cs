using PeterJuhasz.Text.Html.Selectors;
using static PeterJuhasz.Text.Html.Tests.Selectors.TestHelpers;

namespace PeterJuhasz.Text.Html.Tests.Selectors;

[TestClass]
public sealed class AttributeValueSelectorTests : AttributeNameTests
{
	private protected override string Write(string name) => $"[{name}=v]";

	private protected override SimpleSelector Create(string name) => Exact(name, "v");

	[TestMethod]
	[DataRow("=", nameof(AttributeOperator.Exact))]
	[DataRow("~=", nameof(AttributeOperator.ContainsWord))]
	[DataRow("|=", nameof(AttributeOperator.HyphenPrefix))]
	[DataRow("^=", nameof(AttributeOperator.StartsWith))]
	[DataRow("$=", nameof(AttributeOperator.EndsWith))]
	[DataRow("*=", nameof(AttributeOperator.Contains))]
	public void ParsesEachOperator(string written, string name)
	{
		var @operator = Enum.Parse<AttributeOperator>(name);

		AssertParses($"[a{written}v]", Compound(new AttributeValueSelector(Name: "a", Operator: @operator, Value: "v")));
		AssertParses($"[ a {written} 'v' ]", Compound(new AttributeValueSelector(Name: "a", Operator: @operator, Value: "v")));
	}

	[TestMethod]
	[DynamicData(nameof(Identifiers), typeof(TestHelpers))]
	public void ParsesAValueWrittenAsAnIdentifier(string written, string value)
		=> AssertParses($"[a={written}]", Compound(Exact("a", value)));

	[TestMethod]
	[DataRow("\"x y\"", "x y")]
	[DataRow("'x y'", "x y")]
	[DataRow("\"\"", "")]
	[DataRow("''", "")]
	[DataRow("\"1\"", "1")]
	[DataRow("\"]\"", "]")]
	[DataRow("\"it's\"", "it's")]
	[DataRow("'say \"hi\"'", "say \"hi\"")]
	[DataRow("\"a\\\"b\"", "a\"b")]
	[DataRow("'a\\'b'", "a'b")]
	[DataRow("\"a\\\\b\"", "a\\b")]
	[DataRow("\"\\26\"", "&")]
	[DataRow("\"\\26 b\"", "&b")]
	[DataRow("\"a\\\nb\"", "ab")]
	[DataRow("\"a\\\r\nb\"", "ab")]
	[DataRow("\"日本語\"", "日本語")]
	public void ParsesAValueWrittenAsAString(string written, string value)
		=> AssertParses($"[a={written}]", Compound(Exact("a", value)));

	[TestMethod]
	[DataRow("[a=v]", false)]
	[DataRow("[a=v i]", true)]
	[DataRow("[a=v I]", true)]
	[DataRow("[a=v s]", false)]
	[DataRow("[a=v S]", false)]
	[DataRow("[a='v'i]", true)]
	[DataRow("[ a = v i ]", true)]
	public void ParsesTheCaseFlag(string selector, bool ignoreCase)
		=> AssertParses(selector, Compound(new AttributeValueSelector(Name: "a", Operator: AttributeOperator.Exact, Value: "v", IgnoreCase: ignoreCase)));

	[TestMethod]
	[DataRow("[a=]", "expected an identifier")]
	[DataRow("[a=1]", "expected an identifier")]
	[DataRow("[a==v]", "expected an identifier")]
	[DataRow("[a~ =v]", "expected ']' or an attribute operator")]
	[DataRow("[a~v]", "expected ']' or an attribute operator")]
	[DataRow("[a=v w]", "expected an i or s flag")]
	[DataRow("[a=v i s]", "expected ']'")]
	[DataRow("[a=\"v]", "unterminated string")]
	[DataRow("[a='v\nw']", "newline in string")]
	public void RejectsInvalidValuesAndFlags(string selector, string error)
		=> AssertInvalid(selector, error);

	[TestMethod]
	[DataRow("[a^=v]", "[a^=\"v\"]")]
	[DataRow("[a='v' I]", "[a=\"v\" i]")]
	[DataRow("[a='x\"y']", "[a=\"x\\\"y\"]")]
	[DataRow("[a=\"x\\\\y\"]", "[a=\"x\\\\y\"]")]
	public void IsWrittenBackWithAQuotedValue(string selector, string expected)
		=> AssertPrints(selector, expected);

	private const string Links =
		"<a id=a1 rel=\"nofollow external\" lang=en-US href=\"https://x.com/a.pdf\" title=\"Tom &amp; Jerry\"></a>" +
		"<a id=a2 rel=next lang=en href=\"/b\" title=tom></a>" +
		"<a id=a3 rel=\"\" lang=english></a>" +
		"<a id=a4 hidden></a>";

	[TestMethod]
	[DataRow("[rel=next]", new[] { "a2" })]
	[DataRow("[rel='']", new[] { "a3" })]
	[DataRow("[hidden='']", new[] { "a4" })]
	[DataRow("[rel~=external]", new[] { "a1" })]
	[DataRow("[rel~=nofollow]", new[] { "a1" })]
	[DataRow("[rel~='nofollow external']", new string[0])]
	[DataRow("[rel~='']", new string[0])]
	[DataRow("[lang|=en]", new[] { "a1", "a2" })]
	[DataRow("[href^='https:']", new[] { "a1" })]
	[DataRow("[href^='']", new string[0])]
	[DataRow("[href$='.pdf']", new[] { "a1" })]
	[DataRow("[href*='x.com']", new[] { "a1" })]
	[DataRow("[title*='&']", new[] { "a1" })]
	[DataRow("[title='Tom & Jerry']", new[] { "a1" })]
	[DataRow("[title=TOM i]", new[] { "a2" })]
	[DataRow("[title=TOM]", new string[0])]
	[DataRow("[title=TOM s]", new string[0])]
	public void SelectsElementsWhoseValueMatchesByTheOperator(string selector, string[] expected)
		=> AssertSelects(Links, selector, expected);

	private static AttributeValueSelector Exact(string name, string value) => new(Name: name, Operator: AttributeOperator.Exact, Value: value);
}
