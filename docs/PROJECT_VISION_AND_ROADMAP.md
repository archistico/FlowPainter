# FlowPainter — Project vision and living roadmap

**Document status:** living specification  
**Last updated:** 2026-07-16  
**Current validated baseline:** M13.4.1 — Dirty state and data-loss protection (765 tests)  
**Next milestone:** M13.4.2 — Memory and work budgets  
**Rule:** update this document in the same change set that alters scope, architecture or milestone status.

## 1. Product vision

FlowPainter transforms an input image into a new generative artwork that remains recognizably related to the source but is not a mechanical filter or pixel-by-pixel copy.

The software must interpret the visual hierarchy of the source and distribute artistic detail deliberately:

- backgrounds are synthesized with broader, freer and less detailed treatment;
- coherent image regions, user-selected subjects and focal points receive progressively more information;
- region identity is derived from deterministic visual coherence rather than class labels;
- high-contrast, structurally significant, texturally rich or manually protected areas may receive more detail;
- broad backgrounds and uniform colour fields may be represented with larger forms and fewer marks;
- the user can add, reduce or redirect detail by selecting areas directly with the mouse.

The intended result is an authored generative painting in which algorithms, parameters and manual guidance create a new composition.

## 2. Core artistic model

FlowPainter coordinates four distinct but composable control structures:

```text
Region field      → which pixels belong to the same coherent area and at which hierarchy level
Detail field      → how much local information to render
Importance field  → where to invest the artistic/computational budget
Boundary field    → which separations to protect and which tangent direction to follow
```

Automatic analysis and explicit user edits contribute to these fields. They must remain independently inspectable because a wrong classification, an incorrect boundary or an unsuitable rendering policy require different corrections.

The fields influence all generative engines:

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

### 3.3 Automatic regional and structural analysis

The approved future architecture uses deterministic, local and model-free analysis:

- SLIC superpixel segmentation in CIELAB + image-coordinate space;
- connected-region normalization and compact label maps;
- per-region colour, luminance, texture, edge and orientation descriptors;
- Region Adjacency Graph construction;
- hierarchical merging across fine, intermediate and broad-mass levels;
- boundary strength, tangent direction and distance fields;
- structural contrast and optional model-free saliency signals;
- explicit manual roles for subject, background, focus and protected areas.

SAM, MobileSAM, ONNX providers, Python inference and other machine-learning segmentation paths are outside the approved roadmap. The validated M8–M13.3 semantic subsystem remains readable and operational only as a compatibility baseline until M14.7 replaces its active automatic contribution.

### 3.4 Manual detail editing

The user must eventually be able to:

- drag rectangular regions over the preview;
- increase or reduce detail strength;
- assign artistic roles such as subject, background, focal region, protected region or ignored region;
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

The validated M8–M13.3 path currently contributes generic semantic evidence and manual corrections. The approved future path replaces automatic semantic ranking with SLIC region structure: M13.4 stabilizes the application, M14 builds and adopts regional segmentation, M15 applies the hierarchy to rendering, M16 adds advanced region editing and M17 completes high-resolution optimization and release work.

Important boundaries are a first-class artistic signal. The software must identify the contours that separate subjects, figures and objects, estimate their local tangent and preserve the visual discontinuity between their two sides. Near a significant contour, strokes should tend to follow the boundary rather than cross it randomly. This separation is essential for recognition even when the surrounding treatment is broad and abstract.

## 5. Technical principles

- .NET 10 and C# 14.
- Avalonia desktop UI.
- Pure domain model without Avalonia or SkiaSharp dependencies.
- SkiaSharp isolated in dedicated imaging/rendering adapters.
- Deterministic plans independent of rasterization resolution.
- No hidden global randomness.
- No machine-learning runtime, model checkpoint, Python environment or GPU requirement for segmentation.
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
- compact regional labels using `UInt16` when possible and `UInt32` only when required;
- proxy-first global segmentation with high-resolution border refinement;
- explicit memory and work estimation before segmentation, planning and final rendering;
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
Structural analysis + SLIC regional segmentation
    ↓
