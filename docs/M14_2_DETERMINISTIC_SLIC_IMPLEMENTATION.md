# M14.2 — Deterministic SLIC implementation

**Status: DONE — validated with 882 tests**

## Purpose

M14.2 provides the first executable, model-free SLIC implementation behind the M14.1 `IRegionSegmentationAnalyzer` contract. The implementation remains detached from the active FlowPainter analysis/render pipeline: it can be exercised and validated independently, but it does not yet replace the schema-11 semantic compatibility path or add segmentation controls to the desktop UI.

The milestone also adds the approved FlowPainter application icon to the Avalonia window, Windows executable resources and publish output.

## Algorithm

`SlicRegionSegmentationAnalyzer` performs these deterministic stages:

1. checks the M14.1 resource estimate against the shared 2 GiB admission policy before sampling the source;
2. samples the complete proxy without mutating it;
3. composites transparent pixels against white so invisible RGB payloads do not create false colour regions;
4. optionally applies a separable Gaussian pre-blur;
5. converts normalized sRGB to CIELAB using a D65 reference white;
6. derives a regular row/column grid from `TargetRegionSize`;
7. moves each initial centre to the lowest-gradient pixel in its deterministic 3 × 3 neighbourhood;
8. assigns pixels only inside each centre's local SLIC search window;
9. updates Lab and image-coordinate centroids;
10. repeats until `ConvergenceTolerance` or `MaximumIterations` is reached;
11. publishes provisional compact cluster ownership for M14.3 topology normalization;
12. calculates the basic area, bounds and centroid values required by the M14.1 result contract.

The distance policy is the standard SLIC combination of squared CIELAB distance and compactness-weighted normalized spatial distance. Traversal order and equal-distance handling are stable; no random state or seed is used.

## Published result at this stage

M14.2 returns:

- a complete compact `RegionLabelMap`;
- one basic `ImageRegion` per used label;
- deterministic iteration/convergence diagnostics;
- an identity `RegionHierarchy`;
- an empty `RegionAdjacencyGraph`.

Raw SLIC labels can still contain disconnected components. M14.3 owns component repair, undersized-region merging, final connectivity enforcement, relabelling diagnostics and visual overlays. M14.2 was validated with all 882 tests passing and remains the accepted clustering baseline.

`RegionLabelMap` gains a signed-assignment overload so the analyzer can copy its compact `int` assignment buffer directly into 16- or 32-bit immutable storage without allocating an additional full-size `uint` staging map.

## Progress and cancellation

Progress is monotonic across:

```text
Preparing
Smoothing (only when enabled)
ConvertingColor
InitializingClusters
AssigningPixels
UpdatingClusters
BuildingResult
Completed
```

Cancellation is checked before allocation-sensitive work and inside source sampling, smoothing, conversion, initialization, assignment, update, compaction and result-building loops.

## Resource policy

The analyzer calls `RegionSegmentationEstimator` before reading a pixel and rejects an over-budget request through `WorkloadBudgetPolicy`. The implementation reuses its 12-byte colour buffer for RGB, pre-smoothed RGB and final Lab values; local Gaussian work arrays scale only with the largest image dimension.

The algorithm does not allocate a separate full-resolution image copy beyond the estimated colour, distance, assignment and final label buffers.

## Application icon

`FlowPainter.App` now includes:

- `Assets/FlowPainter.ico`, containing 16, 24, 32, 48, 64, 128 and 256 px frames;
- PNG assets at 32, 64, 128, 256 and 512 px;
- the Avalonia `Window.Icon` resource;
- the MSBuild `ApplicationIcon` executable resource;
- icon assets copied to normal build and publish output for future platform packaging.

No font, theme or layout behaviour changes are included.

## Automated validation

M14.2 adds **19** focused tests:

- 17 Application cases for deterministic grids, colour boundaries, basic regions, identity hierarchy, progress, smoothing, cancellation, convergence, source immutability, alpha handling, compact storage and pre-sampling budget rejection;
- 2 Domain cases for direct signed-assignment label-map ownership and negative-label rejection.

Validated complete suite: **882 tests**.

## Manual validation

1. Run `dotnet build` and confirm zero compiler/analyzer warnings.
2. `dotnet test` was confirmed with all 882 tests passing.
3. Start the application and verify the supplied icon in the title bar and taskbar.
4. Publish on Windows and verify the executable icon and copied icon assets.
5. Confirm that existing image, project, analysis and rendering behaviour is unchanged.
6. Confirm there are still no SLIC controls or overlays in the desktop UI.

## Exit criteria

- deterministic SLIC clustering is executable through `IRegionSegmentationAnalyzer`;
- RGB/Lab conversion, optional smoothing, local assignment and centroid update are covered;
- results are compact, complete and reproducible;
- expensive loops honor cancellation and monotonic progress;
- over-budget work is rejected before source sampling;
- the source proxy is never mutated;
- topology repair remains isolated for M14.3;
- the application icon is embedded and publishable;
- all **882** tests pass.
