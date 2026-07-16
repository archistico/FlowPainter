# ADR-0015 — Soft manual-region influence

**Status:** Accepted  
**Date:** 2026-07-14

## Context

Manual detail rectangles previously applied a constant transformation inside their bounds and no transformation outside. Strong differences in stroke placement, length and width could make the editing rectangle visible in the generated painting.

Blurring the rendered image would hide symptoms while damaging intentional edges and would make preview/final equivalence harder to reason about. The discontinuity must instead be removed from the planning field that causes it.

## Decision

Manual detail regions produce a continuous scalar influence field in `DetailMapComposer`.

- Transition width is stored in `DetailInfluenceSettings` as a fraction of the shorter analysis-map dimension.
- The default radius is 5%.
- Influence is 0.5 on the geometric border, approaches 1 inside and 0 outside.
- A cubic smooth-step curve controls both sides.
- Outside corners use Euclidean distance.
- Same-intent regions merge by maximum influence.
- Opposing intent groups are applied in the order determined by their latest region occurrence.
- A zero transition width retains hard-mask compatibility.

The composer continues to return an immutable `DetailMap`. Domain region geometry remains normalized and independent of UI or raster libraries.

## Consequences

Positive:

- region geometry no longer appears as an abrupt painting seam;
- the same field drives placement and all existing detail-dependent scale changes;
- the result remains deterministic and resolution-independent at project level;
- overlapping focus regions do not create cumulative spikes;
- no renderer or brush implementation needs to know about regions.

Costs:

- composition allocates two temporary influence buffers at proxy-map resolution;
- region composition now depends on one additional persisted setting;
- changing transition width invalidates the composed map and therefore the approved preview plan.

## Rejected alternatives

### Blur the final image

Rejected because it damages source-derived boundaries and does not fix planning discontinuities.

### Blur the complete automatic detail map

Rejected because automatic structural/semantic boundaries may be intentionally sharp. Only manual-region influence requires this feathering policy.

### Linear interpolation

Rejected because its slope changes abruptly at the start and end of the band. SmoothStep gives zero derivative at both limits.

### Sum overlapping regions

Rejected because overlap strength would depend on how many rectangles happen to cover a pixel and could create visible hotspots.