Connected label map + regional descriptors + Region Adjacency Graph
    ↓
Hierarchical merge levels + boundary strength / tangent / distance fields
    ↓
Manual region roles and detail corrections
    ↓
Importance / suppression / artistic-detail policies
    ↓
Primitive plan and/or boundary-guided stroke plan
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

**Status: DONE**

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

**Status: DONE**

- immutable resolution-independent `PrimitivePlan`;
- triangle, rectangle, rotated rectangle, circle and ellipse primitives;
- replaceable mask rasterizer, scorer, candidate factory and mutator;
- average-colour initial canvas, optimal colour estimation and local error updates;
- deterministic candidate search and hill climbing on reduced proxies;
- detail-aware placement, size, error weighting and mutation budget;
- selectable Flow painting / Geometric primitives application modes;
- project schema 5 with schema-1 through schema-4 compatibility;
- high-resolution PNG/JPEG rasterization and SVG export;
- expected suite of 545 automated cases.

### M10 — Hybrid primitive and flow-field engine

**Status: DONE**

- immutable `HybridPlan` combining a primitive layer, primitive-guided flow layer and detail-refinement layer;
- primitives establish broad colour masses and background composition;
- axis, rotated-boundary tangent, vortex and mixed primitive influence fields;
- deterministic distance falloff and bounded nearby-influence count;
- configurable primitive, flow and refinement layer budgets;
- refinement-specific detail bias, length and width multipliers;
- selectable Flow painting, Geometric primitives and Hybrid application modes;
- project schema 6 with schema-1 through schema-5 compatibility;
- synchronized preview and high-resolution PNG/JPEG export from the same hybrid plan;
- deterministic layered rendering with explicit native-image ownership;
- validated suite of 576 automated cases and successful visual validation of the hybrid mode.

### M11 — Scene separation and important boundaries

**Status: DONE**

Purpose: identify the structural separations that make subjects and forms recognizable before changing stroke behaviour. M11 is diagnostic: it creates and visualizes the data that M12 consumes.

Deliverables:

- pure Domain `BoundaryVector` and immutable `BoundaryDirectionField`;
- replaceable `ISceneBoundaryAnalyzer` contract;
- deterministic built-in multiscale luminance and colour boundary analyzer;
- distinction between raw edge strength, important edges, subject silhouettes, internal structure and fine texture;
- tangent direction field rather than only the gradient normal;
- continuity and multiscale persistence used to promote coherent contours over isolated texture;
- semantic silhouette contribution from M8;
- explicit background-confidence and uncertainty maps;
- configurable protection radius around subjects and silhouettes;
- diagnostic overlays for all scalar maps and sampled tangent directions;
- project schema 7 and preset schema 5 with backward-compatible defaults;
- complete documentation of M11-M16, including boundary-aware painting and background suppression;
- validated diagnostic baseline of 631 automated cases.

M11 does not redirect or terminate strokes. It validates where the significant boundaries are, how confident the classification is and which tangent guides the M12 painter.

Exit criteria:

- a clear synthetic shape on a uniform background produces a strong subject boundary;
- equal-luminance colour changes remain detectable;
- the tangent field follows horizontal, vertical and curved contours;
- coherent silhouette edges rank above fine background texture;
- areas near a subject are protected from confident background classification;
- uniform low-salience areas receive high background confidence;
- equal inputs and settings produce equal maps;
- all overlays remain aligned during synchronized zoom and pan;
- build has zero warnings/errors and all 631 cases pass.

### M12 — Boundary-aware painting

**Status: DONE**

Purpose: make the stroke planner respect the M11 boundary field so that painterly freedom does not destroy form recognition.

Deliverables:

