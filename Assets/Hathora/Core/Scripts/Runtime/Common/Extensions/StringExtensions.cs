// dylan@hathora.dev

using System.Text.RegularExpressions;

namespace Hathora.Core.Scripts.Runtime.Common.Extensions
{
    public static class StringExtensions
    {
        /// <summary>
        /// Eg: "fooBar" -> "Foo Bar".
        /// * For 2+ CAPS in a row, like "WashingtonDC", it will be "Washington DC".
        /// --------
        /// * Inserts a space before each uppercase letter that is either:
        /// * Followed by a lowercase letter, or
        /// * Preceded by a lowercase letter and not followed by an uppercase letter.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string SplitPascalCase(this string str) =>
            // ##############################################
            // "WashingtonDC" becomes "Washington DC"
            // "WashingtonDc" becomes "Washington Dc"
            // "WashingtonDCFoo" becomes "Washington DC Foo"
            // ##############################################
            Regex.Replace(
                str,
                @"(?<=\p{Ll})(?=\p{Lu})|(?<=\p{Lu})(?=\p{Lu}\p{Ll})",
                " ");
    }
}
