using System.Buffers;
using System.Collections.Frozen;

namespace PeterJuhasz.Text.Html;

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

	// Standard element, attribute and event handler names in their canonical lowercase form, compared ordinally,
	// so a name written that way in the source can be returned as a shared string instead of a new one.
	private static readonly FrozenSet<string>.AlternateLookup<ReadOnlySpan<char>> KnownNames = CreateKnownNames();

	public static bool IsVoidElement(ReadOnlySpan<char> name) => VoidElements.Contains(name);

	public static bool IsRawTextElement(ReadOnlySpan<char> name) => RawTextElements.Contains(name);

	public static bool TryGetImplicitClosers(ReadOnlySpan<char> name, out FrozenSet<string>.AlternateLookup<ReadOnlySpan<char>> closers)
		=> ImplicitClosers.TryGetValue(name, out closers);

	// Returns the shared string for a known name, otherwise a copy of the span.
	public static string ToName(ReadOnlySpan<char> name)
		=> KnownNames.TryGetValue(name, out var known) ? known : name.ToString();

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

	private static FrozenSet<string>.AlternateLookup<ReadOnlySpan<char>> CreateKnownNames()
	{
		// HTML Living Standard elements, followed by obsolete ones that still appear in documents.
		ReadOnlySpan<string> elements =
		[
			"a", "abbr", "address", "area", "article", "aside", "audio", "b", "base", "bdi", "bdo", "blockquote", "body", "br", "button",
			"canvas", "caption", "cite", "code", "col", "colgroup", "data", "datalist", "dd", "del", "details", "dfn", "dialog", "div", "dl", "dt",
			"em", "embed", "fieldset", "figcaption", "figure", "footer", "form", "h1", "h2", "h3", "h4", "h5", "h6", "head", "header", "hgroup", "hr", "html",
			"i", "iframe", "img", "input", "ins", "kbd", "label", "legend", "li", "link", "main", "map", "mark", "math", "menu", "meta", "meter",
			"nav", "noscript", "object", "ol", "optgroup", "option", "output", "p", "picture", "pre", "progress", "q", "rp", "rt", "ruby",
			"s", "samp", "script", "search", "section", "select", "slot", "small", "source", "span", "strong", "style", "sub", "summary", "sup", "svg",
			"table", "tbody", "td", "template", "textarea", "tfoot", "th", "thead", "time", "title", "tr", "track", "u", "ul", "var", "video", "wbr",

			"acronym", "applet", "basefont", "bgsound", "big", "blink", "center", "dir", "font", "frame", "frameset", "isindex", "keygen", "listing",
			"marquee", "menuitem", "nobr", "noembed", "noframes", "param", "plaintext", "rb", "rtc", "strike", "tt", "xmp",
		];

		// HTML Living Standard attributes (global and element-specific), WAI-ARIA attributes, followed by obsolete ones that still appear in documents.
		ReadOnlySpan<string> attributes =
		[
			"abbr", "accept", "accept-charset", "accesskey", "action", "allow", "allowfullscreen", "alpha", "alt", "as", "async", "autocapitalize",
			"autocomplete", "autocorrect", "autofocus", "autoplay", "blocking", "charset", "checked", "cite", "class", "closedby", "color", "colorspace",
			"cols", "colspan", "command", "commandfor", "content", "contenteditable", "controls", "coords", "crossorigin", "data", "datetime", "decoding",
			"default", "defer", "dir", "dirname", "disabled", "download", "draggable", "enctype", "enterkeyhint", "fetchpriority", "for", "form", "formaction",
			"formenctype", "formmethod", "formnovalidate", "formtarget", "headers", "height", "hidden", "high", "href", "hreflang", "http-equiv", "id",
			"imagesizes", "imagesrcset", "inert", "inputmode", "integrity", "is", "ismap", "itemid", "itemprop", "itemref", "itemscope", "itemtype", "kind",
			"label", "lang", "list", "loading", "loop", "low", "max", "maxlength", "media", "method", "min", "minlength", "multiple", "muted", "name",
			"nomodule", "nonce", "novalidate", "open", "optimum", "pattern", "ping", "placeholder", "playsinline", "popover", "popovertarget",
			"popovertargetaction", "poster", "preload", "readonly", "referrerpolicy", "rel", "required", "reversed", "rows", "rowspan", "sandbox", "scope",
			"selected", "shadowrootclonable", "shadowrootcustomelementregistry", "shadowrootdelegatesfocus", "shadowrootmode", "shadowrootserializable",
			"shape", "size", "sizes", "slot", "span", "spellcheck", "src", "srcdoc", "srclang", "srcset", "start", "step", "style", "tabindex", "target",
			"title", "translate", "type", "usemap", "value", "width", "wrap", "writingsuggestions",

			"role", "aria-activedescendant", "aria-atomic", "aria-autocomplete", "aria-braillelabel", "aria-brailleroledescription", "aria-busy",
			"aria-checked", "aria-colcount", "aria-colindex", "aria-colindextext", "aria-colspan", "aria-controls", "aria-current", "aria-describedby",
			"aria-description", "aria-details", "aria-disabled", "aria-dropeffect", "aria-errormessage", "aria-expanded", "aria-flowto", "aria-grabbed",
			"aria-haspopup", "aria-hidden", "aria-invalid", "aria-keyshortcuts", "aria-label", "aria-labelledby", "aria-level", "aria-live", "aria-modal",
			"aria-multiline", "aria-multiselectable", "aria-orientation", "aria-owns", "aria-placeholder", "aria-posinset", "aria-pressed", "aria-readonly",
			"aria-relevant", "aria-required", "aria-roledescription", "aria-rowcount", "aria-rowindex", "aria-rowindextext", "aria-rowspan", "aria-selected",
			"aria-setsize", "aria-sort", "aria-valuemax", "aria-valuemin", "aria-valuenow", "aria-valuetext",

			"align", "alink", "archive", "axis", "background", "bgcolor", "border", "cellpadding", "cellspacing", "char", "charoff", "classid", "clear",
			"code", "codebase", "codetype", "compact", "declare", "event", "face", "frame", "frameborder", "hspace", "language", "link", "longdesc",
			"marginheight", "marginwidth", "methods", "nohref", "noresize", "noshade", "nowrap", "profile", "rules", "scheme", "scrolling", "standby",
			"summary", "text", "valign", "valuetype", "version", "vlink", "vspace",
		];

		// Event handler content attributes: global ones, then those of body/window, then those of pointer events.
		ReadOnlySpan<string> events =
		[
			"onabort", "onauxclick", "onbeforeinput", "onbeforematch", "onbeforetoggle", "onblur", "oncancel", "oncanplay", "oncanplaythrough", "onchange",
			"onclick", "onclose", "oncommand", "oncontextlost", "oncontextmenu", "oncontextrestored", "oncopy", "oncuechange", "oncut", "ondblclick",
			"ondrag", "ondragend", "ondragenter", "ondragleave", "ondragover", "ondragstart", "ondrop", "ondurationchange", "onemptied", "onended",
			"onerror", "onfocus", "onformdata", "oninput", "oninvalid", "onkeydown", "onkeypress", "onkeyup", "onload", "onloadeddata", "onloadedmetadata",
			"onloadstart", "onmousedown", "onmouseenter", "onmouseleave", "onmousemove", "onmouseout", "onmouseover", "onmouseup", "onpaste", "onpause",
			"onplay", "onplaying", "onprogress", "onratechange", "onreset", "onresize", "onscroll", "onscrollend", "onsecuritypolicyviolation", "onseeked",
			"onseeking", "onselect", "onslotchange", "onstalled", "onsubmit", "onsuspend", "ontimeupdate", "ontoggle", "onvolumechange", "onwaiting", "onwheel",

			"onafterprint", "onbeforeprint", "onbeforeunload", "onhashchange", "onlanguagechange", "onmessage", "onmessageerror", "onoffline", "ononline",
			"onpagehide", "onpagereveal", "onpageshow", "onpageswap", "onpopstate", "onrejectionhandled", "onstorage", "onunhandledrejection", "onunload",

			"onpointercancel", "onpointerdown", "onpointerenter", "onpointerleave", "onpointermove", "onpointerout", "onpointerover", "onpointerup",
			"ongotpointercapture", "onlostpointercapture",
		];

		return FrozenSet.Create(StringComparer.Ordinal, [.. elements, .. attributes, .. events]).GetAlternateLookup<ReadOnlySpan<char>>();
	}
}
