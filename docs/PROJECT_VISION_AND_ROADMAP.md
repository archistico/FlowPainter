# FlowPainter — Project vision and living roadmap

**Document status:** living specification  
**Last updated:** 2026-07-13  
**Current milestone:** M4 — Structural detail map and manual regions  
**Rule:** update this document in the same change set that alters scope, architecture or milestone status.

## 1. Product vision

FlowPainter transforms an input image into a new generative artwork that remains recognizably related to the source but is not a mechanical filter or pixel-by-pixel copy.

The software must interpret the visual hierarchy of the source and distribute artistic detail deliberately:

- important subjects and focal points receive more information;
- faces may receive additional attention around eyes, mouth and defining contours;
- high-contrast, structurally significant or visually salient areas may receive more detail;
- broad backgrounds and uniform colour fields may be represented with larger forms and fewer marks;
- the user can add, reduce or redirect detail by selecting areas directly with the mouse.

The intended result is an authored generative painting in which algorithms, parameters and manual guidance create a new composition.

## 2. Core artistic model

A shared **importance/detail map** is the main control surface of the application. It combines automatic analysis and explicit user edits.

The map will influence both principal engines:

### 2.1 Flow painting

The detail value may control:

- stroke density;
- stroke length and width;
- brush complexity;
- colour sampling precision;
- curvature tolerance;
- local flow-field deformation;
- number of refinement passes.

### 2.2 Geometric primitives

The detail value may control:

- primitive size;
- candidate-search budget;
- primitive type;
- local error weighting;
- number of primitives allocated to an area;
- refinement order.

### 2.3 Hybrid painting

The long-term default workflow is expected to be hybrid:

1. large primitives establish colour masses and composition;
2. primitive boundaries, axes and local structure may influence the global vector field;
3. flow-guided brush strokes add painterly movement;
4. important regions receive finer primitives and richer strokes;
5. manually selected regions override or reinforce automatic analysis.

Flow painting and primitive generation must remain independently usable, but their plans and analysis data must be composable.

## 3. Functional objectives

### 3.1 Input and output

- Load local raster images.
- Support decoded RGBA dimensions up to **10,000 × 10,000 pixels**.
- Preserve aspect ratio unless the user explicitly requests cropping or distortion.
- Separate analysis/preview resolution from final export resolution.
- Save final raster output, initially as PNG and later additional formats where useful.
- Save project settings, seed, manual regions and generated plans for reproducible re-rendering.

### 3.2 Parameter control

- Expose algorithm parameters with validated ranges and meaningful units.
- Support deterministic seeds.
- Provide presets without hiding the underlying parameters.
- Allow preview generation, cancellation and final high-resolution rendering.

### 3.3 Automatic detail analysis

The architecture must support interchangeable analyzers for:

- visual saliency;
- local contrast and edge density;
- colour variation;
- subject or focal-point detection;
- face detection;
- facial landmarks such as eyes and mouth;
- semantic segmentation when technically and legally appropriate.

No particular machine-learning runtime is mandated yet. Automatic analysis must be isolated behind application contracts so that models can be replaced without changing the generative engines.

### 3.4 Manual detail editing

The user must eventually be able to:

- drag rectangular regions over the preview;
- increase or reduce detail strength;
- label regions such as face, eye, mouth, subject or background;
- resize, move, remove and reorder regions;
- visualize the combined detail map;
- combine manual regions with automatic detections;
- later use non-rectangular masks or brush-painted selections.

All selections are stored in normalized image coordinates so that they remain valid at every preview and export resolution.

### 3.5 Brush system

The first compatible renderer will use a solid stroked path. The final brush architecture must support:

- procedural brushes;
- raster masks and textures;
- bristle groups;
- pressure, opacity and width variation;
- spacing, scatter and rotation;
- dry-brush and textured deposition;
- deterministic local variation;
- reuse of the same stroke plan with different brush renderers.

### 3.6 Primitive system

The primitive engine will be inspired conceptually by `fogleman/primitive` while being reimplemented for this architecture in C#.

Expected primitive families:

- triangle;
- rectangle and rotated rectangle;
- circle and ellipse;
- polygon;
- Bézier path;
- eventually painterly stroke primitives.

The optimizer, error metric, mutation strategy and rasterizer must be replaceable and independently testable.

## 4. Technical principles

