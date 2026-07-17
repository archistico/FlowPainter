# M15.4 — Primitive coarse-to-fine rendering

**Status:** PLANNED  
**Dependency:** M15.3 staged-plan contracts and budget discipline

## Objective

Organize primitive optimization into ordered broad, structural and detail stages driven by the validated regional hierarchy.

## Planned stages

- **Broad mass:** large low-frequency primitives; broad hierarchy; strong penalty for crossing protected boundaries.
- **Regional structure:** medium primitives fitted to intermediate regions and major colour transitions.
- **Protected/focal detail:** small primitives limited to high-detail or manually prioritized areas.

## Architecture

A coarse-to-fine composer will allocate one accepted primitive budget across stage-specific calls to `PrimitivePlanOptimizer`. The optimizer remains responsible for one bounded stage; it must not infer global stage order internally.

The resulting immutable plan preserves:

- stage identity;
- ordered primitive lists;
- source size and seed lineage;
- exact total count;
- SVG/raster draw order;
- compatibility projection to existing `PrimitivePlan` consumers where needed.

## Candidate policy

Candidate size, mutation radius, scoring area and allowed hierarchy levels become stage constraints. Broad candidates receive explicit boundary-crossing penalties and may not erase high-strength protected separations. Detail candidates must justify their cost through detail/focal evidence.

## Work and memory

- the total primitive budget is admitted once;
- each stage has a hard candidate/mutation/evaluation ceiling;
- detailed stages cannot silently multiply the global search budget;
- Hybrid planning reuses the accepted coarse-to-fine primitive plan.

## Tests and acceptance

Coverage must prove budget conservation, deterministic stage seeds/order, size-range enforcement, boundary protection, SVG layer order, raster determinism, cancellation and Hybrid reuse. Manual validation compares broad-only, broad+structure and full plans to confirm progressive refinement rather than repeated repainting.

## Non-goals

- no new region editor;
- no engine-neutral artistic role classifier yet;
- no full-resolution local optimization outside existing export projection.
