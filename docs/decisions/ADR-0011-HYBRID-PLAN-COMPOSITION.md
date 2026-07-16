# ADR-0011 — Hybrid plan composition and primitive-derived fields

## Status

Accepted for M10.

## Context

FlowPainter has two independently usable, resolution-independent plans: `PrimitivePlan` and `StrokePlan`. The hybrid engine must combine their artistic strengths without coupling Domain to SkiaSharp, mutating accepted plans, introducing global randomness or regenerating different geometry for final export.

## Decision

Introduce an immutable `HybridPlan` containing one primitive plan and two source-background stroke plans with identical proxy dimensions.

`HybridPlanComposer` performs deterministic orchestration in Application:

1. optimize a detail-aware primitive layer;
2. wrap the selected base `IFlowFieldFactory` with a primitive-influence factory;
3. generate the main brush-stroke layer;
4. generate a second detail-biased refinement layer;
5. assign independent fixed seed salts to each planning phase.

Primitive influence is calculated as a weighted directional contribution with distance falloff. The initial implementation supports major-axis, rotated-bounds tangent, vortex and mixed strategies. It remains an `IFlowField` decorator so later spatial indexes, exact boundary fields or user-authored deformers do not change `FlowPainterPlanner`.

`SkiaHybridPlanRenderer` rasterizes the layers sequentially and passes each completed layer as the next stroke plan's source background. Intermediate native images are scoped and disposed by the renderer. The returned image is owned by the caller.

## Consequences

- pure Flow and Primitive modes remain unchanged;
- hybrid preview and final output reuse identical plan geometry;
- Domain remains free from Avalonia and SkiaSharp;
- project schema 6 stores settings, not generated plans;
- deterministic tests can validate each layer separately;
- SVG remains a pure-primitive output until brush layers gain a vector representation;
- primitive influence currently approximates boundaries through normalized rotated extents;
- very large primitive/stroke budgets may require a spatial influence index during later performance work.
