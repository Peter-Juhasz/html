using Microsoft.Extensions.Primitives;

namespace PeterJuhasz.Text.Html.Tests.Model;

[TestClass]
public sealed class ClassesTests
{
	[TestMethod]
	[DataRow("<div></div>")]
	[DataRow("<div class></div>")]
	[DataRow("<div class=\"\"></div>")]
	public void ReturnsEmptyWithoutAClassValue(string html)
	{
		var element = TestHelpers.FirstElement(html);

		Assert.AreEqual(0, element.Classes().Count);
	}

	[TestMethod]
	public void ReturnsASingleClass()
	{
		var element = TestHelpers.FirstElement("<div class=\"target\"></div>");

		Assert.AreSequenceEqual<string?>(["target"], element.Classes());
	}

	[TestMethod]
	public void SharesTheMaterializedValueOfASingleClass()
	{
		var element = TestHelpers.FirstElement("<div class=\"target\"></div>");
		var value = element.GetAttribute("class")!.Value;

		var classes = element.Classes();

		Assert.AreEqual(1, classes.Count);
		Assert.AreSame(value, classes[0]);
	}

	[TestMethod]
	public void DoesNotShareTheMaterializedValueWhenItWasDecoded()
	{
		var element = TestHelpers.FirstElement("<div class=\"a&amp;b\"></div>");
		var value = element.GetAttribute("class")!.Value;

		var classes = element.Classes();

		Assert.AreEqual("a&b", value);
		Assert.AreSequenceEqual<string?>(["a&b"], classes);
	}

	[TestMethod]
	[DataRow("first target last", new[] { "first", "target", "last" })]
	[DataRow("first\ttarget\nlast", new[] { "first", "target", "last" })]
	[DataRow("first  last", new[] { "first", "last" })]
	[DataRow(" first", new[] { "first" })]
	[DataRow("first ", new[] { "first" })]
	[DataRow(" ", new string[0])]
	public void SplitsOnHtmlWhitespace(string classes, string[] expected)
	{
		var element = TestHelpers.FirstElement($"<div class=\"{classes}\"></div>");

		Assert.AreSequenceEqual<string?>(expected, element.Classes());
	}

	[TestMethod]
	public void SplitsTheSameWhetherOrNotTheValueIsMaterialized()
	{
		var element = TestHelpers.FirstElement("<div class=\"first target\"></div>");
		var before = element.Classes();

		_ = element.GetAttribute("class")!.Value;

		Assert.AreEqual(before, element.Classes());
	}
}
