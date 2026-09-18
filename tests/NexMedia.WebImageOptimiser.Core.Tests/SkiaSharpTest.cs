// Verifies that SkiaSharp can encode and read the raster formats required by the application: JPEG, PNG, and WebP.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;

namespace NexMedia.WebImageOptimiser.Core.Tests;

[TestClass]

public class SkiaSharpTest
{
    [TestMethod]
    public void SkiaSharp_CanEncodeAndReadSupportedRasterFormats()
    {
        TestFormat(SKEncodedImageFormat.Jpeg);
        TestFormat(SKEncodedImageFormat.Png);
        TestFormat(SKEncodedImageFormat.Webp);
    }

    private static void TestFormat(SKEncodedImageFormat format)
    {
        const int expectedWidth = 100;
        const int expectedHeight = 50;

        using var bitmap = new SKBitmap(expectedWidth, expectedHeight);
        using var image = SKImage.FromBitmap(bitmap);
        using var encodedData = image.Encode(format, 90);

        Assert.IsNotNull(encodedData);

        using var codec = SKCodec.Create(encodedData);

        Assert.IsNotNull(codec);
        Assert.AreEqual(expectedWidth, codec.Info.Width);
        Assert.AreEqual(expectedHeight, codec.Info.Height);
        Assert.AreEqual(format, codec.EncodedFormat);
    }
}