- .NET 10 and C# 14.
- Avalonia desktop UI.
- Pure domain model without Avalonia or SkiaSharp dependencies.
- SkiaSharp isolated in dedicated imaging/rendering adapters.
- Deterministic plans independent of rasterization resolution.
- No hidden global randomness.
- Explicit ownership and disposal of native resources.
- Cancellation and progress reporting for every expensive phase.
- 64-bit process for high-resolution work.
- Proxy analysis rather than full-size density maps.
- Full-size output allocation only when required.
- Automated tests added with each behaviour.
- Small milestones; build and tests must remain green before continuing.

## 5. Memory boundary

A 10,000 × 10,000 RGBA buffer requires 400,000,000 bytes, approximately 381 MiB.

The architecture therefore assumes:

- one decoded source buffer when necessary;
- one final output buffer when necessary;
- reduced analysis and preview buffers;
- no 10,000 × 10,000 floating-point detail map;
- explicit memory estimation before final rendering;
- immediate disposal of temporary and native buffers;
- tiled rendering only if measurements later prove it necessary.

## 6. Architecture target

```text
FlowPainter.sln

src/
├── FlowPainter.Domain
├── FlowPainter.Application
├── FlowPainter.Imaging.Skia
├── FlowPainter.Rendering.Skia
└── FlowPainter.App

tests/
├── FlowPainter.Domain.Tests
├── FlowPainter.Application.Tests
├── FlowPainter.Imaging.Skia.Tests
└── FlowPainter.Rendering.Skia.Tests
```

The central planned data flow is:

```text
Source image
    ↓
Analysis proxy
    ↓
Automatic importance map + manual regions
    ↓
Composed detail map
    ↓
Primitive plan and/or stroke plan
    ↓
Brush / primitive rasterization
    ↓
Preview or final export
```

## 7. Roadmap

Status legend:

- **DONE** — implementation and local test validation completed;
- **READY FOR VALIDATION** — implementation prepared, awaiting build/test confirmation in the target environment;
- **PLANNED** — not started;
- **DEFERRED** — intentionally postponed.

### M0 — Foundation

**Status: DONE**

Deliverables:

- new .NET 10 solution;
- Avalonia 12 desktop shell;
- centralized package versions;
- warnings treated as errors;
- normalized point and rectangular-region geometry;
- 10,000-pixel image-dimension invariant;
- detail-map and manual-region domain model;
- deterministic region-composition rule;
- deterministic random generator with golden sequence;
- flow, primitive and hybrid modes;
- explicit RGBA buffer memory estimator;
- automated Domain and Application tests;
- preserved unmodified legacy source;
- vision, architecture and testing documents.

Exit criteria:

```text
dotnet build FlowPainter.sln -c Release
dotnet test FlowPainter.sln -c Release
```

must complete with zero errors, zero warnings and all tests passing.

### M1 — Legacy FlowPainter characterization

**Status: DONE**

Deliverables:

- isolated pure-C# legacy planning behaviour;
- repository-owned RGBA/density characterization fixture;
- `IRandomSource` deterministic random boundary;
- scalar-field factory boundary without LibNoiseCore dependency;
- corrected circular angle-distance calculation;
- `IRgbaPixelSource` seam plus immutable, resolution-independent `StrokePlan` and `FlowStroke` models;
- explicit source-image background mode;
- retained legacy random-consumption sequence;
- golden characterization of field seed, colours, widths and path points;
- documented intentional deviations and deferred corrections;
- accepted Skia native-resource ownership ADR before adapter implementation.

Exit criteria:

```text
dotnet build FlowPainter.sln -c Release
dotnet test FlowPainter.sln -c Release
```

must complete with zero warnings and errors and all 77 test cases passing.

### M2 — Imaging and SkiaSharp rendering

**Status: DONE**

Deliverables:

- SkiaSharp 4 imaging and rendering adapters isolated from Domain/Application;
- local PNG, JPEG, WebP and BMP loading;
- decoded metadata validation before target RGBA allocation;
- rejection above 10,000 × 10,000 pixels;
- disposable `SkiaImage` implementation of `IRgbaPixelSource`;
- aspect-ratio-preserving analysis proxy generation without upscaling;
- solid-path rasterization of resolution-independent `StrokePlan` data;
- source-image and transparent backgrounds;
- progress and cancellation for imaging/rendering operations;
- PNG encoding and export;
- first end-to-end Avalonia source/result preview;
- deterministic internal preview field with no LibNoiseCore dependency;
- SkiaSharp managed/native package alignment ADR;
- 110 automated test cases across all non-UI layers.

Exit criteria:

