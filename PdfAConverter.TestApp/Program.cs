using PdfAConverter;

if (args.Length < 2)
{
    Console.WriteLine("Uso:");
    Console.WriteLine("  PdfAConverter.TestApp <input.pdf> <output-pdfa.pdf>");
    Console.WriteLine();
    Console.WriteLine("Ejemplo:");
    Console.WriteLine(@"  dotnet run --project .\PdfAConverter.TestApp -- ""C:\temp\Document.pdf"" ""C:\temp\salida-pdfa.pdf"" ""C:\Program Files\gs\gs10.07.1\bin\gswin64c.exe""");
    return 1;
}

var inputPath = args[0];
var outputPath = args[1];
var ghostscriptPath = args.Length >= 3 ? args[2] : null;
var colorConversionStrategy = args.Length >= 4 ? args[3] : "RGB";
var compliance = args.Length >= 5 && Enum.TryParse<PdfACompliance>(args[4], ignoreCase: true, out var parsedCompliance)
    ? parsedCompliance
    : PdfACompliance.PdfA2b;

IPdfAConverter converter = new GhostscriptPdfAConverter();

try
{
    var result = await converter.ConvertAsync(inputPath, outputPath,
        new PdfAConversionOptions
        {
            Compliance = compliance,
            GhostscriptExecutablePath = ghostscriptPath,
            ColorConversionStrategy = colorConversionStrategy
        });

    Console.WriteLine("Conversion completada correctamente.");
    Console.WriteLine($"Entrada: {result.InputPath}");
    Console.WriteLine($"Salida:  {result.OutputPath}");
    Console.WriteLine($"PDF/A:   {result.Compliance}");
    Console.WriteLine($"Color:   {colorConversionStrategy}");

    if (!string.IsNullOrWhiteSpace(result.GhostscriptOutput))
    {
        Console.WriteLine();
        Console.WriteLine("Salida de Ghostscript:");
        Console.WriteLine(result.GhostscriptOutput);
    }

    return 0;
}
catch (PdfAConversionException exception)
{
    Console.Error.WriteLine("No se pudo convertir el PDF a PDF/A.");
    Console.Error.WriteLine(exception.Message);

    if (!string.IsNullOrWhiteSpace(exception.GhostscriptOutput))
    {
        Console.Error.WriteLine();
        Console.Error.WriteLine("Salida de Ghostscript:");
        Console.Error.WriteLine(exception.GhostscriptOutput);
    }

    return 2;
}
catch (Exception exception)
{
    Console.Error.WriteLine("Error inesperado.");
    Console.Error.WriteLine(exception.Message);
    return 3;
}
