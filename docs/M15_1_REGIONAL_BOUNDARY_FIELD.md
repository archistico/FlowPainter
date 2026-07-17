# M15.1 — Regional boundary field

**Status:** READY FOR VALIDATION  
**Baseline:** M14.8 validated with 1,024 tests  
**Expected suite:** 1,049 tests

## Objective

Turn the validated SLIC label map and Region Adjacency Graph into a continuous painterly guidance field. M15.1 derives per-pixel boundary distance, RAG strength, normal and tangent, distinguishes hard barriers from soft transitions and combines that evidence with the already validated M11–M12 scene-boundary policy.

The milestone does not yet change stroke length, width, density or curvature according to regional detail. Those policies remain M15.2.

## Regional boundary field

`RegionalBoundaryField` is derived entirely from immutable `RegionSegmentationResult` data:

- fine labels identify every observed shared boundary;
- the matching RAG edge supplies normalized strength and prevailing tangent;
- each side of a shared boundary receives a normal oriented toward the opposite region;
- deterministic eight-neighbour propagation records the nearest boundary up to a bounded radius;
- distance is the primary selection criterion;
- equal-distance ties prefer stronger RAG boundaries, then stable seed order;
- pixels outside the configured range retain an empty sample.

`RegionalBoundarySample` exposes:

- distance in proxy pixels;
- original RAG boundary strength;
- distance-decayed influence;
- hard-barrier classification;
- boundary normal and tangent;
- the two fine-region identifiers associated with the nearest boundary.

## Strong barriers and soft transitions

The transition shape is continuous rather than binary:

- weak boundaries use a broader SmoothStep-derived band and a softer exponent;
- strong boundaries retain greater peak influence but use a narrower protection band;
- the existing hard-boundary threshold classifies barriers without changing the underlying continuous strength;
- inclusive threshold classification uses the same single-precision representation stored by the field, so an edge exactly at the configured threshold remains hard after propagation;
- zero alignment radius retains only exact boundary pixels;
- single-region segmentations produce a valid empty field.

`RegionalBoundaryFieldSettings` is derived from the existing boundary-painting controls for this milestone:

- maximum regional distance = twice the current alignment radius;
- hard-barrier threshold = existing hard-boundary threshold;
- transition-shape constants remain internal and deterministic.

No project or preset schema change is required.

## M11–M12 integration

`BoundaryGuidanceField` now has a regional overload. It:

- builds the original image/scene guidance exactly as before;
- applies the validated M11–M12 propagation to scene evidence;
- blends regional influence, hardness, contour reinforcement and tangent evidence afterward;
- retains explicit regional distance, strength, normal and hard-barrier state in each sample;
- recomputes corner evidence after regional tangents are introduced;
- preserves old overloads and legacy planner versions for compatibility.

Flow plans created with regional guidance use `flow-field-regional-boundary-v1`. Background-suppressed regional plans use `flow-field-background-regional-boundary-v1`.

Hybrid planning constructs the regional guidance once and reuses it for both the flow and refinement layers.

## Resource policy

Boundary-field creation performs resource admission before allocation. Its estimate includes fixed per-pixel field/propagation storage and adjacency overhead. Propagation is cancellable and bounded by the configured distance radius.

No external SLIC, graph, distance-transform or machine-learning dependency is introduced.

## Automated coverage

M15.1 adds 25 Application tests:

- defaults, derivation and validation of regional-field settings;
- vertical and horizontal tangent/normal orientation;
- opposite side normals;
- symmetric distance propagation;
- monotonic distance falloff;
- broad weak transitions and narrower strong barriers;
- threshold classification;
- deterministic strongest-boundary tie breaking;
- empty single-region behaviour;
- zero-radius behaviour;
- deterministic output, bounds and cancellation;
- regional/scene guidance blending, soft transitions, dimension validation and contour reinforcement;
- Flow and Hybrid regional planner-version integration;
- Hybrid background planning remains valid when regional segmentation is present but boundary painting is disabled.

## Manual acceptance

1. Open an image and enable boundary-aware painting.
2. Display a regional boundary overlay and render a Flow preview.
3. Verify strokes near strong SLIC boundaries align with the contour and resist crossing.
4. Verify weak regional boundaries produce gradual behaviour rather than a visible seam.
5. Render the same settings and seed twice and confirm identical output.
6. Render in Hybrid mode and confirm both stroke layers use the same regional guidance.
7. Disable boundary-aware painting and confirm the existing detail-only plan remains unchanged.

## Exit criteria

- Release build completes with zero warnings and errors;
- all 1,049 tests pass with zero failures and skips;
- nearest-boundary distance, strength, normal and tangent are deterministic;
- hard barriers and soft transitions are distinguishable without binary seams;
- Flow and both Hybrid stroke layers consume the regional field;
- existing non-regional boundary overloads remain compatible;
- no external dependency or persistence schema change is introduced.
