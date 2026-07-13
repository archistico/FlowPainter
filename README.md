# FlowPainter

FlowPainter is a .NET 10 and Avalonia application that transforms an input image into a new generative painting rather than applying a conventional image filter.

The planned engine combines:

- flow-guided brush strokes;
- optimized geometric primitives;
- automatic and manually edited detail maps;
- hybrid rendering in which broad background masses and important subjects receive different artistic treatment.

## Current milestone: M8

M0 established the solution and domain foundation. M1 characterized the original planner. M2 added local imaging and SkiaSharp rendering. M3 introduced configurable deterministic flow fields. M4 added the first importance-map workflow. M5 added persistent projects and an application-level editing workspace. M6 separates preview from final output and reuses the approved stroke plan for high-resolution PNG/JPEG export. M6.1 adds synchronized zoom and pan for direct source/result comparison. M7 separates stroke geometry from material rendering and adds four deterministic procedural brush families. M8 adds deterministic generic subject, silhouette and focal-area analysis through a replaceable semantic-provider boundary.

Implemented through M8:

- local PNG, JPEG, WebP and BMP loading;
- hard decoded-size limit of 10,000 × 10,000 RGBA;
- aspect-ratio-preserving Draft, Standard and High analysis proxies (256, 512 and 1,024 px maximum side);
- resolution-independent deterministic `StrokePlan` data;
- internal coherent-noise flow field with no LibNoiseCore dependency;
- configurable flow, stroke, brush and background parameters;
- built-in and versioned JSON presets;
- structural detail analysis based on luminance edges and local RGB contrast;
- deterministic generic saliency, subject, silhouette and focal-area maps;
- replaceable `ISemanticImportanceAnalyzer` provider boundary with no bundled ML runtime;
- semantic-region promotion to editable manual focus or critical-detail regions;
- diagnostic overlays for each semantic contribution;
- deterministic smoothing of the proxy detail map;
- heat-map visualization over the source image;
- rectangular mouse selections that increase or reduce local detail;
- normalized manual regions that survive window resizing;
- detail-weighted stroke placement;
- shorter and thinner strokes in detailed areas;
- broader and longer strokes in background areas;
- SkiaSharp 4 preview rendering through SolidRound, SoftRound, Flat and Bristle brush strategies;
- deterministic per-stroke size and opacity jitter shared by preview and final export;
- independent final PNG/JPEG export;
- versioned `*.flowpainter.json` projects containing source reference, seed, settings, preview quality and manual regions;
- portable project-relative source-image paths;
- Draft, Standard and High preview qualities;
- region list with relabel, percentage resize, reorder and delete operations;
- persistent recent-project and recent-preset lists;
- structured application workspace, operation and validation state;
- final output up to 10,000 × 10,000 with preserved aspect ratio;
- cached preview `StrokePlan` reused unchanged for final rasterization;
- synchronized source/result zoom with the mouse wheel and pan with the middle mouse button;
- known-RGBA memory estimate and risk indication before allocation;
- project and preset schema 4 with compatibility for schema 1, 2 and 3;
- 496 automated test cases across Domain, Application, Imaging and Rendering.

M8 provides generic subject-aware importance but does not claim class-specific recognition. Future local providers can add people, animals, objects, faces and landmarks through `ISemanticImportanceAnalyzer` without changing the planner or project format.

## Requirements

- .NET 10 SDK
- 64-bit Windows, Linux or macOS supported by Avalonia Desktop

## Build and test

Windows Command Prompt:

```bat
build.cmd
```

PowerShell:

```powershell
./build.ps1
```

Bash:

```bash
./build.sh
```

Equivalent manual commands:

```bash
dotnet restore FlowPainter.sln
dotnet build FlowPainter.sln -c Release --no-restore
dotnet test FlowPainter.sln -c Release --no-build
```

## Run

```bash
dotnet run --project src/FlowPainter.App/FlowPainter.App.csproj
```

Then:

1. choose **Open image**, or **Open project** for an existing `*.flowpainter.json` file;
2. inspect the combined detail heat map or individual semantic overlays;
3. promote a detected subject/focal region or create `IncreaseDetail` / `ReduceDetail` regions by dragging over the source;
4. select a region to relabel, resize, reorder or delete it;
5. choose Draft, Standard or High and use **Rebuild preview** when required;
6. choose SolidRound, SoftRound, Flat or Bristle and edit deterministic material parameters;
7. edit analysis/painting parameters and explicitly reanalyze or render;
8. save/load reusable `*.flowpreset.json` settings;
9. save the complete image-specific working state with **Save project**;
10. choose **Save preview** to export the current proxy-resolution PNG;
11. configure final maximum dimension and PNG/JPEG format;
12. choose **Export final** to rasterize the approved preview plan and brush against the original source.

Manual regions belong to projects, not presets. Project schema 4 stores normalized geometry, composition order, semantic-analysis settings, final-output settings and brush material; schema-1 through schema-3 files remain readable.

## Documentation

The main living document is [`docs/PROJECT_VISION_AND_ROADMAP.md`](docs/PROJECT_VISION_AND_ROADMAP.md).

Milestone-specific documents:

- [`docs/LEGACY_CHARACTERIZATION.md`](docs/LEGACY_CHARACTERIZATION.md)
- [`docs/M2_IMAGING_AND_RENDERING.md`](docs/M2_IMAGING_AND_RENDERING.md)
- [`docs/M3_PARAMETERS_AND_FLOW_FIELD.md`](docs/M3_PARAMETERS_AND_FLOW_FIELD.md)
- [`docs/M4_DETAIL_MAP_AND_MANUAL_REGIONS.md`](docs/M4_DETAIL_MAP_AND_MANUAL_REGIONS.md)
- [`docs/M5_APPLICATION_WORKFLOW_AND_PROJECTS.md`](docs/M5_APPLICATION_WORKFLOW_AND_PROJECTS.md)
- [`docs/M6_HIGH_RESOLUTION_EXPORT.md`](docs/M6_HIGH_RESOLUTION_EXPORT.md)
- [`docs/M6_1_SYNCHRONIZED_VIEWPORT.md`](docs/M6_1_SYNCHRONIZED_VIEWPORT.md)
- [`docs/M7_BRUSH_ENGINE.md`](docs/M7_BRUSH_ENGINE.md)
- [`docs/M8_SEMANTIC_IMPORTANCE.md`](docs/M8_SEMANTIC_IMPORTANCE.md)
