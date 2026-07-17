# M15.3 — Staged Flow rendering

**Status:** PLANNED — next implementation target  
**Baseline:** M15.2 validated with 1,071 tests

## Objective

Replace optional single-pass painterly planning with an ordered, immutable four-pass Flow plan while preserving the accepted M15.2 path as a compatibility mode.

## Pass model

1. **Broad mass** — sparse, long and wide marks; broad hierarchy level; weak internal boundaries may be crossed.
2. **Regional structure** — medium-scale marks aligned with intermediate regions and dominant regional directions.
3. **Boundary reinforcement** — constrained marks near strong scene/regional boundaries; tangential motion; no mechanical outline fill.
4. **Fine detail** — short, thin and locally curved marks reserved for high detail, focal/manual roles and protected structure.

The passes are planning stages, not semantic object classes.

## Proposed contracts

- `FlowPassKind`
- `FlowPassBudget`
- `FlowPassSettings`
- `StagedFlowPlan`
- `StagedFlowPlanComposer`
- `StagedFlowPlanningProgress`

`StagedFlowPlan` owns four ordered `StrokePlan` instances and validates source size, seed identity, planner version, total budget and pass order.

## Determinism

Each pass seed is derived from:

```text
project seed + stable pass identifier + planner version
```

Seed derivation must not depend on whether another pass was disabled or executed earlier. Equal budget remainders are assigned in a documented stable pass order.

## Budget policy

- one accepted total Flow stroke budget remains the admission unit;
- pass percentages are normalized or rejected before planning;
- allocated integer counts sum exactly to the total;
- zero-budget passes remain represented or omitted according to one documented invariant;
- work estimation uses the maximum local segment count already introduced by M15.2 for each allocated pass.

## Compatibility

A disabled staged policy calls the accepted M15.2 planner path without changing seed consumption, version identity or geometry. Staged plans receive a new explicit planner identity.

## Rendering and export

- preview rendering composites passes in fixed order;
- final PNG/JPEG rendering reuses the accepted staged plan;
- no pass is regenerated at export size;
- progress reports both pass and overall completion;
- cancellation leaves the previously accepted preview untouched.

## Automated coverage

Required tests:

- contract validation and immutability;
- fixed pass order;
- exact budget conservation and deterministic rounding;
- independent derived seeds;
- stage-specific eligibility and settings;
- compatibility recovery of the M15.2 path;
- deterministic planning and rendering;
- progress/cancellation;
- work admission and oversized rejection;
- project/preset migration if controls are exposed;
- Flow and Hybrid reuse of the same pass composer where applicable.

## Manual acceptance

Use representative portrait, landscape, architecture and textured scenes.

- Broad masses must read before detail is added.
- Structural marks must organize surfaces without revealing the SLIC tessellation.
- Boundary reinforcement must follow important contours without drawing a uniform outline.
- Fine detail must appear last and remain concentrated.
- Switching to compatibility mode must recover the accepted M15.2 rendering.
- Preview and final output must contain the same marks at different raster resolutions.

## Exit criteria

- warning-free Release build;
- complete suite passes;
- exact total-budget conservation;
- deterministic pass identity and output;
- no visible hard transitions at region or pass boundaries;
- compatibility mode remains unchanged;
- documentation and any persistence schema are updated.

## Explicit non-goals

- no primitive-stage changes;
- no unified five-level hierarchy yet;
- no topology editing;
- no high-resolution resegmentation;
- no external dependency.
