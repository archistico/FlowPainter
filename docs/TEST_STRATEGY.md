# FlowPainter test strategy

## Objectives

Tests protect reproducibility, geometry, importance guidance, memory boundaries, native-resource ownership and separation of responsibilities. Visual experimentation is expected, but intentional artistic changes must not accidentally alter unrelated deterministic behaviour.

## Test layers

### Domain tests

Fast tests with no filesystem, UI, network or native dependencies:

- normalized geometry;
- image dimension limits;
- angle calculations;
- detail-map invariants and uniform construction;
- manual-region invariants;
- deterministic random sequences;
- stroke and future primitive-plan validation.

### Application tests

Use pure in-memory data:

- generation request validation;
- structural detail analysis;
- edge, colour-contrast and smoothing behaviour;
- automatic/manual detail composition;
- viewport-to-normalized coordinate conversion;
- detail-influence validation and interpolation;
- weighted stroke placement;
- detail-dependent length and width;
- deterministic legacy and production planner orchestration;
- coherent-flow numerical golden samples;
- boundary termination, opacity, progress and cancellation;
- preset schema migration and JSON round trips;
- memory estimates.

### Imaging integration tests

Use very small synthetic images generated in memory:

- decode dimensions and RGBA samples;
- metadata-stage rejection above 10,000 × 10,000;
- unsupported encoded data;
- cancellation and progress ordering;
- aspect-ratio-preserving proxies;
- independent copy ownership;
- PNG round trips;
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
- native cancellation and disposal paths.

Pixel-perfect full-image hashes are avoided because native renderer upgrades may produce harmless antialiasing differences. The primary golden master remains normalized plan data.

### UI tests

Only behaviours that belong to the presentation layer:

- mouse-region coordinate conversion;
- view-model commands;
- cancellation and state transitions;
- parameter validation presentation;
- project dirty state and save prompts.

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

## Importance-map policy

Structural analyzers use small synthetic fixtures with known edges and colour changes. Tests distinguish:

- uniform background baseline;
- edge response;
- chromatic contrast;
- smoothing spread;
- manual positive/negative composition;
- weighted plan effects.

Future semantic analyzers must be tested through provider contracts and stable repository-owned fixtures. Model-dependent confidence values must not silently become planner golden masters without versioning.

## Native-resource policy

Integration tests dispose every returned `SkiaImage`. Tests verify that disposed wrappers reject subsequent use. Temporary Skia objects in fixture factories use deterministic `using` scopes.

Long-running leak detection will be added to the controlled performance suite; routine unit tests do not infer native-memory release from garbage-collector timing.

## High-resolution validation

Routine tests use small synthetic fixtures. Dedicated slower tests will validate:

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

M5 contains 360 cases:

- 55 Domain;
- 275 Application;
- 13 Imaging.Skia;
- 17 Rendering.Skia.


## M5 project and workspace tests

Application tests now cover:

- preview-quality dimensions and aspect-ratio fitting;
- project construction, copied/read-only region collections and duplicate identifiers;
- project JSON round trips, truncation, cancellation and schema rejection;
- relative/absolute project source-path resolution;
- stable region identifiers, labels, movement, resizing, reordering and deletion;
- workspace dirty-state and project load/save transitions;
- structured operation and validation state;
- bounded recent-path ordering, deduplication and persistence;
- recent-items schema validation and cancellation.

UI smoke validation remains manual for native file pickers and bitmap ownership. Domain/Application tests do not reference Avalonia or SkiaSharp.