- `BoundaryPaintingSettings` with explicit validation and backward-compatible disabled defaults;
- derived `BoundaryGuidanceField` containing tangent, influence, hardness, silhouette confidence and corner strength;
- progressive, radius-based tangent alignment blended with the existing artistic flow field;
- separate influence weights for internal structure and low-priority texture edges;
- sampled crossing-risk evaluation along each proposed segment;
- deterministic deflection toward the nearest tangent orientation;
- optional shortening and termination before hard or uncertain boundaries;
- stroke-origin colour sampling that preserves the originating side instead of averaging across a silhouette;
- contour-driven detail reinforcement without drawing an artificial outline;
- corner and junction preservation through local segment shortening;
- reuse of one guidance field by both hybrid flow and refinement layers;
- exact fallback to the validated M10/M11 plan when boundary-aware painting is disabled;
- built-in Soft contour, Strong silhouette and Loose background policies plus tuned existing presets;
- project schema 8 and preset schema 6 with previous-schema compatibility;
- deterministic preview/final plan reuse;
- expected suite of 666 automated cases.

Current intentional limits:

- internal and external sides of a contour are not yet generated as separate stroke layers;
- background suppression is not yet a signed detail field;
- manual boundary/barrier editing remains planned for M14.

Exit criteria:

- strokes near an important contour are measurably more parallel to its tangent;
- hard silhouettes can deflect or terminate a crossing stroke;
- low-importance texture can be configured not to over-constrain the flow;
- corners shorten local segments instead of being smoothed away;
- disabling boundary influence exactly reproduces the validated M10/M11 behaviour;
- influence propagation from free flow to tangent alignment is deterministic and spatially bounded;
- both hybrid stroke layers use the same derived boundary policy;
- project and preset settings round-trip and older schemas load with disabled M12 defaults;
- build has zero warnings/errors and all 666 cases pass.

### M13 — Background suppression and painterly simplification

**Status: DONE**

Purpose: explicitly identify areas that can be simplified and reduce their detail without producing holes or damaging the subject silhouette.

Implemented scene model:

```text
Manual focus / critical protection
Semantic subject and focal protection
Silhouette and uncertain transition protection
Neutral scene area
Confident background suppression
```

Implemented deliverables:

- immutable signed `ArtisticDetailField` in the normalized `[-1, +1]` range;
- `BackgroundSuppressionComposer` combining automatic detail, manually composed detail, semantic importance, subject masks, silhouettes, boundary confidence and uncertainty;
- explicit priority: manual increases → subjects/importance → silhouette → uncertainty → background;
- configurable overall strength, detail floor, uncertainty protection, silhouette protection and transition softness;
- separable suppression, protection and effective-detail maps for diagnostics;
- weighted stroke placement that allocates fewer starts to negative-background areas;
- longer and wider marks, fewer segments and freer curvature as suppression increases;
- deterministic colour quantization in confident background;
- `flow-field-background-v1` plan version for signed-detail planning;
- reuse of the effective detail map by primitive optimization, naturally increasing form size and reducing local precision in background;
- reuse of the signed policy by both hybrid flow layers;
- Background suppression, Background protection and Artistic detail overlays;
- updated Balanced and Loose background presets;
- project schema 9 and preset schema 7 with previous-schema compatibility defaults;
- validated suite of 700 automated cases.

Current intentional limits:

- M13 does not yet paint freehand suppression/protection masks; that belongs to M14;
- primitive candidate count is still globally configured, while local size/error allocation is detail-driven;
- full Background → Supporting → Subject → Focus → Critical budget orchestration remains M15;
- class-aware recognition remains replaceable and optional.

Exit criteria:

- confident background receives fewer stroke origins than protected subject areas;
- average background marks are longer, wider and structurally simpler;
- manual focus, semantic subjects, silhouettes and uncertain areas resist automatic suppression;
- the configured detail floor is never violated;
- suppression transitions are deterministic and softened rather than forming cut-out edges;
- disabling M13 preserves the validated M12 plan and random sequence;
- Flow, Primitive and Hybrid use one consistent effective-detail policy;
- project and preset settings round-trip, and older schemas load with suppression disabled;
- build has zero warnings/errors and all 700 cases pass (validated by the user on Windows).

### M13.2 — Soft manual detail regions

**Status: DONE — validated on Windows**

