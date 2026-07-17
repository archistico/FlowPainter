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

`FlowPainter.Domain` must never depend on Avalonia, SkiaSharp, file dialogs, native image buffers, machine-learning runtimes or model files. `FlowPainter.Application` remains free of Avalonia and SkiaSharp and operates through domain contracts such as `IRgbaPixelSource`.

## Current projects

### FlowPainter.Domain

Contains stable concepts and invariants:

- supported image dimensions;
- normalized geometry;
- detail regions, schema-11 semantic-correction compatibility values and maps;
- M14.1 regional labels, region descriptors, adjacency and hierarchy values;
- generation modes;
- deterministic randomness;
- managed RGBA image values used by pure planning tests;
- relative stroke geometry and immutable versioned `StrokePlan` data;
- normalized geometric primitives and immutable resolution-independent `PrimitivePlan` data;
- immutable `HybridPlan` aggregates with common-dimension and layered-background invariants.

### FlowPainter.Application

Contains orchestration and policies that operate on domain objects:

- generation requests;
- explicit memory estimation;
- `IDetailMapAnalyzer` and deterministic structural analysis;
- historical M8–M13.3 `ISemanticImportanceAnalyzer` implementation retained for compatibility tests and older documentation;
- active M14.7 SLIC regional-structure composition, generalized roles and legacy semantic compatibility adapters;
- region-aware scene-boundary and background composition without automatic class or subject recognition;
- M13.4.4 `AnalysisCoordinator`, detached analysis results, immutable cache identities and transactional adoption;
- M14.1 `IRegionSegmentationAnalyzer`, immutable segmentation request/result/progress contracts and exact resource estimator;
- M14.2 deterministic SLIC clustering, M14.3 connectivity/diagnostics, M14.4 regional descriptors, M14.5 adjacency evidence, M14.6 hierarchy and M14.7 active-pipeline adoption;
- deterministic overlay hit testing, legacy semantic-correction editing and runtime conversion to generalized region roles;
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
- deterministic primitive candidates, mutation, proxy masks and weighted local scoring;
- proxy-space hill-climbing optimization that produces immutable primitive plans;
- deterministic hybrid orchestration, primitive-derived flow-field decoration and refinement policies.

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
- rasterizes ordered primitive plans at preview or final resolution;
- composes primitive, flow and refinement layers while disposing intermediate native images;
- exports primitive plans as deterministic SVG documents;
- returns owned `SkiaImage` results.

Stroke rendering delegates material deposition to the validated SolidRound, SoftRound, Flat and Bristle strategies. Primitive and hybrid renderers retain instance-service semantics so they can later be replaced or decorated without changing the composition root.

### FlowPainter.App

Avalonia desktop composition root and presentation layer. The current workflow is:

```text
Current M14.7 workflow:

Open image or project
    ↓
Resolve source and create selected-quality proxy
    ↓
Create immutable analysis request and cache key
    ↓
Analyze structural detail + deterministic SLIC segmentation
    ↓
Connected labels + descriptors + RAG + hierarchy
    ↓
Convert schema-11 corrections into generalized region roles
    ↓
Compose regional importance and region-aware boundaries
    ↓
Compose automatic/manual detail and background suppression
    ↓
Adopt the complete detached result transactionally
    ↓
Generate Flow / Primitive / Hybrid plans from the shared result
    ↓
Render, persist and export

M14.8 exposes and persists the SLIC, merge, overlay and generalized role controls.
```

The window owns source, proxy, rendered and Avalonia preview resources. Replacement is transactional: new previews are constructed before old resources are released, and failed operations retain or dispose ownership explicitly.

Durable editing state is represented by `FlowPainterWorkspace`; project persistence, region mutation and recent-item policies live in Application. The window remains the native-resource composition root and file-dialog boundary. M5 avoids a framework-heavy rewrite and does not place disposable SkiaSharp/Avalonia resources into view models.

## Project and workspace boundary

`FlowPainterProject` is an immutable schema-versioned snapshot containing source reference, seed, selected generative mode, flow/brush/semantic/primitive/hybrid settings, preview quality, final-output settings and ordered manual regions. `ProjectPathResolver` writes a relative source reference whenever possible and resolves it against the project file on load.

`FlowPainterWorkspace` owns mutable logical session state but no native resources. `DetailRegionEditor` provides stable identifiers and atomic edit/reorder/delete operations. Exposed collections are read-only views; callers cannot mutate internal lists by casting the public contract.

