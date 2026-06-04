using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace PdfAConverter;

/// <summary>
/// Converts PDF files to PDF/A by invoking a local Ghostscript executable.
/// </summary>
public sealed class GhostscriptPdfAConverter : IPdfAConverter
{
    private static readonly string[] WindowsExecutableNames = ["gswin64c.exe", "gswin32c.exe", "gs.exe"];
    private static readonly string[] UnixExecutableNames = ["gs"];

    /// <inheritdoc />
    public async Task<PdfAConversionResult> ConvertAsync(string inputPath, string outputPath, PdfAConversionOptions? options = null, CancellationToken cancellationToken = default)
    {
        options ??= PdfAConversionOptions.Default;

        var normalizedInputPath = NormalizeExistingFile(inputPath, nameof(inputPath));
        var normalizedOutputPath = NormalizeOutputPath(outputPath, options.Overwrite);
        var executablePath = ResolveGhostscriptExecutable(options.GhostscriptExecutablePath);
        var iccProfilePath = ResolveIccProfile(options.IccProfilePath, executablePath);
        var tempDirectory = ResolveTemporaryDirectory(options.TemporaryDirectory);
        var pdfADefinitionPath = Path.Combine(tempDirectory, $"{Guid.NewGuid():N}-pdfa.ps");

        Directory.CreateDirectory(Path.GetDirectoryName(normalizedOutputPath)!);

        try
        {
            await File.WriteAllTextAsync(pdfADefinitionPath, CreatePdfADefinition(iccProfilePath), Encoding.ASCII, cancellationToken).ConfigureAwait(false);

            var invocation = new GhostscriptInvocation(
                executablePath,
                normalizedInputPath,
                normalizedOutputPath,
                iccProfilePath,
                pdfADefinitionPath,
                options);

            var output = await RunGhostscriptAsync(invocation, cancellationToken).ConfigureAwait(false);

            if (!File.Exists(normalizedOutputPath))
            {
                throw new PdfAConversionException("Ghostscript finished without creating the output PDF/A file.")
                {
                    GhostscriptOutput = output
                };
            }

            return new PdfAConversionResult(normalizedInputPath, normalizedOutputPath, options.Compliance, output);
        }
        finally
        {
            TryDelete(pdfADefinitionPath);
        }
    }

    private static async Task<string> RunGhostscriptAsync(GhostscriptInvocation invocation, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = invocation.ExecutablePath,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var argument in BuildArguments(invocation))
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process { StartInfo = startInfo };
        var outputBuilder = new StringBuilder();

        process.OutputDataReceived += (_, eventArgs) => AppendLine(outputBuilder, eventArgs.Data);
        process.ErrorDataReceived += (_, eventArgs) => AppendLine(outputBuilder, eventArgs.Data);

        try
        {
            if (!process.Start())
            {
                throw new PdfAConversionException("Ghostscript process could not be started.");
            }
        }
        catch (Exception exception) when (exception is not PdfAConversionException)
        {
            throw new PdfAConversionException("Ghostscript process could not be started.", exception);
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        var output = outputBuilder.ToString();
        if (process.ExitCode != 0)
        {
            throw new PdfAConversionException($"Ghostscript failed with exit code {process.ExitCode}.")
            {
                ExitCode = process.ExitCode,
                GhostscriptOutput = output
            };
        }

        return output;
    }

    private static IEnumerable<string> BuildArguments(GhostscriptInvocation invocation)
    {
        yield return $"--permit-file-read={CreateGhostscriptPathList(invocation.InputPath, invocation.IccProfilePath, invocation.PdfADefinitionPath)}";
        yield return $"--permit-file-write={Path.GetDirectoryName(invocation.OutputPath)}";
        yield return $"-dPDFA={(int)invocation.Options.Compliance}";
        yield return $"-dPDFACompatibilityPolicy={invocation.Options.CompatibilityPolicy}";
        yield return "-dBATCH";
        yield return "-dNOPAUSE";
        yield return "-dNOOUTERSAVE";
        yield return "-sDEVICE=pdfwrite";
        yield return $"-sColorConversionStrategy={invocation.Options.ColorConversionStrategy}";
        yield return "-dPreserveSeparation=false";
        yield return "-dConvertCMYKImagesToRGB=true";
        yield return "-dEmbedAllFonts=true";
        yield return "-dSubsetFonts=true";
        yield return "-dCompressFonts=true";
        yield return "-dDetectDuplicateImages=true";
        yield return "-dFastWebView=false";
        yield return $"-sOutputFile={invocation.OutputPath}";
        yield return invocation.PdfADefinitionPath;
        yield return invocation.InputPath;
    }

