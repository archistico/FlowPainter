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

M7 contains 440 cases:

- 71 Domain;
- 317 Application;
- 24 Imaging.Skia;
- 28 Rendering.Skia.

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
- project schema-1 migration and schema-2 final-output round trips;
- workspace dirty state for final-output changes;
- PNG and JPEG encoder output, cancellation, progress and truncation;
- white composition for transparent JPEG output;
- original-source compatibility with integer-rounded proxy plan dimensions;
- reuse of normalized renderer geometry at independent output dimensions.

Full 10,000 × 10,000 allocation remains a controlled stress test rather than part of every unit-test run.
