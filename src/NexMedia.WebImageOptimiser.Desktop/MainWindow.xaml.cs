// Handles desktop-only file and folder dialogs and forwards selected paths to the main view model.

using System.IO;
using System.Windows;
using Microsoft.Win32;
using NexMedia.WebImageOptimiser.Core.Configuration;
using NexMedia.WebImageOptimiser.Core.Importing;
using NexMedia.WebImageOptimiser.Core.Models;
using NexMedia.WebImageOptimiser.Core.Processing;
using NexMedia.WebImageOptimiser.Desktop.ViewModels;

namespace NexMedia.WebImageOptimiser.Desktop;

public partial class MainWindow : Window
{
    private bool isOptimising;

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

    private void SelectAll_Click(object sender, RoutedEventArgs e)
    {
        ImagesGrid.SelectAll();
        ImagesGrid.Focus();
    }

    private void ImagesGrid_SelectionChanged(
        object sender,
        System.Windows.Controls.SelectionChangedEventArgs e)
    {
        UpdateActionState();
    }

    private void RemoveSelected_Click(object sender, RoutedEventArgs e)
    {
        ImageBatchItem[] selectedImages = ImagesGrid.SelectedItems.Cast<ImageBatchItem>().ToArray();

        ViewModel.RemoveImages(selectedImages);
    }

    private void ApplyToSelection_Click(object sender, RoutedEventArgs e)
    {
        ImageBatchItem[] selectedImages =
            ImagesGrid.SelectedItems
                .Cast<ImageBatchItem>()
                .ToArray();

        if (!ViewModel.TryCreateOptimisationSettings(
                out OptimisationSettings? settings,
                out string validationMessage))
        {
            MessageBox.Show(
                this,
                validationMessage,
                "Invalid output settings",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        int applied =
            ViewModel.ApplyOptimisationSettings(
                selectedImages,
                settings!);

        int svgCount = selectedImages.Length - applied;

        if (svgCount > 0)
        {
            MessageBox.Show(
                this,
                $"{applied} raster image(s) updated.\n" +
                $"{svgCount} SVG image(s) remain vector.",
                "Settings applied",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }

    private async void Optimise_Click(
        object sender,
        RoutedEventArgs e)
    {
        ImageBatchItem[] selectedImages =
            ImagesGrid.SelectedItems
                .Cast<ImageBatchItem>()
                .ToArray();

        ImageBatchItem[] rasterImages =
            selectedImages
                .Where(image =>
                    image.Format != ImageFileFormat.Svg)
                .ToArray();

        ImageBatchItem[] svgImages =
            selectedImages
                .Where(image =>
                    image.Format == ImageFileFormat.Svg)
                .ToArray();

        if (rasterImages.Length == 0)
        {
            foreach (ImageBatchItem svg in svgImages)
            {
                svg.MarkSvgPreserved();
            }

            MessageBox.Show(
                this,
                "The selection does not contain any raster images.",
                "Nothing to optimise",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            return;
        }

        if (rasterImages.Any(image =>
                image.AssignedOptimisationSettings is null))
        {
            MessageBox.Show(
                this,
                "Apply output settings to every selected raster image first.",
                "Settings required",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        foreach (ImageBatchItem svg in svgImages)
        {
            svg.MarkSvgPreserved();
        }

        isOptimising = true;
        UpdateActionState();

        BatchActionsPanel.IsEnabled = false;
        ImagesGrid.IsEnabled = false;

        OverallProgressBar.Minimum = 0;
        OverallProgressBar.Maximum = rasterImages.Length;
        OverallProgressBar.Value = 0;

        try
        {
            int completed = 0;

            foreach (ImageBatchItem image in rasterImages)
            {
                image.MarkProcessing();

                try
                {
                    OptimisationSettings settings =
                        image.AssignedOptimisationSettings!;

                    AutoOptimisationResult result =
                        await Task.Run(
                            () => RasterAutoOptimiser.Optimise(
                                image,
                                settings));

                    image.SetOptimisationResult(result);
                }
                catch (Exception exception)
                    when (exception is not OutOfMemoryException)
                {
                    image.MarkFailed(exception.Message);
                }
                finally
                {
                    completed++;
                    OverallProgressBar.Value = completed;
                }
            }
        }
        finally
        {
            isOptimising = false;

            BatchActionsPanel.IsEnabled = true;
            ImagesGrid.IsEnabled = true;

            UpdateActionState();
        }
    }

    private void UpdateActionState()
    {
        bool hasSelection =
            ImagesGrid.SelectedItems.Count > 0;

        RemoveSelectedButton.IsEnabled =
            hasSelection && !isOptimising;

        ApplySelectionButton.IsEnabled =
            hasSelection && !isOptimising;

        OptimiseButton.IsEnabled =
            hasSelection && !isOptimising;
    }

    private void ClearBatch_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.Images.Count == 0)
        {
            return;
        }

        MessageBoxResult result =
            MessageBox.Show(
                this,
                "Remove all images from the batch?",
                "Clear batch",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            ViewModel.ClearImages();
        }
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
