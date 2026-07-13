# M4 — Structural detail map and manual regions

**Status:** DONE  
**Date:** 2026-07-13

## Purpose

M4 introduces the shared importance-map pipeline that will eventually guide both FlowPainter strokes and geometric primitives.

The milestone deliberately separates two concepts:

1. **automatic structural analysis**, which can be implemented without an external machine-learning runtime;
2. **manual user guidance**, which can override or reinforce the automatic map through normalized rectangular selections.

Semantic recognition of faces, eyes, mouth, subjects and other object classes is not claimed in M4. Those analyzers will produce additional normalized maps through the same application boundary in a later milestone.

## Automatic structural analysis

`IDetailMapAnalyzer` is the application contract. The initial `ImageDetailAnalyzer` implementation operates on the analysis proxy and combines:

- luminance-gradient magnitude;
- local RGB colour contrast;
- configurable base detail;
- configurable edge and contrast weights;
- deterministic box smoothing.

The result is a `DetailMap` with values in `[0, 1]` at proxy resolution. A full-size floating-point map is never allocated.

## Manual regions

The user can drag a rectangle over the displayed source image and choose:

- `IncreaseDetail` for focal areas;
- `ReduceDetail` for broad or unimportant background areas.

Each region stores:

- stable identifier;
- normalized bounds;
- strength in `[0, 1]`;
- manual origin;
- increase/reduce intent;
- display label.

`UniformImageViewport` handles letterboxing/pillarboxing and maps between Avalonia viewport coordinates and normalized source coordinates. Consequently, regions remain aligned when the window is resized.

The existing deterministic composition rules remain:

```text
Increase: value + strength × (1 - value)
Reduce:   value × (1 - strength)
```

## Detail-aware stroke planning

M4 adds a detail-aware overload of `FlowPainterPlanner` and version `flow-field-detail-v1`.

The composed detail value affects three independent properties:

### Placement

A cumulative weighted sampler assigns a larger share of the fixed stroke budget to important pixels:

```text
placement weight = 1 + placement bias × detail
```

The sampler uses `DeterministicRandom`; equal image, map, seed and settings therefore produce the same plan.

### Length

Background and detailed multipliers are interpolated from the local detail value. Defaults make background strokes longer and detailed strokes shorter.

### Width

Background and detailed width multipliers are likewise interpolated. Defaults make focal marks thinner and background marks broader.

The original `flow-field-v1` overload remains unchanged and retains its M3 golden sequence when no detail map is supplied.

## Visualization

`DetailMapOverlayRenderer` produces a disposable Skia image at proxy dimensions:

- blue represents low detail;
- green/yellow represents intermediate detail;
- red represents high detail.

The UI can toggle the heat map while rectangular region outlines remain visible. Increase regions use orange outlines; reduce regions use cyan outlines.

## Preset migration

Preset schema version increases from 1 to 2 to persist:

- structural-analysis settings;
- detail-influence settings.

Schema version 1 remains readable. Missing M4 values receive documented defaults. Manual regions are image-specific and are intentionally excluded from presets; they belong to the future project document.

## Automated coverage

M4 expands the suite to 249 cases:

- Domain: 55;
- Application: 164;
- Imaging.Skia: 13;
- Rendering.Skia: 17.

New coverage includes:

- structural-map validation and deterministic output;
- hard edges, chromatic contrast and smoothing;
- progress and cancellation;
- region composition and cancellation;
- normalized viewport mapping with letterboxing;
- detail-weighted placement distribution;
- detail-dependent length and width;
- detail-aware plan reproducibility;
- schema-1 preset migration;
- heat-map colour, dimensions, opacity and cancellation.

## Manual validation

1. Open a photograph with a clear subject and a broad background.
2. Toggle the heat-map overlay and verify that strong boundaries are warmer than uniform areas.
3. Add an `IncreaseDetail` rectangle over a face or focal subject.
4. Add a `ReduceDetail` rectangle over a background area.
5. Resize the window and confirm that both rectangles remain aligned.
6. Render with the same seed before and after manual regions and compare stroke distribution.
7. Remove the last region, clear all regions and rerender.
8. Modify edge/contrast settings, choose **Reanalyze detail map**, then rerender.
9. Save and reload a schema-2 preset.
10. Load a schema-1 M3 preset and verify default M4 settings.
## M4.1 validation correction

The first target test run exposed one exact-equality assertion on a derived viewport rectangle (`90` versus `90.00000000000001`). The production mapping was correct. M4.1 changes the viewport tests to compare each `double` component at 12 decimal digits, consistent with the project floating-point assertion policy.

