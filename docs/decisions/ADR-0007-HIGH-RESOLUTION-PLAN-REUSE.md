# ADR-0007 — Reuse the preview stroke plan for final rendering

## Status

Accepted — 2026-07-13

## Context

Preview analysis and planning operate on a reduced proxy, while final output may reach 10,000 × 10,000 pixels. Regenerating a plan during export could consume a different random sequence, drift from the approved preview or couple final quality to proxy resolution.

## Decision

A successful preview retains its immutable `StrokePlan`. Final export reuses that exact plan and changes only:

- raster output dimensions;
- source-background resolution;
- encoded raster format.

All stroke geometry and width data remain normalized. The original source image is accepted as the final background only when fitting it to the plan's maximum proxy dimension reproduces the plan source size.

Final-output settings are persisted separately from preview settings. The project schema advances to version 2, while schema 1 migrates to explicit defaults.

## Consequences

- final output visually corresponds to the preview;
- deterministic planning is performed once per approved preview;
- changing final dimensions does not consume random values;
- final export requires an existing rendered preview;
- control edits made after preview do not alter the cached plan until preview is rendered again;
- brush-engine changes in M7 must preserve the same separation between plan and rasterizer.
