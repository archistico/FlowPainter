# M14.3 — Connectivity and diagnostics

**Status: DONE — validated with 907 tests**

## Purpose

M14.3 converts provisional SLIC cluster ownership into a topologically valid regional partition. The milestone remains detached from the active Flow, Primitive and Hybrid planning pipeline, but every published fine label is now connected and suitable for the descriptor, adjacency and hierarchy stages that follow.

No external SLIC package, machine-learning runtime or additional image-processing dependency is introduced.

## Connectivity normalization

`RegionConnectivityNormalizer` performs a deterministic four-neighbour pass over the raw SLIC assignments:

1. split every raw label into its connected components;
2. count all additional components as disconnected-label repairs;
3. build component adjacency from right/down pixel transitions;
4. identify components smaller than one quarter of the expected SLIC region area;
5. merge each undersized component into an adjacent component using stable priority:
   - longest shared boundary;
   - larger target component;
   - lower stable component identifier;
6. update adjacency after every merge;
7. compact final identifiers by first image-order appearance;
8. publish a new immutable `RegionLabelMap`.

Merging only adjacent connected components preserves connectivity. A single isolated region is retained even when the automatic minimum is larger than the complete image.

## Diagnostics

`SegmentationDiagnostics` now includes:

- connected-component count before undersized merges;
- final connected-region count;
- disconnected components repaired;
- undersized components merged;
- minimum region area;
- maximum region area;
- mean region area;
- population standard deviation of region area.

`RegionSizeDistribution` validates and stores the size statistics independently from UI or rendering code.

## Diagnostic rendering

`SegmentationDiagnosticRenderer` provides two deterministic, on-demand proxy images:

- **mean-colour preview** — every region is filled with its mean source RGBA value;
- **boundary overlay** — source pixels are retained while pixels touching a label transition are drawn with the FlowPainter blue diagnostic colour, or a caller-provided colour.

Both operations:

- require source and label dimensions to match;
- preserve the source object;
- support cancellation;
- use the shared memory admission policy;
- return ordinary immutable `RgbaImage` values;
- do not require Avalonia or SkiaSharp.

The overlays are diagnostic services only. UI controls and active-pipeline adoption remain scheduled for M14.8 and M14.7 respectively.

## Resource estimate

`RegionSegmentationEstimator` now reserves the connectivity working set before source sampling:

- one 32-bit component label per pixel;
- one 32-bit flood-fill queue entry per pixel;
- conservative component/adjacency state per estimated region;
- final compact 16- or 32-bit label storage.

This keeps M13.4.2's pre-allocation rejection guarantee intact while topology repair is added.

## Validation

M14.3 added **25 focused Application tests**, raising the complete validated suite from 882 to **907 tests**.

Coverage includes:

- disconnected raw-label splitting;
- deterministic undersized-component merging;
- shared-boundary, size and identifier tie-breaking;
- compact first-appearance relabelling;
- complete coverage and absence of empty labels;
- final four-neighbour connectivity;
- cancellation and invalid-input rejection;
- region-size statistics;
- mean-colour and boundary diagnostic images;
- source immutability and dimension validation;
- connectivity progress and analyzer diagnostics;
- revised exact segmentation memory estimates.

Run:

```bash
dotnet restore FlowPainter.sln
dotnet build FlowPainter.sln -c Release --no-restore
dotnet test FlowPainter.sln -c Release --no-build
```

Manual checks:

1. all 907 tests were confirmed passing;
2. confirm the application icon remains visible;
3. confirm existing projects still open and render as before;
4. no new segmentation controls should appear yet;
5. no project or preset schema version should change.

## Exit criteria

- every final label is four-neighbour connected;
- every pixel has exactly one compact label;
- no empty label is published;
- undersized repairs are deterministic;
- region-size statistics match the final label map;
- diagnostic images match the proxy dimensions and do not mutate the source;
- connectivity memory is admitted before expensive source sampling;
- the active painting pipeline remains unchanged;
- all **907** tests passed.
