// Automatically selects the highest WebP quality that meets the target size and can progressively reduce dimensions when necessary.

using NexMedia.WebImageOptimiser.Core.Configuration;
using SkiaSharp;

namespace NexMedia.WebImageOptimiser.Core.Processing;

public static class RasterAutoOptimiser
{
    private const double DimensionReductionFactor = 0.90;

    private const int MinimumDimension = 64;

    public static AutoOptimisationResult Optimise(
        ImageBatchItem item,
        OptimisationSettings settings)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(settings);

        var resizeSettings = new ResizeSettings
        {
            Bounds = settings.Bounds,
            Mode = settings.ResizeMode switch
            {
                ResizeMode.FitWithin =>
                    ImageResizeMode.Fit,

                ResizeMode.CropToFill =>
                    ImageResizeMode.Crop,

                _ => throw new ArgumentOutOfRangeException(
                    nameof(settings.ResizeMode))
            }
        };

        ResizeDimensions initialDimensions =
            ResizeCalculator.Calculate(
                item.Width,
                item.Height,
                resizeSettings);

        return settings.OutputFormat switch
        {
            RasterOutputFormat.WebP =>
                OptimiseWebP(
                    item,
                    initialDimensions,
                    settings),

            RasterOutputFormat.Png =>
                OptimisePng(
                    item,
                    initialDimensions,
                    settings),

            _ => throw new ArgumentOutOfRangeException(
                nameof(settings.OutputFormat))
        };
    }

    private static AutoOptimisationResult OptimiseWebP(
        ImageBatchItem item,
        ResizeDimensions initialDimensions,
        OptimisationSettings settings)
    {
        ResizeDimensions dimensions =
            initialDimensions;

        while (true)
        {
            using SKBitmap bitmap =
                RasterResizeProcessor.Resize(
                    item,
                    dimensions);

            (
                RasterEncodeResult encoded,
                int quality,
                bool targetMet
            ) = FindBestWebPQuality(
                bitmap,
                settings);

            bool dimensionsReduced =
                dimensions != initialDimensions;

            if (targetMet)
            {
                return CreateResult(
                    encoded,
                    settings,
                    quality,
                    dimensionsReduced);
            }

            if (!settings.AllowFurtherDimensionReduction ||
                CannotReduceFurther(dimensions))
            {
                return CreateResult(
                    encoded,
                    settings,
                    quality,
                    dimensionsReduced);
            }

            dimensions =
                ReduceDimensions(dimensions);
        }
    }

    private static (
        RasterEncodeResult Encoded,
        int Quality,
        bool TargetMet)
        FindBestWebPQuality(
            SKBitmap bitmap,
            OptimisationSettings settings)
    {
        RasterEncodeResult maximum =
            RasterEncoder.Encode(
                bitmap,
                RasterOutputFormat.WebP,
                100);

        if (maximum.SizeBytes <=
            settings.TargetSizeBytes)
        {
            return (
                maximum,
                100,
                true);
        }

        int minimumQuality =
            settings.MinimumWebPQuality;

        RasterEncodeResult minimum =
            RasterEncoder.Encode(
                bitmap,
                RasterOutputFormat.WebP,
                minimumQuality);

        if (minimum.SizeBytes >
            settings.TargetSizeBytes)
        {
            return (
                minimum,
                minimumQuality,
                false);
        }

        RasterEncodeResult best =
            minimum;

        int bestQuality =
            minimumQuality;

        int low =
            minimumQuality + 1;

        int high = 99;

        while (low <= high)
        {
            int quality =
                low + ((high - low) / 2);

            RasterEncodeResult candidate =
                RasterEncoder.Encode(
                    bitmap,
                    RasterOutputFormat.WebP,
                    quality);

            if (candidate.SizeBytes <=
                settings.TargetSizeBytes)
            {
                best = candidate;
                bestQuality = quality;

                low = quality + 1;
            }
            else
            {
                high = quality - 1;
            }
        }

        return (
            best,
            bestQuality,
            true);
    }

    private static AutoOptimisationResult OptimisePng(
        ImageBatchItem item,
        ResizeDimensions initialDimensions,
        OptimisationSettings settings)
    {
        ResizeDimensions dimensions =
            initialDimensions;

        while (true)
        {
            using SKBitmap bitmap =
                RasterResizeProcessor.Resize(
                    item,
                    dimensions);

            RasterEncodeResult encoded =
                RasterEncoder.Encode(
                    bitmap,
                    RasterOutputFormat.Png);

            bool targetMet =
                encoded.SizeBytes <=
                settings.TargetSizeBytes;

            bool dimensionsReduced =
                dimensions != initialDimensions;

            if (targetMet ||
                !settings.AllowFurtherDimensionReduction ||
                CannotReduceFurther(dimensions))
            {
                return CreateResult(
                    encoded,
                    settings,
                    null,
                    dimensionsReduced);
            }

            dimensions =
                ReduceDimensions(dimensions);
        }
    }

    private static ResizeDimensions ReduceDimensions(
        ResizeDimensions current)
    {
        int width =
            Math.Max(
                1,
                (int)Math.Round(
                    current.Width *
                    DimensionReductionFactor));

        int height =
            Math.Max(
                1,
                (int)Math.Round(
                    current.Height *
                    DimensionReductionFactor));

        return new ResizeDimensions(
            width,
            height);
    }

    private static bool CannotReduceFurther(
        ResizeDimensions dimensions)
    {
        return dimensions.Width <= MinimumDimension ||
               dimensions.Height <= MinimumDimension;
    }

    private static AutoOptimisationResult CreateResult(
        RasterEncodeResult encoded,
        OptimisationSettings settings,
        int? quality,
        bool dimensionsReduced)
    {
        return new AutoOptimisationResult(
            encoded.Data,
            encoded.Format,
            encoded.Width,
            encoded.Height,
            encoded.SizeBytes,
            quality,
            settings.TargetSizeBytes,
            encoded.SizeBytes <=
                settings.TargetSizeBytes,
            dimensionsReduced);
    }
}