`ProjectSessionController` is the testable M13.4.1 boundary for destructive navigation. It observes workspace dirty state, accepts presentation edit notifications and resolves Save / Discard / Cancel through callbacks supplied by the desktop shell. It does not open dialogs, write files or own native resources. Avalonia tracks persisted control changes, while viewport, overlay and selection-only state remain transient. Project-control population is suppressed during final transactional adoption so a loaded project remains clean.

Preset and project responsibilities remain distinct:

```text
Preset  = reusable algorithm parameters
Project = source + seed + parameters + preview quality + final output + image-specific regions
```

Recent project/preset paths are persisted separately as non-critical UI state. Failure to restore or write recent items must never prevent image or project operations.

## Analysis lifecycle and transactional adoption

M13.4.4 and ADR-0020 move the complete derived-map lifecycle into `FlowPainter.Application.Analysis`. `AnalysisCoordinator` owns execution order, progress translation, cache identity, generation ordering, reduced recomposition and publication eligibility. It depends only on Application/domain analyzer and composer contracts. It does not own Avalonia controls, Skia images, file paths or workspace mutation.

```text
AnalysisRequest
├── IRgbaPixelSource proxy
├── structural / SLIC / merge / boundary / background settings
├── copied manual regions and schema-11 correction compatibility values
└── AnalysisCacheKey
        ↓
AnalysisCoordinator.AnalyzeAsync / RecomposeAsync
        ↓
PendingAnalysis
├── monotonic generation
├── immutable cache key
└── detached AnalysisResult
        ↓
caller builds temporary native overlay and rechecks active key
        ↓
AnalysisCoordinator.TryAdopt
        ↓
UI replacement callback succeeds
        ↓
CurrentKey + CurrentResult are published
```

`AnalysisCacheKey` is value-based and includes a non-empty per-source session identity, proxy dimensions, detail-region revision, semantic-correction revision and an invariant fingerprint of every setting that affects derived analysis output. M14.7 includes SLIC and merge settings and deliberately excludes retired semantic-analysis settings. Renderer-only settings are excluded. Candidate image/project loads use a temporary source identity and revision pair; after the workspace is committed, `TryRetagCurrent` moves the accepted result to the committed revisions without recomputation.

Every full analysis or recomposition increments a coordinator generation. `TryAdopt` rejects a pending result unless it belongs to the latest requested generation and matches the caller's freshly rebuilt expected key. Starting, cancelling or failing a newer run does not erase the last successfully adopted result. `Invalidate` deliberately clears it and makes all outstanding generations stale.

The adoption callback is synchronous and intentionally limited to short native/UI ownership swaps. The coordinator publishes its cache only after that callback returns successfully. Overlay creation, source/proxy replacement and Avalonia bitmap construction therefore cannot leave a cache entry claiming that a failed UI adoption succeeded.

Manual detail-region edits use `RecomposeAsync` when structural, regional-role and boundary inputs are unchanged. That path reuses the accepted structural map, SLIC result, regional maps, compatibility envelope, boundary result and automatic-detail map, then rebuilds only manual composition and background suppression. Role-correction, SLIC, merge, structural or boundary-setting changes require the full pipeline.

`MainWindow` remains the desktop composition root: it reads controls, owns disposable native resources, renders temporary overlays and supplies the final adoption callback. It no longer invokes the three analyzers or the map composers directly. M14.7 extends this lifecycle with accepted SLIC labels, descriptors, adjacency, hierarchy and regional-role maps.

## Analysis fields

FlowPainter separates four control structures in the active M14.7 architecture:

```text
Region field      → complete pixel partition, adjacency and hierarchy
Detail field      → local amount and scale of rendered information
Importance field  → allocation of strokes, primitives and optimization effort
Boundary field    → protected separations and tangent direction
```

Detail and importance remain normalized scalar values. The region field is a compact integer label map plus descriptor/graph tables. M11 provides scalar boundary evidence and a vector-valued `BoundaryDirectionField`; M14.7 now feeds the analyzer with shared-boundary evidence derived from the regional graph. Keeping these structures separate allows the UI to diagnose whether a weak result comes from segmentation, hierarchy, boundary direction or rendering policy.

### Structural importance and active regional analysis

M4's `ImageDetailAnalyzer` calculates luminance-edge and local RGB-contrast signals. M8's `ISemanticImportanceAnalyzer` remains a validated historical subsystem, but M14.7 no longer injects or calls it from `AnalysisCoordinator`. `RegionalStructureAnalysisComposer` now combines structural detail, fine-region descriptors, RAG boundary strength, hierarchy specificity and generalized manual roles. `RegionalSemanticCompatibilityAdapter` exposes those maps only to legacy desktop/boundary consumers; it performs no object classification or primary-subject ranking.

