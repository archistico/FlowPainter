# FlowPainter

FlowPainter is a .NET 10 and Avalonia application that transforms an input image into a new generative painting rather than applying a conventional image filter.

The planned engine combines:

- flow-guided brush strokes;
- optimized geometric primitives;
- deterministic SLIC regional segmentation with hierarchical merging;
- automatic structural fields and manually edited artistic roles;
- hybrid rendering in which broad masses, protected boundaries and focal regions receive different treatment.

## Current validated baseline: M15.1 (1,049 tests)

**Current milestone:** M15.2 — High-detail local stroke policy — READY FOR VALIDATION

M0 established the solution and domain foundation. M1 characterized the original planner. M2 added local imaging and SkiaSharp rendering. M3 introduced configurable deterministic flow fields. M4 added the first importance-map workflow. M5 added persistent projects and an application-level editing workspace. M6 separates preview from final output and reuses the approved plan for high-resolution PNG/JPEG export. M6.1 adds synchronized zoom and pan for direct source/result comparison. M7 separates stroke geometry from material rendering and adds four deterministic procedural brush families. M8 adds deterministic generic subject, silhouette and focal-area analysis through a replaceable semantic-provider boundary. M9 adds a deterministic geometric-primitive optimizer with resolution-independent raster and SVG output. M10 combines primitive colour masses, primitive-derived flow deformation and a detail-biased brush refinement pass in a third deterministic hybrid mode. M11 added multiscale scene-boundary analysis, important-edge classification, background confidence, uncertainty and a contour-tangent direction field. M12 uses that evidence to align strokes with important contours, resist silhouette crossings, preserve corners and reinforce subject boundaries without drawing a mechanical outline. M13 counterbalances positive importance with explicit background suppression, preserving subjects and uncertain transitions while simplifying confident background. M13.2 softens manual detail-region borders so rectangular editing controls do not leave visible seams in the painting. M13.3 adds direct click selection, deletion and persistent non-destructive semantic corrections for primary subject, subject, background and ignored detections. The 2026-07-16 audit corrections brought the baseline to 755 passing tests. M13.4.1 then added complete presentation dirty tracking and guarded Save / Discard / Cancel navigation; it was validated with all 765 tests passing. M13.4.2 introduced shared memory/work budgets, mode-aware final-render estimates, future SLIC memory reservation, bounded encoded input and planner-level pre-allocation rejection; it was validated with all 782 tests passing. M13.4.3 routes every local project, preset, preview, raster, SVG and recent-item destination through atomic sibling-file commits; it was validated with all 790 tests passing. M13.4.4 extracts analysis execution, immutable cache keys, revision tracking, detached results, recomposition and transactional stale-result rejection into a non-UI `AnalysisCoordinator`; it was validated with all 804 tests passing. M14.1 introduces the immutable regional-segmentation contracts, compact 16-/32-bit label storage, region/adjacency/hierarchy values, diagnostics, progress and exact SLIC memory/work estimation; it was validated with all 863 tests passing. M14.2 added the first deterministic CIELAB SLIC analyzer, optional Gaussian smoothing, local assignment/update iterations, compact result publication, cancellation/progress coverage and the supplied multi-resolution application icon; it was validated with all 882 tests passing. M14.3 repairs disconnected labels, merges undersized adjacent components deterministically, reports region-size distributions and adds mean-colour and boundary diagnostic rendering; it was validated with all 907 tests passing. M14.4 calculates deterministic geometry, CIELAB statistics, texture energy, edge density and dominant regional tangent descriptors; it was validated with all 920 tests passing. M14.5 constructs the complete Region Adjacency Graph with exact shared-boundary lengths, CIELAB gradient statistics, colour/texture differences, continuity, prevailing tangents and normalized boundary strength; it was validated with all 940 tests passing. M14.6 adds deterministic intermediate and broad-mass merging, strong-edge protection, recomputed aggregate costs and traceable parent/child mappings; it was validated with all 964 tests passing. M14.7 made SLIC segmentation, regional descriptors, the RAG, hierarchy and generalized manual roles the active analysis path for Flow, Primitive and Hybrid; it was validated with all 998 tests passing. M14.8 added direct SLIC/merge controls, diagnostic overlays and inspection, project schema 12, preset schema 9 and direct persistence of generalized region-role overrides; it was validated with all 1,024 tests passing. M15.1 derives a continuous regional boundary field from SLIC labels and the RAG and was validated with all 1,049 tests passing. M15.2 adds continuous local segment, curve, tangent-alignment and crossing-resistance policies; 22 new cases bring the expected suite to 1,071 pending local validation.

