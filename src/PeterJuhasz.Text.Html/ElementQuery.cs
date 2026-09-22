using Microsoft.Extensions.Primitives;

namespace PeterJuhasz.Text.Html;

// Argument validation and matching rules shared by the lazy and model query implementations.
internal static class ElementQuery
{
	public static void ValidateArguments(string? element, StringValues classNames, ReadOnlySpan<KeyValuePair<string, string>> attributes)
	{
		ValidateElement(element);

		foreach (var className in classNames)
			ValidateClassName(className, nameof(classNames));

		ValidateAttributes(attributes);
	}

	// Validates the class names of a GetElementsByClassName call, which must name at least one class.
	public static void ValidateClassNames(StringValues classNames)
	{
		if (classNames.Count == 0)
			throw new ArgumentException("At least one class name is required.", nameof(classNames));

		foreach (var className in classNames)
			ValidateClassName(className, nameof(classNames));
	}

	private static void ValidateElement(string? element)
	{
		if (element is { Length: 0 })
			throw new ArgumentException("The element name must not be empty.", nameof(element));
	}

	// Each entry must be exactly one class token, so a whitespace-separated list is rejected instead of silently split.
	private static void ValidateClassName(string? className, string paramName)
	{
		if (string.IsNullOrEmpty(className) || className.AsSpan().ContainsAny(SyntaxFacts.Whitespace))
			throw new ArgumentException("Each class name must be a single, non-empty class name.", paramName);
	}

	private static void ValidateAttributes(ReadOnlySpan<KeyValuePair<string, string>> attributes)
	{
		foreach (var attribute in attributes)
			ArgumentException.ThrowIfNullOrEmpty(attribute.Key, nameof(attributes));
	}

	// Checks whether the whitespace-separated class list contains every one of the class names.
	public static bool HasClasses(ReadOnlySpan<char> classes, StringValues classNames)
	{
		foreach (var className in classNames)
		{
			if (!HasClass(classes, className))
				return false;
		}

		return true;
	}

	// Checks whether the whitespace-separated class list contains the class name.
	public static bool HasClass(ReadOnlySpan<char> classes, ReadOnlySpan<char> className)
	{
		foreach (var range in classes.SplitAny(SyntaxFacts.Whitespace))
		{
			if (classes[range].SequenceEqual(className))
				return true;
		}

		return false;
	}
}
