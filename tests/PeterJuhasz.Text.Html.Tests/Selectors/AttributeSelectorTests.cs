using PeterJuhasz.Text.Html.Selectors;
using static PeterJuhasz.Text.Html.Tests.Selectors.TestHelpers;

namespace PeterJuhasz.Text.Html.Tests.Selectors;

[TestClass]
public sealed class AttributeSelectorTests : AttributeNameTests
{
	private protected override string Write(string name) => $"[{name}]";

	private protected override SimpleSelector Create(string name) => new AttributeSelector(Name: name);

	[TestMethod]
	public void SelectsElementsWithTheAttributeWithOrWithoutAValue()
		=> AssertSelects("<input id=a disabled><input id=b disabled=''><input id=c disabled=disabled><input id=d>", "[disabled]", "a", "b", "c");

	[TestMethod]
	public void DoesNotSelectAttributesWhoseNamesOnlyStartTheSame()
		=> AssertSelects("<p id=a data-x></p><p id=b data-xy></p>", "[data-x]", "a");

	[TestMethod]
	public void RejectsAFlagWithoutAValue()
		=> AssertInvalid("[a i]", "expected ']' or an attribute operator");

	[TestMethod]
	public void IsWrittenBack()
		=> AssertPrints("[ href ]", "[href]");
}
