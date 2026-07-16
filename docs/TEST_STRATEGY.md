# FlowPainter test strategy

## Objectives

Tests protect reproducibility, geometry, regional topology, importance guidance, memory boundaries, native-resource ownership and separation of responsibilities. Visual experimentation is expected, but intentional artistic changes must not accidentally alter unrelated deterministic behaviour.

## Test layers

### Domain tests

Fast tests with no filesystem, UI, network or native dependencies:

- normalized geometry;
- image dimension limits;
- angle calculations;
- detail-map invariants and uniform construction;
- manual-region and schema-11 semantic-correction invariants;
- planned M14 label-map, region, adjacency and hierarchy invariants;
- deterministic random sequences;
- stroke-plan validation;
- primitive geometry, kind flags and immutable primitive-plan validation;
- normalized boundary-vector and boundary-direction-field invariants.

### Application tests

Use pure in-memory data:

- generation request validation;
- project-session clean bypass and Save / Discard / Cancel decisions;
- presentation edit notification with and without an active source;
- structural detail analysis;
- semantic settings, progress and result invariants;
- deterministic saliency, generic-subject, silhouette and focal maps;
- structural/semantic map composition, semantic-correction composition and cancellation;
- legacy semantic persistence defaults and future region-role migration;
- edge, colour-contrast and smoothing behaviour;
- automatic/manual detail composition;
- viewport-to-normalized coordinate conversion and overlay hit-testing/cycling;
- detail-influence validation and interpolation;
- weighted stroke placement;
- detail-dependent length and width;
- deterministic legacy and production planner orchestration;
- coherent-flow numerical golden samples;
- boundary termination, opacity, progress and cancellation;
- preset schema migration and JSON round trips;
- memory and work estimates;
- planned deterministic SLIC assignment, connectivity, descriptors, adjacency and hierarchical merge;
- primitive settings, masks, colour estimation, local scoring and deterministic hill climbing;
- detail-aware primitive placement, size, error weighting and search budget;
- schema-5 project and workspace persistence for primitive mode;
- boundary settings, progress and result invariants;
- multiscale edge strength, tangent direction, contour continuity and semantic silhouette promotion;
- background-confidence, uncertainty and protected-boundary behaviour;
- schema-7 project and schema-5 preset migration for boundary analysis.

### Imaging integration tests

Use very small synthetic images generated in memory:

- decode dimensions and RGBA samples;
- metadata-stage rejection above 10,000 × 10,000;
- unsupported encoded data;
- cancellation and progress ordering;
- aspect-ratio-preserving proxies;
- independent copy ownership;
- PNG and JPEG round trips;
- JPEG transparency flattening;
- format-specific progress, quality validation and output truncation;
- disposed-wrapper rejection.

### Rendering integration tests

Raster assertions inspect representative pixels rather than full-image hashes:

- transparent and source-image backgrounds;
- requested output dimensions;
- proportional stroke widths;
- clipping of characterized out-of-bounds paths;
- background validation;
- detail-overlay dimensions and colours;
- zero-opacity overlay preservation;
- native cancellation and disposal paths;
- raster output for every primitive family;
- primitive background, progress and cancellation;
- SVG element coverage, deterministic line endings, stream truncation and cancellation;
- boundary-direction overlay dimensions, importance threshold, cancellation and source preservation.

Pixel-perfect full-image hashes are avoided because native renderer upgrades may produce harmless antialiasing differences. The primary golden master remains normalized plan data.

### UI tests

Only behaviours that belong to the presentation layer:

- mouse-region coordinate conversion;
- view-model commands;
- cancellation and state transitions;
- parameter validation presentation;
- project dirty state, persisted-control tracking and Save / Discard / Cancel prompts;
- destructive open-image, open-project, recent-project and close smoke tests.

M4 tests the difficult coordinate conversion as a pure Application service and uses a manual Avalonia smoke test. Automated view-model/UI-state tests enter with the M5 workflow extraction.

