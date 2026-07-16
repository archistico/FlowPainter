# ADR-0010 — Primitive plans and proxy-space optimization

## Status

Accepted for M9.

## Context

Primitive reconstruction requires many repeated error evaluations. Performing that search at final output resolution would be unnecessarily expensive and would conflict with the 10,000 × 10,000 memory boundary. At the same time, preview and export must remain reproducible and resolution independent.

## Decision

The optimizer runs exclusively against the selected analysis proxy and produces an immutable `PrimitivePlan` in normalized coordinates.

The plan contains no SkiaSharp types and no output-resolution pixels. A dedicated Skia adapter rasterizes it, while a separate exporter writes SVG. The optimizer owns replaceable candidate-factory, mutator, mask-rasterizer and scorer boundaries. Error updates are local to candidate masks. The composed detail map weights placement, size, error and local mutation budget.

## Consequences

- preview and final raster output use identical geometry;
- SVG is a natural second representation of the same plan;
- deterministic tests can validate plans without pixel-perfect Skia comparisons;
- high-resolution output cost is paid only once during final rasterization;
- proxy resolution limits the finest structure discoverable by the optimizer;
- M10 can compose primitive geometry with flow fields without changing primitive persistence or rendering contracts.
