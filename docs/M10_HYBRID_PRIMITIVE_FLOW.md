# M10 — Hybrid primitive and flow-field engine

## Status

DONE

## Purpose

M10 combines the two independently validated generative engines into a deterministic layered painting pipeline. Geometric primitives establish broad colour masses and compositional structure; the accepted primitive geometry then deforms the vector field used by the brush-stroke planner; a final detail-biased pass reinforces important regions.

Flow painting and geometric primitives remain available as independent modes. Hybrid mode is a third composition strategy, not a replacement for either engine.

## Pipeline

```text
Source proxy + composed detail map
        ↓
Detail-aware primitive optimization
        ↓
PrimitivePlan
        ↓
Primitive-derived flow influence
        ↓
Base flow StrokePlan
        ↓
Detail-biased refinement StrokePlan
        ↓
HybridPlan
        ├── synchronized preview
        └── final PNG/JPEG rasterization up to 10,000 × 10,000
```

The three plans use normalized geometry and the same source-proxy dimensions. Final rendering changes only rasterization resolution.

## Primitive-derived flow influence

`PrimitiveInfluenceFlowField` wraps the selected coherent or trigonometric base field. Nearby primitives contribute deterministic weighted directions with distance falloff. Four influence strategies are available:

- **AxisAlignment** — follows the major axis of each primitive;
- **BoundaryTangent** — follows a tangent estimated from the primitive's rotated normalized bounds;
- **Vortex** — circulates around primitive centres;
- **Mixed** — combines axis, boundary and vortex contributions.

The user controls influence strength, radius and the maximum number of nearby contributors used for a sample. Primitive geometry is never mutated by the flow pass.

The first boundary-tangent implementation intentionally uses the primitive's rotated width/height envelope. Exact polygonal and Bézier boundary fields remain extensions for later primitive families.

## Layer budgets

Hybrid settings allocate the configured engine counts across three non-zero layers:

- primitive mass budget;
- primitive-guided flow budget;
- detail-refinement budget.

The fractions must sum to one. They scale the configured primitive count and stroke count while preserving the existing validation limits of each engine.

The refinement pass also provides independent multipliers for:

- detail-placement bias;
- stroke length;
- stroke width.

This makes refinement shorter, finer and more concentrated without changing the approved brush material.

## Determinism

The hybrid plan uses three fixed seed salts derived from the project seed:

```text
project seed
    ├── primitive-plan seed
    ├── base-flow seed
    └── refinement seed
```

Equal source, maps, settings and seed therefore produce equal primitive geometry and equal stroke plans. No global random source is introduced.

## Rendering and ownership

`SkiaHybridPlanRenderer` owns only temporary render layers:

1. rasterize the primitive plan;
2. pass that image as the source background of the flow layer;
3. pass the flow result as the source background of the refinement layer;
4. return the refinement result to the caller;
5. dispose both temporary layers on success, cancellation or failure.

Preview and final export use the same `HybridPlan` and brush settings. PNG and JPEG are supported. SVG remains specific to pure primitive mode because the procedural brush layers are raster materials.

## UI and persistence

The generative selector now exposes:

- `FlowPainting`;
- `GeometricPrimitives`;
- `Hybrid`.

Hybrid controls include:

- primitive, flow and refinement percentages;
- primitive influence strategy;
- influence strength and radius;
- maximum nearby primitive influences;
- refinement detail bias;
- refinement length and width percentages.

Project schema 6 persists `HybridGenerationSettings`. Schemas 1–5 remain readable and receive explicit M10 defaults. Generated plans remain derived in-memory output and are invalidated when source, proxy, detail map, seed or relevant settings change.

## Non-goals

M10 does not yet:

- interleave arbitrary user-defined pass graphs;
- edit generated primitive influence vectors directly on the canvas;
- use exact triangle/rectangle edge-distance fields;
- assign different brush families to base and refinement passes;
- persist generated hybrid plans;
- implement local regeneration or undo/redo.

Those concerns belong primarily to M11 and later consolidation work.

## Automated validation

M10 expects 576 automated cases:

```text
Domain                  92
Application            417
Imaging.Skia            24
Rendering.Skia          43
Total                   576
```

The new coverage verifies:

- hybrid-plan layer invariants;
- settings and budget validation;
- every primitive influence strategy;
- local falloff and preservation outside influence bounds;
- deterministic primitive and stroke plans;
- layer counts, progress and cancellation;
- schema-6 round trips and schema-5 defaults;
- workspace state;
- deterministic layered Skia rendering and native ownership paths.

## Manual validation

1. Open an image with a distinguishable subject and background.
2. Render the same seed in Flow painting, Geometric primitives and Hybrid modes.
3. In Hybrid mode, compare AxisAlignment, BoundaryTangent, Vortex and Mixed.
4. Increase influence strength and verify that stroke movement changes near primitive masses.
5. Reduce refinement length and width and verify finer treatment of important regions.
6. Compare source and result with synchronized zoom and pan.
7. Export PNG and JPEG above preview resolution.
8. Save and reopen a schema-6 project and verify every hybrid control.
9. Open a schema-5 project and verify explicit default hybrid settings.
10. Cancel during primitive planning, flow planning and final rendering.
