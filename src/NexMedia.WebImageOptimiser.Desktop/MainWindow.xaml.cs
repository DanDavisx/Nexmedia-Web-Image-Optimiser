using System.Windows;
using NexMedia.WebImageOptimiser.Desktop.ViewModels;

namespace NexMedia.WebImageOptimiser.Desktop;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }
}
