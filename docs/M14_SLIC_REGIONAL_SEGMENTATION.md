# M14 — SLIC regional segmentation

**Status:** IN PROGRESS — M14.1 ready for validation  
**Approved:** 2026-07-16  
**Depends on:** M13.4 pre-SLIC stabilization  
**Decision:** [`ADR-0017-SLIC-REGIONAL-SEGMENTATION.md`](decisions/ADR-0017-SLIC-REGIONAL-SEGMENTATION.md)

## Purpose

M14 replaces the future semantic-recognition direction with a deterministic regional representation tailored to FlowPainter. The goal is not to label objects. The goal is to divide the complete image into coherent connected areas, describe their visual structure, determine which boundaries matter and provide hierarchy levels that the generative engines can use consistently.

SLIC is the only automatic segmentation algorithm planned for this milestone.

## Non-goals

M14 does not include:

- SAM, MobileSAM or any trained segmentation model;
- ONNX Runtime, Python, PyTorch or GPU inference;
- class labels or automatic person/animal/object recognition;
- face or landmark detection;
- full-resolution Lab fields for 10,000 × 10,000 images;
- freehand editing, region splitting UI or partial regeneration, which belong to M16;
- final high-resolution border refinement, which belongs to M17.

## Preconditions from M13.4

Implementation starts only after M13.4 is locally validated. The required stabilization capabilities are:

- dirty-state and destructive-navigation guards are complete;
- project/session adoption is transactional;
- analysis, segmentation, planning and rendering have explicit memory/work budgets;
- encoded input is bounded;
- durable local writes are atomic;
- analysis lifecycle orchestration is testable outside Avalonia through the M13.4.4 `AnalysisCoordinator`.

## Planned data model

Names may be refined during M14.1, but responsibilities are fixed.

```text
RegionSegmentationRequest
├── IRgbaPixelSource Source
├── RegionSegmentationSettings Settings
└── analysis revision / cancellation / progress

RegionSegmentationResult
├── RegionLabelMap Labels
├── IReadOnlyList<ImageRegion> Regions
├── RegionAdjacencyGraph Adjacency
├── RegionHierarchy Hierarchy
└── SegmentationDiagnostics Diagnostics
```

### RegionLabelMap

Required behaviour:

- width and height match the analysis proxy;
- one label per pixel;
- `UInt16` backing for at most 65,536 compact labels;
- `UInt32` backing only when required;
- read-only indexed and row-span access;
- checked byte-size calculation before allocation;
- explicit copy/ownership semantics;
- compact identifiers from zero to `RegionCount - 1`.

### ImageRegion

Planned descriptors:

- identifier and hierarchy level;
- pixel count and normalized area;
- bounds and centroid;
- perimeter and compactness;
- mean and variance in CIELAB;
- mean and variance of luminance;
- texture energy;
- edge density;
- dominant local orientation.

### RegionAdjacency

Each undirected edge records:

- the two region identifiers;
- shared-boundary length;
- mean and maximum gradient along the boundary;
- Lab colour difference;
- luminance and texture difference;
- continuity and prevailing tangent;
- normalized boundary strength.

### RegionHierarchy

The hierarchy maps every fine region through progressively coarser parents:

```text
Level 0 — fine SLIC superpixels
Level 1 — local coherent surfaces
Level 2 — intermediate structural regions
Level 3 — broad painterly masses
```

The precise number of levels may be configurable, but parentage must be deterministic, acyclic and traceable.

## M14.1 — Contracts and invariants

**Status:** READY FOR VALIDATION

Detailed specification: [`M14_1_REGIONAL_SEGMENTATION_CONTRACTS.md`](M14_1_REGIONAL_SEGMENTATION_CONTRACTS.md).

Deliverables:

- segmentation request/settings/result contracts;
- compact label-map storage abstraction;
- region, adjacency, hierarchy and diagnostic values;
- progress-stage contract;
- exact memory/work estimator integration replacing the provisional M13.4.2 reserve;
- validation and immutability tests across Domain and Application.

Core invariants:

```text
one pixel → one region
one region → at least one pixel
all fine regions connected
all labels compact
sum(region areas) = width × height
adjacency symmetric
hierarchy acyclic
same input + settings = same result
```

Exit criteria:

- invalid dimensions, region sizes, compactness and iteration values are rejected;
- storage selection and byte estimates are tested at boundary values;
- Domain/Application contracts have no Avalonia or SkiaSharp references;
- all 863 tests pass after adding 59 focused M14.1 cases.

## M14.2 — Deterministic SLIC implementation

Algorithm stages:

1. optionally pre-smooth the proxy;
2. convert RGB samples to CIELAB;
3. derive target spacing from `TargetRegionSize`;
4. initialize cluster centres on a regular grid;
5. move each centre to a nearby low-gradient location;
6. assign pixels within each centre's local search window;
7. update colour and spatial centroids;
8. repeat until tolerance or maximum iterations;
9. return raw fine labels and diagnostics.

Initial settings:

```text
TargetRegionSize
Compactness
PreBlurSigma
MaximumIterations
ConvergenceTolerance
```

Requirements:

- no global random state;
- stable traversal and tie-breaking order;
- cancellation inside expensive loops;
- monotonic progress stages;
- no mutation of the source proxy.

## M14.3 — Connectivity and diagnostics

Raw cluster labels are normalized into valid FlowPainter regions:

- identify connected components per label;
- retain the principal component;
- reassign or separate disconnected remnants deterministically;
- merge undersized components with the best adjacent candidate;
- compact labels;
- build area/bounds/centroid basics;
- render mean-colour and boundary overlays;
- report iteration count, displacement, region-size distribution and repair counts.

Exit criteria:

- every final fine region is connected;
- all pixels remain covered;
- no empty labels remain;
- diagnostic overlays match label dimensions and never mutate the source.

## M14.4 — Regional descriptors

Descriptors are calculated in deterministic passes over the label map and source proxy.

Validation uses synthetic fixtures with analytically known:

- areas and centroids;
- mean colours and luminance;
- straight and stepped perimeters;
- horizontal/vertical dominant orientations;
- uniform versus textured regions.

Descriptor calculation must not allocate one full-size buffer per region.

## M14.5 — Region Adjacency Graph

The graph is constructed by scanning right/down pixel neighbours once and accumulating undirected edges in normalized identifier order.

Requirements:

- no self edges;
- exactly one edge per adjacent pair;
- symmetric lookup semantics;
- exact shared-boundary pixel counts;
- gradient and tangent statistics sampled along the common border;
- deterministic edge ordering;
- normalized boundary strength with documented weights.

The RAG is the bridge to boundary-aware painting. A SLIC border is not automatically a hard artistic barrier; its strength is continuous.

## M14.6 — Hierarchical merge

Candidate merge cost initially combines:

```text
colour difference
+ texture difference
+ shared-boundary strength
+ shape penalty
+ resulting-size penalty
```

Rules:

- only adjacent regions can merge;
- strong boundaries receive explicit protection;
- costs are recomputed after each accepted merge;
- equal-cost ties use stable identifiers;
- every hierarchy level maps all fine labels to exactly one parent;
- source fine labels remain immutable.

Presets may expose a user-facing merge intensity rather than raw weights.

## M14.7 — Active-pipeline migration

Only after M14.1–M14.6 validation:

- SLIC hierarchy replaces automatic semantic maps as the active regional representation;
- scene-boundary analysis consumes regional/shared-boundary evidence;
- background and focus treatment derive from regional structure, structural contrast and manual roles;
- Flow, Primitive and Hybrid switch together;
- semantic settings no longer affect newly generated plans;
- schema-11 corrections migrate to generalized region-role overrides;
- old project schemas remain readable;
- automatic semantic detections are treated as derived legacy evidence, not durable state.

A compatibility test must prove that intentional manual subject/background/focus decisions survive migration even though the automatic analyzer changes.

## M14.8 — UI and persistence

Planned controls:

- segmentation enabled;
- target region size;
- compactness;
- pre-smoothing;
- merge intensity;
- hierarchy level used for diagnostics/rendering;
- reanalyze command.

Planned overlays:

- fine SLIC labels;
- mean-colour regions;
- raw SLIC borders;
- strong regional boundaries;
- selected hierarchy level;
- region statistics and selected-region descriptors.

Persistence policy:

- project schema advances only when implementation is ready;
- project stores reusable settings and source-specific manual role overrides;
- presets store reusable SLIC/merge settings only;
- large derived label maps are rebuilt rather than persisted initially;
- all existing project and preset schemas remain readable.

## Memory and high-resolution policy

For a 10,000 × 10,000 source:

- `UInt16` full-size labels would require about 191 MiB;
- `UInt32` full-size labels would require about 381 MiB;
- a full-size three-channel `float32` Lab field would require about 1.12 GiB before other buffers.

M14 therefore segments the selected analysis proxy globally. Rendering queries region/hierarchy information through normalized coordinates. M17 may refine borders at source resolution using narrow bands or local SLIC, subject to the M13.4 budget policy.

## Test strategy

M14 adds deterministic tests for:

- contract validation and compact storage;
- uniform, two-colour, striped, gradient and textured fixtures;
- expected region-count ranges rather than fragile full-map hashes;
- exact coverage and connectivity;
- deterministic output and tie breaking;
- cancellation and progress;
- descriptor values;
- adjacency and shared-boundary counts;
- hierarchy parentage and strong-edge protection;
- project/preset migration;
- active-path integration across Flow, Primitive and Hybrid.

Visual/manual validation compares:

- source;
- fine mean-colour segmentation;
- boundary overlay;
- several hierarchy levels;
- resulting Flow, Primitive and Hybrid previews.

## Completion criteria

M14 is complete when:

- SLIC provides a deterministic, connected and complete partition;
- descriptors, RAG and hierarchy pass all invariants;
- memory/work estimates reject unsafe operations before allocation;
- UI diagnostics make segmentation failures inspectable;
- Flow, Primitive and Hybrid use the same regional representation;
- new planning no longer depends on automatic semantic recognition;
- schema-1 through the new schema remain readable;
- build has zero warnings/errors and the full test suite passes.