Implemented and validated through M15.1, plus M15.2 pending validation:

- local PNG, JPEG, WebP and BMP loading;
- hard decoded-size limit of 10,000 × 10,000 RGBA;
- aspect-ratio-preserving Draft, Standard and High analysis proxies (256, 512 and 1,024 px maximum side);
- resolution-independent deterministic `StrokePlan` data;
- resolution-independent immutable `PrimitivePlan` data;
- deterministic hill-climbing reconstruction with any non-empty combination of triangle, rectangle, rotated rectangle, circle and ellipse forms;
- detail-aware primitive placement, size, error weighting and local search budget;
- replaceable primitive candidate, mutation, mask-rasterization and scoring contracts;
- high-resolution primitive rasterization and SVG vector export;
- immutable `HybridPlan` composition with primitive, flow and refinement layers;
- primitive-derived AxisAlignment, BoundaryTangent, Vortex and Mixed vector-field influences;
- configurable hybrid layer budgets, influence falloff and refinement detail/size controls;
- high-resolution hybrid PNG/JPEG rendering from the same plan approved in preview;
- internal coherent-noise flow field with no LibNoiseCore dependency;
- configurable flow, stroke, brush and background parameters;
- built-in and versioned JSON presets;
- structural detail analysis based on luminance edges and local RGB contrast;
- deterministic generic saliency, subject, silhouette and focal-area maps;
- legacy `ISemanticImportanceAnalyzer` provider boundary retained for the validated M8–M13.3 compatibility baseline;
- semantic-region promotion to editable manual focus or critical-detail regions;
- direct click selection and deterministic cycling of overlapping detail, correction and automatic semantic overlays;
- persistent non-destructive semantic corrections for primary subject, subject, background and ignored detections;
- soft semantic-correction borders applied before scene-boundary and detail composition;
- diagnostic overlays for each semantic contribution;
- deterministic multiscale luminance/colour boundary analysis;
- separate important-edge, subject-boundary, internal-structure and texture maps;
- background-confidence and uncertainty maps with silhouette protection;
- normalized tangent direction field and diagnostic direction overlay;
- replaceable `ISceneBoundaryAnalyzer` provider boundary;
- derived `BoundaryGuidanceField` with scene and regional distance, strength, normal, tangent, influence, hardness, contour and corner signals;
- progressive tangent alignment near important contours;
- deterministic crossing deflection and optional hard-boundary termination;
- contour-driven detail reinforcement without an artificial outline;
- corner-aware stroke shortening;
- boundary guidance composed with primitive-derived flow in both hybrid stroke layers;
- signed `ArtisticDetailField` combining positive protection and negative background suppression;
- explicit subject, silhouette, uncertainty and manual-focus protection priority;
- configurable background detail floor and softened suppression transitions;
- fewer starts, longer/wider strokes, fewer segments and freer curvature in confident background;
- deterministic colour simplification away from protected forms;
- effective-detail reuse by primitive and hybrid planning;
- diagnostic overlays for suppression, protection and effective artistic detail;
- Soft contour, Strong silhouette and Loose background policies;
- deterministic smoothing of the proxy detail map;
- heat-map visualization over the source image;
- click-to-select and drag-to-create rectangular mouse interaction for manual regions;
- configurable SmoothStep transition bands inside and outside manual-region borders;
- full-strength region cores, Euclidean corner falloff and maximum merging of same-intent overlaps;
- normalized manual regions that survive window resizing;
- detail-weighted stroke placement;
- shorter and thinner strokes in detailed areas;
- broader and longer strokes in background areas;
- SkiaSharp 4 preview rendering through SolidRound, SoftRound, Flat and Bristle brush strategies;
- deterministic per-stroke size and opacity jitter shared by preview and final export;
- independent final PNG/JPEG export;
- versioned `*.flowpainter.json` projects containing source reference, seed, settings, preview quality, manual detail regions and semantic corrections;
- portable project-relative source-image paths;
- Draft, Standard and High preview qualities;
- region list with relabel, percentage resize, reorder and delete operations, plus `Delete` shortcut support;
- persistent recent-project and recent-preset lists;
- structured application workspace, operation and validation state;
- guarded dirty-session replacement and close workflow through a testable `ProjectSessionController`;
- project-title dirty indicator and persisted-control tracking distinct from transient viewport/overlay state;
- shared 2 GiB peak-memory admission policy for analysis and final export;
- mode-aware Flow, Primitive and Hybrid output-buffer estimates;
- explicit current-analysis, descriptor and adjacency-aware SLIC proxy-memory reserves;
- planner-level Flow segment, primitive scoring and primitive pixel-evaluation budgets;
- bounded 256 MiB encoded-image input with cancellation-aware streaming;
- final output up to 10,000 × 10,000 with preserved aspect ratio;
- cached preview `StrokePlan` reused unchanged for final rasterization;
- synchronized source/result zoom with the mouse wheel and pan with the middle mouse button;
- combined analysis/render memory estimate, risk indication and supported/blocked status before allocation;
- project schema 13 with compatibility for schemas 1–12;
- flow-preset schema 10 with compatibility for schemas 1–9;
- 920 validated automated test cases across Domain, Application, Imaging and Rendering;
- atomic sibling-file commits for projects, presets, previews, raster exports, SVG exports and recent items;
- preservation of an existing destination when serialization, encoding or cancellation fails;
- non-UI `AnalysisCoordinator` with detached full-analysis and manual-region recomposition results;
- immutable value-based analysis cache keys containing source identity, proxy size, settings and workspace revisions;
- monotonic analysis generations with cancellation/failure preservation and stale-result rejection;
- transactional UI adoption only after overlay creation and current-key verification;
- immutable M14.1 regional-segmentation contracts with compact label-map storage, graph/hierarchy validation and deterministic estimates;
- deterministic M14.2 SLIC clustering with CIELAB conversion, optional Gaussian pre-smoothing, local search, convergence, cancellation and monotonic progress;
- deterministic M14.3 four-neighbour connectivity repair, undersized-component merging, size diagnostics and on-demand mean-colour/boundary previews;
- deterministic M14.4 area, bounds, centroid, perimeter, compactness, CIELAB, texture, edge-density and tangent descriptors;
- deterministic M14.5 adjacency with exact shared-boundary counts, perceptual contrast, continuity, tangent and boundary-strength evidence;
- deterministic M14.6 fine/intermediate/broad hierarchy with aggregate merge costs, strong-edge protection and parent/child traceability;
- active M14.7 SLIC regional-analysis pipeline shared by Flow, Primitive and Hybrid;
- generalized `RegionRoleOverride` migration for schema-11 subject, focal, background and ignored decisions;
- fixed regional detail composition independent of retired semantic-analysis settings;
- regional/shared-boundary evidence forwarded into the validated scene-boundary analyzer;
- compact signed-assignment publication without a second full-size staging map;
- embedded multi-resolution FlowPainter icon for the Avalonia window, executable and publish output;
- 1,049 validated cases through M15.1;
- 1,071 expected cases after M15.2, pending local build/test validation.