## Test naming and analyzers

Warnings are treated as errors in test projects as well as production projects. Test method names use descriptive PascalCase without underscores so they comply with `CA1707`; analyzer rules are not disabled merely to preserve an alternative test-naming convention.

## Floating-point assertion policy

Geometry and viewport calculations use IEEE 754 `double` values. Tests compare derived floating-point coordinates with an explicit precision or tolerance rather than record-wide exact equality. Exact equality remains appropriate only for values that are deliberately constructed or clamped to canonical constants such as `0` and `1`.

## Determinism policy

A deterministic algorithm test specifies:

- input fixture version;
- seed;
- complete settings;
- analyzer version;
- planner version;
- expected normalized plan or stable numerical samples.

The no-detail `flow-field-v1` path retains its M3 golden sequence. Detail-map planning uses the separately versioned `flow-field-detail-v1` path.

A change to the deterministic random golden sequence is breaking unless a project-format migration is provided.

## Analysis-field policy

Structural analyzers use small synthetic fixtures with known edges and colour changes. Tests distinguish:

- uniform background baseline;
- edge response;
- chromatic contrast;
- smoothing spread;
- manual positive/negative composition;
- weighted plan effects.

The M8 semantic analyzer tests remain frozen compatibility coverage for the currently implemented M8–M13.3 path. No model-backed provider fixtures are planned.

M14 regional-segmentation tests must distinguish algorithmic layers:

- SLIC assignment and centroid updates;
- complete label coverage and compact identifiers;
- connected-component repair and minimum-region merging;
- descriptor correctness;
- symmetric, complete Region Adjacency Graph construction;
- deterministic merge ordering and parent/child hierarchy;
- proxy/source coordinate mapping;
- memory estimation, cancellation and progress;
- migration of schema-11 manual intent without preserving derived automatic detections.

Tests use synthetic colour fields, gradients, thin boundaries, low-contrast separations and seeded textured fixtures. Full photographic pixel-label golden files are avoided; stable invariants, region statistics and selected label samples are preferred.

Boundary analysis has additional requirements:

- direction assertions use orientation-invariant comparisons where tangent sign is irrelevant;
- equal-luminance chromatic boundaries must be represented;
- coherent multiscale contours must outrank isolated fine texture;
- subject silhouettes must be distinguishable from internal structure;
- confident background must not penetrate the configured silhouette-protection band;
- uncertainty must remain high where neither foreground nor background is reliable;
- direction overlays are diagnostic and must not mutate the source or planner.

## Native-resource policy

Integration tests dispose every returned `SkiaImage`. Tests verify that disposed wrappers reject subsequent use. Temporary Skia objects in fixture factories use deterministic `using` scopes.

Long-running leak detection will be added to the controlled performance suite; routine unit tests do not infer native-memory release from garbage-collector timing.

## High-resolution validation

Routine tests use small synthetic fixtures. M6 automated tests validate estimates and representative independent output sizes. Dedicated slower tests will later validate:

- rejection above 10,000 × 10,000;
- correct byte estimates at the limit;
- output dimensions at representative large sizes;
- cancellation before and during final allocation;
- absence of accidental full-size float detail maps.

The full 10,000 × 10,000 scenario belongs to a controlled performance suite, not every unit-test run.

## Commands

```bash
dotnet build FlowPainter.sln -c Release
dotnet test FlowPainter.sln -c Release
```

Coverage can be collected with:

```bash
dotnet test FlowPainter.sln -c Release --collect:"XPlat Code Coverage"
```

## Current suite

The current validated repository contains **782** cases:

- 115 Domain;
- 590 Application;
- 27 Imaging.Skia;
- 50 Rendering.Skia.

M13.3 plus the audit remediation reached 755 cases. Ten M13.4.1 Application cases established 765. M13.4.2 added 14 Application and 3 Imaging.Skia cases and was validated at **782**. M13.4.3 added 8 Application persistence cases, reaching 790. M13.4.4 added 14 Application analysis-lifecycle cases and was validated at **804**. M14.1 adds 30 Domain and 29 Application contract cases, for an expected total of **863** pending local validation.