Current manual-region composition remains:

```text
Increase: value + strength × (1 - value)
Reduce:   value × (1 - strength)
```

This keeps values in `[0, 1]` and makes repeated adjustments deterministic.

### SLIC regional segmentation and active implementation

M14 introduces a local, deterministic and model-free regional representation across Domain and Application:

```text
IRgbaPixelSource proxy
        + RegionSegmentationSettings
        ↓
IRegionSegmentationAnalyzer
        ↓
RegionSegmentationResult
├── RegionLabelMap (`UInt16` or `UInt32`)
├── ImageRegion descriptors
├── RegionAdjacencyGraph
├── hierarchy levels
└── diagnostics
```

The first implementation is SLIC in CIELAB + image-coordinate space. M14.2 samples the proxy into a reusable colour buffer, composites transparent pixels against white, optionally applies a separable Gaussian pre-blur, converts normalized sRGB to D65 CIELAB, initializes a regular grid at low-gradient locations and performs deterministic local assignment/update iterations. Spatial convergence is measured in proxy pixels and all equal-cost decisions preserve stable cluster order. No random state is involved.

M14.1 places compact `RegionLabelMap`, `ImageRegion`, `RegionAdjacencyGraph` and `RegionHierarchy` values in Domain. Request, settings, result, diagnostics, progress and estimation policies belong in Application because they consume `IRgbaPixelSource` and coordinate use cases. M14.2 implements `SlicRegionSegmentationAnalyzer` in Application, keeping the algorithm independent of SkiaSharp and Avalonia. Imaging.Skia continues to own decode and proxy generation only. No SAM, ONNX, Python, model checkpoint or GPU dependency is permitted by the approved segmentation decision.

M14.3 normalizes the provisional M14.2 assignments into four-neighbour connected regions. `RegionConnectivityNormalizer` splits disconnected raw labels, merges undersized components only into adjacent regions using stable shared-boundary/size/id priority, compacts labels by first appearance and reports all repairs. The detached result has valid topology, basic area/bounds/centroid values, an identity hierarchy and an empty adjacency graph at that stage; M14.7 now adopts the completed M14.6 result in the active pipeline. `SegmentationDiagnosticRenderer` can create mean-colour previews and boundary overlays without Avalonia or SkiaSharp.

M14.4 replaces the basic geometry builder with `RegionDescriptorCalculator`. The calculator rescans the original proxy rather than the optionally blurred SLIC working buffer, composites alpha against white, converts to D65 CIELAB and publishes immutable per-region geometry, population colour statistics, internal texture energy, edge density and an undirected dominant tangent. Digital perimeter counts exposed four-neighbour pixel edges and compactness uses `4πA / P²`. Internal gradients only use same-region neighbours, leaving cross-region contrast to the M14.5 adjacency stage. Memory remains `O(pixels + regions)`: one global `float` lightness buffer and scalar accumulator arrays, never a full-size mask per region.

M14.5 adds `RegionAdjacencyGraphBuilder`. A single deterministic right/down scan creates one normalized undirected edge per adjacent region pair. Each edge stores exact shared digital-boundary length, local CIELAB ΔE mean/maximum, regional mean-colour and lightness differences, texture-energy difference, doubled-angle tangent continuity, prevailing tangent and a documented continuous boundary-strength score. `RegionSegmentationResult` verifies graph completeness and exact shared-boundary lengths against the label map. Per-region neighbour lists are immutable and ordered by neighbouring identifier for M14.6 merge traversal.

M14.6 adds `RegionHierarchyBuilder`, `RegionMergeSettings` and `RegionMergeCostModel`. It starts from the immutable fine graph and produces intermediate and broad-mass levels without rewriting fine labels. Candidate parents must be adjacent; aggregate means are pixel-weighted; perimeter removes twice the shared digital boundary; cost combines colour, texture, mean boundary strength, merged compactness and resulting area. Aggregate boundaries retain weighted mean strength and maximum contributing fine-edge strength, so a protected contour cannot disappear when weak neighbours merge. Versioned priority-queue entries are invalidated whenever an endpoint changes, and affected costs are recomputed before publication. Compact parent identifiers follow first fine-region appearance, while each hierarchy level exposes both fine-to-parent and parent-to-fine-child traversal.

