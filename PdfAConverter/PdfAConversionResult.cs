namespace PdfAConverter;

/// <summary>
/// Result returned after a successful PDF/A conversion.
/// </summary>
/// <param name="InputPath">Original input PDF path.</param>
/// <param name="OutputPath">Generated PDF/A path.</param>
/// <param name="Compliance">Requested PDF/A compliance family.</param>
/// <param name="GhostscriptOutput">Text written by Ghostscript to standard output/error.</param>
public sealed record PdfAConversionResult(string InputPath, string OutputPath, PdfACompliance Compliance, string GhostscriptOutput);
