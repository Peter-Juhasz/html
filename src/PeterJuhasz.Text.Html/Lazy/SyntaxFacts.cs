using System.Buffers;
using System.Collections.Frozen;

namespace System.Text.Html.Lazy;

internal static class SyntaxFacts
{
	public const char OpenTag = '<';

	public const char CloseTag = '>';

	public const char Slash = '/';

	public const char EqualsSign = '=';

	public const char DoubleQuote = '"';

	public const char SingleQuote = '\'';

	public const string EndTagStart = "</";

	public const string CommentStart = "<!--";

	public const string CommentEnd = "-->";

	public static readonly SearchValues<char> Whitespace = SearchValues.Create("\t\n\f\r ");

	public static readonly SearchValues<char> TagNameTerminators = SearchValues.Create("\t\n\f\r />");

	public static readonly SearchValues<char> AttributeNameTerminators = SearchValues.Create("\t\n\f\r /=>");

	public static readonly SearchValues<char> UnquotedValueTerminators = SearchValues.Create("\t\n\f\r >");

	// Elements that never have content or an end tag.
	private static readonly FrozenSet<string>.AlternateLookup<ReadOnlySpan<char>> VoidElements = CreateNameSet(
		"area", "base", "br", "col", "embed", "hr", "img", "input", "link", "meta", "param", "source", "track", "wbr");

	// Elements whose content is plain text, so markup inside them is not parsed.
	private static readonly FrozenSet<string>.AlternateLookup<ReadOnlySpan<char>> RawTextElements = CreateNameSet(
		"script", "style", "textarea", "title");

	// Start tags that implicitly close an open element, keyed by the open element's name.
	private static readonly FrozenDictionary<string, FrozenSet<string>.AlternateLookup<ReadOnlySpan<char>>>.AlternateLookup<ReadOnlySpan<char>> ImplicitClosers = CreateImplicitClosers();

	public static bool IsVoidElement(ReadOnlySpan<char> name) => VoidElements.Contains(name);

	public static bool IsRawTextElement(ReadOnlySpan<char> name) => RawTextElements.Contains(name);

	public static bool TryGetImplicitClosers(ReadOnlySpan<char> name, out FrozenSet<string>.AlternateLookup<ReadOnlySpan<char>> closers)
		=> ImplicitClosers.TryGetValue(name, out closers);

	private static FrozenDictionary<string, FrozenSet<string>.AlternateLookup<ReadOnlySpan<char>>>.AlternateLookup<ReadOnlySpan<char>> CreateImplicitClosers()
	{
		var listItem = CreateNameSet("li");
		var definition = CreateNameSet("dd", "dt");
		var tableSection = CreateNameSet("tbody", "tfoot", "thead");
		var tableRow = CreateNameSet("tbody", "tfoot", "thead", "tr");
		var tableCell = CreateNameSet("tbody", "td", "tfoot", "th", "thead", "tr");
		var option = CreateNameSet("optgroup", "option");
		var optionGroup = CreateNameSet("optgroup");
		var paragraph = CreateNameSet(
			"address", "article", "aside", "blockquote", "details", "dialog", "div", "dl", "fieldset", "figcaption", "figure", "footer", "form",
			"h1", "h2", "h3", "h4", "h5", "h6", "header", "hgroup", "hr", "main", "menu", "nav", "ol", "p", "pre", "section", "table", "ul");

		var closers = new Dictionary<string, FrozenSet<string>.AlternateLookup<ReadOnlySpan<char>>>(StringComparer.OrdinalIgnoreCase)
		{
			["li"] = listItem,
			["dd"] = definition,
			["dt"] = definition,
			["tbody"] = tableSection,
			["tfoot"] = tableSection,
			["thead"] = tableSection,
			["tr"] = tableRow,
			["td"] = tableCell,
			["th"] = tableCell,
			["option"] = option,
			["optgroup"] = optionGroup,
			["p"] = paragraph,
		};
		return closers.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase).GetAlternateLookup<ReadOnlySpan<char>>();
	}

	private static FrozenSet<string>.AlternateLookup<ReadOnlySpan<char>> CreateNameSet(params ReadOnlySpan<string> names)
		=> FrozenSet.Create(StringComparer.OrdinalIgnoreCase, names).GetAlternateLookup<ReadOnlySpan<char>>();
}
