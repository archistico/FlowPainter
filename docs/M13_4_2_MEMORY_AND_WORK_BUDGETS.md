# M13.4.2 — Memory and work budgets

**Status: READY FOR VALIDATION**

## Purpose

M13.4.2 establishes explicit, deterministic resource limits before SLIC introduces a regional label map, additional analysis buffers and hierarchical region data. Unsupported downstream analysis, planning and rendering operations must be rejected before their large allocations or loops begin.

The milestone addresses the memory/work risks recorded in audit findings F-03, F-04 and F-08 without changing the generated plans for settings that remain inside the supported budgets.

## Scope

The milestone adds:

- one shared `WorkloadBudgetPolicy` for memory and planning work;
- mode-aware final-render memory estimates for Flow, Primitive and Hybrid;
- a proxy-analysis estimate that includes an explicit future SLIC reserve;
- pre-allocation analysis checks when loading an image or changing preview quality;
- pre-allocation final-export checks before the save picker is opened;
- planning guards inside the Application services, independent of Avalonia;
- bounded encoded-image input for seekable and streaming sources;
- focused unit and integration coverage for estimates, rejection and streaming cancellation.

No project or preset schema change is required. Budget values are application safety policy, not persisted artistic settings.

The cross-cutting decision is recorded in [`ADR-0018 — Resource admission budgets`](decisions/ADR-0018-RESOURCE-ADMISSION-BUDGETS.md).

## Shared resource policy

`WorkloadBudgetPolicy` defines the currently supported limits:

| Resource | Supported maximum |
|---|---:|
| Estimated combined working set | 2 GiB |
| Flow segment steps | 25,000,000 |
| Primitive score attempts | 5,000,000 |
| Primitive pixel evaluations | 3,000,000,000 |
| Encoded image input | 256 MiB |

These are defensive support limits rather than claims about the absolute capability of a particular computer. They keep the application's accepted workload bounded and reproducible across supported 64-bit desktop environments.

A request over a limit fails with an actionable `InvalidOperationException` or `InvalidDataException` before the expensive operation starts.

## Analysis memory estimate

`AnalysisMemoryEstimator` combines:

- decoded source RGBA storage;
- proxy RGBA storage;
- a conservative reserve for the current structural, semantic, boundary and detail-analysis fields;
- a separate future regional-segmentation reserve.

The current constants are:

```text
Current analysis reserve:      160 bytes per proxy pixel
Future SLIC reserve:            24 bytes per proxy pixel
```

The SLIC reserve does not implement segmentation and is not allocated by M13.4.2. It makes the budget policy ready for the label map, cluster state, descriptors and adjacency data planned for M14.

Analysis is checked before proxy creation when:

- an image is opened;
- a project source image is adopted;
- preview quality is changed and analysis is rebuilt.

## Mode-aware final-render estimate

`FinalRenderMemoryEstimator` now includes:

- decoded source image;
- analysis proxy and working fields;
- future SLIC reserve;
- current rendered preview;
- optional detail overlay;
- output-sized rendering surfaces and retained layers.

The conservative output-buffer model is:

| Mode | Output-sized buffers represented at peak |
|---|---:|
| Flow painting | 3 |
| Geometric primitives | 3 |
| Hybrid | 4 |

Flow and Primitive account for the output surface, the copied bitmap and an encoding reserve. Hybrid accounts for the primitive layer, flow layer, refinement surface and copied result retained by the current composer/renderer path. Encoding is not added again to the Hybrid peak because the layered render peak is already the dominant phase.

The desktop estimate reports:

- output dimensions and selected/approved mode;
- estimated peak MiB;
- output-buffer count;
- Normal, Elevated or High risk;
- whether the operation is allowed or blocked by policy.

When an approved preview plan exists, the estimate uses that plan's mode, matching the mode that final export will actually render.

## Planning work estimate

`GenerationWorkEstimator` evaluates the requested plan before plan collections and large optimization loops are created.

### Flow

```text
Flow segment steps = stroke count × segment count
```

### Primitive

The estimate includes:

- candidate evaluations;
- the maximum detail-scaled mutation iterations;
- the configured primitive count;
- a conservative rotated-raster bounding-box estimate based on working resolution and maximum primitive size.

### Hybrid

Hybrid uses the same rounded primitive, flow and refinement budget fractions as `HybridPlanComposer`, then combines:

- primitive score attempts and pixel evaluations;
- base-flow segment steps;
- refinement-flow segment steps.

The guard is enforced in:

- `FlowPainterPlanner`;
- `PrimitivePlanOptimizer`;
- `HybridPlanComposer`.

This keeps the policy effective for current UI calls, tests and future non-desktop callers.

## Bounded encoded input

`SkiaImageLoader` now has a configurable encoded-input limit with a 256 MiB default.

For seekable sources, the loader:

1. validates the remaining stream length;
2. allocates exactly one managed byte array;
3. reads directly into that array.

This removes the previous `MemoryStream` plus `ToArray` duplication for normal file streams.

For non-seekable sources, the loader:

- copies through an `ArrayPool<byte>` buffer;
- validates the cumulative byte count after every read;
- stops as soon as the configured limit is exceeded;
- honours cancellation during streaming.

Skia still creates native encoded-data storage before decoding. Removing that native copy would require a separately reviewed ownership strategy and is not part of this milestone.

## Behavioural compatibility

For workloads inside the policy limits:

- deterministic seeds and plan versions are unchanged;
- Flow, Primitive and Hybrid geometry is unchanged;
- project and preset schemas remain unchanged;
- preview and final rendering retain their existing visual behaviour.

M13.4.2 changes admission and diagnostics, not the artistic algorithm.

## Automated coverage

The milestone adds or expands coverage for:

- current-analysis and future-SLIC memory components;
- Flow, Primitive and Hybrid output-buffer accounting;
- 10,000-pixel Flow acceptance and Hybrid rejection under the 2 GiB policy;
- exact Flow segment-step estimation;
- Primitive candidate/mutation and pixel-evaluation estimation;
- Hybrid budget scaling matching the composer;
- planner-level rejection before large work begins;
- bounded seekable and non-seekable image input;
- cancellation during non-seekable streaming.

The validated M13.4.1 baseline contains **765** cases. M13.4.2 adds **17** cases, for an expected total of **782**.

## Manual validation checklist

1. Build the full solution in Release with zero warnings and errors.
2. Run all **782** tests with zero failures and zero skips.
3. Open ordinary PNG/JPEG/WebP/BMP samples and confirm analysis still completes.
4. Change Draft, Standard and High preview quality and confirm valid operations rebuild normally.
5. Inspect the final-output estimate in Flow, Primitive and Hybrid modes.
6. Confirm an over-budget final export is blocked before a destination picker appears.
7. Confirm reducing the output dimension or choosing a lighter mode returns the estimate to `allowed`.
8. Enter excessive stroke/segment or primitive-search settings and confirm planning is rejected with an actionable message.
9. Confirm normal presets and previously validated plans render identically.
10. Optionally record process memory while exercising representative 4K and 8K images; report measured peaks separately from the conservative estimator.

## Residual work

The following remain deliberately outside M13.4.2:

- measured native-memory profiling across hardware and codecs;
- reducing the Hybrid renderer's retained full-resolution layer lifetime;
- tiled or out-of-core rendering;
- a zero-copy native encoded-input lifetime design;
- actual SLIC allocations and label-map storage selection;
- opt-in 10,000 × 10,000 stress certification.

Those items belong to M14 implementation work or M17 high-resolution optimization. The present milestone ensures that the current application accepts only bounded work and reserves space for the next architecture.

## Exit criteria

M13.4.2 is complete when:

- every current analysis, planning and final-render entry point has a pre-allocation budget check;
- Hybrid memory is estimated according to its actual retained layer structure;
- future SLIC analysis has an explicit budget reserve;
- encoded input cannot grow without a configured bound;
- supported workloads retain deterministic output;
- over-budget workloads fail before expensive allocation or iteration;
- build and all **782** tests pass locally.
