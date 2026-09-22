using Microsoft.Extensions.Primitives;
using System.Diagnostics.CodeAnalysis;

namespace PeterJuhasz.Text.Html.Lazy;

// Enumerates the elements directly inside a range of the document, skipping text and other markup.
[PerformanceCritical]
public struct ElementsEnumerator
{
	private readonly StringSegment _document;
	private readonly int _end;
	private int _position;
	private LazyHtmlElement _current;

	internal ElementsEnumerator(StringSegment document, int start, int end)
	{
		_document = document;
		_position = start;
		_end = end;
	}

	public readonly LazyHtmlElement Current => _current;

	public readonly ElementsEnumerator GetEnumerator() => this;

	public bool MoveNext()
	{
		var text = _document.AsSpan().Slice(0, _end);
		while (_position < _end)
		{
			var kind = HtmlScanner.FindMarkup(text, _position, out var index);
			if (kind == MarkupKind.None)
				break;

			if (kind == MarkupKind.StartTag)
			{
				_current = new LazyHtmlElement(_document, index);
				_position = _current.End;
				return true;
			}

			_position = HtmlScanner.SkipMarkup(text, index);
		}

		return false;
	}
}

public static partial class Extensions
{
	extension(ElementsEnumerator enumerator)
	{
		public IEnumerable<LazyHtmlElement> AsEnumerable()
		{
			foreach (var element in enumerator)
				yield return element;
		}
	}
}