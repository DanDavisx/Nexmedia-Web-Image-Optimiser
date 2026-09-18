// Verifies SVG metadata reading from explicit dimensions and viewBox values, including malformed and incomplete SVG files.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NexMedia.WebImageOptimiser.Core.Imaging;
using NexMedia.WebImageOptimiser.Core.Models;

namespace NexMedia.WebImageOptimiser.Core.Tests;

[TestClass]
public class SvgMetadataReaderTests
{
    private string _testDirectory = null!;

    [TestInitialize]
    public void Setup()
    {
        _testDirectory = Path.Combine(
            Path.GetTempPath(),
            "NexMedia.WebImageOptimiser.Tests",
            Guid.NewGuid().ToString());

        Directory.CreateDirectory(_testDirectory);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(
                _testDirectory,
                recursive: true);
        }
    }

    [TestMethod]
    public void Read_ExplicitDimensions_ReturnsCorrectMetadata()
    {
        string filePath = CreateSvg(
            "test.svg",
            """
            <svg xmlns="http://www.w3.org/2000/svg"
                 width="800"
                 height="600">
            </svg>
            """);

        ImageEntry result = SvgMetadataReader.Read(filePath);

        Assert.AreEqual(
            ImageFileFormat.Svg,
            result.Format);

        Assert.AreEqual(800d, result.Width);
        Assert.AreEqual(600d, result.Height);
        Assert.IsTrue(result.OriginalSizeBytes > 0);
    }

    [TestMethod]
    public void Read_ViewBox_ReturnsCorrectMetadata()
    {
        string filePath = CreateSvg(
            "viewbox.svg",
            """
            <svg xmlns="http://www.w3.org/2000/svg"
                 viewBox="0 0 1920 1080">
            </svg>
            """);

        ImageEntry result = SvgMetadataReader.Read(filePath);

        Assert.AreEqual(1920d, result.Width);
        Assert.AreEqual(1080d, result.Height);
    }

    [TestMethod]
    public void Read_PixelUnits_ReturnsCorrectMetadata()
    {
        string filePath = CreateSvg(
            "pixels.svg",
            """
            <svg xmlns="http://www.w3.org/2000/svg"
                 width="640px"
                 height="480px">
            </svg>
            """);

        ImageEntry result = SvgMetadataReader.Read(filePath);

        Assert.AreEqual(640d, result.Width);
        Assert.AreEqual(480d, result.Height);
    }

    [TestMethod]
    public void Read_PercentageDimensions_UsesViewBox()
    {
        string filePath = CreateSvg(
            "percentage.svg",
            """
            <svg xmlns="http://www.w3.org/2000/svg"
                 width="100%"
                 height="100%"
                 viewBox="0 0 1200 800">
            </svg>
            """);

        ImageEntry result = SvgMetadataReader.Read(filePath);

        Assert.AreEqual(1200d, result.Width);
        Assert.AreEqual(800d, result.Height);
    }

    [TestMethod]
    public void Read_InvalidXml_ThrowsInvalidDataException()
    {
        string filePath = CreateSvg(
            "invalid.svg",
            """
            <svg>
                <broken>
            </svg>
            """);

        Assert.ThrowsExactly<InvalidDataException>(
            () => SvgMetadataReader.Read(filePath));
    }

    [TestMethod]
    public void Read_NoDimensions_ThrowsInvalidDataException()
    {
        string filePath = CreateSvg(
            "no-dimensions.svg",
            """
            <svg xmlns="http://www.w3.org/2000/svg">
            </svg>
            """);

        Assert.ThrowsExactly<InvalidDataException>(
            () => SvgMetadataReader.Read(filePath));
    }

    [TestMethod]
    public void Read_NonSvgXml_ThrowsInvalidDataException()
    {
        string filePath = CreateSvg(
            "fake.svg",
            """
            <document>
                Not actually an SVG
            </document>
            """);

        Assert.ThrowsExactly<InvalidDataException>(
            () => SvgMetadataReader.Read(filePath));
    }

    private string CreateSvg(
        string fileName,
        string contents)
    {
        string filePath = Path.Combine(
            _testDirectory,
            fileName);

        File.WriteAllText(
            filePath,
            contents);

        return filePath;
    }
}
