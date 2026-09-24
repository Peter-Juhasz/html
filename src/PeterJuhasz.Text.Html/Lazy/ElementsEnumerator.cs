using Microsoft.Extensions.Primitives;
using System.Diagnostics.CodeAnalysis;

namespace PeterJuhasz.Text.Html.Lazy;

// Enumerates the elements directly inside a range of the document, skipping text and other markup.
[PerformanceCritical]
public struct ElementsEnumerator
{
	private readonly StringSegment _document;
	private readonly int _end;

	// The element whose content is enumerated up to whatever ends it, when the range is not known.
	private readonly OpenElement _parent;

	private int _position;
	private LazyHtmlElement _current;

	internal ElementsEnumerator(StringSegment document, int start, int end)
	{
		_document = document;
		_position = start;
		_end = end;
	}

	// Enumerates the child elements of the element. When the end of the element is not known yet, its content is enumerated up to
	// whatever ends it instead of being scanned first.
	internal ElementsEnumerator(LazyHtmlElement parent)
	{
		var name = parent.NameSpan;
		_document = parent.Document;
		_position = parent.ContentStart;

		// raw text has no elements inside
		if (SyntaxFacts.IsRawTextElement(name))
		{
			_end = _position;
			return;
		}

		// a void element has no content, so finding its end is cheap
		if (parent.IsScanned || SyntaxFacts.IsVoidElement(name))
		{
			_end = parent.ContentEnd;
			return;
		}

		_end = _document.Length;
		_parent = new(parent);
	}

	public readonly LazyHtmlElement Current => _current;

	public readonly ElementsEnumerator GetEnumerator() => this;

	public bool MoveNext()
	{
		var text = _document.AsSpan()[.._end];
		while (_position < _end)
		{
			var kind = HtmlScanner.FindMarkup(text, _position, out var index);
			if (kind == MarkupKind.None || (_parent.Exists && _parent.Ends(text, kind, index, out _)))
			{
				_position = _end;
				break;
			}

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
			{
				yield return element;
			}
		}
	}
}