Purpose: prevent rectangular manual-detail controls from becoming visible as hard seams in the generated painting.

Deliverables:

- `DetailInfluenceSettings.RegionTransitionWidth`, defaulting to 5% of the shorter analysis-map dimension;
- distance-based region feathering in analysis-map pixel space;
- SmoothStep interpolation both inside and outside each rectangle;
- full-strength region core and zero influence beyond the exterior transition radius;
- Euclidean corner falloff;
- maximum merging for same-intent overlaps;
- deterministic latest-opposing-intent ordering;
- composed-map invalidation when transition width changes;
- desktop control and explanatory text;
- project schema 10 and preset schema 8 with schema-9/schema-7 defaults;
- suite of 713 automated cases;
- milestone document and ADR-0015.

Exit criteria:

- a strong focus/background rectangle no longer leaves a visible geometric seam at its border;
- the centre of a sufficiently large region still reaches its configured full strength;
- pixels immediately inside and outside the border have similar, continuous influence;
- overlapping same-intent regions do not create an artificial detail spike;
- transition width zero reproduces the former hard rectangle;
- preview rebuild, reanalysis, project reload and all generative modes use the same transition;
- build has zero warnings/errors and all 713 cases pass.

### M13.3 — Region selection and semantic corrections

**Status: DONE — validated baseline on 2026-07-16**

Purpose: make overlay regions directly editable and provide a persistent, reversible correction layer for imperfect automatic subject detection.

Deliverables:

- click/drag discrimination using a 6-pixel display-space threshold;
- deterministic hit testing and cycling for overlapping detail, correction and automatic semantic regions;
- selected-overlay highlighting and synchronized list selection;
- `Delete` shortcut for selected manual detail regions and semantic corrections;
- immutable `SemanticCorrectionRegion` and `SemanticCorrectionKind` Domain values;
- separate correction editor with stable identifiers and one forced primary subject;
- correction intents `ForcePrimarySubject`, `ForceSubject`, `ForceBackground` and `IgnoreAutomaticDetection`;
- non-destructive correction composition before scene-boundary analysis;
- soft SmoothStep correction borders reusing `RegionTransitionWidth`;
- explicit kind precedence and maximum same-kind overlap merging;
- project schema 11 with schema-10 empty-collection compatibility;
- unchanged preset schema 8;
- 35 milestone-specific cases, bringing M13.3 itself to 748 cases;
- 7 audit-remediation Application cases added on 2026-07-16, bringing the validated repository baseline to 755 cases;
- milestone document and ADR-0016.

Exit criteria:

- a click selects an existing overlay without creating a new rectangle;
- a drag beyond the threshold creates a manual detail region;
- overlapping overlays can be selected predictably;
- a detected region can be forced to primary subject, subject, background or ignored status;
- only one forced primary subject exists at a time;
- correction effects reach semantic maps, boundary analysis, background suppression and all generative engines;
- correction borders remain visually gradual rather than rectangular;
- corrections can be deleted/cleared and survive project save/reload;
- schema-10 projects load with no corrections;
- build has zero warnings/errors and all 755 repository cases pass.

Roadmap note: M13.3 remains supported for schema-11 compatibility. No further automatic primary-subject ranking or ML provider work is planned; M14.7 replaces the active automatic semantic path with SLIC regional segmentation while migrating manual corrections to generalized region-role overrides.

### M13.4 — Pre-SLIC stabilization

**Status: IN PROGRESS**

Purpose: remove known state, memory, persistence and orchestration risks before regional label maps and new analysis buffers are introduced.

#### M13.4.1 — Dirty state and data-loss protection

**Status: DONE — validated with 765 tests**

Detailed validation plan: [`M13_4_1_DIRTY_STATE_AND_DATA_LOSS_PROTECTION.md`](M13_4_1_DIRTY_STATE_AND_DATA_LOSS_PROTECTION.md).

