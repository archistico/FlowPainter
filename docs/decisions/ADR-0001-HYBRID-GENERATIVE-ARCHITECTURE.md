# ADR-0001: Hybrid generative architecture

- Status: Accepted
- Date: 2026-07-13

## Context

The product must transform an image into a painterly generative artwork, allocate detail according to visual importance, accept manual area guidance, support flow-guided brushes and generate geometric primitives.

## Decision

Use a shared normalized importance map consumed by two independent but composable planners:

- a stroke planner guided by vector fields;
- a geometric primitive optimizer.

A hybrid coordinator may use primitive geometry to influence vector fields and allocate detail across both plans.

## Consequences

- Analysis is reusable and does not belong to either renderer.
- Preview and final output use the same normalized plans.
- Brush rendering can evolve without regenerating paths.
- Primitive optimization can evolve without changing the UI or image loader.
- Hybrid features can be added without merging all algorithms into one monolithic class.