```text
dotnet build FlowPainter.sln -c Release
dotnet test FlowPainter.sln -c Release
```

must complete with zero warnings and errors and all 110 test cases passing. The Avalonia smoke test must load, render, cancel, save and close cleanly.

### M3 — Parameters, presets and production flow field

**Status: DONE**

Deliverables:

- permanent `IFlowField` and `IFlowFieldFactory` application boundaries;
- internal deterministic coherent-noise field with versioned numerical golden samples;
- selectable legacy trigonometric comparison field without LibNoiseCore;
- field scale, octave, persistence, lacunarity and global rotation controls;
- production `FlowPainterPlanner` with versioned `flow-field-v1` plans;
- deterministic seed, stroke count, segment count, density, length, curvature, width, opacity and background settings;
- path termination at normalized image boundaries;
- planning progress and cancellation;
- built-in Balanced, Fine detail, Expressive and Legacy comparison presets;
- versioned JSON preset import/export;
- Avalonia parameter panel and seed generation;
- 183 automated test cases across all non-UI layers;
- explicit decision that LibNoiseCore will not enter the shipping solution.

Exit criteria:

```text
dotnet build FlowPainter.sln -c Release
dotnet test FlowPainter.sln -c Release
```

must complete with zero warnings and errors and all 183 test cases passing. The Avalonia smoke test must load an image, apply every built-in preset, edit parameters, generate a new seed, cancel/rerun planning, save/load a JSON preset and export a PNG.

### M4 — Structural detail map and manual regions

**Status: DONE**

Deliverables:

- `IDetailMapAnalyzer` application boundary;
- deterministic proxy-resolution structural analysis using luminance edges and local RGB contrast;
- configurable base detail, edge weight, contrast weight and smoothing radius;
- proxy heat-map visualization;
- normalized mouse-drag rectangle selection with letterbox/pillarbox correction;
- increase-detail and reduce-detail regions with configurable strength;
- remove-last and clear-region workflows;
- detail-weighted deterministic stroke placement;
- detailed/background length and width multipliers;
- versioned `flow-field-detail-v1` plan path while preserving `flow-field-v1` compatibility;
- preset schema version 2 with schema-1 migration;
- 249 automated test cases across all non-UI layers.

Exit criteria:

```text
dotnet build FlowPainter.sln -c Release
dotnet test FlowPainter.sln -c Release
```

must complete with zero warnings and errors and all 249 test cases passing. The Avalonia smoke test must analyze an image, toggle the heat map, create positive and negative regions, preserve region alignment during window resizing, render a deterministic detail-aware preview, remove/clear regions and load both schema-1 and schema-2 presets.

### M5 — Application workflow and project model

**Status: READY FOR VALIDATION**

Implemented scope:

- `FlowPainterWorkspace` application state, dirty tracking, operation state and validation messages;
- versioned `*.flowpainter.json` project documents;
- portable relative source-image references;
- Draft (256), Standard (512) and High (1,024) preview quality;
- save/reopen source reference, seed, all settings and ordered manual regions;
- list, relabel, resize, reorder and delete existing regions;
- side-by-side source/result comparison retained in the desktop UI;
- persistent recent-project and recent-preset lists;
- explicit rebuild/reanalysis commands for expensive operations;
- automated tests for project, workspace, region-editor and recent-item behaviour.

The Avalonia window remains the native-resource composition root. M5 deliberately extracts durable state and policies without placing disposable Skia/Avalonia resources inside view models.

Acceptance command:

```bash
dotnet build FlowPainter.sln -c Release
dotnet test FlowPainter.sln -c Release
```

must complete with zero warnings and errors and all 360 test cases passing. The smoke test must save and reopen a project, preserve relative source references and manual regions, edit/reorder regions, rebuild all three preview qualities and restore recent project/preset entries.

### M6 — High-resolution export

**Status: PLANNED**

- render the same plan at independent output resolution;
- PNG export up to 10,000 × 10,000;
- memory estimate and user warning before allocation;
- render cancellation;
- output metadata and deterministic project save;
- integration tests for dimensions, aspect ratio and repeatability.

### M7 — Brush engine

**Status: PLANNED**

- `IBrushRenderer` abstraction;
- compatible solid-stroke brush;
- procedural soft and flat brushes;
- texture-mask brush;
- width, opacity, pressure, spacing and rotation curves;
- brush presets;
- tests for spacing, transforms and deterministic variation.

### M8 — Semantic importance analysis

**Status: PLANNED**

