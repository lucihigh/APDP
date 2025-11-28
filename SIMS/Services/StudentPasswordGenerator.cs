using System.Globalization;
using System.Text;

namespace SIMS.Services;

public static class StudentPasswordGenerator
{
    public static string Generate(string firstName, string lastName, DateOnly dateOfBirth)
    {
        var fullName = $"{firstName} {lastName}".Trim();
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return $"Student#{Guid.NewGuid().ToString("N")[..8]}";
        }

        static string RemoveDiacritics(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            var normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(text.Length);

            foreach (var c in normalized)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(c);
                if (category != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }

            // Normalize again and map characters that do not decompose,
            // for example Vietnamese Đ/đ.
            var result = sb.ToString().Normalize(NormalizationForm.FormC);
            result = result
                .Replace('Đ', 'D')
                .Replace('đ', 'd');

            return result;
        }

        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return $"Student#{Guid.NewGuid().ToString("N")[..8]}";
        }

        var givenRaw = RemoveDiacritics(parts[^1]);
        if (string.IsNullOrWhiteSpace(givenRaw))
        {
            return $"Student#{Guid.NewGuid().ToString("N")[..8]}";
        }

        var given = char.ToUpperInvariant(givenRaw[0]) +
                    (givenRaw.Length > 1 ? givenRaw[1..].ToLowerInvariant() : string.Empty);

        var initialsBuilder = new StringBuilder();
        for (var i = 0; i < parts.Length - 1; i++)
        {
            var segment = RemoveDiacritics(parts[i]);
            if (!string.IsNullOrWhiteSpace(segment))
            {
                initialsBuilder.Append(char.ToLowerInvariant(segment[0]));
            }
        }

        var year = dateOfBirth.Year;
        return $"{given}{initialsBuilder}@{year}";
    }
}
