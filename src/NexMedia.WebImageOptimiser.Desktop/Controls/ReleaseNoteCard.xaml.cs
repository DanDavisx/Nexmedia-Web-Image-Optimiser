using System.Windows.Controls;
using System.Windows;

namespace NexMedia.WebImageOptimiser.Desktop.Controls;

public partial class ReleaseNoteCard : UserControl
{
    public static readonly RoutedEvent UpdateRequestedEvent = EventManager.RegisterRoutedEvent(
        nameof(UpdateRequested), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ReleaseNoteCard));
    public event RoutedEventHandler UpdateRequested
    {
        add => AddHandler(UpdateRequestedEvent, value);
        remove => RemoveHandler(UpdateRequestedEvent, value);
    }
    private void Update_Click(object sender, RoutedEventArgs e)
        => RaiseEvent(new RoutedEventArgs(UpdateRequestedEvent, this));

    public ReleaseNoteCard() => InitializeComponent();
}
