# M14.4 — Regional descriptors

**Status: DONE — validated with 920 tests**

## Purpose

M14.4 enriches every connected fine SLIC region with deterministic geometric, colour and internal-structure descriptors. The result remains detached from the active Flow, Primitive and Hybrid planning pipeline; M14.5 and M14.6 will consume these values to build the Region Adjacency Graph and hierarchy before M14.7 activates the regional path.

The implementation remains fully local, model-free and independent of Avalonia, SkiaSharp and external SLIC packages.

## Descriptor calculation

`RegionDescriptorCalculator` scans the final compact label map and the original analysis proxy. It deliberately describes the source proxy rather than the optionally blurred working image used by SLIC clustering, so descriptor values do not change merely because `PreBlurSigma` changes.

### Geometry

For each region the calculator publishes:

- pixel count and normalized area;
- exclusive `PixelBounds`;
- centroid measured from pixel centres;
- digital perimeter in exposed four-neighbour pixel-edge units;
- normalized compactness `4πA / P²`, clamped to `[0, 1]`.

The digital definition is exact for synthetic fixtures and remains stable across platforms.

### CIELAB statistics

Source RGBA pixels are composited against white, converted from normalized sRGB to D65 CIELAB and accumulated without per-region masks.

`RegionVisualDescriptors` stores population statistics for:

- mean CIELAB `L*`, `a*` and `b*`;
- variance of `L*`, `a*` and `b*`.

`L*` is FlowPainter's perceptual lightness descriptor. Values are clamped to the valid `[0, 100]` range before accumulation so numerical conversion noise cannot produce an invalid white value.

### Texture, edge density and orientation

Internal structure is calculated only from same-region neighbours. Contrast across a SLIC boundary is excluded because shared-boundary evidence belongs to M14.5.

For each pixel:

1. the strongest valid one-sided horizontal and vertical `L*` differences are selected;
2. squared gradient magnitude contributes to `TextureEnergy`;
3. pixels with gradient magnitude of at least `2 L*` units contribute to `EdgeDensity`;
4. qualifying gradients contribute an undirected tangent orientation using doubled-angle weighted accumulation.

Published values are:

- `TextureEnergy` — mean squared internal lightness-gradient magnitude;
- `EdgeDensity` — fraction of region pixels above the fixed internal-edge threshold;
- `DominantOrientationRadians` — prevailing **tangent** in the half-open range `[0, π)`; uniform regions use `0`.

Using tangent rather than gradient direction prepares the descriptor for painterly stroke alignment while preserving the separate shared-boundary tangent work planned for M14.5.

## Memory and cancellation

The calculator uses:

- one global 32-bit lightness buffer for the proxy;
- fixed-size scalar accumulator arrays proportional to region count;
- final immutable `ImageRegion` and `RegionVisualDescriptors` objects.

It never allocates a full-size mask or image buffer per region. `RegionSegmentationEstimator` now reserves the lightness buffer and a conservative 320-byte descriptor/output allowance per estimated region before SLIC begins. Direct descriptor calculation also uses the shared 2 GiB admission policy.

Cancellation is observed:

- before descriptor allocations;
- during source/geometry rows;
- during texture/orientation rows;
- while final region objects are materialized.

## Domain contract refinement

`RegionVisualDescriptors.DominantOrientationRadians` now rejects values outside `[0, π)`. The value is undirected, so angles separated by π represent the same orientation and must be normalized before publication.

No project or preset schema changes are required. Descriptor values are derived analysis data and are not persisted in M14.4.

## Validation

M14.4 adds **13 focused Application tests**, raising the complete suite from the validated 907-case M14.3 baseline to **920 tests**, all confirmed passing on Windows.

Coverage includes:

- analytically known rectangular and stepped geometry;
- exact digital perimeter and compactness;
- black/white CIELAB mean and population variance;
- transparent-pixel compositing against white;
- uniform versus textured regions;
- exclusion of inter-region contrast from texture statistics;
- horizontal and vertical dominant tangents;
- deterministic repeatability;
- source immutability;
- dimension validation and cancellation;
- analyzer publication of non-empty descriptors;
- revised descriptor-aware memory estimates.

Run:

```bash
dotnet restore FlowPainter.sln
dotnet build FlowPainter.sln -c Release --no-restore
dotnet test FlowPainter.sln -c Release --no-build
```

Manual checks:

1. confirmed: all 920 tests pass;
2. confirm the application icon remains visible;
3. open an existing project and confirm preview/final rendering is unchanged;
4. no new segmentation controls or overlays should appear yet;
5. project schema remains 11 and preset schema remains 8.

## Exit criteria

- every final region carries non-empty immutable descriptors;
- areas, bounds, centroids, digital perimeters and compactness match known fixtures;
- CIELAB means and population variances are deterministic and finite;
- texture/orientation calculations ignore cross-region contrast;
- dominant orientations are normalized to `[0, π)`;
- descriptor memory is admitted before segmentation allocations;
- no per-region full-size image or mask is allocated;
- the active painting pipeline remains unchanged;
- all **920** tests pass.