## M11 scene-boundary tests

The M11 suite verifies:

- normalized `BoundaryVector` construction and directional alignment;
- immutable `BoundaryDirectionField` indexing, sampling and copy ownership;
- settings ranges, progress stages, result dimensions and provider identifiers;
- uniform images producing no false edges and high background confidence;
- horizontal and vertical boundaries producing the corresponding tangent orientation;
- chromatic boundaries with limited luminance difference;
- promotion of semantic silhouettes over unrelated edges;
- separation of silhouette, internal structure and fine texture maps;
- subject-protection radius and background confidence;
- deterministic maps, cancellation and progress ordering;
- diagnostic direction-overlay rendering and cancellation;
- project schema 7 / preset schema 5 round trips and previous-schema defaults.

M11 remains covered as an independently inspectable diagnostic analyzer. M12 adds planning-policy tests without weakening those assertions.

## M12 boundary-aware tests

The M12 suite verifies:

- all `BoundaryPaintingSettings` ranges and compatibility defaults;
- normalized guidance samples and deterministic nearest sampling;
- subject silhouettes receiving stronger guidance than low-priority texture;
- radius-bounded propagation of tangent influence;
- contour-detail reinforcement without mutating the source map;
- corner/junction strength from tangent discontinuity;
- cancellation during guidance-field creation;
- exact recovery of the validated detail-aware plan when boundary policy is disabled;
- angular alignment of nearby strokes with boundary tangents;
- deflection and termination at hard protected boundaries;
- separately configurable internal and texture-edge influence;
- deterministic `flow-field-boundary-v1` plans;
- rejection of mismatched detail/boundary dimensions;
- reuse of one boundary policy by both hybrid stroke layers;
- schema-8 project and schema-6 preset round trips;
- disabled M12 defaults when opening previous schemas;
- built-in Soft contour, Strong silhouette and Loose background policies.

M12 preserves colour by sampling each stroke from its origin. Independent internal/external contour layers are not asserted because they remain a later extension.

## M13 background-suppression tests

M13 verifies:

- lower mark density and larger average mark size in confident background;
- higher primitive/stroke budget inside subjects and focal regions;
- manual protection precedence;
- uncertainty and silhouette-band protection;
- enforcement of a non-zero background detail floor;
- smooth, deterministic suppression transitions;
- reduced background complexity without reduced subject recognizability on controlled fixtures.

## M13.2 soft-region tests

M13.2 verifies:

- `RegionTransitionWidth` defaults and the accepted 0–50% range;
- full-strength cores and zero influence beyond the exterior feather radius;
- near-continuous values immediately inside and outside the rectangle border;
- Euclidean corner falloff rather than square expansion;
- exact hard-mask behaviour when transition width is zero;
- maximum merging of same-intent overlaps without cumulative hotspots;
- deterministic latest-opposing-intent ordering;
- source-map immutability and cancellation;
- project schema 10 / preset schema 8 round trips;
- schema-9 projects and schema-7 presets loading the 5% compatibility default.

## M10 hybrid-composition tests

The hybrid suite verifies:

- common dimensions, source-background requirements and planner-version invariants of `HybridPlan`;
- layer-budget totals and all hybrid setting ranges;
- axis, rotated-boundary tangent, vortex and mixed primitive influence strategies;
- local distance falloff and preservation of the base field outside influence bounds;
- deterministic primitive geometry and both deterministic stroke layers;
- scaled layer counts, stage progress and cancellation;
- schema-6 project round trips and schema-5 default migration;
- workspace dirty state and hybrid-mode restoration;
- layered Skia compositing, deterministic PNG output, progress and cancellation.

Full hybrid image golden files are intentionally avoided. Determinism is asserted between renders produced by the same native library, while plan geometry and representative pixels provide stable behavioural checks.


