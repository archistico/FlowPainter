# ADR-0012 — Separate boundary analysis from boundary-aware painting

**Status:** Accepted  
**Date:** 2026-07-14

## Context

FlowPainter must simplify backgrounds while preserving the contours that make subjects and forms recognizable. A single scalar detail map cannot express both how much detail to use and which direction a stroke should follow. It also cannot distinguish an analyzer error from an unsuitable stroke policy.

Important boundaries have at least three independent properties:

- scalar strength/importance;
- confidence/classification, such as silhouette, internal structure or texture;
- local tangent direction.

## Decision

Boundary analysis is a separate Application service:

```text
ISceneBoundaryAnalyzer
    ↓
SceneBoundaryAnalysisResult
    ├── scalar diagnostic maps
    └── Domain BoundaryDirectionField
```

The built-in M11 provider is deterministic and proxy-based. It may use semantic-analysis output but does not depend on a renderer or planner.

M11 only computes and visualizes boundary data. M12 introduces a distinct `BoundaryGuidanceField` and planner policy that combines:

- base artistic `IFlowField`;
- boundary tangent and importance;
- crossing penalty;
- deflection/termination rules;
- side-aware colour sampling.

The renderer receives only an approved plan and never decides whether a boundary may be crossed.

## Consequences

Positive consequences:

- boundary maps can be visually validated before changing paintings;
- analyzers remain replaceable;
- Domain stores direction without Avalonia or SkiaSharp;
- M10 behaviour remains recoverable by disabling boundary influence;
- background suppression can consume confidence/uncertainty without altering analysis;
- manual boundary editing can later override derived data through a separate layer.

Costs:

- more maps and UI overlays;
- an additional analysis phase;
- project/preset schema updates for analyzer settings;
- M12 explicitly coordinates field blending and crossing policy outside the analyzer.

## Rejected alternatives

### Put tangent alignment directly in the semantic analyzer

Rejected because analysis would then produce rendering policy rather than evidence about the scene.

### Use the gradient normal as the flow direction

Rejected because the normal crosses the contour; painterly separation requires the tangent.

### Define background as the inverse of the subject map

Rejected because missed subject details and uncertain boundaries would be degraded aggressively.

### Persist generated boundary maps in project files

Rejected for M11 because maps are reproducible proxy-derived data, potentially large and provider-version-dependent. Persisting analyzer settings is sufficient.