M8–M13.3 remain a validated historical and schema-compatibility subsystem. New development does not extend class-aware or model-backed recognition. M13.4 hardened state, memory, persistence and analysis orchestration; M14.1–M14.6 introduced deterministic SLIC superpixels, regional descriptors, a Region Adjacency Graph and hierarchical merging. M14.7 replaced the active automatic semantic contribution. M14.8 exposed the regional controls and diagnostics and persists reusable SLIC/merge settings plus image-specific role overrides without storing derived label maps. M15.1 begins region-guided rendering with a continuous SLIC/RAG boundary field. M15.2 applies that field and the detail map to local stroke geometry.

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
2. inspect the combined detail heat map, regional compatibility overlays, important boundaries, background confidence or tangent directions;
3. tune Scene boundaries settings and use **Reanalyze detail + subjects + boundaries** when required;
4. configure **Boundary-aware painting** and **Background suppression**, or start with Balanced, Strong silhouette or Loose background;
5. compare Background confidence, Background suppression, Background protection and Artistic detail overlays;
6. review or edit persisted schema-11 role corrections; automatic semantic detections are no longer generated by the active pipeline;
7. promote a detected region to painterly focus/critical detail, or drag to create `IncreaseDetail` / `ReduceDetail` regions;
8. click a manual detail region or legacy role correction to select it, then edit it or press `Delete`;
9. choose Draft, Standard or High and use **Rebuild preview** when required;
10. choose **FlowPainting**, **GeometricPrimitives** or **Hybrid** as the generative engine;
11. for flow painting, choose SolidRound, SoftRound, Flat or Bristle and edit deterministic material parameters;
12. for primitives, choose any allowed-form combination, search budget, size range, opacity and detail influences;
13. for hybrid mode, configure layer budgets, primitive flow influence and refinement scale;
14. edit analysis/painting parameters and explicitly reanalyze or render;
15. save/load reusable `*.flowpreset.json` flow settings;
16. save the complete image-specific working state with **Save project**;
17. choose **Save preview** to export the current proxy-resolution PNG;
18. configure final maximum dimension and PNG/JPEG format;
19. choose **Export final** to rasterize the approved stroke, primitive or hybrid plan;
20. in primitive mode, choose **Export SVG** for resolution-independent vector output.