M14.7 adds `RegionRole`, `RegionRoleOverride`, `LegacySemanticCorrectionAdapter`, `RegionalStructureAnalysisComposer` and `RegionalDetailMapComposer`. `AnalysisCoordinator` now runs structural analysis, SLIC, role composition, region-aware boundary analysis, shared detail composition and background suppression as one detached transaction. Existing schema-11 corrections are converted at runtime to `Focal`, `Subject`, `Background` or `Ignore`; legacy semantic settings remain readable but do not participate in the cache fingerprint. `AnalysisResult` carries both the immutable regional result and a read-only semantic compatibility envelope for presentation code pending M14.8.

For large sources, the global partition is computed on an aspect-ratio-preserving proxy. Labels are projected to source coordinates and M17 may refine only border bands or selected complex regions. A full-resolution floating-point segmentation field is not allowed.

### Scene-boundary analysis

M11 introduced `ISceneBoundaryAnalyzer`. In M14.7, `RegionalSceneBoundaryAnalyzerAdapter` converts the active regional maps into a read-only compatibility envelope and delegates to the validated `HeuristicSceneBoundaryAnalyzer`, which produces:

```text
SceneBoundaryAnalysisResult
├── EdgeStrengthMap
├── EdgeImportanceMap
├── SubjectBoundaryMap
├── InternalStructureMap
├── TextureEdgeMap
├── BackgroundConfidenceMap
├── UncertaintyMap
└── BoundaryDirectionField
```

The implementation combines luminance and colour gradients at fine/coarse scales, contour continuity and the regional shared-boundary/protection evidence exposed through the compatibility envelope. The direction field stores the **tangent** of the estimated contour, not the gradient normal, because future strokes must follow rather than cross important edges.

M11 remains a diagnostic analyzer boundary. M12 consumes its immutable result through a separate `BoundaryGuidanceField`; the analyzer itself still does not decide whether a stroke aligns, deflects or terminates. This staged design prevents an analyzer mistake from being confused with a planning-policy mistake.

### Background confidence and uncertainty

Background confidence is not defined as `1 - subject`. In the active path it combines low regional importance, low saliency, distance from protected/focal areas and structural freedom. A configurable protection radius lowers background confidence around subjects and silhouettes. The uncertainty map identifies pixels for which neither subject nor background confidence is sufficiently strong.

M12 uses uncertainty as boundary protection. M13 consumes background confidence and uncertainty through a signed suppression policy while keeping the M11 analyzer immutable. M14.7 now adapts this stage to regional descriptors, shared-boundary strength and manual region roles without subject-class recognition.

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


## Planned staged rendering after M15.2

M15.2 changes local geometry inside one accepted stroke plan; it does not yet define a painterly pass order. M15.3 will add a separate orchestration layer rather than embedding pass decisions inside `SkiaStrokePlanRenderer`.

The planned boundary is:

```text
Regional/detail/boundary analysis
        ↓
FlowPassAllocator
        ↓
StagedFlowPlan
├── BroadMass StrokePlan
├── RegionalStructure StrokePlan
├── BoundaryReinforcement StrokePlan
└── FineDetail StrokePlan
        ↓
Staged renderer / high-resolution exporter
```

`FlowPainterPlanner` remains responsible for deterministic stroke geometry within a pass. The allocator owns budget conservation, stage-specific settings, seed derivation and ordering. Rendering remains a projection of immutable plans and must not infer artistic roles from pixels.

M15.4 will apply the same separation to primitives: a coarse-to-fine allocator composes ordered `PrimitivePlan` stages while `PrimitivePlanOptimizer` continues to optimize one explicitly bounded stage. M15.5 then introduces an engine-neutral artistic-allocation result so Flow, Primitive and Hybrid do not independently reinterpret the same SLIC hierarchy.

Key constraints:

- planning roles may be discrete, but their spatial influence must remain continuous;
- pass budgets must be fixed before expensive allocation begins;
- each pass has a deterministic seed derived from the project seed and stable pass identifier;
- preview, final raster and SVG consume the same accepted plan objects;
- compatibility paths remain available until the staged paths have passed regression and manual visual validation;
- advanced editing in M16 persists user intent, not full derived label maps;
- high-resolution refinement in M17 is restricted to measured boundary bands or invalidated regions.

## Resolution independence

Every region, path and future primitive is stored in normalized source-image coordinates. `UniformImageViewport` maps between the letterboxed Avalonia viewport and normalized image space. Preview and final rendering remain projections of the same data.

The project must not use preview pixels as permanent geometry.

## Determinism

`System.Random` is not accepted as a persistence contract. `DeterministicRandom` uses SplitMix64 and has a golden-sequence test. Changing its output requires an explicit versioning decision because saved projects may depend on it.

