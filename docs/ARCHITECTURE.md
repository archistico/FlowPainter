# FlowPainter architecture

## Dependency rule

```text
FlowPainter.App
    ├── FlowPainter.Application
    ├── FlowPainter.Imaging.Skia
    └── FlowPainter.Rendering.Skia

FlowPainter.Rendering.Skia
    ├── FlowPainter.Imaging.Skia
    └── FlowPainter.Domain

FlowPainter.Imaging.Skia
    └── FlowPainter.Domain

FlowPainter.Application
    └── FlowPainter.Domain
```

`FlowPainter.Domain` must never depend on Avalonia, SkiaSharp, file dialogs, native image buffers or machine-learning runtimes. `FlowPainter.Application` remains free of Avalonia and SkiaSharp and operates through domain contracts such as `IRgbaPixelSource`.

## Current projects

### FlowPainter.Domain

Contains stable concepts and invariants:

- supported image dimensions;
- normalized geometry;
- detail regions and maps;
- generation modes;
- deterministic randomness;
- managed RGBA image values used by pure planning tests;
- relative stroke geometry and immutable versioned `StrokePlan` data.

### FlowPainter.Application

Contains orchestration and policies that operate on domain objects:

- generation requests;
- explicit memory estimation;
- `IDetailMapAnalyzer` and deterministic structural analysis;
- `ISemanticImportanceAnalyzer`, semantic maps and normalized semantic regions;
- provider-independent composition of structural, subject, silhouette and focal importance;
- composition of automatic/manual detail information;
- normalized viewport coordinate mapping;
- characterized legacy density and planning behaviour;
- immutable production flow-field and stroke-planning settings;
- internal coherent-noise and comparison field factories;
- compatible `flow-field-v1` planning;
- detail-aware `flow-field-detail-v1` planning;
- versioned JSON presets with schema migration;
- versioned project documents and portable source references;
- application-level workspace, region editing, operation and validation state;
- bounded recent-project and recent-preset state;
- future semantic-analysis and primitive use cases.

### FlowPainter.Imaging.Skia

Owns native image operations:

- metadata inspection and image decoding;
- the disposable `SkiaImage` pixel-source adapter;
- aspect-ratio-preserving proxy generation;
- PNG and JPEG encoding;
- progress and cancellation for image operations.

The loader validates decoded dimensions before allocating the target RGBA bitmap. The project does not expose any Skia type through Domain or Application contracts.

The loader, proxy generator and PNG encoder deliberately retain instance-service semantics. The Avalonia composition root owns these services; later milestones may inject decorators, caching, diagnostics or alternative implementations without changing consumers. `CA1822` is suppressed only on those service entry points, not disabled project-wide.

### FlowPainter.Rendering.Skia

Rasterizes plans and diagnostic overlays:

- creates the requested output surface;
- draws the source or transparent background;
- projects normalized stroke coordinates to output pixels;
- scales stroke width from the plan reference dimension;
- clips legacy paths through the canvas;
- renders a proxy-resolution detail heat map;
- returns owned `SkiaImage` results.

The compatible renderer intentionally draws solid antialiased paths. Brush stamping and material simulation belong to M7. Renderers retain instance-service semantics so they can later be replaced or decorated without changing the composition root.

### FlowPainter.App

Avalonia desktop composition root and presentation layer. M5 provides this workflow:

```text
Open image or project
    ↓
Resolve source and create selected-quality proxy
    ↓
Analyze structural and semantic importance
    ↓
Inspect/promote generic subjects and focal regions
    ↓
Create/edit ordered normalized manual regions
    ↓
Visualize the detail heat map
    ↓
Create a deterministic detail-aware StrokePlan
    ↓
Render and retain immutable preview StrokePlan
    ↓
Save project, export preview or rasterize final output
```

The window owns source, proxy, rendered and Avalonia preview resources. Replacement is transactional: new previews are constructed before old resources are released, and failed operations retain or dispose ownership explicitly.

Durable editing state is represented by `FlowPainterWorkspace`; project persistence, region mutation and recent-item policies live in Application. The window remains the native-resource composition root and file-dialog boundary. M5 avoids a framework-heavy rewrite and does not place disposable SkiaSharp/Avalonia resources into view models.

## Project and workspace boundary

`FlowPainterProject` is an immutable schema-versioned snapshot containing source reference, seed, settings, preview quality, final-output settings and ordered manual regions. `ProjectPathResolver` writes a relative source reference whenever possible and resolves it against the project file on load.

`FlowPainterWorkspace` owns mutable logical session state but no native resources. `DetailRegionEditor` provides stable identifiers and atomic edit/reorder/delete operations. Exposed collections are read-only views; callers cannot mutate internal lists by casting the public contract.

Preset and project responsibilities remain distinct:

```text
Preset  = reusable algorithm parameters
Project = source + seed + parameters + preview quality + final output + image-specific regions
```

Recent project/preset paths are persisted separately as non-critical UI state. Failure to restore or write recent items must never prevent image or project operations.

## Shared importance map

The importance map is not tied to a rendering algorithm. Automatic analyzers produce normalized values, manual regions adjust them, and both generative engines consume the result.

Current composition formula for a selected region:

```text
Increase: value + strength × (1 - value)
Reduce:   value × (1 - strength)
```

This keeps values in `[0, 1]` and makes repeated adjustments deterministic.

