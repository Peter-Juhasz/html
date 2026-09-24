using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;

namespace PeterJuhasz.Text.Html.Lazy;

// An element whose content has not been scanned, so its end is found while enumerating its content, by the markup that ends it.
// The default value is no element, which nothing ends.
[PerformanceCritical]
internal readonly struct OpenElement
{
	private readonly int _nameStart;
	private readonly int _nameLength;
	private readonly bool _hasClosers;
	private readonly FrozenSet<string>.AlternateLookup<ReadOnlySpan<char>> _closers;

	public OpenElement(LazyHtmlElement element)
	{
		_nameStart = element.Start + 1;
		_nameLength = element.NameLength;
		_hasClosers = SyntaxFacts.TryGetImplicitClosers(element.NameSpan, out _closers);
	}

	// A start tag always has a name, so only the default value has none.
	public bool Exists => _nameLength > 0;

	// Checks whether the markup found at `index` ends the content; if so, `end` is the index right after the element.
	// `text` must be the whole document, as the end of the element is not known.
	public bool Ends(ReadOnlySpan<char> text, MarkupKind kind, int index, out int end)
		=> HtmlScanner.EndsContent(text, kind, index, text.Slice(_nameStart, _nameLength), _hasClosers, _closers, out end);
}
