namespace PdfAConverter;

/// <summary>
/// Converts PDF documents to PDF/A.
/// </summary>
public interface IPdfAConverter
{
    /// <summary>
    /// Converts a PDF file to PDF/A.
    /// </summary>
    /// <param name="inputPath">Source PDF file.</param>
    /// <param name="outputPath">Destination PDF/A file.</param>
    /// <param name="options">Optional conversion settings.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<PdfAConversionResult> ConvertAsync(string inputPath, string outputPath, PdfAConversionOptions? options = null, CancellationToken cancellationToken = default);
}
