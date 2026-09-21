using Microsoft.Extensions.Primitives;
using System.Diagnostics.CodeAnalysis;

namespace System.Text.Html.Lazy;

// Enumerates the attributes of a start tag.
[PerformanceCritical]
public struct AttributesEnumerator
{
	private readonly StringSegment _document;
	private readonly int _end;
	private int _position;
	private LazyHtmlAttribute _current;

	internal AttributesEnumerator(StringSegment document, int start, int end)
	{
		_document = document;
		_position = start;
		_end = end;
	}

	public readonly LazyHtmlAttribute Current => _current;

	public readonly AttributesEnumerator GetEnumerator() => this;

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

			_current = new LazyHtmlAttribute(_document, _position);
			_position = _current.End;

			// an attribute without a name (e.g. a stray '=') is invalid and skipped
			if (!_current.NameSpan.IsEmpty)
				return true;
		}

		return false;
	}
}