`CoherentNoise` is the production flow-field default and has numerical golden tests. Global rotation is an explicit field transform. Detail-map weighted sampling consumes the same versioned deterministic source.

## Memory and work admission policy

The 10,000 × 10,000 decoded-size limit is enforced by `ImageSize` and metadata validation in `SkiaImageLoader`. Full-size floating-point analysis maps are prohibited; analysis uses the selected proxy resolution. `SkiaImageLoader` also bounds encoded input to 256 MiB by default. Seekable sources are validated and read directly into one managed array; non-seekable sources are copied through a pooled buffer with cumulative limit checks and cancellation.

M13.4.2 and ADR-0018 introduce `WorkloadBudgetPolicy` as the shared Application-level admission boundary. The supported estimated peak working set is 2 GiB. The policy is invoked before proxy analysis, before final export and inside each generative planner, so future callers cannot bypass the desktop checks.

`AnalysisMemoryEstimator` includes decoded source RGBA, proxy RGBA, a conservative 160-byte-per-proxy-pixel reserve for current analysis fields and the exact M14.1/M14.2 `RegionSegmentationEstimator` peak. The segmentation estimate selects two- or four-byte labels from the projected region count and includes Lab, distance, assignment, optional smoothing, connectivity flood-fill/adjacency and cluster-state buffers plus deterministic assignment-work estimates.

`FinalRenderMemoryEstimator` is mode-aware. Flow and Primitive conservatively represent three output-sized buffers: render surface, copied bitmap and encoding reserve. Hybrid represents four output-sized buffers at its layered render peak: retained primitive and flow layers, refinement surface and copied result. The estimate also includes source, proxy, preview, optional overlay and analysis/SLIC reserves. The desktop reports estimated MiB, output-buffer count, risk and whether policy allows the export. An approved preview plan takes precedence over the currently selected combo-box mode, matching final-render behaviour.

`GenerationWorkEstimator` bounds non-memory complexity:

- Flow uses `stroke count × segment count`;
- Primitive combines candidate and maximum detail-scaled mutation attempts with an area-proportional pixel-evaluation estimate;
- Hybrid scales primitive, base-flow and refinement counts with the same rounding rules as `HybridPlanComposer`.

The current policy allows at most 25,000,000 flow-segment steps, 5,000,000 primitive score attempts and 3,000,000,000 primitive pixel evaluations. Requests over a limit fail before large loops or plan collections are created. These values are support-policy limits, not persistent artistic settings, and therefore do not alter project or preset schemas.

Native Skia allocation, codec scratch space and exact operating-system working-set behaviour cannot be inferred perfectly. Estimates are intentionally conservative admission controls; measured profiling and Hybrid layer-lifetime reduction remain M17 work.

## Atomic local persistence

M13.4.3 and ADR-0019 introduce `AtomicFileWriter` in `FlowPainter.Application.Persistence` as the only production path for local destination commits. Project, preset, preview PNG, final raster, SVG and recent-item writes serialize or encode into a unique temporary sibling file. The temporary stream is flushed, closed and then moved over the destination in the same directory.

The previous destination is never opened or truncated before commit. A failed serializer, encoder, cancellation, flush or move therefore leaves an existing file unchanged; failed creation of a new file leaves no published destination. Temporary deletion is best-effort and never replaces the primary exception.

Serializers and encoders retain generic `Stream` contracts and remain independent of filesystem policy. Avalonia selects the local destination path, while the Application layer owns the transactional write boundary. Non-local storage requires an explicit future provider-specific transaction rather than a silent direct-stream fallback.

## Engine boundaries

### Flow engine

```text
IDetailMapAnalyzer + IRegionSegmentationAnalyzer + IRegionalSceneBoundaryAnalyzer
            ↓
Structural importance + SLIC regional structure + scene-boundary evidence
            ↓
DetailRegion[] + BoundaryPaintingSettings
            ↓
DetailMap + BoundaryGuidanceField
            ↓
IFlowField → FlowPainterPlanner → StrokePlan → IBrushRenderer
```

Current implementation:

```text
ImageDetailAnalyzer + SLIC regional analysis + region-aware scene-boundary analysis
            ↓
DetailMapComposer + BoundaryGuidanceField.Create
            ↓
FlowPainterSettings + IFlowField
            ↓
FlowPainterPlanner
├── free flow away from boundaries
├── tangent alignment near boundaries
├── crossing deflection / hard stop
└── corner shortening
            ↓
SkiaStrokePlanRenderer → ISkiaBrushRenderer → SkiaImage / PNG/JPEG
```

