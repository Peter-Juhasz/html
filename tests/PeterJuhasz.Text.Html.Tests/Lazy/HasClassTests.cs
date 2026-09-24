using Microsoft.Extensions.Primitives;

namespace PeterJuhasz.Text.Html.Tests.Lazy;

[TestClass]
public sealed class HasClassTests
{
	[TestMethod]
	[DataRow("target", "target", true)]
	[DataRow("target other", "target", true)]
	[DataRow("first target last", "target", true)]
	[DataRow("other target", "target", true)]
	[DataRow("target target", "target", true)]
	[DataRow("target-extra", "target", false)]
	[DataRow("extra-target", "target", false)]
	[DataRow("pretargetsuffix", "target", false)]
	[DataRow("target", "target-extra", false)]
	[DataRow("other", "target", false)]
	[DataRow("Target", "target", false)]
	[DataRow("target", "Target", false)]
	public void MatchesOnlyCompleteCaseSensitiveTokens(string classes, string className, bool expected)
	{
		var element = TestHelpers.FirstElement($"<div class=\"{classes}\"></div>");

		Assert.AreEqual(expected, element.HasClass(className));
	}

	[TestMethod]
	[DataRow(" ")]
	[DataRow("\t")]
	[DataRow("\n")]
	[DataRow("\r")]
	[DataRow("\f")]
	public void SplitsOnHtmlWhitespaceIncludingRepeatedAndSurroundingSeparators(string separator)
	{
		var element = TestHelpers.FirstElement($"<div class=\"{separator}first{separator}{separator}target{separator}last{separator}\"></div>");

		Assert.IsTrue(element.HasClass("first"));
		Assert.IsTrue(element.HasClass("target"));
		Assert.IsTrue(element.HasClass("last"));
	}

	[TestMethod]
	[DataRow("\v")]
	[DataRow("\u00a0")]
	[DataRow("\u2003")]
	public void NonHtmlWhitespaceIsPartOfTheClassName(string separator)
	{
		var className = $"first{separator}target";
		var element = TestHelpers.FirstElement($"<div class=\"{className}\"></div>");

		Assert.IsTrue(element.HasClass(className));
		Assert.IsFalse(element.HasClass("first"));
		Assert.IsFalse(element.HasClass("target"));
	}

	[TestMethod]
	[DataRow("<div></div>")]
	[DataRow("<div id=\"target\" data-class=\"target\"></div>")]
	[DataRow("<div class></div>")]
	[DataRow("<div class=\"\"></div>")]
	[DataRow("<div class=\" \t\n\r\f \"></div>")]
	public void ReturnsFalseWhenThereAreNoClasses(string html)
	{
		var element = TestHelpers.FirstElement(html);

		Assert.IsFalse(element.HasClass("target"));
	}

	[TestMethod]
	[DataRow("<div CLASS=\"target\"></div>")]
	[DataRow("<div ClAsS='target'></div>")]
	[DataRow("<div id=x class=target title=y></div>")]
	public void SupportsCaseInsensitiveAttributeNamesAndDifferentQuotingStyles(string html)
	{
		var element = TestHelpers.FirstElement(html);

		Assert.IsTrue(element.HasClass("target"));
	}

	[TestMethod]
	[DataRow("<div class=\"target\" class=\"other\"></div>", true)]
	[DataRow("<div class=\"other\" class=\"target\"></div>", false)]
	[DataRow("<div class=\"\" class=\"target\"></div>", false)]
	public void FirstDuplicateClassAttributeWins(string html, bool expected)
	{
		var element = TestHelpers.FirstElement(html);

		Assert.AreEqual(expected, element.HasClass("target"));
	}

	[TestMethod]
	public void DoesNotMatchClassesOnDescendants()
	{
		var element = TestHelpers.FirstElement("<div class=\"other\"><span class=\"target\"></span></div>");

		Assert.IsTrue(element.HasClass("other"));
		Assert.IsFalse(element.HasClass("target"));
	}

	[TestMethod]
	public void DecodesCharacterReferencesInTheAttributeValue()
	{
		var element = TestHelpers.FirstElement("<div class=\"a&amp;b &#116;arget &#x63;\"></div>");

		Assert.IsTrue(element.HasClass("a&b"));
		Assert.IsTrue(element.HasClass("target"));
		Assert.IsTrue(element.HasClass("c"));
		Assert.IsFalse(element.HasClass("a&amp;b"));
		Assert.IsFalse(element.HasClass("&#116;arget"));
		Assert.IsFalse(element.HasClass("&#x63;"));
	}