## M13.3 region-selection and semantic-correction tests

M13.3 verifies:

- correction identifiers, normalized optional text and enum validation;
- read-only correction collections and duplicate-identifier rejection;
- automatic demotion when a second primary subject is added;
- deterministic selection priority and repeated-click cycling for overlapping regions;
- null selection outside all overlays;
- primary-subject, subject, background and ignore map transformations;
- preservation of raw saliency for ignored detections;
- explicit correction-kind precedence and soft border transitions;
- project schema 11 round trip and schema-10 empty-list compatibility;
- workspace dirty state, source reset, project creation and project loading;
- XAML handler/control consistency through packaging checks.

UI smoke validation additionally covers the 6-pixel click/drag threshold, selected-overlay styling, `Delete` behaviour and end-to-end recomposition after adding or removing corrections.

## M13.4 stabilization tests

M13.4.1 validates:

- complete dirty tracking and Save / Discard / Cancel outcomes;
- value-equivalent settings not producing false dirty state;
- failed or cancelled Save retaining the active session;
- destructive navigation proceeding only after an accepted decision.

M13.4.2 validates:

- analysis estimates containing source, proxy, current-analysis and future-SLIC reserves;
- Flow/Primitive three-buffer and Hybrid four-buffer final-render peaks;
- 10,000-pixel Flow acceptance and Hybrid rejection under the shared 2 GiB policy;
- exact Flow segment-step accounting;
- detail-scaled Primitive candidate/mutation and pixel-evaluation accounting;
- Hybrid budget scaling matching `HybridPlanComposer`;
- planner-level rejection before large loops or plan collections are allocated;
- bounded seekable and non-seekable encoded input;
- cancellation during non-seekable streaming.

M13.4.3 validates:

- successful creation and replacement through a temporary sibling;
- preservation of an existing destination after failure or cancellation;
- no published destination after a failed new write;
- missing-directory creation and path validation;
- temporary-file cleanup on committed and non-committed paths;
- all production project, preset, preview, raster, SVG and recent-item writes using the shared commit boundary.

M13.4.4 validates:

- value-based cache keys for equivalent source, dimensions, analysis settings and workspace revisions;
- cache invalidation when source identity, settings or revisions change;
- defensive copying of mutable detail-region and semantic-correction collections;
- complete detached structural, semantic, boundary, automatic, manual and background-suppression output;
- monotonic stage progress through pipeline completion;
- cancellation and analyzer failure preserving the previously adopted result;
- transactional publication only after the caller adoption callback succeeds;
- rejection of older generations and mismatched expected keys;
- callback failure leaving the coordinator cache unpublished;
- manual-region/background recomposition reusing valid automatic maps without analyzer reruns;
- cache-key retagging after candidate project/image adoption;
- explicit invalidation clearing current state and rejecting pending results.

These fourteen focused Application cases established the validated **804**-case M13.4.4 baseline.

## M14 SLIC regional-segmentation tests

M14 is validated incrementally. M14.1 contributes **59** focused tests (30 Domain and 29 Application), raising the expected suite from 804 to **863**:


- **M14.1:** contract ranges, `UInt16`/`UInt32` storage boundaries, row/index access, defensive ownership, region-area consistency, graph symmetry, monotonic hierarchy, progress and exact memory/work estimates;
- **M14.2:** deterministic SLIC results, local assignment bounds, convergence, cancellation and progress;
- **M14.3:** connected labels, minimum-region repair, compact relabelling and complete coverage;
- **M14.4:** analytically verifiable colour, luminance, area, centroid, bounds, perimeter and orientation descriptors;
- **M14.5:** symmetric adjacency, exact shared-boundary counts and boundary-strength normalization;
- **M14.6:** deterministic merge costs, protected strong edges and traceable hierarchy;
- **M14.7:** Flow, Primitive and Hybrid consume the SLIC path while legacy projects preserve manual intent;
- **M14.8:** project/preset migration, settings round trip, overlay diagnostics and cache invalidation.

