using Microsoft.Extensions.Primitives;
using System.Diagnostics.CodeAnalysis;

namespace PeterJuhasz.Text.Html.Lazy;

// Enumerates the attributes of a start tag.
[PerformanceCritical]
public struct AttributesEnumerator
{
	private readonly StringSegment _document;
	private readonly int _elementStart;
	private readonly int _end;
	private int _position;
	private LazyHtmlAttribute _current;

	// `elementStart` is the index of the start tag whose attributes are in the [start, end) range.
	internal AttributesEnumerator(StringSegment document, int elementStart, int start, int end)
	{
		_document = document;
		_elementStart = elementStart;
		_position = start;
		_end = end;
	}

	public readonly LazyHtmlAttribute Current => _current;

	public readonly AttributesEnumerator GetEnumerator() => this;

	// Finds the first attribute with the given name, starting from the current position.
	internal bool TryFind(ReadOnlySpan<char> name, out LazyHtmlAttribute attribute)
	{
		while (MoveNext())
		{
			if (_current.NameSpan.Equals(name, StringComparison.OrdinalIgnoreCase))
			{
				attribute = _current;
				return true;
			}
		}

		attribute = default;
		return false;
	}

	public bool MoveNext()
	{
		var text = _document.AsSpan().Slice(0, _end);
		while (_position < _end)
		{
			var c = text[_position];
			if (c == SyntaxFacts.CloseTag)
				break;

			if (c == SyntaxFacts.Slash || SyntaxFacts.Whitespace.Contains(c))
			{
				_position++;
				continue;
			}

			_current = new LazyHtmlAttribute(_document, _elementStart, _position);
			_position = _current.End;

			// an attribute without a name (e.g. a stray '=') is invalid and skipped
			if (!_current.NameSpan.IsEmpty)
				return true;
		}

		return false;
	}
}

public static partial class Extensions
{
	extension(AttributesEnumerator enumerator)
	{
		public IEnumerable<LazyHtmlAttribute> AsEnumerable()
		{
			foreach (var attribute in enumerator)
				yield return attribute;
		}
	}
}