	[TestMethod]
	public void DecodesCharacterReferencesOnlyOnce()
	{
		var element = TestHelpers.FirstElement("<div class=\"a&amp;amp;b\"></div>");

		Assert.IsTrue(element.HasClass("a&amp;b"));
		Assert.IsFalse(element.HasClass("a&b"));
	}

	[TestMethod]
	[DataRow("&#32;")]
	[DataRow("&#9;")]
	[DataRow("&#10;")]
	[DataRow("&#13;")]
	[DataRow("&#12;")]
	public void EncodedWhitespaceSeparatesClasses(string separator)
	{
		var element = TestHelpers.FirstElement($"<div class=\"first{separator}target\"></div>");

		Assert.IsTrue(element.HasClass("first"));
		Assert.IsTrue(element.HasClass("target"));
		Assert.IsFalse(element.HasClass($"first{separator}target"));
	}

	[TestMethod]
	public void EncodedNonHtmlWhitespaceIsPartOfTheClassName()
	{
		var element = TestHelpers.FirstElement("<div class=\"first&nbsp;target\"></div>");

		Assert.IsTrue(element.HasClass("first\u00a0target"));
		Assert.IsFalse(element.HasClass("first"));
		Assert.IsFalse(element.HasClass("target"));
	}

	[TestMethod]
	[DataRow("first target last", true)]
	[DataRow("last first target", true)]
	[DataRow("first target", false)]
	[DataRow("first last", false)]
	[DataRow("", false)]
	public void MultipleClassNamesMustAllBePresentInAnyOrder(string classes, bool expected)
	{
		var element = TestHelpers.FirstElement($"<div class=\"{classes}\"></div>");

		Assert.AreEqual(expected, element.HasClass(new StringValues(["first", "target", "last"])));
		Assert.AreEqual(expected, element.HasClass(new StringValues(["last", "target", "first"])));
	}

	[TestMethod]
	public void MultipleClassNamesMatchRepeatedTokensAndDuplicateNames()
	{
		var element = TestHelpers.FirstElement("<div class=\"target target\"></div>");

		Assert.IsTrue(element.HasClass(new StringValues(["target", "target"])));
		Assert.IsFalse(element.HasClass(new StringValues(["target", "other"])));
	}

	[TestMethod]
	public void MultipleClassNamesAreDecodedFromTheAttributeValue()
	{
		var element = TestHelpers.FirstElement("<div class=\"a&amp;b&#32;&#116;arget\"></div>");

		Assert.IsTrue(element.HasClass(new StringValues(["a&b", "target"])));
		Assert.IsFalse(element.HasClass(new StringValues(["a&amp;b", "target"])));
		Assert.IsFalse(element.HasClass(new StringValues(["a&b&#32;&#116;arget"])));
	}

	[TestMethod]
	public void ManyClassNamesAreMatchedWithoutStackAllocation()
	{
		var names = Enumerable.Range(0, 40).Select(i => $"c{i}").ToArray();
		var element = TestHelpers.FirstElement($"<div class=\"{string.Join(' ', names.Reverse())}\"></div>");

		Assert.IsTrue(element.HasClass(new StringValues(names)));
		Assert.IsFalse(element.HasClass(new StringValues([.. names, "missing"])));
	}

	[TestMethod]
	[DataRow(" target")]
	[DataRow("target ")]
	[DataRow("target other")]
	[DataRow(" ")]
	[DataRow("\t")]
	public void DoesNotTrimOrSplitTheRequestedClassName(string className)
	{
		var element = TestHelpers.FirstElement("<div class=\"target other\"></div>");

		Assert.IsFalse(element.HasClass(className));
	}

	[TestMethod]
	[DataRow("<div></div>")]
	[DataRow("<div class=\"target\"></div>")]
	public void RejectsNullOrEmptyClassNamesEvenWithoutAClassAttribute(string html)
	{
		var element = TestHelpers.FirstElement(html);

		Assert.AreEqual("className", Assert.ThrowsExactly<ArgumentException>(() => element.HasClass((string)null!)).ParamName);
		Assert.AreEqual("className", Assert.ThrowsExactly<ArgumentException>(() => element.HasClass("")).ParamName);
	}
}
