using System.Buffers;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace PeterJuhasz.Text.Html.Selectors;

/// <summary>
/// Parses CSS selectors into <see cref="ElementSelector"/> trees. Supported are selector lists; the descendant, child (<c>&gt;</c>) and next-sibling (<c>+</c>) combinators;
/// type, universal, class, ID and attribute selectors with every operator and the <c>i</c> and <c>s</c> flags; and the <c>:not()</c>, <c>:has()</c>, <c>:nth-child()</c>,
/// <c>:nth-last-child()</c>, <c>:first-child</c>, <c>:last-child</c>, <c>:empty</c> and <c>:disabled</c> pseudo-classes. Escapes are decoded in identifiers and strings.
/// Namespaces, comments, pseudo-elements, the <c>~</c> combinator and other pseudo-classes are rejected.
/// </summary>
/// <remarks>
/// The text is read as a span from start to end without backtracking: identifiers, strings and numbers are found with <see cref="SearchValues{T}"/> and sliced from it,
/// so apart from the selectors themselves, only class names, IDs, attribute values and identifiers with escapes allocate strings.
/// </remarks>
internal ref struct ElementSelectorParser(ReadOnlySpan<char> text)
{
	/// <summary>
	/// How deeply the arguments of pseudo-classes may be nested, so that the recursion of the parser stays bounded for pathological input.
	/// </summary>
	internal const int MaxDepth = 32;

	/// <summary>
	/// How many compound selectors a complex selector may chain with combinators. Matching recurses once per combinator, and siblings, unlike ancestors,
	/// are not limited by the depth of the document, so this keeps the stack bounded for pathological input while leaving room for long paths like copied ones.
	/// </summary>
	internal const int MaxCompoundSelectors = 64;

	private static readonly SearchValues<char> HexDigits = SearchValues.Create("0123456789ABCDEFabcdef");

	/// <summary>
	/// The pseudo-classes without arguments, by name compared case-insensitively, as the shared instances of the selectors.
	/// </summary>
	private static readonly FrozenDictionary<string, SimpleSelector> PseudoClasses = new Dictionary<string, SimpleSelector>
	{
		["disabled"] = DisabledSelector.Instance,
		["empty"] = EmptySelector.Instance,
		["first-child"] = NthChildSelector.First,
		["last-child"] = NthChildSelector.Last,
	}.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

	/// <summary>
	/// Looks up <see cref="PseudoClasses"/> by a span of the text, so the name does not have to be allocated.
	/// </summary>
	private static readonly FrozenDictionary<string, SimpleSelector>.AlternateLookup<ReadOnlySpan<char>> PseudoClassesBySpan = PseudoClasses.GetAlternateLookup<ReadOnlySpan<char>>();

	/// <summary>
	/// The pseudo-classes with arguments in parentheses, by name compared case-insensitively.
	/// </summary>
	private static readonly FrozenDictionary<string, FunctionalPseudoClass> FunctionalPseudoClasses = new Dictionary<string, FunctionalPseudoClass>
	{
		["not"] = FunctionalPseudoClass.Not,
		["has"] = FunctionalPseudoClass.Has,
		["nth-child"] = FunctionalPseudoClass.NthChild,
		["nth-last-child"] = FunctionalPseudoClass.NthLastChild,
	}.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

	/// <summary>
	/// Looks up <see cref="FunctionalPseudoClasses"/> by a span of the text, so the name does not have to be allocated.
	/// </summary>
	private static readonly FrozenDictionary<string, FunctionalPseudoClass>.AlternateLookup<ReadOnlySpan<char>> FunctionalPseudoClassesBySpan = FunctionalPseudoClasses.GetAlternateLookup<ReadOnlySpan<char>>();

	private readonly int _length = text.Length;

	/// <summary>
	/// The text that is not read yet.
	/// </summary>
	private ReadOnlySpan<char> _rest = text;

	private int _depth;

	private string? _error;

	private int _errorPosition;

	/// <summary>
	/// The position of the next character to read in the text.
	/// </summary>
	private readonly int Position => _length - _rest.Length;

	/// <summary>
	/// Parses a selector, throwing if it is not valid or uses syntax that is not supported.
	/// </summary>
	/// <exception cref="FormatException">The selector is not valid or not supported; the message tells why and where.</exception>
	public static ElementSelector Parse(string selector)
	{
		ArgumentNullException.ThrowIfNull(selector);
		return TryParse(selector, out var result, out var error) ? result : throw new FormatException(error);
	}

	/// <summary>
	/// Parses a selector, returning false if it is not valid or uses syntax that is not supported.
	/// </summary>
	public static bool TryParse(ReadOnlySpan<char> selector, [NotNullWhen(true)] out ElementSelector? result) => TryParse(selector, out result, out _);

	/// <summary>
	/// Parses a selector, returning false with a message that tells why and where, if it is not valid or uses syntax that is not supported.
	/// </summary>
	public static bool TryParse(ReadOnlySpan<char> selector, [NotNullWhen(true)] out ElementSelector? result, [NotNullWhen(false)] out string? error)
	{
		var parser = new ElementSelectorParser(selector);
		if (parser.TryReadSelector(out result))
		{
			error = null;
			return true;
		}

		error = $"'{selector}' is not a supported selector: {parser._error} at position {parser._errorPosition}.";
		return false;
	}

	/// <summary>
	/// Reads the whole text as a selector list.
	/// </summary>
	private bool TryReadSelector([NotNullWhen(true)] out ElementSelector? selector)
	{
		if (!TryReadSelectorList(relative: false, out selector))
		{
			return false;
		}

		if (_rest is [var unexpected, ..])
		{
			selector = null;
			return Fail($"unexpected '{unexpected}'");
		}

		return true;
	}

	/// <summary>
	/// Reads complex selectors separated by commas, up to the end of the text or a <c>)</c>; a single selector is returned as is, several as an <see cref="OrSelector"/>.
	/// </summary>
	private bool TryReadSelectorList(bool relative, [NotNullWhen(true)] out ElementSelector? selector)
	{
		selector = null;
		using var selectors = new PooledArrayBuilder<ElementSelector>();
		do
		{
			SkipWhitespace();
			if (!TryReadComplexSelector(relative, out var complex))
			{
				return false;
			}

			selectors.Add(complex);
		}
		while (TrySkip(','));

		var array = selectors.ToImmutableArray();
		selector = array is [var single] ? single : new OrSelector(Selectors: array);
		return true;
	}

	/// <summary>
	/// Reads compound selectors joined by combinators. A relative selector, as in <c>:has()</c>, starts from the anchor with an optional combinator.
	/// </summary>
	private bool TryReadComplexSelector(bool relative, [NotNullWhen(true)] out ElementSelector? selector)
	{
		selector = relative ? AnchorSelector.Instance : null;
		var combinator = relative && TryReadCombinatorSymbol(out var symbol) ? symbol : ' ';
		var count = 0;
		do
		{
			if (++count > MaxCompoundSelectors)
			{
				selector = null;
				return Fail("too many compound selectors");
			}

			if (!TryReadCompoundSelector(out var compound))
			{
				selector = null;
				return false;
			}

			selector = selector switch
			{
				null => compound,
				_ => Combine(combinator, selector, compound),
			};
		}
		while (TryReadCombinator(out combinator));

		return true;
	}

	/// <summary>
	/// Reads the combinator after a compound selector with the whitespace around it. Whitespace alone is the descendant combinator, unless the selector ends there.
	/// </summary>
	private bool TryReadCombinator(out char combinator)
	{
		var hasWhitespace = SkipWhitespace();
		if (TryReadCombinatorSymbol(out combinator))
		{
			return true;
		}

		combinator = ' ';
		return hasWhitespace && _rest is not ([] or [',' or ')', ..]);
	}

	/// <summary>
	/// Reads a <c>&gt;</c> or <c>+</c> combinator and the whitespace after it.
	/// </summary>
	private bool TryReadCombinatorSymbol(out char combinator)
	{
		if (_rest is [('>' or '+') and var symbol, ..])
		{
			_rest = _rest[1..];
			SkipWhitespace();
			combinator = symbol;
			return true;
		}

		combinator = default;
		return false;
	}

	/// <summary>
	/// Joins two selectors with the combinator, as the derived type of <see cref="ComplexSelector"/> that stands for it.
	/// </summary>
	private static ComplexSelector Combine(char combinator, ElementSelector left, CompoundSelector right) => combinator switch
	{
		'>' => new ChildSelector(Left: left, Right: right),
		'+' => new NextSiblingSelector(Left: left, Right: right),
		_ => new DescendantSelector(Left: left, Right: right),
	};

	/// <summary>
	/// Reads a type or universal selector, if any, then any number of class, ID, attribute and pseudo-class selectors; at least one in all.
	/// </summary>
	private bool TryReadCompoundSelector([NotNullWhen(true)] out CompoundSelector? compound)
	{
		compound = null;
		using var selectors = new PooledArrayBuilder<SimpleSelector>();

		if (TrySkip('*'))
		{
			selectors.Add(UniversalSelector.Instance);
		}
		else if (_rest is [var first, ..] && IsIdentifierCharacter(first))
		{
			if (!TryReadIdentifier(out var name))
			{
				return false;
			}

			// known element names are shared strings, so they are not allocated
			selectors.Add(new ElementNameSelector(Name: SyntaxFacts.ToName(name)));
		}

		while (_rest is ['.' or '#' or '[' or ':', ..])
		{
			if (!TryReadSubclassSelector(out var selector))
			{
				return false;
			}

			selectors.Add(selector);
		}

		var array = selectors.ToImmutableArray();
		if (array.IsEmpty)
		{
			return Fail(_rest is [var unexpected, ..] ? $"unexpected '{unexpected}'" : "expected a selector");
		}

		compound = new CompoundSelector(Selectors: array);
		return true;
	}

	/// <summary>
	/// Reads a class, ID, attribute or pseudo-class selector, by the character it starts with.
	/// </summary>
	private bool TryReadSubclassSelector([NotNullWhen(true)] out SimpleSelector? selector)
	{
		selector = null;
		switch (_rest[0])
		{
			case '.':
				_rest = _rest[1..];
				if (!TryReadIdentifier(out var className))
				{
					return false;
				}

				selector = new ElementClassSelector(ClassName: className.ToString());
				return true;

			case '#':
				_rest = _rest[1..];
				if (!TryReadIdentifier(out var id))
				{
					return false;
				}

				selector = new IdSelector(Id: id.ToString());
				return true;

			case '[':
				return TryReadAttributeSelector(out selector);

			default:
				return TryReadPseudoClass(out selector);
		}
	}

	/// <summary>
	/// Reads an attribute selector within brackets: a name, optionally followed by an operator, a value as an identifier or a string, and an <c>i</c> or <c>s</c> flag.
	/// </summary>
	private bool TryReadAttributeSelector([NotNullWhen(true)] out SimpleSelector? selector)
	{
		selector = null;
		_rest = _rest[1..];
		SkipWhitespace();
		if (!TryReadIdentifier(out var nameSpan))
		{
			return false;
		}

		var name = SyntaxFacts.ToName(nameSpan);
		SkipWhitespace();
		if (TrySkip(']'))
		{
			selector = new AttributeSelector(Name: name);
			return true;
		}

		if (!TryReadAttributeOperator(out var @operator))
		{
			return false;
		}

		SkipWhitespace();
		ReadOnlySpan<char> value;
		var hasValue = _rest is ['"' or '\'', ..] ? TryReadString(out value) : TryReadIdentifier(out value);
		if (!hasValue)
		{
			return false;
		}

		var valueString = value.ToString();
		SkipWhitespace();

		var ignoreCase = false;
		if (_rest is [not ']', ..])
		{
			var flagStart = _rest;
			if (!TryReadIdentifier(out var flag))
			{
				return false;
			}

			if (flag is "i" or "I")
			{
				ignoreCase = true;
			}
			else if (flag is not ("s" or "S"))
			{
				_rest = flagStart;
				return Fail("expected an i or s flag");
			}

			SkipWhitespace();
		}

		if (!TrySkip(']'))
		{
			return Fail("expected ']'");
		}

		selector = new AttributeValueSelector(Name: name, Operator: @operator, Value: valueString, IgnoreCase: ignoreCase);
		return true;
	}

	/// <summary>
	/// Reads the operator of an attribute selector: <c>=</c>, <c>~=</c>, <c>|=</c>, <c>^=</c>, <c>$=</c> or <c>*=</c>.
	/// </summary>
	private bool TryReadAttributeOperator(out AttributeOperator @operator)
	{
		(@operator, var length) = _rest switch
		{
			['=', ..] => (AttributeOperator.Exact, 1),
			['~', '=', ..] => (AttributeOperator.ContainsWord, 2),
			['|', '=', ..] => (AttributeOperator.HyphenPrefix, 2),
			['^', '=', ..] => (AttributeOperator.StartsWith, 2),
			['$', '=', ..] => (AttributeOperator.EndsWith, 2),
			['*', '=', ..] => (AttributeOperator.Contains, 2),
			_ => (default, 0),
		};

		if (length is 0)
		{
			return Fail("expected ']' or an attribute operator");
		}

		_rest = _rest[length..];
		return true;
	}

	/// <summary>
	/// Reads a pseudo-class, with the arguments of <c>:not()</c>, <c>:has()</c>, <c>:nth-child()</c> and <c>:nth-last-child()</c> in parentheses.
	/// </summary>
	private bool TryReadPseudoClass([NotNullWhen(true)] out SimpleSelector? selector)
	{
		selector = null;
		_rest = _rest[1..];
		if (_rest is [':', ..])
		{
			return Fail("pseudo-elements are not supported");
		}

		var nameStart = _rest;
		if (!TryReadIdentifier(out var name))
		{
			return false;
		}

		if (!TrySkip('('))
		{
			if (PseudoClassesBySpan.TryGetValue(name, out selector))
			{
				return true;
			}

			_rest = nameStart;
			return Fail($"unsupported pseudo-class ':{name}'");
		}

		if (!FunctionalPseudoClassesBySpan.TryGetValue(name, out var pseudoClass))
		{
			_rest = nameStart;
			return Fail($"unsupported pseudo-class ':{name}()'");
		}

		if (++_depth > MaxDepth)
		{
			return Fail("pseudo-classes are nested too deeply");
		}

		SkipWhitespace();
		if (!TryReadPseudoClassArguments(pseudoClass, out selector))
		{
			return false;
		}

		_depth--;
		SkipWhitespace();
		if (!TrySkip(')'))
		{
			selector = null;
			return Fail("expected ')'");
		}

		return true;
	}

	/// <summary>
	/// Reads the arguments of a functional pseudo-class up to its <c>)</c>.
	/// </summary>
	private bool TryReadPseudoClassArguments(FunctionalPseudoClass pseudoClass, [NotNullWhen(true)] out SimpleSelector? selector)
	{
		selector = null;
		switch (pseudoClass)
		{
			case FunctionalPseudoClass.Not:
				if (!TryReadSelectorList(relative: false, out var negated))
				{
					return false;
				}

				selector = new NotSelector(Selector: negated);
				return true;

			case FunctionalPseudoClass.Has:
				if (!TryReadSelectorList(relative: true, out var relative))
				{
					return false;
				}

				selector = new HasSelector(Selector: relative);
				return true;

			case FunctionalPseudoClass.NthChild:
				return TryReadNthChild(fromEnd: false, out selector);

			case FunctionalPseudoClass.NthLastChild:
				return TryReadNthChild(fromEnd: true, out selector);

			default:
				throw new InvalidOperationException($"Unknown pseudo-class '{pseudoClass}'.");
		}
	}

	/// <summary>
	/// Reads the arguments of <c>:nth-child()</c> and <c>:nth-last-child()</c>: An+B, optionally followed by <c>of</c> and a selector list.
	/// </summary>
	private bool TryReadNthChild(bool fromEnd, [NotNullWhen(true)] out SimpleSelector? selector)
	{
		selector = null;
		if (!TryReadAnPlusB(out var a, out var b))
		{
			return false;
		}

		ElementSelector? of = null;
		SkipWhitespace();
		if (TryReadKeyword("of"))
		{
			if (!TryReadSelectorList(relative: false, out of))
			{
				return false;
			}
		}

		// the well-known variations are shared, however they are written, like odd or 2n+1
		selector = (a, b, fromEnd, of) switch
		{
			(0, 1, false, null) => NthChildSelector.First,
			(0, 1, true, null) => NthChildSelector.Last,
			(2, 1, false, null) => NthChildSelector.Odd,
			(2, 0, false, null) => NthChildSelector.Even,
			_ => new NthChildSelector(A: a, B: b, FromEnd: fromEnd, Of: of),
		};
		return true;
	}

	/// <summary>
	/// Reads An+B: <c>odd</c>, <c>even</c>, an integer, or a step with <c>n</c> and an optional offset, like <c>2n+1</c>, <c>-n + 3</c> or <c>n</c>.
	/// The sign of the step must be right before it, while the sign of the offset may have whitespace on either side.
	/// </summary>
	private bool TryReadAnPlusB(out int a, out int b)
	{
		a = 0;
		b = 0;
		if (TryReadKeyword("odd"))
		{
			a = 2;
			b = 1;
			return true;
		}

		if (TryReadKeyword("even"))
		{
			a = 2;
			return true;
		}

		var sign = _rest is ['-', ..] ? -1 : 1;
		if (_rest is ['-' or '+', ..])
		{
			_rest = _rest[1..];
		}

		if (!TryReadDigits(out var digits))
		{
			return false;
		}

		// without n, the number is the offset alone
		if (!TrySkip('n') && !TrySkip('N'))
		{
			if (digits is not { } integer)
			{
				return Fail("expected An+B");
			}

			b = sign * integer;
			return IsEndOfNumber();
		}

		a = sign * (digits ?? 1);
		if (_rest is [not '-' and var next, ..] && IsIdentifierCharacter(next))
		{
			return Fail("expected An+B");
		}

		SkipWhitespace();
		if (_rest is not [('+' or '-') and var offsetSign, ..])
		{
			return true;
		}

		_rest = _rest[1..];
		SkipWhitespace();
		if (!TryReadDigits(out var offsetDigits))
		{
			return false;
		}

		if (offsetDigits is not { } offset)
		{
			return Fail("expected the offset of An+B");
		}

		b = offsetSign is '-' ? -offset : offset;
		return IsEndOfNumber();
	}

	/// <summary>
	/// Reads decimal digits as a non-negative integer, or null if there are none; false if the number does not fit an <see cref="int"/>.
	/// </summary>
	private bool TryReadDigits(out int? value)
	{
		value = null;
		var length = LengthUntil(_rest, _rest.IndexOfAnyExceptInRange('0', '9'));
		if (length is 0)
		{
			return true;
		}

		if (!int.TryParse(_rest[..length], NumberStyles.None, CultureInfo.InvariantCulture, out var parsed))
		{
			return Fail("number is too large");
		}

		_rest = _rest[length..];
		value = parsed;
		return true;
	}

	/// <summary>
	/// Checks that a number is not followed by identifier characters, which would make it something else, like <c>3px</c>.
	/// </summary>
	private bool IsEndOfNumber() => _rest is not [var next, ..] || !IsIdentifierCharacter(next) || Fail("expected An+B");

	/// <summary>
	/// Reads a keyword, compared case-insensitively, unless it is only the start of a longer identifier.
	/// </summary>
	private bool TryReadKeyword(string keyword)
	{
		if (!_rest.StartsWith(keyword, StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		if (_rest[keyword.Length..] is [var next, ..] && IsIdentifierCharacter(next))
		{
			return false;
		}

		_rest = _rest[keyword.Length..];
		return true;
	}

	/// <summary>
	/// Reads a CSS identifier, decoding escapes. As written, it must not start with a digit, or with a <c>-</c> followed by a digit or nothing.
	/// </summary>
	private bool TryReadIdentifier(out ReadOnlySpan<char> identifier)
	{
		var start = _rest;
		var length = LengthUntil(_rest, _rest.IndexOfAny(SelectorParser.IdentifierTerminators));
		if (_rest[length..] is ['\\', ..])
		{
			// escapes are rare, so only then is the identifier built as a new string
			if (!TryReadEscapedIdentifier(out var decoded))
			{
				identifier = default;
				return false;
			}

			identifier = decoded;
		}
		else
		{
			identifier = _rest[..length];
			_rest = _rest[length..];
		}

		// an escaped digit is not a digit here, so the identifier is checked as written
		var written = start[..(start.Length - _rest.Length)];
		if (written is [] or ['-'] or [>= '0' and <= '9', ..] or ['-', >= '0' and <= '9', ..])
		{
			_rest = start;
			identifier = default;
			return Fail("expected an identifier");
		}

		return true;
	}

	/// <summary>
	/// Reads an identifier that contains escapes into a new string.
	/// </summary>
	private bool TryReadEscapedIdentifier([NotNullWhen(true)] out string? identifier)
	{
		identifier = null;
		using var pooled = StringBuilderPool.GetPooledObject(out var builder);
		while (true)
		{
			var length = LengthUntil(_rest, _rest.IndexOfAny(SelectorParser.IdentifierTerminators));
			builder.Append(_rest[..length]);
			_rest = _rest[length..];
			if (_rest is not ['\\', ..])
			{
				break;
			}

			if (!TryReadEscape(builder))
			{
				return false;
			}
		}

		identifier = builder.ToString();
		return true;
	}

	/// <summary>
	/// Reads a quoted string, decoding escapes. An escaped newline continues the string, and an unescaped one is not allowed.
	/// </summary>
	private bool TryReadString(out ReadOnlySpan<char> value)
	{
		value = default;
		var quote = _rest[0];
		var terminators = quote is '"' ? SelectorParser.DoubleQuotedValueTerminators : SelectorParser.SingleQuotedValueTerminators;
		_rest = _rest[1..];

		// strings without escapes are the common case, and they are sliced from the text
		var length = _rest.IndexOfAny(terminators);
		if (length >= 0 && _rest[length] == quote)
		{
			value = _rest[..length];
			_rest = _rest[(length + 1)..];
			return true;
		}

		using var pooled = StringBuilderPool.GetPooledObject(out var builder);
		while (true)
		{
			length = _rest.IndexOfAny(terminators);
			if (length < 0)
			{
				return Fail("unterminated string");
			}

			builder.Append(_rest[..length]);
			_rest = _rest[length..];
			if (_rest[0] == quote)
			{
				_rest = _rest[1..];
				value = builder.ToString();
				return true;
			}

			switch (_rest)
			{
				case ['\\', '\r', '\n', ..]:
					_rest = _rest[3..];
					break;

				case ['\\', '\n' or '\r' or '\f', ..]:
					_rest = _rest[2..];
					break;

				case ['\\', ..]:
					if (!TryReadEscape(builder))
					{
						return false;
					}

					break;

				default:
					return Fail("newline in string");
			}
		}
	}

	/// <summary>
	/// Reads an escape: a backslash and up to 6 hexadecimal digits of a code point, with a whitespace after them that only ends the escape,
	/// or a backslash and any other character, which stands for itself.
	/// </summary>
	private bool TryReadEscape(StringBuilder builder)
	{
		_rest = _rest[1..];
		if (_rest is ['\n' or '\r' or '\f', ..])
		{
			return Fail("invalid escape");
		}

		if (_rest.IsEmpty)
		{
			builder.Append('�');
			return true;
		}

		var digits = Math.Min(LengthUntil(_rest, _rest.IndexOfAnyExcept(HexDigits)), 6);
		if (digits is 0)
		{
			builder.Append(_rest[0]);
			_rest = _rest[1..];
			return true;
		}

		var codePoint = int.Parse(_rest[..digits], NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
		_rest = _rest[digits..] switch
		{
			['\r', '\n', ..] => _rest[(digits + 2)..],
			[' ' or '\t' or '\n' or '\r' or '\f', ..] => _rest[(digits + 1)..],
			_ => _rest[digits..],
		};

		// zero, surrogates and values beyond Unicode stand for the replacement character, as in CSS
		var rune = codePoint is not 0 && Rune.IsValid(codePoint) ? new Rune(codePoint) : Rune.ReplacementChar;
		Span<char> buffer = stackalloc char[2];
		builder.Append(buffer[..rune.EncodeToUtf16(buffer)]);
		return true;
	}

	/// <summary>
	/// Checks whether the character can be part of an identifier: a letter, a digit, <c>-</c>, <c>_</c>, a non-ASCII character, or the backslash of an escape.
	/// </summary>
	private static bool IsIdentifierCharacter(char c) => c is '\\' || !SelectorParser.IdentifierTerminators.Contains(c);

	/// <summary>
	/// Turns the result of an <c>IndexOf</c> search in the text into the length of the text before the match, which is all of it without one.
	/// </summary>
	private static int LengthUntil(ReadOnlySpan<char> text, int index) => index < 0 ? text.Length : index;

	/// <summary>
	/// Skips whitespace and returns whether there was any.
	/// </summary>
	private bool SkipWhitespace()
	{
		var length = LengthUntil(_rest, _rest.IndexOfAnyExcept(SyntaxFacts.Whitespace));
		_rest = _rest[length..];
		return length > 0;
	}

	/// <summary>
	/// Skips the character if it is the next one.
	/// </summary>
	private bool TrySkip(char c)
	{
		if (_rest is [var next, ..] && next == c)
		{
			_rest = _rest[1..];
			return true;
		}

		return false;
	}

	/// <summary>
	/// Records the first error and where it happened, and returns false, so that a failure can be returned directly.
	/// </summary>
	private bool Fail(string message)
	{
		if (_error is null)
		{
			_error = message;
			_errorPosition = Position;
		}

		return false;
	}

	/// <summary>
	/// The pseudo-classes with arguments, which <see cref="TryReadPseudoClassArguments"/> reads each in its own way.
	/// </summary>
	private enum FunctionalPseudoClass
	{
		/// <summary>
		/// <c>:not()</c>, with a selector list.
		/// </summary>
		Not,

		/// <summary>
		/// <c>:has()</c>, with a relative selector list.
		/// </summary>
		Has,

		/// <summary>
		/// <c>:nth-child()</c>, with An+B and an optional selector list after <c>of</c>.
		/// </summary>
		NthChild,

		/// <summary>
		/// <c>:nth-last-child()</c>, with the same arguments as <c>:nth-child()</c>.
		/// </summary>
		NthLastChild,
	}
}
