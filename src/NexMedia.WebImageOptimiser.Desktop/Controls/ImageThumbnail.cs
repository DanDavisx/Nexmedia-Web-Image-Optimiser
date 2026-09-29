using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NexMedia.WebImageOptimiser.Core.Processing;
using NexMedia.WebImageOptimiser.Desktop.Converters;

namespace NexMedia.WebImageOptimiser.Desktop.Controls;

// Cache per batch item so recycling cards does not decode the same file repeatedly.
// Weak keys release thumbnails when their batch items are removed.
public sealed class ImageThumbnail : Image
{
    private static readonly ConditionalWeakTable<ImageBatchItem, Lazy<Task<ImageSource?>>> Cache = new();
    private static readonly SemaphoreSlim DecodeSlots = new(2);

    public static readonly DependencyProperty ItemProperty = DependencyProperty.Register(
        nameof(Item), typeof(ImageBatchItem), typeof(ImageThumbnail),
        new PropertyMetadata(null, ItemChanged));

    public ImageBatchItem? Item
    {
        get => (ImageBatchItem?)GetValue(ItemProperty);
        set => SetValue(ItemProperty, value);
    }

    private static async void ItemChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var thumbnail = (ImageThumbnail)sender;
        thumbnail.Source = null;
        if (args.NewValue is not ImageBatchItem item) return;
        ImageSource? source = await Cache.GetValue(item, key => new Lazy<Task<ImageSource?>>(
            () => DecodeAsync(key))).Value;
        // A recycled card may now represent another image.
        if (ReferenceEquals(thumbnail.Item, item))
        {
            thumbnail.Stretch = source is not null && source.Height > source.Width
                ? Stretch.Uniform : Stretch.UniformToFill;
            thumbnail.Source = source;
        }
    }

    private static async Task<ImageSource?> DecodeAsync(ImageBatchItem item)
    {
        await DecodeSlots.WaitAsync().ConfigureAwait(false);
        try
        {
            return await Task.Run(() => new RasterThumbnailConverter().Convert(
                item, typeof(ImageSource), null!, CultureInfo.InvariantCulture) as ImageSource)
                .ConfigureAwait(false);
        }
        finally { DecodeSlots.Release(); }
    }
}