- compose multiple automatic analyzer outputs;
- visual saliency and subject-region providers;
- face detection provider contract;
- facial-landmark regions for eyes, mouth and defining contours;
- explicit confidence and weighting policies;
- optional semantic segmentation provider;
- non-rectangular and brush-painted masks;
- deterministic merge tests and model/license ADRs.

### M9 — Geometric primitive engine

**Status: PLANNED**

- normalized `PrimitivePlan`;
- primitive factories and mutation strategies;
- triangle, rectangle, circle and ellipse;
- local error scoring and optimal colour calculation;
- deterministic hill-climbing baseline;
- detail-weighted candidate budgets and primitive sizes;
- proxy optimization and full-resolution rasterization;
- SVG export where the selected primitive types permit it.

### M10 — Hybrid artistic engine

**Status: PLANNED**

- combine primitive and stroke plans;
- use primitive geometry to influence local vector fields;
- coarse background primitives and fine focal-region refinement;
- layered rendering order and blending policies;
- shared detail budget across engines;
- hybrid presets and regression fixtures.

### M11 — Performance, packaging and release

**Status: PLANNED**

- benchmark representative image sizes;
- reduce allocations and native peak memory;
- controlled parallel candidate search;
- verify 10,000 × 10,000 export scenarios;
- Windows/Linux publishing;
- README screenshots and usage documentation;
- dependency/license audit;
- release checklist.

## 8. Definition of done for every milestone

A milestone is complete only when:

1. its behaviour is documented;
2. automated tests cover normal cases, limits and relevant failures;
3. `dotnet build -c Release` has zero warnings and errors;
4. `dotnet test -c Release` passes completely;
5. native resources are disposed deterministically where applicable;
6. no UI dependency leaks into Domain or Application;
7. this roadmap and the architecture document are updated;
8. the resulting ZIP can be compiled independently.

## 9. Open decisions

The following are intentionally not fixed yet:

- computer-vision runtime and model format;
- exact saliency and semantic-analysis algorithms;
- whether full-size rendering needs tile output in practice;
- precise artistic weighting formulas;
- final set of raster export formats;
- licensing implications of any future model files.

These decisions must be made through explicit ADRs after measurements or prototypes, not embedded silently in UI code.

## 10. Change log

### 2026-07-13 — M5.3 project rectangle serialization correction

- Added an Application-layer JSON converter for immutable `NormalizedRect` project values.
- Persisted only stable rectangle edges and reconstructed the validated domain value during loading.
- Preserved compatibility with previous schema-1 documents containing derived `width` and `height` properties.
- Added tests for the explicit JSON shape, previous-payload compatibility and invalid rectangle rejection.
- Increased the expected suite from 357 to 360 test cases.

### 2026-07-13 — M5.2 .NET 10 analyzer and Path ambiguity correction

- Resolved the `Path` ambiguity between `System.IO.Path` and `Avalonia.Controls.Shapes.Path` through an explicit `IoPath` alias.
- Changed the private recent-item lookup helper to accept the concrete `string[]` values already produced by the caller, satisfying `CA1859`.
- Reused static readonly expected-order arrays in region-editor tests, satisfying `CA1861` without weakening analyzers.
- Preserved the M5.1 scrollbar spacing, application behavior and 357-case suite.

### 2026-07-13 — M5.1 configuration scrollbar spacing

- Added an 18-pixel right gutter inside the configuration ScrollViewer.
- Disabled horizontal scrolling in the configuration panel.
- Prevented the vertical overlay scrollbar from covering input boxes and making text editing difficult.
- Kept the M5 feature set and 357-case suite unchanged.

### 2026-07-13 — M5 prepared

- Added the versioned `*.flowpainter.json` project document.
- Added portable source-image references, preview quality and persistent recent items.
- Added the testable workspace, structured operation state and editable ordered region collection.
- Connected project, preset, image, analysis, rendering and export operations to the workspace state.
- Expanded the suite from 249 to 357 test cases.
- Added ADR-0007 for the project/workspace boundary.

### 2026-07-13 — M4.1 validated on Windows

- The user confirmed the corrected package and continued development.
- Marked M4 DONE with the complete 249-case suite.

### 2026-07-13 — M4.1 floating-point test correction

- Preserved the viewport transformation implementation unchanged.
- Replaced exact `ViewportRect` record equality with component-wise assertions at 12 decimal digits.
- Applied the tolerant helper to all viewport-rectangle tests to prevent platform/runtime rounding noise.
- Kept production geometry unchanged and prepared the tolerance-based test correction later validated on Windows.

