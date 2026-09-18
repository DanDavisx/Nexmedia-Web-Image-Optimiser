# Milestones and review points

Work stops for review after each milestone. This document describes planned behaviour, not capabilities already shipped.

## 1. Baseplate — current milestone

- [x] .NET 10 solution compatible with Visual Studio 2026.
- [x] Separate WPF desktop and UI-independent Core projects.
- [x] Basic workflow window, empty batch list and preset preview.
- [x] Eleven starting presets and validated immutable settings.
- [x] VS Code build, test, run and debug configuration.
- [x] MSTest project, dependency lock files and build/run documentation.

Review the Windows/WPF choice, basic layout and 200 KB / quality 60 starting defaults.

## 2. Import and batch selection

- Choose a maintained raster library after checking .NET 10 compatibility, WebP encoding, alpha support, memory behaviour and all native dependency licences; record the selected version and licences.
- Import multiple JPEG, PNG, WebP and SVG files, or a folder. Decide folder recursion in the UI.
- Read filename, detected format, oriented dimensions and original byte count. Check file content, not just extension; isolate malformed-file failures.
- Correctly inspect EXIF orientation, SVG viewBox/unit dimensions and transparent inputs.
- Add individual/extended selection, select all, removal and deduplication. Keep originals untouched.
- Keep large imports responsive and bound metadata work.

Acceptance: valid format fixtures and malformed/unsupported files; rotated JPEG dimension reporting; SVG dimensions; duplicates and inaccessible files; selection behaviour.

## 3. Settings and resize pipeline

- Edit and persist presets; allow custom width and/or height.
- Apply settings snapshots only to selected images, allowing multiple groups in one batch.
- Provide global defaults, per-group byte-target overrides and configurable minimum WebP quality.
- Implement fit within bounds, explicit crop to fill, aspect-ratio preservation and no upscaling. Define crop behaviour for inputs smaller than the requested output without enlarging them.
- Apply orientation before resizing. Preserve alpha in WebP/PNG.
- Keep SVG vector data; do not route it through raster encoders. Define conservative SVG optimisation and how dimension presets apply to viewBox-based assets.

Acceptance: landscape/portrait/square fixtures, width-only bounds, exact crop geometry, small sources with no upscaling, EXIF rotation, alpha-channel preservation, independent settings for different selections, invalid options.

## 4. Optimisation and batch execution

- Search WebP quality using actual encoded byte lengths; keep the highest practical quality within the target and never go below the configured floor. Verify candidate output sizes rather than assuming perfectly monotonic sizes at each quality.
- Flag targets that cannot be reached at selected dimensions. Further dimension reduction is explicit and optional, with final dimensions visible.
- Use format-appropriate lossless optimisation for PNG and conservative vector-preserving SVG optimisation. Neither has a lossy WebP quality slider.
- Run processing asynchronously with bounded parallelism, bounded decoded-image memory, cancellation and per-file error isolation. Reject unsupported animation rather than silently discarding frames unless animation support is deliberately added.
- Report per-image status, overall progress, final dimensions/bytes, percentage saved and target achieved/unreachable. Handle outputs larger than inputs honestly.

Acceptance: achievable targets measured from real encoded bytes; highest practical selected quality; quality-floor failures; optional reduction; PNG/SVG unreachable targets; cancelled batches; corrupt/decompression-heavy input; one failing file does not stop others; concurrency bound.

## 5. Export and end-to-end verification

- Export all successful results or selected successful results.
- Preserve source files. Use output name collision checks plus atomic no-overwrite creation, including repeat exports, same basenames from different folders and attempts to export into the source folder.
- Manage temporary results and cancellation cleanup. Keep results available for retry if export fails.
- Finalise SVG safety/feature limitations, animation policy, metadata/colour-profile policy and image-library notices.

Acceptance: unchanged original file hashes, output collision races, partial export failure, cancelled export, selected-only output, end-to-end mixed-format batch and memory/performance checks with representative large images.
