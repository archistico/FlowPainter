# M15.5 — Unified artistic hierarchy

**Status:** PLANNED  
**Dependencies:** M15.3 staged Flow and M15.4 coarse-to-fine primitives

## Objective

Publish one engine-neutral artistic allocation that coordinates Flow, Primitive and Hybrid treatment.

## Levels

```text
Broad mass
Supporting region
Protected region
Focal region
Critical detail
```

These levels describe rendering priority, not recognized object classes.

## Evidence

Classification uses:

- fine/intermediate/broad SLIC hierarchy membership;
- artistic detail and background suppression;
- RAG and scene-boundary strength;
- regional contrast, texture and scale;
- generalized manual roles and protected areas;
- optional composition heuristics that remain deterministic and non-semantic.

## Output

The planned result contains both:

- a discrete ownership level for budget accounting;
- continuous normalized influence weights for smooth spatial rendering.

Flow, Primitive and Hybrid receive the same immutable result. Engines may translate it into engine-specific geometry, but may not independently reclassify regions.

## Precedence

Manual Critical/Focal/Protected/Background decisions override automatic structural evidence. Ignore removes an explicit manual role but does not delete the region. Conflicts use documented deterministic precedence and stable edit order.

## Validation

Tests must prove deterministic classification, precedence, continuous transitions, monotonic budget allocation, cross-engine equality of source allocation, compatibility migration and independence from retired semantic settings. Manual comparison must show a legible hierarchy without rectangular, SLIC-shaped or role-band seams.

## Non-goals

- no object labels;
- no learned saliency model;
- no region topology editing;
- no performance optimization beyond bounded allocation.