M4's `ImageDetailAnalyzer` calculates edge and local RGB-contrast signals. M8 adds `ISemanticImportanceAnalyzer`, whose built-in deterministic provider produces generic saliency, subject, silhouette and focal maps. Structural and semantic maps are composed before manual adjustments. Future class-aware or model-backed providers must return the same Application result contract and must not be embedded in the planner.

## Detail-aware stroke policy

When a composed map is supplied, `FlowPainterPlanner` uses a cumulative weighted sampler:

```text
weight = 1 + placementBias × detail
```

The fixed stroke budget is therefore concentrated in important areas without making plan size unpredictable.

Local detail also interpolates:

- background-to-detailed length multipliers;
- background-to-detailed width multipliers.

No-map calls preserve the M3 random sequence and `flow-field-v1` plan version. Detail-map calls produce `flow-field-detail-v1` plans.

## Resolution independence

Every region, path and future primitive is stored in normalized source-image coordinates. `UniformImageViewport` maps between the letterboxed Avalonia viewport and normalized image space. Preview and final rendering remain projections of the same data.

The project must not use preview pixels as permanent geometry.

## Determinism

`System.Random` is not accepted as a persistence contract. `DeterministicRandom` uses SplitMix64 and has a golden-sequence test. Changing its output requires an explicit versioning decision because saved projects may depend on it.

`CoherentNoise` is the production flow-field default and has numerical golden tests. Global rotation is an explicit field transform. Detail-map weighted sampling consumes the same versioned deterministic source.

## High-resolution memory policy

The 10,000 × 10,000 limit is enforced by `ImageSize` and metadata validation in `SkiaImageLoader`. Full-size floating-point analysis maps are prohibited. M4 analysis and heat-map rendering use the maximum-512-pixel proxy.

`FinalRenderMemoryEstimator` reports source, proxy, preview, overlay and two known final-output RGBA buffers. Native overhead, encoded data, Avalonia copies and codec scratch memory remain additional costs. M6 displays the estimate and a risk band before final export.

## Engine boundaries

### Flow engine

```text
IDetailMapAnalyzer + ISemanticImportanceAnalyzer
            ↓
Structural + semantic importance
            ↓
        DetailRegion[]
            ↓
        DetailMap
            ↓
IFlowField → FlowPainterPlanner → StrokePlan → IBrushRenderer
```

Current implementation:

```text
ImageDetailAnalyzer + DetailMapComposer
            ↓
FlowPainterSettings + IFlowField
            ↓
FlowPainterPlanner
            ↓
SkiaStrokePlanRenderer → ISkiaBrushRenderer → SkiaImage / PNG/JPEG
```

### Primitive engine

```text
DetailMap + IPrimitiveFactory / IPrimitiveMutator
            ↓
IPrimitiveOptimizer → PrimitivePlan → IPrimitiveRenderer
```

### Hybrid engine

```text
DetailMap + PrimitivePlan
            ↓
Field composition / stroke allocation
            ↓
StrokePlan
            ↓
Layered rasterization
```

## Legacy policy

`legacy/original` is a read-only behavioural reference and is not included in the solution. Migration occurs by extracting observable behaviour into the new architecture, never by continuing feature work in the WPF example browser.

## Native ownership

`ADR-0003-SKIA-RESOURCE-OWNERSHIP.md` is implemented by the adapters. Every returned `SkiaImage` transfers disposal responsibility to its caller. Temporary native objects are deterministically disposed. Stroke geometry uses `SKPathBuilder` and detail overlays transfer their bitmap ownership to `SkiaImage` only on successful completion.

## Final rendering boundary

A successful preview retains its immutable `StrokePlan`. Final export reuses that same plan and changes only output dimensions, source-background resolution and encoder format. The plan is invalidated when source, proxy or composed detail information changes.

```text
Proxy analysis → StrokePlan → preview bitmap
                         └──→ final Skia surface → PNG/JPEG
```

`FinalRenderSettings` lives in Application and is persisted in project schema 2. `RasterImageFormat` is a Domain-level format value shared by Application and Imaging without reversing dependencies. `SkiaImageEncoder` owns native PNG/JPEG encoding; JPEG explicitly composes transparent pixels over white.

The original background is considered compatible when fitting it to the plan's proxy maximum dimension reproduces `StrokePlan.SourceSize`. This avoids strict floating-point aspect comparisons while still rejecting unrelated images.


## Synchronized comparison viewport

`SynchronizedImageViewportState` is an Application-layer interaction model shared by the source and rendered-preview controls. It stores a normalized image center and zoom, computes a separate affine transform for each control size, inverse-maps source-selection input, and contains no Avalonia types. The desktop composition root applies the resulting matrices through top-left-origin `MatrixTransform` instances. This keeps navigation synchronized without modifying image data, render plans or project persistence.


## Brush rendering boundary

`BrushSettings` belongs to Domain because it is a serializable, renderer-independent description of material intent. Skia-specific strategies remain inside `FlowPainter.Rendering.Skia`. The desktop composition root stores the brush settings associated with the approved preview and supplies the same value to final rendering.

```text
StrokePlan + BrushSettings
          ↓
SkiaStrokePlanRenderer
          ↓
ISkiaBrushRenderer
          ↓
SolidRound | SoftRound | Flat | Bristle
```

Procedural variation is keyed by plan seed and stroke index. Renderer implementations cannot mutate the plan or consume global randomness.
