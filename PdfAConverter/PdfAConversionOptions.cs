namespace PdfAConverter;

/// <summary>
/// Options used when converting a PDF document to PDF/A.
/// </summary>
public sealed class PdfAConversionOptions
{
    /// <summary>
    /// PDF/A family to generate. Defaults to PDF/A-2b.
    /// </summary>
    public PdfACompliance Compliance { get; init; } = PdfACompliance.PdfA2b;

    /// <summary>
    /// Optional Ghostscript executable path. When empty, common executable names are resolved from PATH.
    /// </summary>
    public string? GhostscriptExecutablePath { get; init; }

    /// <summary>
    /// Optional ICC profile path used for the PDF/A output intent.
    /// When empty, common sRGB profile locations are probed.
    /// </summary>
    public string? IccProfilePath { get; init; }

    /// <summary>
    /// Policy used by Ghostscript when the input cannot be represented as valid PDF/A.
    /// 0 logs warnings, 1 fails conversion, 2 converts by dropping non-compliant features when possible.
    /// </summary>
    public int CompatibilityPolicy { get; init; } = 1;

    /// <summary>
    /// Ghostscript color conversion strategy. Common values are RGB, CMYK, Gray, and UseDeviceIndependentColor.
    /// </summary>
    public string ColorConversionStrategy { get; init; } = "RGB";

    /// <summary>
    /// Whether an existing output file may be overwritten.
    /// </summary>
    public bool Overwrite { get; init; } = true;

    /// <summary>
    /// Optional temporary directory. When empty, the system temporary directory is used.
    /// </summary>
    public string? TemporaryDirectory { get; init; }

    /// <summary>
    /// Creates a default options instance.
    /// </summary>
    public static PdfAConversionOptions Default { get; } = new();
}
