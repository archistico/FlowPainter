# M14.5 — Region Adjacency Graph

**Status: DONE — validated with 940 tests**

## Purpose

M14.5 turns the connected SLIC label map and M14.4 descriptors into a complete immutable Region Adjacency Graph (RAG). The graph is still detached from active Flow, Primitive and Hybrid planning; M14.6 will consume it for deterministic hierarchical merging and M14.7 will activate the regional pipeline.

No external package, model or imaging dependency is introduced.

## Graph construction

`RegionAdjacencyGraphBuilder` scans only right and lower pixel neighbours. Every differing pair contributes one digital boundary segment to a normalized `(lowerId, higherId)` accumulator.

The resulting graph guarantees:

- one node for every compact fine-region label;
- no self edge;
- exactly one edge for every adjacent pair;
- exact shared-boundary segment counts;
- deterministic edge ordering;
- symmetric lookup and immutable ordered per-region edge lists.

`RegionSegmentationResult` independently rescans the label map and rejects missing, extra or incorrectly sized adjacency edges.

## Boundary evidence

Every `RegionAdjacency` stores:

- shared boundary length;
- mean and maximum local CIELAB ΔE across the common boundary;
- CIELAB distance between regional means;
- absolute regional lightness difference;
- absolute texture-energy difference;
- continuity of the undirected tangent field;
- prevailing tangent normalized to `[0, π)`;
- continuous boundary strength normalized to `[0, 1]`.

Horizontal digital segments contribute tangent `0`; vertical segments contribute tangent `π/2`. Doubled-angle accumulation makes tangent direction sign-independent. Continuity is the normalized resultant magnitude: straight boundaries approach `1`, while balanced stepped boundaries approach `0`.

## Boundary-strength model

`RegionBoundaryStrengthModel` uses fixed documented weights:

```text
0.35 × normalized mean local gradient
0.15 × normalized maximum local gradient
0.25 × normalized regional CIELAB difference
0.10 × normalized texture difference
0.15 × tangent continuity
```

Normalization uses `value / (value + scale)` with scales 20, 20, 30 and 15 respectively. A geometrically continuous but visually identical SLIC partition therefore remains a weak boundary (`0.15`) rather than becoming a hard artistic barrier.

## Memory, cancellation and progress

The graph uses storage proportional to region/edge count and never allocates a full-size mask per edge. `RegionSegmentationEstimator` reserves a conservative 1,024 bytes per estimated region for accumulators, dictionary entries, immutable edges, neighbour lists and result validation.

Cancellation is observed before allocation and during row scanning. The analyzer publishes the new `BuildingAdjacency` progress stage after descriptors and before completion.

## Validation

M14.5 adds **20 focused tests**, raising the suite from the validated 920-case M14.4 baseline to **940 tests**, all validated on Windows.

Coverage includes:

- exact lengths for multiple adjacent pairs;
- vertical, horizontal and stepped boundaries;
- tangent and continuity calculations;
- CIELAB local/mean contrast and chromatic evidence;
- texture difference and normalized strength ordering;
- deterministic empty/single-region behaviour;
- symmetric ordered neighbourhood access;
- graph completeness validation in `RegionSegmentationResult`;
- source immutability, invalid inputs and cancellation;
- analyzer graph publication and progress;
- adjacency-aware memory estimates.

Run:

```bash
dotnet restore FlowPainter.sln
dotnet build FlowPainter.sln -c Release --no-restore
dotnet test FlowPainter.sln -c Release --no-build
```

Manual checks:

1. confirmed: all 940 tests pass;
2. open an existing project and confirm current rendering is unchanged;
3. confirm no new segmentation UI controls are visible yet;
4. project schema remains 11 and preset schema remains 8;
5. application icon remains present in the window and executable.

## Exit criteria

- every adjacent label pair has exactly one graph edge;
- shared-boundary counts match the label map exactly;
- lookups and neighbourhood lists are symmetric and deterministic;
- tangent values are normalized to `[0, π)` and continuity to `[0, 1]`;
- boundary strength is finite, normalized and ranks strong contrast above identical partitions;
- memory is admitted before graph allocation;
- build has zero warnings/errors and all 940 tests pass;
- active rendering and persisted schemas remain unchanged.