- complete dirty tracking for project-affecting controls and committed regional edits;
- Save / Discard / Cancel guards before opening an image, opening a project, using a recent project or closing;
- testable `ProjectSessionController` outside Avalonia;
- dirty-title indicator and suppression during transactional project adoption;
- destructive navigation proceeds after Save only when persistence succeeds;
- ten new Application test cases, bringing the validated suite to 765.

#### M13.4.2 — Memory and work budgets

**Status: READY FOR VALIDATION**

Detailed validation plan: [`M13_4_2_MEMORY_AND_WORK_BUDGETS.md`](M13_4_2_MEMORY_AND_WORK_BUDGETS.md). Architecture decision: [`ADR-0018`](decisions/ADR-0018-RESOURCE-ADMISSION-BUDGETS.md).

- one shared 2 GiB working-set admission policy for analysis and final rendering;
- current-analysis estimate plus an explicit 24-byte-per-proxy-pixel future SLIC reserve;
- mode-aware Flow, Primitive and Hybrid final-render estimates using three or four output-sized buffers;
- bounded 256 MiB encoded input with direct seekable reads and cancellation-aware streaming;
- Application-level Flow segment, primitive score-attempt and primitive pixel-evaluation budgets;
- pre-allocation rejection inside planners and desktop analysis/export entry points;
- seventeen new test cases, bringing the expected suite to 782;
- representative measured 4K/8K profiling and opt-in 10K certification remain follow-up validation/M17 work.

#### M13.4.3 — Atomic durable writes

- temporary sibling files for local project, preset, preview, SVG and final-image writes;
- flush, close and atomic replace/move semantics;
- preservation of the previous valid destination on cancellation or failure;
- documented fallback for storage providers without atomic replacement.

#### M13.4.4 — Analysis orchestration extraction

- extract an `AnalysisCoordinator` from `MainWindow`;
- immutable analysis cache keys and revision tracking;
- cancellation, failure and stale-result tests without Avalonia;
- adopt derived resources only after successful completion;
- retain a thin UI composition root rather than performing a framework-wide rewrite.

Exit criteria:

- no unsaved edit is lost without an explicit user decision;
- failed open or analysis leaves the active session unchanged;
- every accepted operation has a defensible memory/work bound;
- failed local writes preserve the previous destination;
- analysis lifecycle behaviour is covered outside the desktop window.

### M14 — SLIC regional segmentation

**Status: PLANNED — approved direction**

Detailed plan: [`M14_SLIC_REGIONAL_SEGMENTATION.md`](M14_SLIC_REGIONAL_SEGMENTATION.md). Architectural decision: [`ADR-0017`](decisions/ADR-0017-SLIC-REGIONAL-SEGMENTATION.md).

#### M14.1 — Regional segmentation contracts

- `IRegionSegmentationAnalyzer` and immutable request/settings/result contracts;
- compact `RegionLabelMap` with `UInt16`/`UInt32` storage policy;
- `ImageRegion`, bounds, diagnostics and progress contracts;
- invariants for complete coverage, unique ownership, connectedness, compact labels and determinism.

#### M14.2 — Deterministic SLIC implementation

- RGB to CIELAB conversion;
- regular centroid initialization and low-gradient relocation;
- localized assignment/update iterations;
- `TargetRegionSize`, `Compactness`, `PreBlurSigma`, iteration and convergence controls;
- cancellation and progress reporting.

#### M14.3 — Connectivity and diagnostics

- split disconnected components;
- merge undersized components;
- compact relabelling;
- mean-colour preview, boundary overlay and region statistics;
- topology and reproducibility tests.

#### M14.4 — Regional descriptors

- area, bounds, centroid, perimeter and compactness;
- mean/variance in Lab and luminance;
- texture energy, edge density and dominant orientation;
- immutable descriptor tables derived from the label map.

#### M14.5 — Region Adjacency Graph

- one node per region and one edge per shared boundary;
- shared-boundary length, colour/texture difference and gradient statistics;
- normalized boundary strength and prevailing tangent direction;
- deterministic adjacency ordering and graph validation.

#### M14.6 — Hierarchical merge

