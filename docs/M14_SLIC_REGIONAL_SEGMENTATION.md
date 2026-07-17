# M14 — SLIC regional segmentation

**Status:** IN PROGRESS — M14.8 ready for validation
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

**Status:** DONE — validated with 863 tests

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

**Status:** DONE — validated with 882 tests

Detailed specification: [`M14_2_DETERMINISTIC_SLIC_IMPLEMENTATION.md`](M14_2_DETERMINISTIC_SLIC_IMPLEMENTATION.md).

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

**Status:** DONE — validated with 907 tests

Detailed specification: [`M14_3_CONNECTIVITY_AND_DIAGNOSTICS.md`](M14_3_CONNECTIVITY_AND_DIAGNOSTICS.md).

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

**Status:** DONE — validated with 920 tests

Detailed specification: [`M14_4_REGIONAL_DESCRIPTORS.md`](M14_4_REGIONAL_DESCRIPTORS.md).

Descriptors are calculated in deterministic passes over the final connected label map and the original source proxy.

Implemented values:

- area, exclusive bounds and pixel-centre centroid;
- exposed four-neighbour digital perimeter and `4πA / P²` compactness;
- mean and population variance of D65 CIELAB `L*`, `a*` and `b*`;
- mean squared internal-lightness gradient energy;
- internal edge-pixel density above a fixed `2 L*` threshold;
- doubled-angle dominant tangent orientation in `[0, π)`.

Cross-region contrast is deliberately excluded from texture/orientation descriptors because shared-boundary evidence belongs to M14.5. The implementation allocates one full-proxy `float` lightness buffer and scalar arrays proportional to region count, never one full-size buffer per region.

Validation uses synthetic fixtures with analytically known geometry, colour statistics, straight/stepped perimeters, horizontal/vertical tangents and uniform/textured regions. Thirteen focused Application cases were validated at 920 total tests.

## M14.5 — Region Adjacency Graph

**Status: DONE — validated with 940 tests**

Detailed specification: [`M14_5_REGION_ADJACENCY_GRAPH.md`](M14_5_REGION_ADJACENCY_GRAPH.md).

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

**Status: DONE — validated with 964 tests**

Detailed specification: [`M14_6_HIERARCHICAL_MERGE.md`](M14_6_HIERARCHICAL_MERGE.md).

Implemented behaviour:

- three deterministic levels: fine, intermediate and broad mass;
- adjacent-only priority-queue merging with stale-candidate version rejection;
- weighted colour, texture, boundary, shape and resulting-size cost;
- explicit maximum fine-edge protection across aggregate boundaries;
- cost and perimeter recomputation after every accepted merge;
- stable equal-cost ordering and compact first-appearance parent identifiers;
- immutable fine labels plus direct parent/child traceability;
- cancellation and hierarchy-aware memory admission;
- request-level merge settings, with UI/persistence deferred to M14.8.

Twenty-four focused cases were validated, raising the suite from 940 to 964.

## M14.7 — Active-pipeline migration

**Status: DONE — validated with 998 tests**

Detailed specification: [`M14_7_ACTIVE_PIPELINE_MIGRATION.md`](M14_7_ACTIVE_PIPELINE_MIGRATION.md).

Implemented behaviour:

- `AnalysisCoordinator` executes structural analysis followed by SLIC segmentation, descriptors, RAG and hierarchy;
- automatic semantic analysis is no longer injected or called by the active path;
- regional/shared-boundary evidence reaches scene-boundary analysis through a compatibility adapter;
- background and focus treatment derive from regional structure, structural contrast and manual roles;
- Flow, Primitive and Hybrid switch together through their shared detail/boundary inputs;
- semantic settings no longer affect cache identity or newly generated plans;
- schema-11 corrections migrate at runtime to generalized `RegionRoleOverride` values;
- old project schemas remain readable without a project-schema increment;
- legacy semantic maps remain a read-only UI compatibility envelope only.

Thirty-four focused Domain/Application cases were validated, raising the suite from 964 to 998.

## M14.8 — UI and persistence

**Status: DONE — validated with 1,024 tests**

Detailed specification: [`M14_8_REGIONAL_UI_SETTINGS_AND_PERSISTENCE.md`](M14_8_REGIONAL_UI_SETTINGS_AND_PERSISTENCE.md).

Implemented controls:

- segmentation enabled;
- target region size and compactness;
- pre-smoothing, iteration limit and convergence tolerance;
- simplified merge intensity;
- hierarchy level used for diagnostics;
- explicit reanalysis command.

Implemented overlays and diagnostics:

- fine mean-colour regions;
- raw SLIC borders;
- strong RAG boundaries;
- selected hierarchy level;
- region/hierarchy/connectivity statistics;
- click inspection of selected-region descriptors.

Persistence policy:

- project schema 12 stores reusable settings and source-specific generalized role overrides;
- preset schema 9 stores reusable SLIC/merge settings only;
- large derived label maps, descriptors, graphs and hierarchy values are rebuilt rather than persisted;
- project schemas 1–11 and preset schemas 1–8 remain readable;
- obsolete semantic tuning values remain hidden and round-trippable only for compatibility.

Twenty-six focused Application/Imaging cases raised the validated suite from 998 to 1,024.
