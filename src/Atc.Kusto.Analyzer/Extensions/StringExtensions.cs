namespace Atc.Kusto.Analyzer.Extensions;

/// <summary>
/// Extension methods for string operations.
/// </summary>
internal static class StringExtensions
{
    /// <summary>
    /// Gets the file name from a path.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <returns>The file name with extension.</returns>
    public static string GetFileName(this string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return string.Empty;
        }

        var lastSlash = Math.Max(path.LastIndexOf('/'), path.LastIndexOf('\\'));
        return lastSlash >= 0 ? path.Substring(lastSlash + 1) : path;
    }

    /// <summary>
    /// Gets the file name without extension from a path.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <returns>The file name without extension.</returns>
    public static string GetFileNameWithoutExtension(this string path)
    {
        var fileName = path.GetFileName();
        var lastDot = fileName.LastIndexOf('.');
        return lastDot >= 0 ? fileName.Substring(0, lastDot) : fileName;
    }

    /// <summary>
    /// Gets the directory name from a path.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <returns>The directory path, or empty string if no directory is present.</returns>
    public static string GetDirectoryName(this string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return string.Empty;
        }

        var lastSlash = Math.Max(path.LastIndexOf('/'), path.LastIndexOf('\\'));
        return lastSlash >= 0 ? path.Substring(0, lastSlash) : string.Empty;
    }
}