- deterministic merge costs based on colour, texture, boundary and shape;
- protection of strong contours;
- fine, intermediate and broad-mass hierarchy levels;
- traceable parent/child region mapping.

#### M14.7 — Pipeline adoption and semantic-path retirement

- SLIC regions become the active automatic regional representation;
- no automatic class labels or primary-subject recognizer are required;
- legacy M8 semantic output is removed from active Flow, Primitive and Hybrid planning;
- schema-11 semantic corrections migrate to generalized region-role overrides;
- old project schemas remain readable and retain their intentional manual decisions;
- all three generative modes consume one shared regional/boundary/detail pipeline.

#### M14.8 — UI, settings and persistence

- region-size, compactness, smoothing, merge and hierarchy controls;
- label, boundary, hierarchy and diagnostic overlays;
- explicit reanalysis and cache invalidation;
- project-schema migration for SLIC settings and image-specific role overrides;
- presets persist reusable segmentation parameters but never source-specific region edits.

Exit criteria:

- SLIC produces a complete, connected and deterministic partition;
- regional descriptors, adjacency and hierarchy are internally consistent;
- memory is estimated before allocation and remains within the M13.4 budget policy;
- the active rendering pipeline no longer depends on automatic semantic recognition;
- legacy projects open without losing manual intent;
- build has zero warnings/errors and the complete test suite passes.

### M15 — Region-guided painterly rendering

**Status: PLANNED**

#### M15.1 — Regional boundary field

- derive distance, strength, normal and tangent from the SLIC/RAG boundary model;
- distinguish strong barriers from soft transitions;
- integrate with the validated M11–M12 guidance policy;
- preserve gradual parameter blending around weak borders.

#### M15.2 — High-detail local stroke policy

- shorter and thinner marks in detailed regions;
- increased local segment count and controlled curvature;
- stronger tangent alignment and crossing resistance near important boundaries;
- continuous interpolation of density, length, width, segmentation and curvature.

#### M15.3 — Staged Flow rendering

- broad base masses first;
- regional structure second;
- important contours third;
- fine local detail last;
- independent deterministic budgets and seeds with preview/final plan reuse.

#### M15.4 — Primitive coarse-to-fine rendering

- large diffuse forms for broad masses;
- medium primitives for regional structure;
- small forms for protected and focal regions;
- one hierarchy shared with Flow and Hybrid refinement.

#### M15.5 — Unified artistic hierarchy

- Broad mass → Supporting region → Protected region → Focal region → Critical detail;
- coordinated stroke, primitive, colour and boundary budgets;
- roles derived from SLIC scale, structural evidence and manual intent rather than class labels;
- quantitative tests proving deliberate resource allocation without abrupt seams.

### M16 — Advanced regional editing

**Status: PLANNED**

- select a fine SLIC region or one of its hierarchical ancestors;
- merge, split, exclude or locally resegment regions;
- assign subject, background, focus, protected and ignored roles;
- evolve `SemanticCorrectionRegion` into a generalized compatibility-preserving region-role override;
- brush-painted increase/reduce detail masks;
- manual barriers and false-boundary erasing;
- polygonal and freehand masks;
- locked areas, partial regeneration and before/after comparison;
- undo/redo command history.

### M17 — High resolution, optimization, packaging and release

**Status: PLANNED**

- global proxy segmentation with projection to source resolution;
- high-resolution border refinement and optional local SLIC in complex areas;
- overlap-aware tiling only if measurements prove it necessary;
- incremental caches and partial reanalysis;
- controlled 10,000 × 10,000 stress suite;
- managed/native memory and CPU profiling;
- deterministic parallelization where safe;
- autosave and recovery;
- Windows/Linux packaging, publish profiles, end-user documentation and release checklist.

## 9. Milestone discipline

Every milestone must:

1. update this living roadmap;
2. add or update architecture decisions when boundaries change;
3. include automated tests for each new non-UI behaviour;
4. build with zero warnings and zero errors;
5. pass the complete existing test suite;
6. document any manual smoke validation that cannot yet be automated.