### 2026-07-13 — M4 prepared

- Added deterministic structural detail analysis at proxy resolution.
- Added heat-map visualization and normalized positive/negative mouse regions.
- Added detail-weighted placement, length and width policies with `flow-field-detail-v1` plans.
- Preserved the M3 `flow-field-v1` path and schema-1 preset compatibility.
- Expanded the suite from 183 to 249 test cases.
- Added ADR-0006 for the shared normalized detail-map pipeline.

### 2026-07-13 — M3 validated on Windows

- The user confirmed a clean build and all 183 tests passing.
- M3 is marked DONE.

### 2026-07-13 — M2.3 SkiaSharp 4 path-builder correction

- Replaced obsolete mutable `SKPath.MoveTo` and `SKPath.LineTo` calls with `SKPathBuilder`.
- Used `Detach()` to create the immutable `SKPath` consumed by `SKCanvas.DrawPath`.
- Preserved deterministic disposal of both the builder and detached path.
- Scanned all source and test projects for remaining mutable `SKPath` construction calls; none remain.
- Kept M2 status at READY FOR VALIDATION pending a clean target build, test pass and UI smoke test.

### 2026-07-13 — M2.2 stateless service analyzer correction

- Addressed `CA1822` on the image loader, proxy generator, PNG encoder and stroke renderer.
- Preserved instance service semantics for future injection, decoration and replacement.
- Added narrow method-level suppressions with explicit architectural justifications; global analyzer severity remains unchanged.
- Kept M2 status at READY FOR VALIDATION pending a clean target build, test pass and UI smoke test.

### 2026-07-13 — M2.1 .NET 10 exception correction

- Corrected `UnsupportedImageDimensionsException` to derive from `IOException` because `InvalidDataException` is sealed in .NET 10.
- Preserved exception semantics, public properties and all image-loader call sites.
- Kept M2 status at READY FOR VALIDATION pending a clean target build and test pass.

### 2026-07-13 — M2 prepared

- Added SkiaSharp 4 image loading, proxy generation, stroke-plan rasterization and PNG encoding.
- Added the first runnable Avalonia image-to-preview workflow with cancellation and explicit native ownership.
- Rejected decoded images above 10,000 × 10,000 before target bitmap allocation.
- Added deterministic temporary scalar field and uniform preview density without LibNoiseCore.
- Added package-alignment ADR for SkiaSharp managed and Linux native assets.
- Expanded the suite from 77 to 110 test cases.
- Marked M2 READY FOR VALIDATION.

### 2026-07-13 — M1 validated

- User validation confirmed clean compilation and all 77 tests passing for M1.1.
- Marked M1 as DONE.

### 2026-07-13 — M1.1 analyzer correction

- Corrected two `CA1859` findings reported by the first Windows build.
- Kept analyzer severity unchanged and introduced no suppressions.
- Preserved all public contracts, random draw order and golden-plan behavior.
- The corrected M1.1 package was subsequently validated with all 77 tests passing.

### 2026-07-13 — M1 prepared

- Extracted legacy stroke planning into pure Domain/Application code.
- Added immutable RGBA image, relative path, stroke and stroke-plan models.
- Added a repository-owned characterization fixture and deterministic golden plan.
- Preserved source colour sampling, field seeding, unused random consumption and source-image background behaviour.
- Corrected circular angle comparison and documented every intentional deviation.
- Added the Skia resource-ownership ADR without introducing a native dependency prematurely.
- Expanded the suite from 37 to 77 test cases.

### 2026-07-13 — M0 validated

- User validation confirmed clean restore, Release build and complete test pass for M0.1.
- Marked M0 as DONE with 37 passing test cases.

### 2026-07-13 — M0.1 validation fixes

- Kept analyzer severity unchanged and renamed all test methods to comply with `CA1707`.
- Resolved the namespace collision between `FlowPainter.Application` and `Avalonia.Application`.
- Recorded the first successful restore and failed build feedback in the validation log.
- Prepared the corrected package that was subsequently validated successfully.

### 2026-07-13 — M0 prepared

- Defined the software as a hybrid image-to-generative-painting system.
- Fixed the supported decoded image limit at 10,000 × 10,000 RGBA.
- Added automatic/manual importance-map architecture.
- Added planned face, eye, mouth and focal-region detail handling.
- Added planned mouse-selected detail regions.
- Added flow, primitive and hybrid engine roadmap.
- Created the .NET 10 solution foundation and automated tests.
