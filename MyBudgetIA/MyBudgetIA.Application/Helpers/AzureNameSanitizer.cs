using System.Text;
using System.Text.RegularExpressions;

namespace MyBudgetIA.Application.Helpers
{
    /// <summary>
    /// Provides utility methods for sanitizing strings to conform to Azure resource naming requirements.
    /// </summary>
    /// <remarks>This class is intended for internal use to ensure that resource names are valid for Azure
    /// services. It removes or replaces invalid characters and normalizes whitespace and dashes according to common
    /// Azure naming conventions. The class is static and cannot be instantiated.</remarks>
    internal static partial class AzureNameSanitizer
    {
        public const int MaxLenght = 100;

        [GeneratedRegex(@"[^a-zA-Z0-9._\-/]")]
        private static partial Regex InvalidCharactersRegex();

        [GeneratedRegex(@"/+")]
        private static partial Regex MultipleSlashesRegex();

        [GeneratedRegex(@"\.{2,}")]
        private static partial Regex MultipleDotsRegex();

        [GeneratedRegex(@"_{2,}")]
        private static partial Regex MultipleUnderscoresRegex();

        /// <summary>
        /// Sanitizes a string for use as an Azure Blob Storage name by removing or replacing invalid characters and
        /// enforcing length constraints.
        /// </summary>
        /// <remarks>The sanitized name contains only alphanumeric characters, dashes, and underscores,
        /// and does not begin or end with a slash or period. If the input has a file extension, the method attempts to
        /// preserve it when truncating to the maximum length. This method is intended to help ensure compatibility with
        /// Azure Blob Storage naming rules.</remarks>
        /// <param name="input">The input string to sanitize for use as a blob name. Can include any characters.</param>
        /// <param name="maxLength">The maximum allowed length of the resulting blob name. Must be a positive integer. Defaults to the value of
        /// MaxLenght.</param>
        /// <param name="defaultName">The name to use if the input is null, empty, or results in an empty string after sanitization. Defaults to
        /// "unnamed_file".</param>
        /// <returns>A sanitized string suitable for use as an Azure Blob Storage name. If the input is null, empty, or results
        /// in an empty string after sanitization, returns the value of defaultName.</returns>
        public static string SanitizeBlobName(
            string input,
            int maxLength = MaxLenght,
            string defaultName = "unnamed_file")
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return defaultName;
            }

            string sanitized = input;

            sanitized = RemoveDiacritics(sanitized);

            sanitized = InvalidCharactersRegex().Replace(sanitized, "_");

            sanitized = MultipleSlashesRegex().Replace(sanitized, "/");

            sanitized = sanitized.Trim('/');

            sanitized = MultipleDotsRegex().Replace(sanitized, ".");

            sanitized = MultipleUnderscoresRegex().Replace(sanitized, "_");

            sanitized = sanitized.Trim('.');

            // After collapsing dots, we may have introduced leading/trailing slashes again (e.g. "./_")
            sanitized = sanitized.Trim('/');

            // If the sanitized name has no letters/digits, consider it empty and fallback to default.
            if (!sanitized.Any(char.IsLetterOrDigit))
            {
                return defaultName;
            }

            if (sanitized.Length > maxLength)
            {
                string extension = Path.GetExtension(sanitized);

                if (!string.IsNullOrEmpty(extension))
                {
                    int nameLength = maxLength - extension.Length;

                    if (nameLength > 0)
                    {
                        sanitized = string.Concat(sanitized.AsSpan(0, nameLength), extension);
                    }
                    else
                    {
                        sanitized = sanitized[..maxLength];
                    }
                }
                else
                {
                    sanitized = sanitized[..maxLength];
                }
            }

            if (string.IsNullOrWhiteSpace(sanitized))
            {
                sanitized = defaultName;
            }

            return sanitized;
        }

        /// <summary>
        /// Removes all diacritical marks from the specified string, returning a new string containing only base
        /// characters.
        /// </summary>
        /// <remarks>This method normalizes the input string to decompose characters with diacritics and
        /// then removes non-spacing marks. The resulting string is re-normalized to compose base characters. This is
        /// useful for text processing scenarios where diacritics should be ignored, such as search or comparison
        /// operations.</remarks>
        /// <param name="text">The input string from which diacritical marks will be removed. Cannot be null.</param>
        /// <returns>A new string with all diacritical marks removed from the input. If the input contains no diacritics, the
        /// original string is returned unchanged.</returns>
        public static string RemoveDiacritics(string text)
        {
            var specialCases = new Dictionary<char, string>
            {
                // German
                { 'ß', "ss"}, { 'ẞ', "SS"},
                
                // Nordish
                { 'Æ', "AE"}, { 'æ', "ae"},
                { 'Ø', "O"}, { 'ø', "o"},
                { 'Å', "A"}, { 'å', "a"},
                
                // Slavish
                { 'Č', "C"}, { 'č', "c"},
                { 'Š', "S"}, { 'š', "s"},
                { 'Ł', "L"}, { 'ł', "l"},
                
                
                { 'Ð', "D"}, { 'ð', "d"},
                { 'Þ', "TH"}, { 'þ', "th"},
                { 'Œ', "OE"}, { 'œ', "oe"},
                { 'Ĳ', "IJ"}, { 'ĳ', "ij"},
            };

            var stringBuilder = new StringBuilder();

            foreach (var c in text)
            {
                // check if special case
                if (specialCases.TryGetValue(c, out var replacement) && replacement is not null)
                {
                    stringBuilder.Append(replacement);
                }
                else
                {
                    // otherwise remove diacritics normally
                    var normalized = c.ToString().Normalize(NormalizationForm.FormD);
                    foreach (var nc in normalized)
                    {
                        var unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(nc);
                        if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
                        {
                            stringBuilder.Append(nc);
                        }
                    }
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