Boundary-aware plans use `flow-field-boundary-v1`. When `BoundaryPaintingSettings.Enabled` is false, the planner delegates to the unchanged `flow-field-detail-v1` path without consuming additional random values.

### Primitive engine

```text
Source proxy + DetailMap
        ↓
IPrimitiveCandidateFactory + IPrimitiveMutator
        ↓
IPrimitiveMaskRasterizer + IPrimitiveScorer
        ↓
PrimitivePlanOptimizer
        ↓
PrimitivePlan
    ├── SkiaPrimitivePlanRenderer → PNG/JPEG
    └── SvgPrimitivePlanExporter  → SVG
```

The optimizer starts from the source average colour and evaluates deterministic candidates only on their proxy-space masks. The scorer analytically estimates a colour for the configured opacity and measures weighted local squared-error reduction. Accepted primitive geometry is normalized; preview, high-resolution raster output and SVG therefore reuse the same ordered plan. Detail values influence candidate placement, local size, error weighting and mutation budget.

### Hybrid engine

```text
Source proxy + composed DetailMap
            ↓
PrimitivePlanOptimizer
            ↓
PrimitivePlan
            ↓
PrimitiveInfluenceFlowFieldFactory
            ↓
FlowPainterPlanner → base StrokePlan
            ↓
FlowPainterPlanner → refinement StrokePlan
            ↓
HybridPlan
            ↓
SkiaHybridPlanRenderer → PNG/JPEG
```

`HybridPlan` is a pure Domain aggregate. All three layers share proxy dimensions and the two stroke layers use `SourceImage` background mode so rasterization can feed each completed layer into the next pass. `HybridPlanComposer` remains in Application and derives three deterministic seeds from the project seed.

`PrimitiveInfluenceFlowField` decorates the selected base field. It combines the base vector with distance-weighted primitive directions using AxisAlignment, rotated-envelope BoundaryTangent, Vortex or Mixed strategies. The primitive plan remains immutable. M12 reuses the same `FlowPainterPlanner` with an optional precomputed `BoundaryGuidanceField`, so primitive-derived deformation and scene-boundary guidance compose in both hybrid stroke layers.

`SkiaHybridPlanRenderer` owns the intermediate primitive and flow images. It returns only the final refinement image, transferring that ownership to the caller. Preview and high-resolution export reuse the same `HybridPlan` and brush settings. Hybrid settings introduced in project schema 6 remain persisted in current schema 13; generated plans are never persisted.

## Boundary-aware planner boundary

M12 keeps scene interpretation separate from stroke policy:

```text
SceneBoundaryAnalysisResult
        + BoundaryPaintingSettings
        ↓
BoundaryGuidanceField
├── tangent
├── influence
├── hardness
├── subject-boundary confidence
└── corner strength
        ↓
FlowPainterPlanner
├── blend base direction with nearest tangent orientation
├── sample crossing risk along a proposed segment
├── deflect or terminate at hard boundaries
└── shorten near corners and junctions
        ↓
flow-field-boundary-v1 StrokePlan
```

Responsibilities remain distinct:

- the analyzer estimates edge evidence, classification and tangent;
- `BoundaryGuidanceField` converts that evidence into a smooth, radius-bounded policy;
- the planner decides whether to continue, deflect, shorten or terminate;
- colour remains sampled from the stroke origin, preserving its starting side;
- the renderer only rasterizes the approved plan.

`BoundaryGuidanceField` is computed once per planning operation. `HybridPlanComposer` reuses the same instance for base-flow and refinement layers. Contour reinforcement creates a new detail map and never mutates the composed source map.

The validated M10/M11 path remains exact when boundary-aware painting is disabled. M12 does not yet create independent internal/external contour layers; this can be added later without changing the M11 analyzer contract.

## Signed background-suppression boundary

M13 derives a signed artistic-detail field from positive importance, background confidence, uncertainty and manual overrides. Suppression changes planning budgets and mark scale; it does not mutate M11 maps. Priority is explicit:

```text
manual protection
> critical detail
> focal area
> subject / protected silhouette
> uncertainty
> confident background
```

A configurable detail floor and transition band prevent empty areas or abrupt technical seams.

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


## Primitive-plan persistence and output boundary

Project schema 5 persists the selected engine and `PrimitiveGenerationSettings`, but not generated primitive plans. A plan is derived output, retained only for the approved in-memory preview and invalidated whenever source, proxy, detail composition, seed or primitive settings change. Schemas 1–4 load as Flow painting with explicit M9 primitive defaults.

