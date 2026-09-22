namespace PeterJuhasz.Text.Html;

// Argument validation and matching rules shared by the lazy and model query implementations.
internal static class ElementQuery
{
	public static void ValidateArguments(string? element, string? className, ReadOnlySpan<KeyValuePair<string, string>> attributes)
	{
		if (element is { Length: 0 })
			throw new ArgumentException("The element name must not be empty.", nameof(element));

		if (className is { Length: 0 } || (className is not null && className.AsSpan().ContainsAny(SyntaxFacts.Whitespace)))
			throw new ArgumentException("The class name must be a single, non-empty class name.", nameof(className));

		foreach (var attribute in attributes)
			ArgumentException.ThrowIfNullOrEmpty(attribute.Key, nameof(attributes));
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
