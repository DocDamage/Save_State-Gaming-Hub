using System.Text.RegularExpressions;

namespace SaveState.Core.Common.Validation;

/// <summary>
/// Provides input sanitization and validation utilities to prevent common security vulnerabilities
/// including SQL injection, XSS, path traversal, and command injection attacks.
/// </summary>
public static class InputSanitizer
{
    // Characters that are potentially dangerous in file paths
    private static readonly char[] DangerousPathChars = ['<', '>', '|', '"', '*', '?'];

    // SQL injection patterns
    private static readonly string[] SqlInjectionPatterns =
    [
        @"(\b(union|select|insert|update|delete|drop|create|alter|exec|execute)\b)",
        @"(-{2,})", // SQL comments
        @"(/\*.*?\*/)", // Block comments
        @"(;.*?$)", // Semicolon followed by anything
        @"('.*?(or|and).*?')", // SQL logical operators in strings
        @"(\bor\b\s+\d+\s*=\s*\d+)", // Classic OR 1=1 pattern
        @"(\band\b\s+\d+\s*=\s*\d+)" // AND 1=1 pattern
    ];

    // XSS patterns
    private static readonly string[] XssPatterns =
    [
        @"<script[^>]*>.*?</script>",
        @"javascript:",
        @"vbscript:",
        @"onload\s*=",
        @"onerror\s*=",
        @"onclick\s*=",
        @"onmouseover\s*=",
        @"<iframe[^>]*>",
        @"<object[^>]*>",
        @"<embed[^>]*>",
        @"<form[^>]*>",
        @"<input[^>]*>",
        @"<meta[^>]*>"
    ];

    // Command injection patterns
    private static readonly string[] CommandInjectionPatterns =
    [
        @"[;&|`$()<>]",
        @"\\x[0-9a-fA-F]{2}", // Hex encoded characters
        @"\\u[0-9a-fA-F]{4}", // Unicode escape sequences
        @"%[0-9a-fA-F]{2}", // URL encoded characters
        @"\|\|", // Pipe operators
        @"&&" // Logical AND in shell
    ];

    /// <summary>
    /// Sanitizes a string for safe use in database queries by removing or escaping dangerous characters.
    /// </summary>
    public static string SanitizeForDatabase(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        // Remove or escape dangerous characters
        var sanitized = input
            .Replace("\\", "\\\\") // Escape backslashes
            .Replace("'", "\\'")   // Escape single quotes
            .Replace("\"", "\\\"") // Escape double quotes
            .Replace("\0", "")     // Remove null bytes
            .Replace("\b", "")     // Remove backspace
            .Replace("\n", "")     // Remove newlines
            .Replace("\r", "")     // Remove carriage returns
            .Replace("\t", "")     // Remove tabs
            .Replace("\x1a", "");  // Remove substitute character

        // Check for SQL injection patterns
        if (IsSqlInjection(sanitized))
            throw new ArgumentException("Input contains potentially dangerous SQL patterns", nameof(input));

        return sanitized;
    }

    /// <summary>
    /// Sanitizes a string for safe display in HTML by encoding dangerous characters.
    /// </summary>
    public static string SanitizeForHtml(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        return input
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#x27;")
            .Replace("/", "&#x2F;");
    }

    /// <summary>
    /// Checks if a path is safe from path traversal attacks.
    /// </summary>
    public static bool IsSafePath(string? path)
    {
        if (string.IsNullOrEmpty(path))
            return true;

        // Check for dangerous characters
        if (path.IndexOfAny(DangerousPathChars) >= 0)
            return false;

        // Check for path traversal patterns
        if (path.Contains("..") || path.Contains("~") || path.Contains(":"))
        {
            // Normalize the path and check if it goes outside allowed directories
            try
            {
                var fullPath = Path.GetFullPath(path);
                var currentDir = Directory.GetCurrentDirectory();

                // Ensure the path doesn't go above the current directory
                if (!fullPath.StartsWith(currentDir, StringComparison.OrdinalIgnoreCase))
                    return false;
            }
            catch
            {
                return false; // If path normalization fails, it's likely unsafe
            }
        }

        return true;
    }

    /// <summary>
    /// Checks if command line arguments are safe from command injection.
    /// </summary>
    public static bool IsSafeCommandLine(string? command)
    {
        if (string.IsNullOrEmpty(command))
            return true;

        // Check for command injection patterns
        foreach (var pattern in CommandInjectionPatterns)
        {
            if (Regex.IsMatch(command, pattern, RegexOptions.IgnoreCase))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Checks if input contains potential SQL injection patterns.
    /// </summary>
    public static bool IsSqlInjection(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return false;

        foreach (var pattern in SqlInjectionPatterns)
        {
            if (Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if input contains potential XSS patterns.
    /// </summary>
    public static bool IsXss(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return false;

        foreach (var pattern in XssPatterns)
        {
            if (Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Normalizes and validates a game title.
    /// </summary>
    public static string NormalizeGameTitle(string? title)
    {
        if (string.IsNullOrEmpty(title))
            return string.Empty;

        // Trim whitespace and normalize
        var normalized = title.Trim();

        // Remove excessive whitespace
        normalized = Regex.Replace(normalized, @"\s+", " ");

        // Check for dangerous patterns
        if (IsXss(normalized) || IsSqlInjection(normalized))
            throw new ArgumentException("Game title contains potentially dangerous content", nameof(title));

        return normalized;
    }

    /// <summary>
    /// Normalizes and validates a platform name.
    /// </summary>
    public static string NormalizePlatformName(string? platformName)
    {
        if (string.IsNullOrEmpty(platformName))
            return string.Empty;

        // Trim and normalize
        var normalized = platformName.Trim();

        // Remove excessive whitespace
        normalized = Regex.Replace(normalized, @"\s+", " ");

        // Check for dangerous patterns
        if (IsXss(normalized) || IsSqlInjection(normalized))
            throw new ArgumentException("Platform name contains potentially dangerous content", nameof(platformName));

        return normalized;
    }

    /// <summary>
    /// Validates and normalizes tags.
    /// </summary>
    public static IEnumerable<string> NormalizeTags(IEnumerable<string>? tags)
    {
        if (tags == null)
            return Array.Empty<string>();

        var normalizedTags = new List<string>();

        foreach (var tag in tags)
        {
            if (string.IsNullOrWhiteSpace(tag))
                continue;

            var normalized = tag.Trim().ToLowerInvariant();

            // Remove special characters and excessive whitespace
            normalized = Regex.Replace(normalized, @"[^\w\s-]", "");
            normalized = Regex.Replace(normalized, @"\s+", " ");

            if (!string.IsNullOrEmpty(normalized) && normalized.Length <= 30)
            {
                // Check for dangerous patterns
                if (!IsXss(normalized) && !IsSqlInjection(normalized))
                {
                    normalizedTags.Add(normalized);
                }
            }
        }

        return normalizedTags.Distinct();
    }
}