The allowed-kind flag set supports every non-empty combination of triangle, rectangle, rotated rectangle, circle and ellipse. The desktop selector exposes all combinations so a project round trip cannot silently replace a mixed set.

```text
Proxy + detail map + seed + primitive settings
                ↓
          PrimitivePlan
           ├── preview raster
           ├── final PNG/JPEG raster
           └── SVG vector document
```


## Hybrid-plan persistence and invalidation

Hybrid output is derived state. The desktop composition root retains the approved `HybridPlan` only while source, proxy, composed detail map, seed, flow settings, primitive settings and hybrid settings remain unchanged. Any relevant edit invalidates the cached preview plan.

```text
Project schema 12 settings
(including schema-6 hybrid and schema-12 regional settings)
        ↓
Reproducible HybridPlan
        ├── proxy preview
        └── final PNG/JPEG rasterization
```

SVG remains restricted to pure primitive mode because procedural brush layers do not yet have a vector material representation.


## M13.2 soft manual-region composition

Manual rectangles remain normalized Domain values, but their raster-space influence is computed in Application by `DetailMapComposer`:

```text
DetailRegion bounds + strength + intent
        + RegionTransitionWidth
        ↓
Smooth distance influence at proxy resolution
        ├── full-strength core
        ├── 50% influence on geometric border
        └── zero beyond exterior feather radius
        ↓
Immutable manually composed DetailMap
```

The transition radius is a fraction of the shorter map dimension, so portrait and landscape proxies receive comparable feathering. Inside and outside use a cubic SmoothStep. Euclidean outside distance rounds corner falloff naturally. Same-intent regions merge by maximum local influence; opposing intent groups are applied according to the latest region order.

The composed-map cache includes `RegionTransitionWidth`. Editing it invalidates the effective detail map and any approved Flow, Primitive or Hybrid plan. Renderers remain unaware of manual regions and consume only immutable plans/maps.


## M13.3 historical semantic-correction pipeline and M14.7 compatibility

M13.3 originally represented automatic semantic evidence and user decisions separately:

```text
ISemanticImportanceAnalyzer
        ↓
SemanticAnalysisResult (automatic, immutable)
        + SemanticCorrectionRegion[]
        + RegionTransitionWidth
        ↓
SemanticCorrectionComposer
        ├── corrected saliency map
        ├── corrected subject map
        ├── corrected silhouette map
        ├── corrected focal map
        └── corrected importance map
        ↓
ISceneBoundaryAnalyzer → detail/background composition → planners
```

`SemanticCorrectionRegionEditor` owns stable project-local identifiers and the one-primary-subject invariant. `OverlayRegionHitTester` contains pure selection/cycling policy so display hit testing can be tested without Avalonia. `MainWindow` remains responsible only for mapping viewport clicks to normalized coordinates, synchronizing list selection and triggering recomposition.

Corrections never mutate or remove the analyzer's `SemanticRegion` list. The desktop renders automatic evidence and manual corrections as different overlays. Correction kinds use explicit precedence: ordinary subject promotion, background/ignore suppression, then primary-subject promotion. Same-kind overlap uses maximum local influence.

Project schema 11 introduced semantic corrections; schema-10 and earlier documents receive an empty collection. M14.7 converts those values at runtime into generalized `RegionRoleOverride` values. M14.8 advances the project schema to 12 and persists those generalized overrides directly while retaining the legacy values for round-trip compatibility. Preset schema 9 persists only reusable SLIC/merge settings because role overrides are image-specific.

## M13 signed background-suppression pipeline

```text
Automatic structural detail ─┐
Manual composed detail ──────┼─→ BackgroundSuppressionComposer
Semantic subject/importance ─┤          ├─ ProtectionMap [0,1]
Silhouette + uncertainty ────┤          ├─ SuppressionMap [0,1]
Background confidence ───────┘          ├─ EffectiveDetailMap [0,1]
                                        └─ ArtisticDetailField [-1,1]
```

The four outputs have distinct responsibilities. `ProtectionMap` and `SuppressionMap` remain inspectable diagnostics. `EffectiveDetailMap` is compatible with existing primitive/error/detail consumers. `ArtisticDetailField` preserves the sign needed by the flow planner to distinguish positive detail from negative simplification.

`FlowPainterPlanner` uses `ArtisticDetailPointSampler` to lower start probability in negative areas. Local suppression then interpolates stroke length, width, segment count and curve tolerance and applies deterministic colour simplification. The disabled path does not instantiate the signed sampler and therefore preserves the validated M12 random sequence exactly.