    private static string CreatePdfADefinition(string iccProfilePath)
    {
        var escapedProfilePath = EscapePostScriptString(iccProfilePath.Replace('\\', '/'));

        return $$"""
               %!
               [ /Title (PDF/A conversion)
                 /DOCINFO pdfmark

               /ICCProfile ({{escapedProfilePath}}) def

               [ /_objdef {icc_PDFA} /type /stream /OBJ pdfmark

               [ {icc_PDFA}
               <<
                 systemdict /ColorConversionStrategy known {
                   systemdict /ColorConversionStrategy get cvn dup /Gray eq {
                     pop /N 1 false
                   }{
                     dup /RGB eq {
                       pop /N 3 false
                     }{
                       /CMYK eq {
                         /N 4 false
                       }{
                         true
                       } ifelse
                     } ifelse
                   } ifelse
                 } {
                   true
                 } ifelse

                 {
                   currentpagedevice /ProcessColorModel get
                   dup /DeviceGray eq {
                     pop /N 1
                   }{
                     dup /DeviceRGB eq {
                       pop /N 3
                     }{
                       dup /DeviceCMYK eq {
                         pop /N 4
                       } {
                         /ProcessColorModel cvx /rangecheck signalerror
                       } ifelse
                     } ifelse
                   } ifelse
                 } if
               >> /PUT pdfmark

               [
               {icc_PDFA}
               {ICCProfile (r) file} stopped
               {
                 cleartomark
               }
               {
                 /PUT pdfmark

               [ /_objdef {OutputIntent_PDFA} /type /dict /OBJ pdfmark
               [ {OutputIntent_PDFA} <<
                 /Type /OutputIntent
                 /S /GTS_PDFA1
                 /DestOutputProfile {icc_PDFA}
                 /OutputConditionIdentifier (sRGB)
               >> /PUT pdfmark
               [ {Catalog} << /OutputIntents [ {OutputIntent_PDFA} ] >> /PUT pdfmark
               } ifelse
               """;
    }

    private static string ResolveGhostscriptExecutable(string? configuredPath)
    {
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            return NormalizeExistingFile(configuredPath, nameof(PdfAConversionOptions.GhostscriptExecutablePath));
        }

        var executableNames = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? WindowsExecutableNames : UnixExecutableNames;

        foreach (var executableName in executableNames)
        {
            if (CanStartProcess(executableName))
            {
                return executableName;
            }
        }

        throw new PdfAConversionException("Ghostscript executable was not found. Install Ghostscript or set PdfAConversionOptions.GhostscriptExecutablePath.");
    }

    private static string ResolveIccProfile(string? configuredPath, string ghostscriptExecutablePath)
    {
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            return NormalizeExistingFile(configuredPath, nameof(PdfAConversionOptions.IccProfilePath));
        }

        foreach (var candidate in GetDefaultIccProfileCandidates(ghostscriptExecutablePath))
        {
            if (File.Exists(candidate))
            {
                return Path.GetFullPath(candidate);
            }
        }

        throw new PdfAConversionException("An sRGB ICC profile was not found. Set PdfAConversionOptions.IccProfilePath to a valid ICC/ICM profile.");
    }

    private static IEnumerable<string> GetDefaultIccProfileCandidates(string ghostscriptExecutablePath)
    {
        foreach (var candidate in GetGhostscriptIccProfileCandidates(ghostscriptExecutablePath))
        {
            yield return candidate;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var systemRoot = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            if (!string.IsNullOrWhiteSpace(systemRoot))
            {
                yield return Path.Combine(systemRoot, "System32", "spool", "drivers", "color", "sRGB Color Space Profile.icm");
            }
        }

        yield return "/usr/share/color/icc/sRGB.icc";
        yield return "/usr/share/color/icc/colord/sRGB.icc";
        yield return "/Library/ColorSync/Profiles/sRGB Profile.icc";
        yield return "/System/Library/ColorSync/Profiles/sRGB Profile.icc";
    }

    private static IEnumerable<string> GetGhostscriptIccProfileCandidates(string ghostscriptExecutablePath)
    {
        if (!Path.IsPathFullyQualified(ghostscriptExecutablePath))
        {
            yield break;
        }

        var executableDirectory = Path.GetDirectoryName(ghostscriptExecutablePath);
        var ghostscriptRoot = Directory.GetParent(executableDirectory ?? string.Empty)?.FullName;
        if (string.IsNullOrWhiteSpace(ghostscriptRoot))
        {
            yield break;
        }

        yield return Path.Combine(ghostscriptRoot, "iccprofiles", "default_rgb.icc");
        yield return Path.Combine(ghostscriptRoot, "iccprofiles", "srgb.icc");
    }

    private static string ResolveTemporaryDirectory(string? configuredDirectory)
    {
        var directory = string.IsNullOrWhiteSpace(configuredDirectory)
            ? Path.GetTempPath()
            : configuredDirectory;

        Directory.CreateDirectory(directory);
        return Path.GetFullPath(directory);
    }

    private static string NormalizeExistingFile(string path, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("A file path is required.", parameterName);
        }

        var fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("File was not found.", fullPath);
        }

        return fullPath;
    }

    private static string NormalizeOutputPath(string path, bool overwrite)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("A file path is required.", nameof(path));
        }

        var fullPath = Path.GetFullPath(path);
        if (File.Exists(fullPath) && !overwrite)
        {
            throw new IOException($"The output file already exists: {fullPath}");
        }

        return fullPath;
    }

    private static bool CanStartProcess(string executableName)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = executableName,
                ArgumentList = { "--version" },
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            process?.WaitForExit(2_000);
            return process?.HasExited == true && process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static void AppendLine(StringBuilder builder, string? value)
    {
        if (value is not null)
        {
            builder.AppendLine(value);
        }
    }

    private static string EscapePostScriptString(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("(", "\\(", StringComparison.Ordinal)
            .Replace(")", "\\)", StringComparison.Ordinal);
    }

    private static string CreateGhostscriptPathList(params string[] paths)
    {
        var separator = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ";" : ":";

        return string.Join(separator, paths.Select(path => Path.GetFullPath(path)));
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        { }
    }

    private sealed record GhostscriptInvocation(
        string ExecutablePath,
        string InputPath,
        string OutputPath,
        string IccProfilePath,
        string PdfADefinitionPath,
        PdfAConversionOptions Options);
}