Manual detail regions and generalized role overrides belong to projects, not presets. Project schema 13 stores normalized detail/role geometry, SLIC and merge settings, soft transitions, high-detail stroke policy, boundary/background policies, final-output settings and all generative settings; schemas 1–12 remain readable. Preset schema 10 stores reusable SLIC, merge and stroke-policy parameters but never image-specific role overrides or derived label maps.

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
- [`docs/M9_GEOMETRIC_PRIMITIVES.md`](docs/M9_GEOMETRIC_PRIMITIVES.md)
- [`docs/M10_HYBRID_PRIMITIVE_FLOW.md`](docs/M10_HYBRID_PRIMITIVE_FLOW.md)
- [`docs/M11_SCENE_BOUNDARIES.md`](docs/M11_SCENE_BOUNDARIES.md)
- [`docs/M12_BOUNDARY_AWARE_PAINTING.md`](docs/M12_BOUNDARY_AWARE_PAINTING.md)
- [`docs/M13_BACKGROUND_SUPPRESSION.md`](docs/M13_BACKGROUND_SUPPRESSION.md)
- [`docs/M13_2_SOFT_DETAIL_REGIONS.md`](docs/M13_2_SOFT_DETAIL_REGIONS.md)
- [`docs/M13_3_REGION_SELECTION_AND_SEMANTIC_CORRECTIONS.md`](docs/M13_3_REGION_SELECTION_AND_SEMANTIC_CORRECTIONS.md)
- [`docs/M13_4_1_DIRTY_STATE_AND_DATA_LOSS_PROTECTION.md`](docs/M13_4_1_DIRTY_STATE_AND_DATA_LOSS_PROTECTION.md)
- [`docs/M13_4_2_MEMORY_AND_WORK_BUDGETS.md`](docs/M13_4_2_MEMORY_AND_WORK_BUDGETS.md)
- [`docs/M13_4_3_ATOMIC_DURABLE_WRITES.md`](docs/M13_4_3_ATOMIC_DURABLE_WRITES.md)
- [`docs/M13_4_4_ANALYSIS_ORCHESTRATION.md`](docs/M13_4_4_ANALYSIS_ORCHESTRATION.md)
- [`docs/M14_SLIC_REGIONAL_SEGMENTATION.md`](docs/M14_SLIC_REGIONAL_SEGMENTATION.md) — approved implementation plan
- [`docs/M14_1_REGIONAL_SEGMENTATION_CONTRACTS.md`](docs/M14_1_REGIONAL_SEGMENTATION_CONTRACTS.md)
- [`docs/M14_2_DETERMINISTIC_SLIC_IMPLEMENTATION.md`](docs/M14_2_DETERMINISTIC_SLIC_IMPLEMENTATION.md)
- [`docs/M14_3_CONNECTIVITY_AND_DIAGNOSTICS.md`](docs/M14_3_CONNECTIVITY_AND_DIAGNOSTICS.md)
- [`docs/M14_4_REGIONAL_DESCRIPTORS.md`](docs/M14_4_REGIONAL_DESCRIPTORS.md)
- [`docs/M14_5_REGION_ADJACENCY_GRAPH.md`](docs/M14_5_REGION_ADJACENCY_GRAPH.md)
- [`docs/M14_6_HIERARCHICAL_MERGE.md`](docs/M14_6_HIERARCHICAL_MERGE.md)
- [`docs/M14_7_ACTIVE_PIPELINE_MIGRATION.md`](docs/M14_7_ACTIVE_PIPELINE_MIGRATION.md)
- [`docs/M14_8_REGIONAL_UI_SETTINGS_AND_PERSISTENCE.md`](docs/M14_8_REGIONAL_UI_SETTINGS_AND_PERSISTENCE.md)
- [`docs/M15_1_REGIONAL_BOUNDARY_FIELD.md`](docs/M15_1_REGIONAL_BOUNDARY_FIELD.md)
- [`docs/M15_2_HIGH_DETAIL_LOCAL_STROKE_POLICY.md`](docs/M15_2_HIGH_DETAIL_LOCAL_STROKE_POLICY.md)
