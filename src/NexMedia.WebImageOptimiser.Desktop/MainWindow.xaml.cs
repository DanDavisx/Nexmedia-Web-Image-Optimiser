// Handles desktop-only file and folder dialogs and forwards selected paths to the main view model.

using System.IO;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;
using NexMedia.WebImageOptimiser.Core.Configuration;
using NexMedia.WebImageOptimiser.Core.Exporting;
using NexMedia.WebImageOptimiser.Core.Importing;
using NexMedia.WebImageOptimiser.Core.Processing;
using NexMedia.WebImageOptimiser.Desktop.Configuration;
using NexMedia.WebImageOptimiser.Desktop.ViewModels;

namespace NexMedia.WebImageOptimiser.Desktop;

public partial class MainWindow : Window
{
    private readonly AppPreferencesStore preferencesStore = new(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "NexMedia", "WebImageOptimiser", "settings.json"));

    private CancellationTokenSource headlineAnimationCancellation = new();

    private static readonly TimeSpan OptimisationAreaFadeDuration =
        TimeSpan.FromMilliseconds(250);

    private readonly DispatcherTimer headlineRotationTimer = new();

    private readonly DispatcherTimer optimisingSpinnerTimer = new();

    private BitmapSource[] optimisingSpinnerFrames = [];

    private int optimisingSpinnerFrameIndex;

    private IReadOnlyList<string> headlineMessages = [];

    private TimeSpan headlineCharacterDelay;

    private TimeSpan headlineCharacterFadeDuration;

    private int headlineMessageIndex;

    private bool isChangingHeadline;

    private bool isOptimising;

    private bool isExporting;

    private MainWindowViewModel ViewModel =>
        (MainWindowViewModel)DataContext;

    public MainWindow()
    {
        AppPreferences preferences = preferencesStore.Load(out string? warning);
        AppearanceManager.ApplyTheme(preferences.Theme);
        AppearanceManager.ApplyAnimations(preferences.InterfaceAnimationsEnabled);
        InitializeComponent();

        var viewModel = new MainWindowViewModel();
        viewModel.LoadPreferences(preferences);
        DataContext = viewModel;

        ConfigureHeadlineRotation();
        ConfigureOptimisingSpinner();
        viewModel.PropertyChanged += Preferences_PropertyChanged;
        SettingsFeedbackText.Text = warning ?? "Save settings to remember your preferences on this computer.";
    }

    private void Preferences_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.SelectedTheme))
            AppearanceManager.ApplyTheme(ViewModel.SelectedTheme);

        if (e.PropertyName == nameof(MainWindowViewModel.InterfaceAnimationsEnabled))
        {
            AppearanceManager.ApplyAnimations(ViewModel.InterfaceAnimationsEnabled);
            headlineAnimationCancellation.Cancel();
            headlineAnimationCancellation.Dispose();
            headlineAnimationCancellation = new();
            headlineRotationTimer.Stop();
            SetHeadlineImmediately(headlineMessages[headlineMessageIndex]);
            if (ViewModel.InterfaceAnimationsEnabled && headlineMessages.Count > 1)
                headlineRotationTimer.Start();
            if (!ViewModel.InterfaceAnimationsEnabled) StopOptimisingSpinner();
        }

        if (e.PropertyName is nameof(MainWindowViewModel.SelectedTheme)
            or nameof(MainWindowViewModel.InterfaceAnimationsEnabled)
            or nameof(MainWindowViewModel.SelectedPreset)
            or nameof(MainWindowViewModel.SelectedOutputFormat)
            or nameof(MainWindowViewModel.TargetSizeKilobytesText)
            or nameof(MainWindowViewModel.MinimumWebPQualityText)
            or nameof(MainWindowViewModel.SelectedResizeMode)
            or nameof(MainWindowViewModel.AllowUpscaling)
            or nameof(MainWindowViewModel.AllowFurtherDimensionReduction))
            SettingsFeedbackText.Text = "Unsaved changes. Choose Save settings to remember these preferences.";
    }

    private void SaveSettings_Click(object sender, RoutedEventArgs e)
    {
        if (!ViewModel.TryCreatePreferences(out AppPreferences preferences, out string validationMessage))
        {
            SettingsFeedbackText.Text = validationMessage;
            return;
        }
        if (SavePreferences(preferences))
            SettingsFeedbackText.Text = "Settings saved. They will be restored the next time you open the app.";
    }

    private void ResetSettings_Click(object sender, RoutedEventArgs e)
    {
        var defaults = new AppPreferences();
        if (!SavePreferences(defaults)) return;
        ViewModel.LoadPreferences(defaults);
        SettingsFeedbackText.Text = "Default settings restored and saved.";
    }

    private bool SavePreferences(AppPreferences preferences)
    {
        try
        {
            preferencesStore.Save(preferences);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            SettingsFeedbackText.Text = "Settings could not be saved. " + ex.Message;
            return false;
        }
    }

    private void ConfigureOptimisingSpinner()
    {
        var sheet =
            new BitmapImage(
                new Uri(
                    "pack://application:,,,/NexMedia.WebImageOptimiser.Desktop;component/Assets/loading-spinner-sheet.png",
                    UriKind.Absolute));
        sheet.Freeze();

        const int frameSize = 128;
        const int columns = 4;
        const int frameCount = 12;

        optimisingSpinnerFrames =
            new BitmapSource[frameCount];

        for (int index = 0; index < frameCount; index++)
        {
            int column = index % columns;
            int row = index / columns;

            var frame =
                new CroppedBitmap(
                    sheet,
                    new Int32Rect(
                        column * frameSize,
                        row * frameSize,
                        frameSize,
                        frameSize));
            frame.Freeze();
            optimisingSpinnerFrames[index] = frame;
        }

        OptimisingSpinnerFrame.Source =
            optimisingSpinnerFrames[0];

        optimisingSpinnerTimer.Interval =
            TimeSpan.FromMilliseconds(1000d / 12d);
        optimisingSpinnerTimer.Tick +=
            OptimisingSpinnerTimer_Tick;
    }

    private void ConfigureHeadlineRotation()
    {
        string configurationPath =
            Path.Combine(
                AppContext.BaseDirectory,
                "headline-rotation.json");

        HeadlineRotationOptions options =
            HeadlineRotationOptions.Load(configurationPath);

        headlineMessages = options.Messages;
        headlineCharacterDelay =
            TimeSpan.FromMilliseconds(
                options.CharacterDelayMilliseconds);
        headlineCharacterFadeDuration =
            TimeSpan.FromMilliseconds(
                options.CharacterFadeDurationMilliseconds);

        SetHeadlineImmediately(headlineMessages[0]);

        if (headlineMessages.Count < 2)
        {
            return;
        }

        headlineRotationTimer.Interval =
            TimeSpan.FromSeconds(options.IntervalSeconds);
        headlineRotationTimer.Tick +=
            HeadlineRotationTimer_Tick;
        if (ViewModel.InterfaceAnimationsEnabled)
            headlineRotationTimer.Start();
    }

    private async void HeadlineRotationTimer_Tick(
        object? sender,
        EventArgs e)
    {
        if (isChangingHeadline || !ViewModel.InterfaceAnimationsEnabled)
        {
            return;
        }

        isChangingHeadline = true;
        headlineRotationTimer.Stop();
        CancellationToken cancellationToken = headlineAnimationCancellation.Token;

        try
        {
            await AnimateTextOutAsync(RotatingHeadlineText, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            headlineMessageIndex =
                (headlineMessageIndex + 1) % headlineMessages.Count;

            await AnimateTextInAsync(RotatingHeadlineText,
                headlineMessages[headlineMessageIndex], cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        finally
        {
            isChangingHeadline = false;

            if (IsLoaded && ViewModel.InterfaceAnimationsEnabled && !headlineAnimationCancellation.IsCancellationRequested)
            {
                headlineRotationTimer.Start();
            }
        }
    }

    private void SetHeadlineImmediately(string message)
    {
        SetAnimatedTextImmediately(
            RotatingHeadlineText,
            message);
    }

    private void SetAnimatedTextImmediately(
        TextBlock target,
        string message)
    {
        target.Inlines.Clear();

        foreach (char character in message)
        {
            target.Inlines.Add(
                CreateAnimatedCharacter(
                    target,
                    character,
                    1));
        }
    }

    private async Task AnimateTextOutAsync(TextBlock target, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!ViewModel.InterfaceAnimationsEnabled)
        {
            target.Inlines.Clear();
            return;
        }
        Run[] characters =
            target.Inlines
                .OfType<Run>()
                .ToArray();

        var animations =
            new List<Task>(characters.Length);

        for (int index = characters.Length - 1;
             index >= 0;
             index--)
        {
            cancellationToken.ThrowIfCancellationRequested();
            animations.Add(
                AnimateCharacterOpacityAsync(
                    characters[index],
                    0));

            if (index > 0 &&
                headlineCharacterDelay > TimeSpan.Zero)
            {
                await Task.Delay(headlineCharacterDelay, cancellationToken);
            }
        }

        await Task.WhenAll(animations);
        cancellationToken.ThrowIfCancellationRequested();
        target.Inlines.Clear();
    }

    private async Task AnimateTextInAsync(
        TextBlock target,
        string message,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!ViewModel.InterfaceAnimationsEnabled)
        {
            SetAnimatedTextImmediately(target, message);
            return;
        }
        target.Inlines.Clear();

        var animations =
            new List<Task>(message.Length);

        for (int index = 0;
             index < message.Length;
             index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Run character =
                CreateAnimatedCharacter(
                    target,
                    message[index],
                    0);

            target.Inlines.Add(character);
            animations.Add(
                AnimateCharacterOpacityAsync(
                    character,
                    1));

            if (index < message.Length - 1 &&
                headlineCharacterDelay > TimeSpan.Zero)
            {
                await Task.Delay(headlineCharacterDelay, cancellationToken);
            }
        }

        await Task.WhenAll(animations);
    }

    private static Run CreateAnimatedCharacter(
        TextBlock target,
        char character,
        double opacity)
    {
        Color color =
            target.Foreground is SolidColorBrush foreground
                ? foreground.Color
                : Colors.White;

        var characterBrush =
            new SolidColorBrush(color)
            {
                Opacity = opacity
            };

        // Keep existing letters in sync when the theme changes during a headline.
        BindingOperations.SetBinding(characterBrush, SolidColorBrush.ColorProperty,
            new Binding("Foreground.Color") { Source = target });

        return new Run(character.ToString())
        {
            Foreground = characterBrush
        };
    }

    private Task AnimateCharacterOpacityAsync(
        Run character,
        double targetOpacity)
    {
        if (character.Foreground is not SolidColorBrush brush)
        {
            return Task.CompletedTask;
        }

        if (!ViewModel.InterfaceAnimationsEnabled || headlineCharacterFadeDuration <= TimeSpan.Zero)
        {
            brush.Opacity = targetOpacity;
            return Task.CompletedTask;
        }

        var completion =
            new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);

        var animation =
            new DoubleAnimation
            {
                From = brush.Opacity,
                To = targetOpacity,
                Duration = headlineCharacterFadeDuration,
                FillBehavior = FillBehavior.Stop
            };

        animation.Completed += (_, _) =>
        {
            brush.Opacity = targetOpacity;
            brush.BeginAnimation(
                Brush.OpacityProperty,
                null);
            completion.TrySetResult();
        };

        brush.BeginAnimation(
            Brush.OpacityProperty,
            animation);

        return completion.Task;
    }

    private static Task AnimateElementOpacityAsync(
        UIElement target,
        double targetOpacity,
        TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero)
        {
            target.Opacity = targetOpacity;
            return Task.CompletedTask;
        }

        var completion =
            new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);

        var animation =
            new DoubleAnimation
            {
                From = target.Opacity,
                To = targetOpacity,
                Duration = duration,
                FillBehavior = FillBehavior.Stop
            };

        animation.Completed += (_, _) =>
        {
            target.Opacity = targetOpacity;
            target.BeginAnimation(
                OpacityProperty,
                null);
            completion.TrySetResult();
        };

        target.BeginAnimation(
            OpacityProperty,
            animation);

        return completion.Task;
    }

    private async Task ShowOptimisingOverlayAsync(int imageCount)
    {
        OptimisingMessageText.Inlines.Clear();
        OptimisingProgressText.Text =
            $"Preparing 0 of {imageCount}";

        OptimisingOverlay.Opacity = 0;
        OptimisingOverlay.Visibility = Visibility.Visible;

        StartOptimisingSpinner();

        await Task.WhenAll(
            AnimateElementOpacityAsync(
                OptimisingOverlay,
                1,
                ViewModel.InterfaceAnimationsEnabled ? OptimisationAreaFadeDuration : TimeSpan.Zero),
            AnimateTextInAsync(
                OptimisingMessageText,
                "Optimising your images…"));

        ImageAreaContent.Visibility = Visibility.Collapsed;
    }

    private async Task HideOptimisingOverlayAsync()
    {
        await AnimateTextOutAsync(OptimisingMessageText);

        ImageAreaContent.Opacity = 1;
        ImageAreaContent.Visibility = Visibility.Visible;

        await AnimateElementOpacityAsync(
            OptimisingOverlay,
            0,
            ViewModel.InterfaceAnimationsEnabled ? OptimisationAreaFadeDuration : TimeSpan.Zero);

        StopOptimisingSpinner();
        OptimisingOverlay.Visibility = Visibility.Collapsed;
    }

    private void StartOptimisingSpinner()
    {
        optimisingSpinnerFrameIndex = 0;
        OptimisingSpinnerFrame.Source =
            optimisingSpinnerFrames[0];
        if (ViewModel.InterfaceAnimationsEnabled)
            optimisingSpinnerTimer.Start();
    }

    private void StopOptimisingSpinner()
    {
        optimisingSpinnerTimer.Stop();
        optimisingSpinnerFrameIndex = 0;

        if (optimisingSpinnerFrames.Length > 0)
        {
            OptimisingSpinnerFrame.Source =
                optimisingSpinnerFrames[0];
        }
    }

    private void OptimisingSpinnerTimer_Tick(
        object? sender,
        EventArgs e)
    {
        optimisingSpinnerFrameIndex =
            (optimisingSpinnerFrameIndex + 1) %
            optimisingSpinnerFrames.Length;
        OptimisingSpinnerFrame.Source =
            optimisingSpinnerFrames[optimisingSpinnerFrameIndex];
    }

    protected override void OnClosed(EventArgs e)
    {
        ViewModel.PropertyChanged -= Preferences_PropertyChanged;
        headlineAnimationCancellation.Cancel();
        headlineAnimationCancellation.Dispose();
        headlineRotationTimer.Stop();
        headlineRotationTimer.Tick -=
            HeadlineRotationTimer_Tick;
        StopOptimisingSpinner();
        optimisingSpinnerTimer.Tick -=
            OptimisingSpinnerTimer_Tick;

        base.OnClosed(e);
    }

    private void MinimiseWindow_Click(
        object sender,
        RoutedEventArgs e)
    {
        SystemCommands.MinimizeWindow(this);
    }

    private void MaximiseRestoreWindow_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (WindowState == WindowState.Maximized)
        {
            SystemCommands.RestoreWindow(this);
        }
        else
        {
            SystemCommands.MaximizeWindow(this);
        }
    }

    private void CloseWindow_Click(
        object sender,
        RoutedEventArgs e)
    {
        SystemCommands.CloseWindow(this);
    }

    private void Window_StateChanged(
        object? sender,
        EventArgs e)
    {
        if (MaximiseRestoreButton is null)
        {
            return;
        }

        MaximiseRestoreButton.Content =
            WindowState == WindowState.Maximized
                ? "\uE923"
                : "\uE922";
    }

    private void SettingsNavigation_Click(
        object sender,
        RoutedEventArgs e)
    {
        bool showSettings =
            SettingsNavigationButton.IsChecked == true;

        SettingsPage.Visibility = showSettings
            ? Visibility.Visible
            : Visibility.Collapsed;
        HelpPage.Visibility = Visibility.Collapsed;
        HelpNavigationButton.IsChecked = false;
        MainHeaderCopy.Visibility = showSettings
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    private void HelpNavigation_Click(
        object sender,
        RoutedEventArgs e)
    {
        bool showHelp =
            HelpNavigationButton.IsChecked == true;

        HelpPage.Visibility = showHelp
            ? Visibility.Visible
            : Visibility.Collapsed;
        SettingsPage.Visibility = Visibility.Collapsed;
        SettingsNavigationButton.IsChecked = false;
        MainHeaderCopy.Visibility = showHelp
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    private void BackToOptimiser_Click(
        object sender,
        RoutedEventArgs e)
    {
        SettingsPage.Visibility = Visibility.Collapsed;
        HelpPage.Visibility = Visibility.Collapsed;
        SettingsNavigationButton.IsChecked = false;
        HelpNavigationButton.IsChecked = false;
        MainHeaderCopy.Visibility = Visibility.Visible;
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
                "Supported images|*.jpg;*.jpeg;*.png;*.webp|" +
                "JPEG images|*.jpg;*.jpeg|" +
                "PNG images|*.png|" +
                "WebP images|*.webp"
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
        SelectionChangedEventArgs e)
    {
        UpdateActionState();
    }

    private void ImageSelectionCheckBox_PreviewMouseLeftButtonDown(
        object sender,
        MouseButtonEventArgs e)
    {
        if (sender is not DependencyObject selectionControl)
        {
            return;
        }

        ListBoxItem? item =
            FindVisualParent<ListBoxItem>(selectionControl);

        if (item is null)
        {
            return;
        }

        item.IsSelected = !item.IsSelected;
        item.Focus();
        e.Handled = true;
    }

    private static T? FindVisualParent<T>(DependencyObject child)
        where T : DependencyObject
    {
        DependencyObject? current = child;

        while (current is not null)
        {
            if (current is T match)
            {
                return match;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return null;
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

        ViewModel.ApplyOptimisationSettings(
            selectedImages,
            settings!);

        UpdateActionState();
    }

    private async void Optimise_Click(
        object sender,
        RoutedEventArgs e)
    {
        ImageBatchItem[] selectedImages =
            ImagesGrid.SelectedItems
                .Cast<ImageBatchItem>()
                .ToArray();

        if (selectedImages.Length == 0)
        {
            return;
        }

        if (selectedImages.Any(image =>
                image.AssignedOptimisationSettings is null))
        {
            MessageBox.Show(
                this,
                "Apply output settings to every selected image first.",
                "Settings required",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        isOptimising = true;
        UpdateActionState();

        BatchActionsPanel.IsEnabled = false;

        OverallProgressBar.Minimum = 0;
        OverallProgressBar.Maximum = selectedImages.Length;
        OverallProgressBar.Value = 0;

        try
        {
            await ShowOptimisingOverlayAsync(
                selectedImages.Length);

            int completed = 0;

            foreach (ImageBatchItem image in selectedImages)
            {
                OptimisingProgressText.Text =
                    $"Optimising {completed + 1} of {selectedImages.Length}";

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
                    OptimisingProgressText.Text =
                        $"{completed} of {selectedImages.Length} complete";
                }
            }
        }
        finally
        {
            await HideOptimisingOverlayAsync();

            isOptimising = false;

            BatchActionsPanel.IsEnabled = true;

            UpdateActionState();
        }
    }

    private void UpdateActionState()
    {
        bool isBusy =
            isOptimising || isExporting;

        ImageBatchItem[] selectedImages =
            ImagesGrid.SelectedItems
                .Cast<ImageBatchItem>()
                .ToArray();

        bool hasSelection =
            selectedImages.Length > 0;

        bool hasSuccessfulResults =
            ViewModel.Images.Any(image =>
                image.OptimisationResult is not null);

        bool selectionHasSuccessfulResults =
            selectedImages.Any(image =>
                image.OptimisationResult is not null);

        RemoveSelectedButton.IsEnabled =
            hasSelection && !isBusy;

        ApplySelectionButton.IsEnabled =
            hasSelection && !isBusy;

        OptimiseButton.IsEnabled =
            hasSelection && !isBusy;

        SettingsNavigationButton.IsEnabled = !isBusy;
        HelpNavigationButton.IsEnabled = !isBusy;

        ExportAllButton.IsEnabled =
            hasSuccessfulResults && !isBusy;

        ExportSelectedButton.IsEnabled =
            selectionHasSuccessfulResults && !isBusy;
    }

    private async void ExportAll_Click(
        object sender,
        RoutedEventArgs e)
    {
        ImageBatchItem[] images =
            ViewModel.Images
                .Where(image =>
                    image.OptimisationResult is not null)
                .ToArray();

        await ExportImagesAsync(images);
    }

    private async void ExportSelected_Click(
        object sender,
        RoutedEventArgs e)
    {
        ImageBatchItem[] images =
            ImagesGrid.SelectedItems
                .Cast<ImageBatchItem>()
                .Where(image =>
                    image.OptimisationResult is not null)
                .ToArray();

        await ExportImagesAsync(images);
    }

    private async Task ExportImagesAsync(
        IReadOnlyCollection<ImageBatchItem> images)
    {
        if (images.Count == 0)
        {
            MessageBox.Show(
                this,
                "There are no successfully optimised images to export.",
                "Nothing to export",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            return;
        }

        var dialog = new OpenFolderDialog
        {
            Title = "Choose export folder",
            Multiselect = false
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        isExporting = true;
        UpdateActionState();

        BatchActionsPanel.IsEnabled = false;
        ImagesGrid.IsEnabled = false;

        OverallProgressBar.Minimum = 0;
        OverallProgressBar.Maximum = images.Count;
        OverallProgressBar.Value = 0;

        int successful = 0;
        int failed = 0;
        int completed = 0;

        try
        {
            foreach (ImageBatchItem image in images)
            {
                try
                {
                    string outputPath =
                        await OptimisedImageExportService.ExportAsync(
                            image,
                            dialog.FolderName);

                    image.MarkExported(outputPath);
                    successful++;
                }
                catch (Exception exception)
                    when (exception is not OutOfMemoryException)
                {
                    image.MarkExportFailed(exception.Message);
                    failed++;
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
            isExporting = false;

            BatchActionsPanel.IsEnabled = true;
            ImagesGrid.IsEnabled = true;

            UpdateActionState();
        }

        MessageBox.Show(
            this,
            $"Exported: {successful}\nFailed: {failed}",
            "Export complete",
            MessageBoxButton.OK,
            failed == 0
                ? MessageBoxImage.Information
                : MessageBoxImage.Warning);
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
