# ADR-0013 — Boundary-aware stroke policy

**Status:** Accepted for M12 validation  
**Date:** 2026-07-14

## Context

M11 deliberately separated boundary detection from stroke planning. The analyzer estimates edge hierarchy, silhouette confidence and tangent direction, but it must not decide artistic stroke behaviour.

Putting tangent blending, crossing rules or termination inside the analyzer would make diagnostic errors indistinguishable from rendering-policy errors and would prevent reuse by primitive, flow and hybrid engines.

## Decision

Introduce an Application-layer `BoundaryGuidanceField` derived from `SceneBoundaryAnalysisResult` and `BoundaryPaintingSettings`.

Responsibilities are divided as follows:

- `ISceneBoundaryAnalyzer` estimates visual evidence;
- `BoundaryGuidanceField` converts evidence into a smooth local planning policy;
- `FlowPainterPlanner` blends direction, evaluates crossings, shortens corners and terminates protected strokes;
- `SkiaStrokePlanRenderer` remains unaware of boundaries;
- project and preset documents persist policy settings, never generated boundary maps.

Boundary-aware plans use the version identifier `flow-field-boundary-v1`.

When the policy is disabled, the planner delegates to the previously validated detail-aware path without consuming additional random values.

## Consequences

### Positive

- exact backward-compatible fallback;
- analyzer maps remain independently inspectable;
- the same guidance field can be reused by both hybrid stroke passes;
- rasterization remains resolution-independent and simple;
- future manual boundary edits can replace or compose guidance without rewriting the analyzer;
- M13 can add background suppression without mutating M11 evidence.

### Trade-offs

- proxy resolution limits the spatial precision of guidance;
- current colour preservation is based on the stroke origin rather than a fully segmented two-sided colour model;
- internal and external contour flows are not separate plans yet;
- crossing control adds CPU work to path planning.

## Rejected alternatives

### Modify the renderer to clip strokes at edges

Rejected because final-resolution clipping would make preview and export behaviour resolution-dependent and would leave the immutable plan unaware of the artistic decision.

### Replace the base flow field with the tangent field

Rejected because it would remove artistic movement instead of blending it progressively near important boundaries.

### Treat every edge equally

Rejected because fine texture would over-constrain the painting and create noisy, mechanical results.
