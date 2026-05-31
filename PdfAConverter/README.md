# PdfAConverter

Small .NET library for converting PDF files to PDF/A by invoking a local Ghostscript installation.

## Requirements

- .NET 10 or later, matching the current project target.
- Ghostscript installed and available in `PATH`, or configured through `PdfAConversionOptions.GhostscriptExecutablePath`.
- An sRGB ICC/ICM profile. On Windows the library probes the default system profile automatically.

## Usage

```csharp
using PdfAConverter;

IPdfAConverter converter = new GhostscriptPdfAConverter();

var result = await converter.ConvertAsync(
    inputPath: @"C:\docs\input.pdf",
    outputPath: @"C:\docs\output-pdfa.pdf",
    options: new PdfAConversionOptions
    {
        Compliance = PdfACompliance.PdfA2b
    });

Console.WriteLine(result.OutputPath);
```

When Ghostscript is not in `PATH`:

```csharp
var result = await converter.ConvertAsync(
    @"C:\docs\input.pdf",
    @"C:\docs\output-pdfa.pdf",
    new PdfAConversionOptions
    {
        GhostscriptExecutablePath = @"C:\Program Files\gs\gs10.05.1\bin\gswin64c.exe",
        IccProfilePath = @"C:\Windows\System32\spool\drivers\color\sRGB Color Space Profile.icm"
    });
```

## Test app

The repository includes `PdfAConverter.TestApp`, a small console app for trying a real conversion.

From the solution folder:

```powershell
dotnet run --project .\PdfAConverter.TestApp -- "C:\temp\Document.pdf" "C:\temp\salida-pdfa.pdf" "C:\Program Files\gs\gs10.07.1\bin\gswin64c.exe"
```

You can also configure the same arguments for Visual Studio debugging in `PdfAConverter.TestApp/Properties/launchSettings.json`:

```json
{
  "profiles": {
    "PdfAConverter.TestApp": {
      "commandName": "Project",
      "commandLineArgs": "\"C:\\temp\\Document.pdf\" \"C:\\temp\\salida-pdfa.pdf\" \"C:\\Program Files\\gs\\gs10.07.1\\bin\\gswin64c.exe\""
    }
  }
}
```

The three arguments are:

1. Input PDF path.
2. Output PDF/A path.
3. Optional Ghostscript executable path.

## Notes

This library performs conversion. For strict acceptance workflows, validate the generated file with a PDF/A validator such as veraPDF after conversion.
