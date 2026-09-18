// Reads dimensions and file metadata from SVG files without rasterising them, using width/height attributes or the viewBox as a fallback.

using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using NexMedia.WebImageOptimiser.Core.Models;

namespace NexMedia.WebImageOptimiser.Core.Imaging;

public static class SvgMetadataReader
{
    public static ImageEntry Read(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException(
                "A file path must be provided.",
                nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException(
                "The SVG file could not be found.",
                filePath);
        }

        try
        {
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersInDocument = 10_000_000
            };

            using var stream = File.OpenRead(filePath);
            using var reader = XmlReader.Create(stream, settings);

            XDocument document = XDocument.Load(reader);

            XElement? root = document.Root;

            if (root is null ||
                !string.Equals(
                    root.Name.LocalName,
                    "svg",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(
                    "The file does not contain a valid SVG root element.");
            }

            double? width = ParseLength(
                root.Attribute("width")?.Value);

            double? height = ParseLength(
                root.Attribute("height")?.Value);

            if (width is null || height is null)
            {
                (double viewBoxWidth, double viewBoxHeight) =
                    ReadViewBox(root);

                width ??= viewBoxWidth;
                height ??= viewBoxHeight;
            }

            if (width <= 0 || height <= 0)
            {
                throw new InvalidDataException(
                    "The SVG does not contain valid dimensions.");
            }

            var fileInfo = new FileInfo(filePath);

            return new ImageEntry
            {
                FilePath = fileInfo.FullName,
                Format = ImageFileFormat.Svg,
                Width = width.Value,
                Height = height.Value,
                OriginalSizeBytes = fileInfo.Length
            };
        }
        catch (XmlException exception)
        {
            throw new InvalidDataException(
                "The file contains invalid SVG/XML data.",
                exception);
        }
    }

    private static double? ParseLength(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        value = value.Trim();

        string[] supportedUnits =
        {
            "px",
            "in",
            "cm",
            "mm",
            "pt",
            "pc"
        };

        string? unit = supportedUnits.FirstOrDefault(
            candidate => value.EndsWith(
                candidate,
                StringComparison.OrdinalIgnoreCase));

        string numericValue = unit is null
            ? value
            : value[..^unit.Length];

        if (!double.TryParse(
                numericValue,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double number))
        {
            return null;
        }

        if (number <= 0)
        {
            return null;
        }

        return unit?.ToLowerInvariant() switch
        {
            null => number,
            "px" => number,
            "in" => number * 96,
            "cm" => number * 96 / 2.54,
            "mm" => number * 96 / 25.4,
            "pt" => number * 96 / 72,
            "pc" => number * 16,

            _ => null
        };
    }

    private static (double Width, double Height) ReadViewBox(
        XElement root)
    {
        string? viewBox = root.Attribute("viewBox")?.Value;

        if (string.IsNullOrWhiteSpace(viewBox))
        {
            throw new InvalidDataException(
                "The SVG does not define usable width, height, or viewBox dimensions.");
        }

        string[] parts = viewBox.Split(
            [' ', ','],
            StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length != 4 ||
            !double.TryParse(
                parts[2],
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double width) ||
            !double.TryParse(
                parts[3],
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double height) ||
            width <= 0 ||
            height <= 0)
        {
            throw new InvalidDataException(
                "The SVG contains an invalid viewBox.");
        }

        return (width, height);
    }
}
