# M12 — Boundary-aware painting

**Status:** DONE — validated on Windows  
**Project schema:** 8  
**Flow-preset schema:** 6  
**Validated automated cases:** 666

## Purpose

M11 identifies the scene boundaries that make subjects, objects and figures recognizable. M12 turns those diagnostic maps into an explicit stroke-planning policy.

The artistic rule is:

> Near an important contour, a stroke should increasingly follow the contour, preserve the side from which it originated and avoid crossing a protected silhouette indiscriminately.

This makes broad, painterly treatment possible without dissolving the separation between subject and background.

## Implemented pipeline

```text
Base artistic IFlowField
        +
SceneBoundaryAnalysisResult
        ↓
BoundaryGuidanceField
        ↓
FlowPainterPlanner
├── free flow away from important edges
├── tangent alignment near edges
├── crossing-risk sampling
├── deflection toward the contour
├── shortening near corners
└── optional termination at hard boundaries
        ↓
flow-field-boundary-v1 StrokePlan
```

The renderer remains unchanged: it only rasterizes the approved immutable plan.

## Boundary-guidance field

`BoundaryGuidanceField` is derived from M11 data and stores, at every proxy pixel:

- tangent direction;
- local influence;
- crossing hardness;
- subject-boundary confidence;
- corner strength.

Its influence combines:

- important-edge confidence;
- subject silhouette confidence;
- internal-structure contribution;
- separately weighted texture edges;
- uncertainty protection.

The configured alignment radius propagates guidance smoothly away from the exact edge. This avoids an abrupt switch between the artistic field and the contour field.

## Stroke behaviour

### Tangent alignment

The base flow direction is blended with the closest equivalent tangent orientation. A tangent has no intrinsic forward/backward direction, so the planner chooses the orientation that requires the smallest turn from the current artistic flow.

### Crossing control

Before accepting a segment, the planner samples the proposed path at intermediate points. If the segment approaches or crosses a hard boundary, it can:

1. reduce the attempted crossing;
2. deflect toward the local tangent;
3. shorten the segment near corners;
4. terminate the stroke after the last valid point.

The response is controlled independently by crossing penalty, hard-boundary threshold and termination strength.

### Colour-side preservation

A stroke continues to sample its colour from its starting pixel. M12 therefore does not average colour across a silhouette while planning a single stroke. A future dual-side contour pass may add separate internal and external layers, but the current implementation already preserves the originating side for each planned mark.

### Contour reinforcement

Subject-boundary confidence can reinforce the composed detail map before placement. This allocates more marks around important silhouettes without drawing an artificial outline.

### Corner preservation

Large local changes in tangent direction are classified as corners or junctions. Strokes are shortened in these areas according to `CornerPreservation`, reducing the risk that a long smooth mark erases a cusp or defining angle.

## Hybrid mode

`HybridPlanComposer` creates one guidance field and reuses it for:

- the primitive-influenced base-flow layer;
- the finer refinement layer.

Primitive deformation and scene-boundary guidance are therefore composed rather than replacing one another.

## Settings

- enable boundary-aware painting;
- tangent alignment;
- alignment radius;
- crossing penalty;
- hard-boundary threshold;
- termination strength;
- internal-edge influence;
- texture-edge influence;
- contour reinforcement;
- corner preservation.

Setting boundary-aware painting to disabled takes the exact validated M10/M11 planning path and preserves its planner version and deterministic sequence.

## Built-in presets

M12 adds or updates presets that demonstrate different policies:

- **Soft contour** — gradual alignment and limited termination;
- **Strong silhouette** — high tangent alignment, crossing resistance and contour reinforcement;
- **Loose background** — broad expressive treatment while protecting important silhouettes;
- existing Balanced, Fine detail, Expressive and Bristle study presets now use tuned boundary guidance;
- **Legacy comparison** keeps boundary-aware painting disabled.

## Persistence

Project schema 8 and preset schema 6 persist every `BoundaryPaintingSettings` value.

Earlier project schemas 1–7 and preset schemas 1–5 remain readable. Missing M12 settings receive the disabled compatibility default, so old documents reproduce their previous planning behaviour.

## Automated verification

The M12 additions verify:

- complete settings validation;
- normalized and immutable guidance samples;
- stronger subject-boundary guidance than texture guidance;
- smooth influence propagation over the configured radius;
- contour-detail reinforcement without source-map mutation;
- corner detection;
- cancellation;
- exact M10/M11 fallback when disabled;
- tangent alignment near boundaries;
- hard-boundary termination;
- independently configurable texture influence;
- deterministic boundary-aware plans;
- dimension mismatch rejection;
- hybrid use of the same boundary policy in both stroke layers;
- project and preset round trips;
- backward-compatible schema defaults;
- built-in preset policy.

## Intentional limits

M12 does not yet implement:

- signed background-detail suppression;
- independently planned internal and external contour layers;
- manual boundary/barrier painting;
- user-edited tangent vectors;
- partial regeneration around an edited boundary.

Those belong to M13 and M14. M12 establishes the deterministic, testable policy on which they can safely build.
