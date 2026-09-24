using Microsoft.Extensions.Primitives;
using System.Diagnostics.CodeAnalysis;

namespace PeterJuhasz.Text.Html;

// Argument validation and matching rules shared by the lazy and model query implementations.
[PerformanceCritical]
internal static class ElementQuery
{
	public static void ValidateArguments(string? element, StringValues classNames, ReadOnlySpan<KeyValuePair<string, string>> attributes)
	{
		ValidateElement(element);
		ValidateArguments(classNames, attributes);
	}

	// Validates the filters other than the element name, for callers where an empty element name means any element.
	public static void ValidateArguments(StringValues classNames, ReadOnlySpan<KeyValuePair<string, string>> attributes)
	{
		foreach (var className in classNames)
		{
			ValidateClassName(className, nameof(classNames));
		}

		ValidateAttributes(attributes);
	}

	// Validates the class names of a GetElementsByClassName call, which must name at least one class.
	public static void ValidateClassNames(StringValues classNames)
	{
		if (classNames.Count == 0)
		{
			throw new ArgumentException("At least one class name is required.", nameof(classNames));
		}

		foreach (var className in classNames)
		{
			ValidateClassName(className, nameof(classNames));
		}
	}

	private static void ValidateElement(string? element)
	{
		if (element is { Length: 0 })
		{
			throw new ArgumentException("The element name must not be empty.", nameof(element));
		}
	}

	// Each entry must be exactly one class token, so a whitespace-separated list is rejected instead of silently split.
	private static void ValidateClassName(string? className, string paramName)
	{
		if (string.IsNullOrEmpty(className) || className.AsSpan().ContainsAny(SyntaxFacts.Whitespace))
		{
			throw new ArgumentException("Each class name must be a single, non-empty class name.", paramName);
		}
	}

	private static void ValidateAttributes(ReadOnlySpan<KeyValuePair<string, string>> attributes)
	{
		foreach (var attribute in attributes)
		{
			ArgumentException.ThrowIfNullOrEmpty(attribute.Key, nameof(attributes));
		}
	}

	// Checks whether the class attribute value, as written in the document, contains every one of the class names.
	// The value is decoded before splitting, because e.g. "&#32;" is a separator and "&amp;" is not.
	public static bool HasClasses(ReadOnlySpan<char> classes, StringValues classNames)
	{
		if (classNames.Count == 0)
		{
			return true;
		}

		if (!SyntaxFacts.NeedsDecoding(classes))
		{
			return HasDecodedClasses(classes, classNames);
		}

		var decodedBuffer = classes.Length < HtmlDecoder.StackAllocThreshold ? stackalloc char[classes.Length] : new char[classes.Length];
		HtmlDecoder.Decode(classes, decodedBuffer, out int charsWritten);
		return HasDecodedClasses(decodedBuffer[..charsWritten], classNames);
	}

	// Checks whether the class attribute value, as written in the document, contains the class name.
	public static bool HasClass(ReadOnlySpan<char> classes, ReadOnlySpan<char> className)
	{
		if (!SyntaxFacts.NeedsDecoding(classes))
		{
			return HasDecodedClass(classes, className);
		}

		var decodedBuffer = classes.Length < HtmlDecoder.StackAllocThreshold ? stackalloc char[classes.Length] : new char[classes.Length];
		HtmlDecoder.Decode(classes, decodedBuffer, out int charsWritten);
		return HasDecodedClass(decodedBuffer[..charsWritten], className);
	}

	// Checks whether the attribute value, as written in the document, equals the value.
	public static bool HasAttributeValue(ReadOnlySpan<char> attributeValue, ReadOnlySpan<char> value)
	{
		// decoding never makes the text longer, so a longer value cannot match and the decoding is skipped
		if (value.Length > attributeValue.Length)
		{
			return false;
		}

		if (!SyntaxFacts.NeedsDecoding(attributeValue))
		{
			return attributeValue.SequenceEqual(value);
		}

		var decodedBuffer = attributeValue.Length < HtmlDecoder.StackAllocThreshold ? stackalloc char[attributeValue.Length] : new char[attributeValue.Length];
		HtmlDecoder.Decode(attributeValue, decodedBuffer, out int charsWritten);
		return decodedBuffer[..charsWritten].SequenceEqual(value);
	}

	// Splits the whitespace-separated class list only once, ticking off each class name as its token is found.
	internal static bool HasDecodedClasses(ReadOnlySpan<char> classes, StringValues classNames)
	{
		var count = classNames.Count;
		if (count == 1)
		{
			return HasDecodedClass(classes, classNames[0]);
		}

		Span<bool> found = count <= 32 ? stackalloc bool[count] : new bool[count];
		var remaining = count;

		foreach (var range in classes.SplitAny(SyntaxFacts.Whitespace))
		{
			var token = classes[range];
			if (token.IsEmpty)
			{
				continue;
			}

			for (var i = 0; i < count; i++)
			{
				if (!found[i] && token.SequenceEqual(classNames[i]))
				{
					found[i] = true;
					if (--remaining == 0)
					{
						return true;
					}
				}
			}
		}

		return false;
	}

	internal static bool HasDecodedClass(ReadOnlySpan<char> classes, ReadOnlySpan<char> className)
	{
		foreach (var range in classes.SplitAny(SyntaxFacts.Whitespace))
		{
			if (classes[range].SequenceEqual(className))
			{
				return true;
			}
		}

		return false;
	}
}
