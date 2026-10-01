using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NexMedia.WebImageOptimiser.Desktop.Controls;

public sealed class ImageListBox : ListBox
{
    protected override DependencyObject GetContainerForItemOverride() => new ImageListBoxItem();

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        var focused = Keyboard.FocusedElement as ImageListBoxItem;
        int index = focused is null ? -1 : ItemContainerGenerator.IndexFromContainer(focused);
        if (e.Key is Key.Space or Key.Enter && index >= 0)
        {
            focused!.IsSelected = !focused.IsSelected;
            e.Handled = true;
            return;
        }

        if (Items.Count > 0 && e.Key is Key.Up or Key.Down or Key.Left or Key.Right or Key.Home or Key.End)
        {
            int target = e.Key switch
            {
                Key.Home => 0,
                Key.End => Items.Count - 1,
                Key.Up or Key.Left => Math.Max(0, index - 1),
                _ => Math.Min(Items.Count - 1, index + 1)
            };
            ScrollIntoView(Items[target]);
            UpdateLayout();
            (ItemContainerGenerator.ContainerFromIndex(target) as ListBoxItem)?.Focus();
            e.Handled = true;
            return;
        }
        base.OnPreviewKeyDown(e);
    }

    private sealed class ImageListBoxItem : ListBoxItem
    {
        protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            // ListBoxItem's default focus handler also selects the focused item.
            // Mouse clicks retain their standard selection behaviour.
            e.Handled = true;
        }
    }
}
