# App settings

Open the cog in the header to change preferences.

- **App appearance:** choose Dark or Light. Both retain the orange accent. Turn off **Enable interface animations** to keep the headline and spinner still and disable typewriter, fade and dropdown animations. Progress text and the progress bar continue updating.
- **Output defaults:** output format, maximum file size, minimum WebP quality and optional further dimension reduction.
- **Resize defaults:** preset, fit/crop mode and upscaling.

Changes apply immediately in the current session. **Save settings** validates and remembers all the options above for the next launch. Changes to the same controls on the optimiser page can also be saved here. **Reset to defaults** restores and saves Dark mode, animations enabled, the 1600 × 400 banner preset, WebP, 200 KB, quality 60, fit within dimensions, no upscaling and no further dimension reduction. Neither operation changes settings already assigned to batch images.

Preferences are stored per Windows user at `%LOCALAPPDATA%\NexMedia\WebImageOptimiser\settings.json`, outside the installation folder. Invalid or unreadable preferences fall back to defaults, with a message on the settings page. Failed saves are reported there and do not replace the last successfully saved preferences. Batch images and optimisation results are not saved as preferences.

Theme colours live in `src/NexMedia.WebImageOptimiser.Desktop/Themes/DarkPalette.xaml` and `LightPalette.xaml`. Shared control styles remain in `DarkTheme.xaml`. The headline messages and timing remain configured separately in `headline-rotation.json`.

## Desktop checks

1. Switch between Dark and Light. Check the main page, image cards, selection highlights, settings, help, dropdowns and optimisation overlay. Orange accents should remain, and text should stay readable.
2. Disable animations, including while a headline is being typed. The full headline should remain still. Optimising should show a static spinner and message immediately, with progress continuing to update.
3. Re-enable animations and confirm headline rotation and the animated loading screen return.
4. Change every preference, save, close and reopen. Verify all saved values return and the saved theme appears from startup.
5. Enter an invalid target or quality and save. A validation message should appear without replacing saved preferences.
6. Reset, close and reopen. Confirm the original defaults return. Already assigned batch settings should remain unchanged during reset.
