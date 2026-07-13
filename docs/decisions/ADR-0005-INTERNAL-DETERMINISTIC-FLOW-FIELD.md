# ADR-0005 — Internal deterministic flow field and LibNoiseCore removal

**Status:** Accepted  
**Date:** 2026-07-13

## Context

The original WPF experiment used LibNoiseCore to obtain a scalar value that was converted into a stroke direction. The new application needs deterministic saved projects, configurable field composition and future deformation driven by primitives, importance maps and user selections.

Depending directly on a third-party noise object would make the field representation, numerical behaviour and future composition rules dependent on that package.

## Decision

The shipping solution uses the application-owned contracts:

```text
IFlowField.SampleAngle(x, y)
IFlowFieldFactory.Create(seed, settings)
```

The default implementation is an internal deterministic coherent value-noise field with fractal octaves. Representative numerical samples are protected by golden tests.

A repository-owned trigonometric field remains selectable for visual comparison with the M1/M2 transition. It is not the permanent default.

LibNoiseCore is not referenced and will not be added to the shipping projects.

## Consequences

Positive:

- field output is versionable and reproducible;
- no external package controls saved-project behaviour;
- field transforms can later compose primitive and detail-map influences;
- all non-native planning remains testable in pure C#;
- cross-platform behaviour can be validated directly.

Costs:

- the project owns numerical implementation and tests;
- changing the algorithm requires a new planner/field version or migration policy;
- visual equivalence with the old experiment is not guaranteed and must be evaluated through the retained comparison mode.
