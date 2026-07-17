# M14.6 — Hierarchical regional merge

**Status: DONE — validated with 964 tests**

## Purpose

M14.6 converts the immutable fine SLIC partition and M14.5 Region Adjacency Graph into deterministic intermediate and broad-mass region levels. Fine labels remain unchanged. The hierarchy records only parent mappings, so later rendering can choose a scale without rewriting or losing the original segmentation.

No external package, machine-learning model or new imaging dependency is introduced. Flow, Primitive and Hybrid remained on the validated compatibility pipeline during M14.6; M14.7 owns active adoption.

## Hierarchy levels

Every segmentation result now contains exactly three ordered levels:

```text
Level 0 — fine connected SLIC regions
Level 1 — intermediate coherent surfaces
Level 2 — broad colour/texture masses
```

Each level maps every fine region to exactly one compact parent identifier. A coarser level may merge a previous parent but can never split it. `RegionHierarchyLevel.GetFineRegionIds` exposes stable child lists for direct parent/child traceability.

Default target counts are expressed as ratios of the fine-region count:

- intermediate: 60%;
- broad mass: 30%.

These are targets, not forced counts. Merging stops early when no admissible adjacent pair remains.

## Deterministic merge cost

Only currently adjacent parents are candidates. `RegionMergeCostModel` combines:

```text
0.30 × normalized CIELAB mean-colour difference
0.15 × normalized texture-energy difference
0.35 × mean shared-boundary strength
0.10 × merged-shape penalty
0.10 × resulting-size penalty
```

Colour and texture use the same saturating normalization family as the boundary model. Shape uses the digital merged perimeter:

```text
Pmerged = Pfirst + Psecond - 2 × sharedBoundaryLength
compactness = clamp(4πA / Pmerged², 0, 1)
shape penalty = 1 - compactness
```

Regional means are pixel-weighted. Costs for all affected neighbours are recomputed after every accepted merge. Equal-cost candidates are ordered by stable region identifiers.

## Strong-edge and size protection

A candidate is rejected before cost comparison when:

- the maximum contributing fine-edge strength reaches the strong-boundary threshold;
- the resulting normalized area exceeds the maximum parent-area fraction;
- the two current parents are not adjacent.

When several fine boundaries collapse into one parent boundary, shared lengths and weighted mean strength are combined while the maximum strength is retained. A protected contour therefore remains protected after neighbouring weak regions merge.

## Implementation

`RegionHierarchyBuilder` uses:

- mutable aggregate statistics proportional to current region count;
- one aggregate boundary per current adjacent pair;
- a priority queue with versioned stale-candidate rejection;
- deterministic lower-identifier survival;
- union-parent compression and first-fine-appearance compact relabelling;
- cancellation during iterative merging;
- explicit M13.4 memory admission before hierarchy allocation.

`RegionSegmentationEstimator` now reserves an additional 2,048 bytes per estimated fine region for hierarchy groups, aggregate boundaries, queue entries, union parents and compact mappings.

`SlicRegionSegmentationAnalyzer` publishes a new `BuildingHierarchy` progress stage after adjacency construction and returns the completed three-level hierarchy in `RegionSegmentationResult`.

## Settings

`RegionMergeSettings` currently exposes application-level defaults for:

- intermediate and broad target ratios;
- intermediate and broad maximum costs;
- strong-boundary threshold;
- maximum parent-area fraction.

They are not yet shown in the UI or persisted. M14.8 owns user-facing controls and schema migration.

## Validation

M14.6 added **24 focused tests**, raising the suite from the validated 940-case M14.5 baseline to **964 validated tests**:

- 2 Domain hierarchy child-traceability cases;
- 22 Application settings, cost, builder, request, analyzer and memory-estimate cases.

Coverage includes:

- fine/intermediate/broad level counts;
- adjacency-only merging;
- strong-edge protection before and after neighbouring merges;
- maximum cost and maximum area stopping rules;
- cost recomputation after accepted merges;
- stable equal-cost tie breaking and repeatability;
- compact parent identifiers and child lookup order;
- cancellation and invalid graph rejection;
- request-level merge settings and hierarchy progress;
- hierarchy-aware peak-memory estimates.

Run:

```bash
dotnet restore FlowPainter.sln
dotnet build FlowPainter.sln -c Release --no-restore
dotnet test FlowPainter.sln -c Release --no-build
```

Manual checks:

1. confirm all 964 tests pass;
2. open an existing project and confirm current rendering remains unchanged;
3. confirm no new segmentation or merge controls are visible yet;
4. project schema remains 11 and preset schema remains 8;
5. application icon remains present in the window and executable.

## Exit criteria

- every result exposes valid fine, intermediate and broad-mass levels;
- every coarser level is a deterministic non-splitting mapping of the previous level;
- only adjacent parents merge;
- strong boundaries survive aggregate-region updates;
- affected costs are recomputed after every accepted merge;
- fine labels and source pixels remain immutable;
- memory is admitted before hierarchy allocation;
- progress includes `BuildingHierarchy`;
- build has zero warnings/errors and all 964 tests pass;
- active rendering and persisted schemas remain unchanged.
