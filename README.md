# FlowPainter

FlowPainter is a .NET 10 and Avalonia application that transforms an input image into a new generative painting rather than applying a conventional image filter.

The planned engine combines:

- flow-guided brush strokes;
- optimized geometric primitives;
- automatic and manually edited detail maps;
- hybrid rendering in which broad background masses and important subjects receive different artistic treatment.

## Current milestone: M5.1

M0 established the solution and domain foundation. M1 characterized the original planner. M2 added local imaging and SkiaSharp rendering. M3 introduced configurable deterministic flow fields. M4 added the first importance-map workflow. M5 adds persistent projects and an application-level editing workspace. M5.1 reserves a stable right-side gutter so the configuration scrollbar never overlaps the input controls.

Implemented through M5:

- local PNG, JPEG, WebP and BMP loading;
- hard decoded-size limit of 10,000 × 10,000 RGBA;
- aspect-ratio-preserving Draft, Standard and High analysis proxies (256, 512 and 1,024 px maximum side);
- resolution-independent deterministic `StrokePlan` data;
- internal coherent-noise flow field with no LibNoiseCore dependency;
- configurable flow, stroke and background parameters;
- built-in and versioned JSON presets;
- structural detail analysis based on luminance edges and local RGB contrast;
- deterministic smoothing of the proxy detail map;
- heat-map visualization over the source image;
- rectangular mouse selections that increase or reduce local detail;
- normalized manual regions that survive window resizing;
- detail-weighted stroke placement;
- shorter and thinner strokes in detailed areas;
- broader and longer strokes in background areas;
- SkiaSharp 4 preview rendering and PNG export;
- versioned `*.flowpainter.json` projects containing source reference, seed, settings, preview quality and manual regions;
- portable project-relative source-image paths;
- Draft, Standard and High preview qualities;
- region list with relabel, percentage resize, reorder and delete operations;
- persistent recent-project and recent-preset lists;
- structured application workspace, operation and validation state;
- 360 automated test cases across Domain, Application, Imaging and Rendering.

M5 structural analysis does not yet recognize semantic objects. Face, eye, mouth and subject-aware analyzers will plug into the same `IDetailMapAnalyzer` and `DetailMap` pipeline in a later milestone.

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
2. inspect the automatically generated detail heat map;
3. create `IncreaseDetail` or `ReduceDetail` regions by dragging over the source;
4. select a region to relabel, resize, reorder or delete it;
5. choose Draft, Standard or High and use **Rebuild preview** when required;
6. edit analysis/painting parameters and explicitly reanalyze or render;
7. save/load reusable `*.flowpreset.json` settings;
8. save the complete image-specific working state with **Save project**;
9. choose **Save PNG** to export the current preview.

Manual regions belong to projects, not presets. The M5 project format stores their normalized geometry and composition order.

## Documentation

The main living document is [`docs/PROJECT_VISION_AND_ROADMAP.md`](docs/PROJECT_VISION_AND_ROADMAP.md).

Milestone-specific documents:

- [`docs/LEGACY_CHARACTERIZATION.md`](docs/LEGACY_CHARACTERIZATION.md)
- [`docs/M2_IMAGING_AND_RENDERING.md`](docs/M2_IMAGING_AND_RENDERING.md)
- [`docs/M3_PARAMETERS_AND_FLOW_FIELD.md`](docs/M3_PARAMETERS_AND_FLOW_FIELD.md)
- [`docs/M4_DETAIL_MAP_AND_MANUAL_REGIONS.md`](docs/M4_DETAIL_MAP_AND_MANUAL_REGIONS.md)
- [`docs/M5_APPLICATION_WORKFLOW_AND_PROJECTS.md`](docs/M5_APPLICATION_WORKFLOW_AND_PROJECTS.md)
