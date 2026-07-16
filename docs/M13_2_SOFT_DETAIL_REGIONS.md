# M13.2 — Soft manual detail regions

**Status:** DONE — validated on Windows  
**Date:** 2026-07-14

## Purpose

Rectangular detail regions are editing controls, not shapes that should become visible in the finished painting. Before M13.2, each manual region changed the detail map abruptly at its geometric border. When detailed and neutral/background policies differed strongly, this could reveal the rectangle as a visible seam.

M13.2 replaces the hard mask with a continuous distance-based influence field. It changes planning inputs only; it does not blur the source image, the generated plan or the final raster output.

## Artistic rule

A manual region now has three spatial zones:

```text
outside policy
    ↓
soft exterior transition
    ↓
50% influence at the geometric border
    ↓
soft interior transition
    ↓
full-strength core
```

The user-selected rectangle identifies the core area of interest. It is no longer treated as a discontinuous rendering boundary.

## Transition width

`DetailInfluenceSettings.RegionTransitionWidth` stores the feather radius as a normalized fraction of the shorter analysis-map dimension.

Default:

```text
5% of min(map width, map height)
```

Allowed range:

```text
0% through 50%
```

A zero value intentionally restores the previous hard-rectangle behaviour for comparison or specialized workflows.

The same percentage therefore has a consistent visual meaning on portrait, landscape and differently sized preview proxies.

## Influence calculation

`DetailMapComposer` evaluates pixel centres in analysis-map pixel coordinates.

- Inside the rectangle, influence rises from `0.5` at the border to `1.0` in the core.
- Outside the rectangle, influence falls from `0.5` at the border to `0.0` at the configured transition radius.
- Both sides use `SmoothStep(t) = t² × (3 − 2t)`.
- Outside corners use Euclidean distance, producing rounded falloff rather than square or cross-shaped artifacts.
- Very small regions automatically reduce the inward radius so their centre can still reach full influence.

The resulting influence multiplies the region strength before applying the existing increase/reduce formulas:

```text
Increase: value + influence × (1 - value)
Reduce:   value × (1 - influence)
```

## Overlapping regions

Regions with the same intent are merged through the maximum local influence. Two overlapping focus rectangles therefore do not compound into an unintended detail spike.

When increase and reduce intents overlap, each intent is first merged independently. The intent whose most recent region appears later in the project region order is applied last. This preserves an explicit, deterministic “latest opposing edit wins last” rule while avoiding same-intent accumulation.

## UI and persistence

The Detail influence panel adds:

```text
Region transition (%)
```

The value is used consistently when:

- loading a project;
- rebuilding the proxy;
- reanalysing detail;
- adding, editing, promoting or removing regions;
- planning Flow, Primitive or Hybrid previews.

Project schema advances to **10** and flow-preset schema advances to **8**. Schema-9 projects and schema-7 presets that do not contain `regionTransitionWidth` load the 5% default.

## Automated coverage

M13.2 adds focused tests for:

- default and validation bounds;
- full-strength region cores;
- continuous inside/outside border transitions;
- Euclidean corner falloff;
- zero-width hard-mask compatibility;
- maximum merging for equal-intent overlaps;
- deterministic opposing-intent order;
- source-map immutability and cancellation;
- project/preset round trips and previous-schema defaults.

The full suite increases from **700** to **713** cases and was validated successfully on Windows.

## Manual validation

1. Open an image in which a strong manual detail rectangle previously produced a visible border.
2. Keep the same seed and rendering parameters.
3. Set **Region transition** to `0` and render a comparison image.
4. Set it to `5` and render again.
5. Verify that the centre remains detailed while the rectangular seam disappears progressively near the border.
6. Try `8–12%` when inside/outside policies are especially different.
7. Overlap two focus regions and confirm that the overlap does not become an artificial high-detail block.
8. Save and reopen the project and verify that the transition value and result remain unchanged.

## Intentional limits

M13.2 softens rectangular influence only. It does not yet:

- select a rectangle directly by clicking its overlay;
- exclude or relabel automatic semantic detections;
- improve primary-subject ranking;
- add detail-specific segment, curve or boundary-alignment controls;
- guarantee that all refinement marks are painted after broad base marks.

Those are separated into the next roadmap steps so each behaviour remains testable and reversible.
