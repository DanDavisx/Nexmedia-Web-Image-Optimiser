// Handles desktop-only file and folder dialogs and forwards selected paths to the main view model.

using System.IO;
using System.Windows;
using Microsoft.Win32;
using NexMedia.WebImageOptimiser.Core.Importing;
using NexMedia.WebImageOptimiser.Desktop.ViewModels;

namespace NexMedia.WebImageOptimiser.Desktop;

public partial class MainWindow : Window
{
    private MainWindowViewModel ViewModel =>
        (MainWindowViewModel)DataContext;

    public MainWindow()
    {
        InitializeComponent();

        DataContext =
            new MainWindowViewModel();
    }

    private async void ImportImages_Click(
        object sender,
        RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Import images",
            Multiselect = true,
            CheckFileExists = true,

            Filter =
                "Supported images|*.jpg;*.jpeg;*.png;*.webp;*.svg|" +
                "JPEG images|*.jpg;*.jpeg|" +
                "PNG images|*.png|" +
                "WebP images|*.webp|" +
                "SVG images|*.svg"
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        ImageImportResult result =
            await ViewModel.ImportFilesAsync(
                dialog.FileNames);

        ShowImportSummary(result);
    }

    private async void ImportFolder_Click(
        object sender,
        RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Import image folder",
            Multiselect = false
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            ImageImportResult result =
                await ViewModel.ImportFolderAsync(
                    dialog.FolderName);

            ShowImportSummary(result);
        }
        catch (Exception exception) when (
            exception is IOException or
            UnauthorizedAccessException or
            ArgumentException)
        {
            MessageBox.Show(
                this,
                exception.Message,
                "Unable to import folder",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void ShowImportSummary(
        ImageImportResult result)
    {
        if (result.DuplicatePaths.Count == 0 &&
            result.FailedFiles.Count == 0)
        {
            return;
        }

        string message =
            $"Imported: {result.ImportedImages.Count}";

        if (result.DuplicatePaths.Count > 0)
        {
            message +=
                $"\nDuplicates skipped: {result.DuplicatePaths.Count}";
        }

        if (result.FailedFiles.Count > 0)
        {
            message +=
                $"\nInvalid files skipped: {result.FailedFiles.Count}";
        }

        MessageBox.Show(
            this,
            message,
            "Import complete",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
}
