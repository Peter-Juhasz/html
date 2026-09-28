namespace PeterJuhasz.Text.Html.Selectors;

/// <summary>
/// A selector that checks a single property of an element, like its name, a class or an attribute; the parts a <see cref="CompoundSelector"/> is made of.
/// </summary>
internal abstract record class SimpleSelector : ElementSelector;
