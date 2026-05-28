namespace PdfAConverter;

/// <summary>
/// Represents a failure while converting a PDF document to PDF/A.
/// </summary>
public sealed class PdfAConversionException : Exception
{
    /// <summary>
    /// Creates a conversion exception.
    /// </summary>
    public PdfAConversionException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Creates a conversion exception with an inner exception.
    /// </summary>
    public PdfAConversionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Ghostscript exit code, when available.
    /// </summary>
    public int? ExitCode { get; init; }

    /// <summary>
    /// Text written by Ghostscript to standard output/error.
    /// </summary>
    public string? GhostscriptOutput { get; init; }
}
