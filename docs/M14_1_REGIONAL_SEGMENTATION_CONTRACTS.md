# M14.1 — Regional segmentation contracts

**Status: DONE — validated 2026-07-17 with 863 tests**

## Purpose

M14.1 establishes the pure, immutable boundary that the deterministic SLIC implementation will satisfy. It deliberately does not cluster pixels, change the desktop UI, migrate projects or replace the current schema-11 semantic compatibility path.

The milestone makes invalid or ambiguous segmentation data difficult to represent before the expensive algorithm is introduced.

## Domain contracts

`FlowPainter.Domain.Segmentation` contains:

- `RegionLabelMap`, with one compact label per proxy pixel;
- `RegionLabelRow`, for allocation-free read-only row access;
- `RegionLabelStorageKind`, selecting `Compact` 16-bit storage through 65,536 regions and `Wide` 32-bit storage above that boundary;
- `PixelBounds`, `RegionCentroid`, `ImageRegion` and immutable `RegionVisualDescriptors`;
- `RegionAdjacency` and deterministic `RegionAdjacencyGraph` lookup;
- `RegionHierarchyLevel` and `RegionHierarchy` with identity level zero and monotonic coarsening.

### Label-map ownership

`RegionLabelMap.Create` copies caller labels and validates:

- dimensions and label count agree;
- region count is positive and cannot exceed pixel count;
- every label lies in `0 .. RegionCount - 1`;
- every compact label is used by at least one pixel.

The map exposes indexed access, allocation-free row views, explicit copies and per-region pixel counts. Callers cannot mutate the backing storage.

Connectivity remains a required active-pipeline invariant. M14.2 publishes a detached provisional result for algorithm validation; M14.3 repairs and verifies connectivity before any regional result can be adopted by the active analysis and painting pipeline.

### Graph and hierarchy invariants

Adjacency identifiers are stored once in lower/higher order. Graph lookup is symmetric, duplicate edges and out-of-range region references are rejected, and edge ordering is deterministic.

Hierarchy level zero is an identity mapping. Every later level maps every fine label to a compact parent, may only reduce the number of regions and cannot split a parent established at the previous level. This level structure is acyclic by construction.

## Application contracts

`FlowPainter.Application.Segmentation` contains:

- `IRegionSegmentationAnalyzer`;
- immutable `RegionSegmentationRequest` and `RegionSegmentationSettings`;
- `RegionSegmentationProgress` and `RegionSegmentationStage`;
- `SegmentationDiagnostics`;
- detached `RegionSegmentationResult`;
- `RegionSegmentationEstimate` and `RegionSegmentationEstimator`.

`RegionSegmentationResult` validates that:

- there is one ordered `ImageRegion` for every label;
- region pixel counts and normalized areas match the label map;
- bounds remain inside the proxy;
- total region area equals total image area;
- graph, hierarchy and diagnostics use the same final region count.

## Settings

Initial settings are fixed for the M14.2 algorithm contract:

```text
TargetRegionSize       default 64 px, range 4–2048
Compactness            default 10, finite and > 0
PreBlurSigma            default 0.8, range 0–10
MaximumIterations       default 10, range 1–100
ConvergenceTolerance    default 0.5 px, finite and > 0
```

No random seed is required because SLIC traversal, initialization and tie breaking must be deterministic.

## Resource estimation

`RegionSegmentationEstimator` predicts before allocation:

- grid-derived region count;
- label storage kind and bytes;
- Lab colour buffer;
- distance buffer;
- assignment buffer;
- optional smoothing buffer;
- cluster-state bytes;
- assignment evaluations across configured iterations.

`AnalysisMemoryEstimator` now consumes this exact peak instead of the provisional fixed 24-byte-per-pixel reserve from M13.4.2. The global 2 GiB admission policy remains unchanged.

## Automated validation

M14.1 adds **59** tests:

- 30 Domain cases for label ownership/storage, rows, bounds, descriptors, adjacency and hierarchy;
- 29 Application cases for settings, requests, progress, diagnostics, detached-result consistency and resource estimation.

Validated complete suite: **863 tests**.

## Manual validation

No new visual behaviour is expected. Confirm only that:

1. the solution builds with zero analyzer warnings;
2. all existing image/project/render workflows behave unchanged;
3. project schema remains 11 and preset schema remains 8;
4. the memory estimate shown for normal previews remains supported;
5. no SLIC controls or overlays appear yet.

## Exit criteria

- Domain and Application contracts have no Avalonia or SkiaSharp dependency;
- labels are compact, immutable and storage-aware;
- regions, graph, hierarchy and diagnostics reject inconsistent identities/counts;
- settings and progress reject invalid finite/range values;
- segmentation resource estimates are checked and deterministic;
- current runtime analysis and rendering behaviour is unchanged;
- all **863** tests pass (validated 2026-07-17).
