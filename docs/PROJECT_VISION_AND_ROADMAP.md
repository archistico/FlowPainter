# FlowPainter — Project vision and living roadmap

**Document status:** living specification  
**Last updated:** 2026-07-13  
**Current milestone:** M8 — Semantic importance and generic subject analysis  
**Rule:** update this document in the same change set that alters scope, architecture or milestone status.

## 1. Product vision

FlowPainter transforms an input image into a new generative artwork that remains recognizably related to the source but is not a mechanical filter or pixel-by-pixel copy.

The software must interpret the visual hierarchy of the source and distribute artistic detail deliberately:

- backgrounds are synthesized with broader, freer and less detailed treatment;
- complete subjects, people, figures, objects and focal points receive progressively more information;
- faces may receive additional attention around eyes, mouth and defining contours, but facial landmarks are only one part of the subject hierarchy;
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

## 4. Artistic hierarchy and focus

The intended painting is not uniformly detailed. FlowPainter allocates artistic and computational resources according to a visual hierarchy:

```text
Background → Supporting area → Subject → Focal area → Critical detail
```

The background establishes colour, movement and atmosphere with larger primitives and broader marks. Subjects and silhouettes remain recognizable. Focal areas and critical details receive higher density, smaller forms, stronger edge preservation and more refinement passes. A subject is not limited to a face: it may be a person, group, animal, object, building, landscape feature or user-selected narrative element.

Automatic analysis and manual masks will eventually contribute to an `ArtisticFocusMap` and a configurable detail budget. M8 prepares semantic subject information; M9-M10 consume it in primitive and hybrid engines; M11 provides direct editing; M12 consolidates the full visual hierarchy.

## 5. Technical principles

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

## 6. Memory boundary

A 10,000 × 10,000 RGBA buffer requires 400,000,000 bytes, approximately 381 MiB.

The architecture therefore assumes:

- one decoded source buffer when necessary;
- one final output buffer when necessary;
- reduced analysis and preview buffers;
- no 10,000 × 10,000 floating-point detail map;
- explicit memory estimation before final rendering;
- immediate disposal of temporary and native buffers;
- tiled rendering only if measurements later prove it necessary.

## 7. Architecture target

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

## 8. Roadmap

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

**Status: DONE**

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

completed with zero warnings and errors and all 360 test cases passing. The smoke test covers project save/reopen, relative source references, manual-region editing, all three preview qualities and recent project/preset restoration.

### M6 — High-resolution final rendering

**Status: DONE**

- persist final maximum dimension, PNG/JPEG format and JPEG quality;
- derive exact output dimensions from source aspect ratio up to 10,000 × 10,000;
- retain the immutable preview `StrokePlan` and reuse it for final export;
- render against the original source background rather than the proxy;
- estimate known RGBA peak buffers and display memory risk;
- preserve alpha in PNG and flatten transparency over white for JPEG;
- support cancellation and combined rendering/encoding progress;
- migrate schema-1 projects to schema 2 defaults;
- validate with 400 automated cases.

Validated on Windows with zero build errors and all 400 tests passing.

### M6.1 — Synchronized comparison viewport

**Status: DONE**

- zoom source and rendered preview with the mouse wheel;
- pan either panel with the middle mouse button;
- maintain one shared normalized center and zoom factor;
- preserve cursor anchoring while zooming;
- keep manual detail-region selection aligned under viewport transforms;
- preserve the view when rebuilding or rerendering, and reset it for a new source;
- validate with 410 automated cases.

### M7 — Brush engine

**Status: DONE**

- pure Domain `BrushSettings` and `BrushKind` values;
- Skia-specific `ISkiaBrushRenderer` strategy boundary;
- compatible `SolidRound` renderer;
- procedural `SoftRound`, `Flat` and `Bristle` renderers;
- deterministic per-stroke size and opacity variation derived from plan seed and stroke index;
- hardness, bristle count and bristle spread controls;
- exact preview/final reuse of both `StrokePlan` and approved brush settings;
- brush-aware built-in presets and desktop controls;
- project and preset schema 3 with schema-1/schema-2 compatibility defaults;
- rendering and persistence tests for all built-in brush families;
- validated suite of 440 automated cases.

Raster texture masks, custom brush-tip loading, pressure curves, stamp spacing and point-wise rotation are extensions of this foundation and remain planned after the initial procedural engine is validated.

### M8 — Semantic importance and subject analysis

**Status: READY FOR VALIDATION**

- pure Domain semantic roles, subject kinds and normalized regions;
- replaceable `ISemanticImportanceAnalyzer` Application contract;
- deterministic built-in saliency and generic subject segmentation;
- separate subject, silhouette, focal and combined importance maps;
- structural/semantic composition before manual-region adjustment;
- confidence and role visualization with selectable diagnostic overlays;
- promotion of detected subjects/focal points to editable manual regions;
- project and preset schema 4 with schema-1/schema-2/schema-3 defaults;
- no bundled machine-learning runtime or model in the first provider;
- expected suite of 496 automated cases.

The built-in provider identifies generic subject-like regions and must not be presented as a person/animal/object classifier. Class-aware local providers remain a compatible later extension through the same contracts.

### M9 — Geometric primitive engine

**Status: PLANNED**

- immutable `PrimitivePlan`;
- triangle, rectangle, rotated rectangle, circle and ellipse primitives;
- replaceable rasterizer, scorer, factory and mutator;
- optimal colour estimation and local error updates;
- deterministic hill climbing on reduced proxies;
- detail-aware primitive size and search budget;
- high-resolution raster and SVG export.

### M10 — Hybrid primitive and flow-field engine

**Status: PLANNED**

- primitives establish broad colour masses and background composition;
- primitive axes, boundaries and influence fields deform local stroke flow;
- layered primitive, flow-painting and refinement passes;
- detail-budget allocation between engines;
- independent and hybrid modes remain available;
- deterministic plan composition and tests.

### M11 — Advanced visual editing

**Status: PLANNED**

- non-rectangular masks and brush-painted focus/background selections;
- direct move, resize and edit operations on generated primitives;
- local regeneration and locked/protected areas;
- before/after comparison;
- undo/redo command history;
- editable subject, focal and critical-detail roles.

### M12 — Artistic focus and visual hierarchy

**Status: PLANNED**

- explicit Background → Supporting area → Subject → Focal area → Critical detail hierarchy;
- global and local detail budgets;
- progressive preservation of silhouettes, subjects and narrative details;
- coordinated stroke density, brush complexity, primitive size, edge fidelity and colour precision;
- deliberate background simplification and painterly freedom;
- presets such as portrait focus, central subject, multiple subjects and cinematic background;
- quantitative tests proving that important regions receive more artistic resources than background regions.

### M13 — Performance, packaging and release

**Status: PLANNED**

- controlled 10,000 × 10,000 stress suite;
- CPU and native-memory profiling;
- deterministic parallelization where safe;
- autosave and recovery;
- Windows/Linux packaging and publish profiles;
- end-user documentation, screenshots and release checklist.

## 9. Milestone discipline

Every milestone must:

1. update this living roadmap;
2. add or update architecture decisions when boundaries change;
3. include automated tests for each new non-UI behaviour;
4. build with zero warnings and zero errors;
5. pass the complete existing test suite;
6. document any manual smoke validation that cannot yet be automated.
