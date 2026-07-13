# ADR-0006 — Shared normalized detail-map guidance

**Status:** Accepted  
**Date:** 2026-07-13

## Context

FlowPainter must eventually combine structural analysis, semantic detections, manual selections, brush strokes and optimized primitives. Coupling those systems directly would make each new analyzer or engine change every other component.

## Decision

All automatic and manual importance information is represented as a normalized proxy-resolution `DetailMap`.

- Domain owns the map and region invariants.
- Application analyzers produce maps through `IDetailMapAnalyzer`.
- `DetailMapComposer` applies deterministic manual overrides.
- Generative planners consume the composed map but do not depend on the analyzer implementation.
- Rendering adapters visualize maps without changing their values.
- Permanent geometry and regions remain normalized to source-image coordinates.

M4's structural analyzer is intentionally non-semantic. Future face, landmark, saliency and segmentation providers must contribute maps or regions through explicit application contracts rather than being embedded in the planner or Avalonia window.

## Consequences

### Positive

- stroke and primitive engines can share one guidance surface;
- semantic providers can be replaced independently;
- manual edits remain deterministic and resolution independent;
- proxy analysis prevents a full-size float-map allocation;
- plans can be tested without Avalonia, SkiaSharp or a machine-learning runtime.

### Costs

- maps from different analyzers require an explicit future merge policy;
- proxy resolution limits very fine semantic boundaries;
- a project format is required before manual regions can persist across sessions;
- rectangular regions are only the first editing tool and will later need move/resize and painted masks.
