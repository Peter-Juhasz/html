using PeterJuhasz.Text.Html.Selectors;

namespace PeterJuhasz.Text.Html.Tests.Selectors;

/// <summary>
/// Builders of the expected selectors, assertions on parsing, and test data shared by the selector tests.
/// </summary>
internal static class TestHelpers
{
	/// <summary>
	/// Identifiers as written in a selector, and the names they stand for: escapes are decoded, and every other character is kept as it is.
	/// </summary>
	public static IEnumerable<object[]> Identifiers =>
	[
		["x", "x"],
		["main-nav", "main-nav"],
		["_private", "_private"],
		["-webkit-box", "-webkit-box"],
		["--custom", "--custom"],
		["h1", "h1"],
		["MixedCase", "MixedCase"],
		["é", "é"],
		["日本語", "日本語"],
		["md\\:flex", "md:flex"],
		["w-1\\/2", "w-1/2"],
		["a\\ b", "a b"],
		["\\31 0", "10"],
		["\\61", "a"],
		["\\61 b", "ab"],
		["\\000061b", "ab"],
		["\\1F600", "😀"],
		["\\0", "�"],
		["\\D800", "�"],
		["\\110000", "�"],
	];

	/// <summary>
	/// Text that is not an identifier, where one is expected.
	/// </summary>
	public static IEnumerable<object[]> InvalidIdentifiers =>
	[
		[""],
		["1a"],
		["-1"],
		["-"],
		["\\\n"],
	];

	/// <summary>
	/// A compound selector of the simple selectors, in order.
	/// </summary>
	public static CompoundSelector Compound(params SimpleSelector[] selectors) => new(Selectors: [.. selectors]);

	/// <summary>
	/// A compound selector of an element name alone, like <c>div</c>.
	/// </summary>
	public static CompoundSelector Element(string name) => Compound(ElementName(name));

	public static ElementNameSelector ElementName(string name) => new(Name: name);

	public static ElementClassSelector Class(string className) => new(ClassName: className);

	public static IdSelector Id(string id) => new(Id: id);

	/// <summary>
	/// Asserts that the selector parses into the expected one, with both <see cref="ElementSelector.Parse"/> and <see cref="ElementSelector.TryParse"/>.
	/// </summary>
	public static void AssertParses(string selector, ElementSelector expected)
	{
		Assert.AreEqual(expected, ElementSelector.Parse(selector), $"Parsing '{selector}'.");
		Assert.IsTrue(ElementSelector.TryParse(selector, out var result), $"Trying to parse '{selector}'.");
		Assert.AreEqual(expected, result, $"Trying to parse '{selector}'.");
	}

	/// <summary>
	/// Asserts that the selector is rejected: <see cref="ElementSelector.Parse"/> throws, with the error in its message if one is given,
	/// and <see cref="ElementSelector.TryParse"/> returns false.
	/// </summary>
	public static void AssertInvalid(string selector, string? error = null)
	{
		var exception = Assert.ThrowsExactly<FormatException>(() => ElementSelector.Parse(selector), $"Parsing '{selector}'.");
		Assert.IsFalse(ElementSelector.TryParse(selector, out _), $"Trying to parse '{selector}'.");
		if (error is not null)
		{
			Assert.Contains(error, exception.Message);
		}
	}

	/// <summary>
	/// Asserts that the selector is written back as the expected CSS once parsed.
	/// </summary>
	public static void AssertPrints(string selector, string expected)
		=> Assert.AreEqual(expected, ElementSelector.Parse(selector).ToString(), $"Printing '{selector}'.");

	/// <summary>
	/// Asserts that the selector finds the expected elements in the document parsed from the HTML, in document order.
	/// Elements are named as <see cref="Describe"/> does.
	/// </summary>
	public static void AssertSelects(string html, string selector, params string[] expected)
	{
		var document = HtmlDocument.Parse(html);
		AssertFinds(selector, expected, document.QuerySelectorAll(selector), document.QuerySelectorAll(ElementSelector.Parse(selector)), document.QuerySelector(selector));
	}

	/// <summary>
	/// Asserts that the selector finds the expected elements inside the element with the <paramref name="scope"/> ID,
	/// in the document parsed from the HTML, in document order. Elements are named as <see cref="Describe"/> does.
	/// </summary>
	public static void AssertSelectsWithin(string html, string scope, string selector, params string[] expected)
	{
		var element = HtmlDocument.Parse(html).GetElementById(scope) ?? throw new AssertFailedException($"There is no element with the ID '{scope}'.");
		AssertFinds(selector, expected, element.QuerySelectorAll(selector), element.QuerySelectorAll(ElementSelector.Parse(selector)), element.QuerySelector(selector));
	}

	/// <summary>
	/// Asserts that the results of a query with the selector as a string, with the parsed selector and of the query for the first element all agree with the expected elements.
	/// The string query takes the structured query for the simplest selectors, so it is checked against the parsed selector, which does not.
	/// </summary>
	private static void AssertFinds(string selector, string[] expected, IEnumerable<HtmlElement> queried, IEnumerable<HtmlElement> matched, HtmlElement? first)
	{
		Assert.AreSequenceEqual(expected, queried.Select(Describe).ToList(), $"Querying '{selector}'.");
		Assert.AreSequenceEqual(expected, matched.Select(Describe).ToList(), $"Matching '{selector}'.");
		Assert.AreEqual(expected.FirstOrDefault(), first is null ? null : Describe(first), $"Querying the first match of '{selector}'.");
	}

	/// <summary>
	/// Names an element in the results of a query: by its ID, or by its element name when it has none.
	/// </summary>
	public static string Describe(HtmlElement element) => element.GetAttribute("id")?.Value ?? element.Name;

	/// <summary>
	/// Parses a selector that is a single simple selector, like <c>:empty</c>, and returns that simple selector.
	/// </summary>
	public static SimpleSelector ParseSimple(string selector)
	{
		if (ElementSelector.Parse(selector) is CompoundSelector { Selectors: [var simple] })
		{
			return simple;
		}

		throw new AssertFailedException($"'{selector}' is not a single simple selector.");
	}
}
