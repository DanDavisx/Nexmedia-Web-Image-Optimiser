// Verifies WebP and PNG encoding, output dimensions, file signatures, transparency, and quality validation.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NexMedia.WebImageOptimiser.Core.Configuration;
using NexMedia.WebImageOptimiser.Core.Processing;
using SkiaSharp;

namespace NexMedia.WebImageOptimiser.Core.Tests;

[TestClass]
public class RasterEncoderTests
{
    [TestMethod]
    public void Encode_WebP_ProducesEncodedData()
    {
        using var bitmap =
            new SKBitmap(800, 600);

        RasterEncodeResult result =
            RasterEncoder.Encode(
                bitmap,
                RasterOutputFormat.WebP,
                80);

        Assert.AreEqual(
            RasterOutputFormat.WebP,
            result.Format);

        Assert.AreEqual(800, result.Width);
        Assert.AreEqual(600, result.Height);

        Assert.IsTrue(
            result.SizeBytes > 0);
    }

    [TestMethod]
    public void Encode_Png_ProducesEncodedData()
    {
        using var bitmap =
            new SKBitmap(
                640,
                480,
                SKColorType.Rgba8888,
                SKAlphaType.Premul);

        RasterEncodeResult result =
            RasterEncoder.Encode(
                bitmap,
                RasterOutputFormat.Png);

        Assert.AreEqual(
            RasterOutputFormat.Png,
            result.Format);

        Assert.AreEqual(640, result.Width);
        Assert.AreEqual(480, result.Height);

        Assert.IsTrue(
            result.SizeBytes > 0);
    }

    [TestMethod]
    public void Encode_WebP_OutputCanBeDecoded()
    {
        using var bitmap =
            new SKBitmap(400, 300);

        RasterEncodeResult result =
            RasterEncoder.Encode(
                bitmap,
                RasterOutputFormat.WebP,
                80);

        using SKData data =
            SKData.CreateCopy(result.Data);

        using SKCodec codec =
            SKCodec.Create(data)
            ?? throw new AssertFailedException(
                "Encoded WebP could not be decoded.");

        Assert.AreEqual(
            SKEncodedImageFormat.Webp,
            codec.EncodedFormat);

        Assert.AreEqual(
            400,
            codec.Info.Width);

        Assert.AreEqual(
            300,
            codec.Info.Height);
    }

    [TestMethod]
    public void Encode_Png_OutputCanBeDecoded()
    {
        using var bitmap =
            new SKBitmap(400, 300);

        RasterEncodeResult result =
            RasterEncoder.Encode(
                bitmap,
                RasterOutputFormat.Png);

        using SKData data =
            SKData.CreateCopy(result.Data);

        using SKCodec codec =
            SKCodec.Create(data)
            ?? throw new AssertFailedException(
                "Encoded PNG could not be decoded.");

        Assert.AreEqual(
            SKEncodedImageFormat.Png,
            codec.EncodedFormat);

        Assert.AreEqual(
            400,
            codec.Info.Width);

        Assert.AreEqual(
            300,
            codec.Info.Height);
    }

    [TestMethod]
    public void Encode_Png_PreservesTransparency()
    {
        using var bitmap =
            new SKBitmap(
                100,
                100,
                SKColorType.Rgba8888,
                SKAlphaType.Premul);

        bitmap.Erase(
            new SKColor(
                255,
                0,
                0,
                100));

        RasterEncodeResult result =
            RasterEncoder.Encode(
                bitmap,
                RasterOutputFormat.Png);

        using SKData data =
            SKData.CreateCopy(result.Data);

        using SKBitmap decoded =
            SKBitmap.Decode(data)
            ?? throw new AssertFailedException(
                "Encoded PNG could not be decoded.");

        SKColor pixel =
            decoded.GetPixel(50, 50);

        Assert.IsTrue(
            pixel.Alpha < 255);
    }

    [TestMethod]
    public void Encode_InvalidWebPQuality_ThrowsException()
    {
        using var bitmap =
            new SKBitmap(100, 100);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => RasterEncoder.Encode(
                bitmap,
                RasterOutputFormat.WebP,
                101));
    }

    [TestMethod]
    public void ResizeThenEncode_ProducesExpectedOutput()
    {
        using var source =
            new SKBitmap(1600, 1200);

        using SKImage sourceImage =
            SKImage.FromBitmap(source);

        using SKData sourceData =
            sourceImage.Encode(
                SKEncodedImageFormat.Jpeg,
                90);

        string filePath =
            Path.Combine(
                Path.GetTempPath(),
                $"{Guid.NewGuid()}.jpg");

        try
        {
            using (FileStream output =
                   File.Create(filePath))
            {
                sourceData.SaveTo(output);
            }

            var entry =
                new NexMedia.WebImageOptimiser.Core.Models.ImageEntry
                {
                    FilePath = filePath,
                    Format =
                        NexMedia.WebImageOptimiser.Core.Models
                            .ImageFileFormat.Jpeg,
                    Width = 1600,
                    Height = 1200,
                    OriginalSizeBytes =
                        new FileInfo(filePath).Length
                };

            var item =
                new ImageBatchItem(entry);

            item.ApplyResizeSettings(
                new ResizeSettings
                {
                    Bounds =
                        new ResizeBounds(
                            800,
                            800)
                });

            using SKBitmap resized =
                RasterResizeProcessor.Resize(item);

            RasterEncodeResult encoded =
                RasterEncoder.Encode(
                    resized,
                    RasterOutputFormat.WebP,
                    80);

            Assert.AreEqual(
                800,
                encoded.Width);

            Assert.AreEqual(
                600,
                encoded.Height);

            Assert.IsTrue(
                encoded.SizeBytes > 0);
        }
        finally
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }
}
