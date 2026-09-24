using System.Runtime.CompilerServices;

namespace PeterJuhasz.Text.Html;

// https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Text/UnicodeUtility.cs

public static partial class HtmlDecoder
{
	private static class UnicodeUtility
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void GetUtf16SurrogatesFromSupplementaryPlaneScalar(uint value, out char highSurrogateCodePoint, out char lowSurrogateCodePoint)
		{
			// This calculation comes from the Unicode specification, Table 3-5.

			highSurrogateCodePoint = (char)((value + ((0xD800u - 0x40u) << 10)) >> 10);
			lowSurrogateCodePoint = (char)((value & 0x3FFu) + 0xDC00u);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsValidUnicodeScalar(uint value)
		{
			// This is an optimized check that on x86 is just three instructions: lea, xor, cmp.
			//
			// After the subtraction operation, the input value is modified as such:
			// [ 00000000..0010FFFF ] -> [ FFEF0000..FFFFFFFF ]
			//
			// We now want to _exclude_ the range [ FFEFD800..FFEFDFFF ] (surrogates) from being valid.
			// After the xor, this particular exclusion range becomes [ FFEF0000..FFEF07FF ].
			//
			// So now the range [ FFEF0800..FFFFFFFF ] contains all valid code points,
			// excluding surrogates. This allows us to perform a single comparison.

			return ((value - 0x110000u) ^ 0xD800u) >= 0xFFEF0800u;
		}

		/// <summary>
		/// Returns <see langword="true"/> iff <paramref name="value"/> is in the
		/// Basic Multilingual Plane (BMP).
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsBmpCodePoint(uint value) => value <= 0xFFFFu;
	}
}
