using PeterJuhasz.Text.Html.Selectors;

namespace PeterJuhasz.Text.Html.Tests.Selectors;

[TestClass]
public sealed class DisabledSelectorTests : KeywordPseudoClassTests
{
	private protected override string Keyword => "disabled";

	private protected override SimpleSelector Instance => DisabledSelector.Instance;

	private protected override string Document =>
		"<form id=f>" +
			"<button id=b1 disabled></button><button id=b2></button>" +
			"<input id=i1 disabled><input id=i2>" +
			"<select id=s1 disabled><option id=o1>a</option></select>" +
			"<textarea id=t1 disabled></textarea>" +
			"<fieldset id=fs1 disabled>" +
				"<legend id=lg1><input id=i3></legend>" +
				"<legend id=lg2><input id=i4></legend>" +
				"<input id=i5>" +
				"<fieldset id=fs2><input id=i6></fieldset>" +
			"</fieldset>" +
			"<select id=s2><optgroup id=og1 disabled><option id=o2>b</option></optgroup><option id=o3 disabled>c</option><option id=o4>d</option></select>" +
			"<a id=a1 disabled></a>" +
		"</form>";

	/// <summary>
	/// The controls disabled by their own attribute, by a disabled fieldset (except in its first legend), or by a disabled optgroup.
	/// </summary>
	private protected override string[] Selected => ["b1", "i1", "s1", "t1", "fs1", "i4", "i5", "fs2", "i6", "og1", "o2", "o3"];
}