Primitive optimization receives the effective normalized map rather than a new primitive-specific policy. Hybrid planning passes the same immutable suppression result to both stroke layers. No renderer interprets scene importance; renderers consume already-approved immutable plans.

## M14 active regional pipeline

M14.7 activates the approved replacement pipeline:

```text
Structural proxy ───────────────────────────────┐
SLIC label map → descriptors → adjacency graph ├─→ regional hierarchy
Manual region-role overrides ──────────────────┘          ↓
                                             boundary strength / tangent / distance
                                                          ↓
                              artistic importance + background suppression + detail
                                                          ↓
                                      Flow / Primitive / Hybrid immutable plans
```

Migration rules:

- M8–M13.3 files remain historical documentation of implemented behaviour;
- schema-1 through schema-11 projects remain readable and migrate into current schema 13 values;
- manual user decisions are preserved, even when their internal representation changes;
- automatic semantic regions are derived legacy data and need not be persisted into the new pipeline;
- renderers remain unaware of SLIC and consume only approved immutable plans;
- M14.7 changes the active path only after contracts, topology, descriptors, graph and hierarchy received independent validation through M14.6.


## M14.8 regional desktop and persistence boundary

M14.8 keeps derived segmentation data out of project files:

```text
Project schema 13 / preset schema 10
        ├── RegionSegmentationSettings
        ├── RegionMergeSettings
        └── project-only RegionRoleOverride[]
                    ↓ deterministic reanalysis
RegionLabelMap + descriptors + RAG + hierarchy
                    ↓
diagnostic overlays / active generative plans
```

`MainWindow` exposes SLIC settings, a simplified merge-intensity mapper, hierarchy selection and diagnostic overlays. Overlay mode and inspected-region selection are presentation state and do not mark the project dirty. `SegmentationDiagnosticRenderer` remains in Application and produces immutable RGBA diagnostics; `SkiaImageFactory` is the Imaging.Skia ownership boundary used to present those values in Avalonia.

Obsolete semantic tuning values are hidden but still populated from old documents so a load/save cycle does not discard historical data. Compatibility role lists and commands consume the active regional compatibility envelope; no semantic analyzer or model is reintroduced.

Project schema 13 persists generalized role overrides and the high-detail stroke policy, while retaining deterministic compatibility with schema-11 corrections and schema-12 regional settings. Preset schema 10 stores reusable SLIC, merge and stroke-policy settings only. Labels, descriptors, adjacency and hierarchy are derived, potentially large values and are intentionally recomputed rather than serialized.


## M15.1 regional boundary field

M15.1 converts the discrete SLIC/RAG structure into a continuous field used during stroke planning. `RegionalBoundaryField` scans exact fine-label adjacencies, reads each corresponding `RegionAdjacency.BoundaryStrength` and `PrevailingTangentRadians`, assigns side-aware normals and propagates the nearest boundary through a deterministic bounded eight-neighbour search. Distance is primary; equal-distance candidates prefer higher strength and then stable seed order.

`RegionalBoundaryFieldSettings` is derived from existing boundary-painting controls, so M15.1 requires no schema change. Weak boundaries receive a broad SmoothStep-derived transition, while strong barriers keep higher peak strength in a narrower band. Samples outside the bounded range remain empty.

`BoundaryGuidanceField` first builds and propagates the validated M11–M12 scene evidence, then blends regional strength, distance, normal, tangent, hardness and contour reinforcement. Regional tangents participate in corner detection. Existing scene-only overloads remain unchanged.

M15.1 Flow plans publish the regional boundary planner identity and Hybrid reuses one combined field for both stroke layers. M15.2 advances all detail-aware planner identities to v2 and consumes the same samples to vary local stroke geometry.

## M15.2 high-detail local stroke policy

`HighDetailStrokePolicy` keeps geometry and boundary response separate. `LocalStrokeGeometry` continuously interpolates length, width, segment count and curve freedom from background to detailed settings. The planner combines those values with background suppression, clamps local segments to the supported maximum and retains the no-detail v1 path unchanged.

At every boundary-aware step, continuous detail and scene/regional evidence add bounded tangent-alignment and crossing-resistance boosts. Zero detail reproduces the validated M12/M15.1 response; no binary region role is consulted. Project schema 13 and preset schema 10 persist the six new reusable controls, while older documents receive deterministic defaults. Workload estimation admits the maximum possible local segment count before allocation.