## M15–M17 future validation

- M15 compares stroke/primitive budgets across hierarchy levels and verifies smooth transitions, boundary alignment and stage ordering;
- M16 tests merge/split/role commands, undo/redo, local resegmentation and compatibility-preserving region overrides;
- M17 owns controlled high-resolution, native-memory, incremental-cache, packaging and startup smoke suites.

## M9 geometric-primitive tests

The primitive suite verifies:

- normalized geometry, rotation and contiguous plan indexes;
- validation of count, candidate, mutation, size, opacity, detail influences and allowed-kind flags;
- proxy masks for triangle, rectangle, rotated rectangle, circle and ellipse, including boundary clipping;
- analytically selected colours and positive weighted local error reduction;
- deterministic plans for equal source, settings and seed;
- early-stop index continuity, progress and cancellation;
- additional mutation effort in detailed areas;
- schema-5 project round trips and schema-4 defaults;
- workspace mode and primitive-setting state;
- Skia rasterization for every primitive family;
- SVG output for every supported element, stable LF line endings, truncation and cancellation.

Full proxy-result pixel hashes are avoided. Plan geometry and representative pixels are the stable assertions; optimizer quality and performance at larger counts belong to controlled benchmarks.

## M8 semantic-importance tests

The semantic suite verifies:

- settings, progress and semantic-region invariants;
- empty maps for disabled or uniform analysis;
- deterministic generic-subject, silhouette and focal maps;
- subject-count limits and stable provider identifiers;
- structural/semantic composition without mutating source maps;
- cancellation and progress stage ordering;
- schema-4 project/preset round trips and schema-3 defaults.

Synthetic fixtures deliberately test generic importance rather than claiming trained class recognition. These tests are retained as historical compatibility coverage. New automatic segmentation coverage belongs to M14 SLIC tests rather than new semantic/model providers.

## M7 brush-rendering tests

The brush suite verifies:

- validation and defaults of every brush parameter;
- compatibility between the implicit renderer default and explicit SolidRound;
- visible output from all built-in brush kinds;
- distinct rasterizations for round, flat and bristle materials;
- soft-edge coverage outside the solid core;
- deterministic bristle and jitter output for equal seeds;
- changed local variation for changed plan seeds;
- persistence and migration of brush settings in presets and projects;
- built-in preset coverage of every brush family.

Full-image byte comparison is used only between two renders produced by the same native library in the same test to verify determinism or difference. Cross-version visual golden files remain intentionally avoided.

## M6.1 synchronized-viewport tests

The Application suite also verifies cursor-anchored zoom, normalized shared centers, pan clamping, reset behavior and coordinate conversion under the active transform. UI smoke testing confirms wheel and middle-button input in both panels.

## M6 final-render tests

Tests now cover:

- final dimension and JPEG-quality validation;
- aspect-preserving upscaling and downscaling;
- known RGBA peak-memory accounting and risk bands;
- project/preset round trips and compatibility defaults through project schema 11 / preset schema 8;
- workspace dirty state for final-output changes;
- PNG and JPEG encoder output, cancellation, progress and truncation;
- white composition for transparent JPEG output;
- original-source compatibility with integer-rounded proxy plan dimensions;
- reuse of normalized renderer geometry at independent output dimensions.

Full 10,000 × 10,000 allocation remains a controlled stress test rather than part of every unit-test run.


Focused M13 coverage includes:

- signed artistic-detail validation, immutability and normalized sampling;
- settings validation and interpolation of placement/length/width/segment/curvature policies;
- confident-background suppression and configured detail floor;
- semantic subject, silhouette, uncertainty and manual-focus protection priority;
- disabled-path compatibility;
- cancellation during composition;
- project and preset round trips plus schema-9/schema-7 soft-region defaults and earlier background-suppression defaults;
- preservation of all M11/M12 boundary diagnostics and planning tests.
