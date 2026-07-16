# ADR-0018 — Resource admission budgets

## Status

Accepted, implemented and validated by M13.4.2 on 2026-07-16.

## Context

FlowPainter accepts decoded images up to 10,000 × 10,000 pixels and supports three generative modes with materially different memory and computational behaviour. The future SLIC pipeline will add label storage, clustering state, descriptors and adjacency data. Constructor-level setting ranges prevent invalid values, but valid maxima can still combine into workloads that are impractical or unsafe.

Earlier estimates represented only a subset of explicit RGBA buffers and did not model Hybrid's retained full-resolution layers. Encoded streams were copied without a configured upper bound. These gaps made accepted resource use dependent on where an operation was invoked and on the user's machine.

## Decision

Introduce one Application-level `WorkloadBudgetPolicy` that rejects unsupported work before downstream allocation or iteration.

The initial policy limits are:

- 2 GiB estimated combined working set;
- 25,000,000 Flow segment steps;
- 5,000,000 Primitive score attempts;
- 3,000,000,000 Primitive raster/scoring pixel evaluations;
- 256 MiB encoded image input in `SkiaImageLoader`.

Memory admission uses:

- `AnalysisMemoryEstimator` for source, proxy, current analysis and future SLIC reserve;
- mode-aware `FinalRenderMemoryEstimator` for Flow, Primitive and Hybrid peaks.

Planning admission uses `GenerationWorkEstimator` and is enforced inside `FlowPainterPlanner`, `PrimitivePlanOptimizer` and `HybridPlanComposer`, not only in the desktop presentation layer.

The limits are support policy, not artistic settings. They are not persisted in projects or presets.

## Estimation principles

- estimates are conservative and deterministic;
- known current lifetimes take precedence over ideal future implementations;
- Hybrid is estimated from its actual retained primitive/flow/refinement rendering layers;
- Primitive work includes the maximum detail-scaled mutation budget and a conservative rotated-raster bounding box;
- SLIC capacity is reserved before its implementation so M14 cannot silently exceed the accepted analysis envelope;
- approved preview-plan mode controls final-export estimation;
- measured operating-system working set remains validation evidence, not a replacement for pre-admission estimates.

## Alternatives considered

### Rely only on setting constructor maxima

Rejected. Independently valid maxima can multiply into unbounded composite work.

### Check budgets only in Avalonia

Rejected. Tests, future services and other callers could bypass presentation checks.

### Use available physical memory dynamically

Rejected for the initial policy. Results would vary by machine and current system load, weakening predictability and supportability.

### Remove the 10,000-pixel feature immediately

Rejected. Flow and Primitive can remain within the conservative 2 GiB envelope for supported configurations. Hybrid 10K is blocked until its layer lifetime is optimized.

## Consequences

Positive:

- accepted work has explicit upper bounds;
- dangerous combinations fail before expensive loops or output allocation;
- Hybrid estimates reflect current implementation rather than a two-buffer idealization;
- encoded input cannot grow without limit;
- M14 begins with a declared memory envelope;
- non-UI callers receive the same protections.

Costs:

- conservative estimates may reject a workload that could succeed on a high-memory computer;
- policy constants require review when implementations or supported hardware change;
- exact native Skia and codec peaks remain measurable rather than perfectly predictable;
- Hybrid 10K final export is intentionally unavailable until later optimization.

These costs are accepted in favour of deterministic, supportable failure before resource exhaustion.
