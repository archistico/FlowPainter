# Validation checklist

## M14.1 regional segmentation contracts

**Status: READY FOR VALIDATION**

Expected automated suite: **863 cases** (804 validated baseline + 59 new Domain/Application segmentation-contract cases).

Required commands:

```bash
dotnet restore FlowPainter.sln
dotnet build FlowPainter.sln -c Release --no-restore
dotnet test FlowPainter.sln -c Release --no-build
```

Manual acceptance is defined in [`M14_1_REGIONAL_SEGMENTATION_CONTRACTS.md`](M14_1_REGIONAL_SEGMENTATION_CONTRACTS.md). Validation must cover compact-label storage boundaries, ownership, row/index access, region-area consistency, symmetric adjacency, monotonic hierarchy, range validation and deterministic memory/work estimates. Existing image, project and render behaviour must remain unchanged.

Run from the repository root with the .NET 10 SDK installed.

```bash
dotnet --info
dotnet restore FlowPainter.sln
dotnet build FlowPainter.sln -c Release --no-restore
dotnet test FlowPainter.sln -c Release --no-build --logger "console;verbosity=normal"
dotnet run --project src/FlowPainter.App/FlowPainter.App.csproj
```

## Current validated baseline — M13.4.4

- restore succeeds;
- all nine projects build with zero warnings and zero errors;
- all **804** test cases pass with zero failures and zero skips;
- project schema remains 11 and preset schema remains 8;
- dirty state and Save / Discard / Cancel navigation remain validated;
- analysis and final export are admitted through the shared 2 GiB memory policy;
- Flow, Primitive and Hybrid planning enforce bounded work before large loops or collections;
- encoded image input is bounded to 256 MiB with cancellation-aware streaming;
- M14.1 SLIC proxy memory/work uses an exact deterministic admission estimate;
- all current local writes use atomic sibling-file commit and preserve previous valid destinations on failure;
- Domain and Application remain free of Avalonia, SkiaSharp, LibNoiseCore, machine-learning runtimes and model files.

M13.3 plus audit corrections established 755 cases. M13.4.1 added ten Application cases, reaching 765. M13.4.2 added fourteen Application and three Imaging.Skia cases, reaching 782. M13.4.3 added eight Application persistence cases, reaching 790. M13.4.4 added fourteen Application analysis-lifecycle cases, establishing the current validated baseline of 804.

## Next validation target — M14.1 regional segmentation contracts

M13.4 is fully validated. M14.1 must pass before deterministic SLIC clustering begins. Its exit checks are:

- compact immutable labels with tested `UInt16`/`UInt32` selection;
- consistent region, graph, hierarchy and diagnostic identities;
- exact deterministic segmentation memory/work estimates integrated into admission;
- no Avalonia or SkiaSharp dependency in Domain/Application contracts;
- no UI, project-schema or active semantic-pipeline change.

## Validation history

### 2026-07-16 — M14.1 regional segmentation contracts prepared

M14.1 adds pure Domain/Application contracts for compact regional labels, immutable region descriptors, symmetric adjacency, monotonic hierarchy, detached results, diagnostics, progress and deterministic SLIC memory/work estimates. The exact estimator replaces the provisional fixed per-pixel SLIC reserve introduced by M13.4.2. Fifty-nine focused cases raise the expected suite from 804 to 863. No SLIC clustering, UI integration, schema migration or semantic-path replacement occurs in this milestone.

### 2026-07-16 — M13.4.4 validated on Windows

The user confirmed that the complete solution builds and all **804** tests pass after the analyzer and test-fixture follow-ups. Detached analysis orchestration and transactional result adoption are accepted as the baseline for M14.1.

### 2026-07-16 — M13.4.4 Windows test follow-up

The first executable test pass built all production projects but exposed two defects in the new coordinator tests. The defensive-copy test constructed a zero-width `NormalizedRect`, and completed-stage progress mapping could exceed its nominal boundary by a floating-point epsilon before the next exact stage boundary was reported. The fixture now uses a valid correction rectangle, while `AnalysisCoordinator` maps phase endpoints exactly through `MapStageFraction`. The monotonic-progress assertion remains strict and meaningful. No schema, runtime pipeline order or expected 804-case count changed.

### 2026-07-16 — M13.4.4 Windows build follow-up

The first Windows build exposed analyzer-only issues: four private progress factories returned the interface type instead of the concrete forwarding implementation, fourteen new test names used underscores forbidden by CA1707, `CreateAnalysisRequest` was eligible for `static`, and the recomposition branch passed a nullable basis through a conditional expression. The follow-up package uses concrete private return types, analyzer-compliant PascalCase test names, a static request factory and a directly null-guarded recomposition branch. No runtime behaviour, schema or expected test count changed.

### 2026-07-16 — M13.4.4 analysis orchestration prepared

M13.4.4 adds the non-UI `AnalysisCoordinator`, detached `AnalysisResult`/`PendingAnalysis` contracts, immutable source/settings/revision cache keys, monotonic generations, manual-region recomposition and transactional UI adoption after overlay creation. Cancellation, failure, callback failure, key mismatch and older-generation results preserve the active session. Fourteen focused Application cases raise the expected suite from 790 to 804.

Project schema remains 11 and preset schema remains 8. Executable validation could not be run in the packaging environment because the .NET SDK is unavailable; source, delimiter, documentation, test-count and ZIP-integrity checks are performed before delivery.

### 2026-07-16 — M13.4.3 validated on Windows

The user confirmed that the Release build succeeds and all **790** tests pass. Atomic sibling-file commits for project, preset, preview, raster, SVG and recent-item destinations are accepted as the baseline for M13.4.4.

### 2026-07-16 — M13.4.3 atomic durable writes prepared

M13.4.3 adds the Application-level `AtomicFileWriter` and routes project, preset, rendered preview, final Flow/Primitive/Hybrid raster, primitive SVG and recent-item writes through temporary sibling files. The temporary output is flushed and closed before a same-directory replace/move publishes it. Existing destinations are preserved on cancellation or failure, and failed new writes publish no destination. Eight focused Application cases raise the expected suite from 782 to 790.

No project or preset schema changes. Executable validation could not be run in the packaging environment because the .NET SDK is unavailable; source, documentation, test-count and ZIP-integrity checks are performed before delivery.

### 2026-07-16 — M13.4.2 validated on Windows

The user confirmed that the Release build succeeds and all **782** tests pass. Shared memory/work admission, Hybrid buffer accounting, future SLIC reserve, bounded encoded input and planner-level workload rejection are accepted as the baseline for M13.4.3.

### 2026-07-16 — M13.4.1 validated on Windows

The user confirmed that the Release build succeeds and all **765** tests pass. Dirty tracking and the guarded Save / Discard / Cancel navigation are accepted as the baseline for M13.4.2.

### 2026-07-16 — Documentation and roadmap realigned around SLIC

M13.3 is recorded as DONE and the 755-test audit baseline is authoritative. The future M13.4–M17 roadmap now uses deterministic SLIC regional segmentation, regional descriptors, a Region Adjacency Graph and hierarchical merging. SAM, MobileSAM, ONNX/model-backed providers, Python inference and Felzenszwalb are not part of the approved implementation path. M8 and M13.3 remain supported historical/schema compatibility behaviour until M14.7 replaces their active automatic contribution.

No production code or schema changed in this documentation-only update.

### 2026-07-16 — M13.3 validated baseline and audit remediation

Release build succeeded with zero warnings/errors and all 755 tests passed. M13.3 supplied 748 cases; seven additional Application tests cover workspace revision, transaction and project-load behaviour identified during the audit. Project schema remains 11 and preset schema remains 8.

### 2026-07-14 — M13.3 region selection and semantic corrections prepared (historical)

M13.3 separates painterly detail regions from semantic corrections. Clicks below a six-pixel display-space threshold select existing overlays, while larger drags create detail rectangles. Pure Application hit testing defines deterministic priority and repeated-click cycling. Selected manual detail regions and semantic corrections can be removed with `Delete` or the existing controls.

Four persistent correction intentions modify copied semantic maps before boundary analysis: forced primary subject, subject, background and ignored detection. The original automatic-region list remains inspectable. Corrections reuse the M13.2 SmoothStep transition field, same-kind overlaps merge by maximum influence and explicit kind precedence keeps the forced primary subject authoritative. Project schema advances to 11 with schema-10 empty-list compatibility; preset schema remains 8. Thirty-five focused cases increase the expected suite from 713 to 748.

Executable validation could not be run in the packaging container because no usable .NET SDK is installed and the official SDK archive could not be installed through the available download channel. XML/XAML, named-control/handler, schema, source-structure, test-count and ZIP-integrity checks are performed before packaging.

### 2026-07-14 — M13.2 validated on Windows

The user confirmed that M13.2 compiles successfully and all 713 tests pass. Soft manual-region transitions are accepted as the baseline for M13.3.

### 2026-07-14 — M13.2 soft manual detail regions prepared

M13.2 replaces hard rectangular detail masks with a continuous distance-based influence field. The configurable transition radius defaults to 5% of the shorter proxy dimension, uses SmoothStep inside and outside the border and applies Euclidean falloff around corners. Same-intent overlaps merge by maximum influence; opposing intent groups retain deterministic latest-edit ordering. Project schema moves to 10 and preset schema to 8 with schema-9/schema-7 defaults. Thirteen focused cases increase the expected suite from 700 to 713.

The desktop UI exposes **Region transition (%)**. The composed-map cache now includes this value, so Flow, Primitive and Hybrid previews recompose whenever it changes. The roadmap records separate next steps for region selection/semantic corrections, primary-subject ranking, stronger high-detail stroke policy, staged base-to-refinement rendering and primitive coarse-to-fine ordering.

Executable validation could not be run in the packaging container because no .NET SDK is installed; XML/XAML, schema, reference and static source checks are performed before packaging.

### 2026-07-14 — M13 validated on Windows

The user confirmed that the corrected M13.1 build and test suite pass. The 700-case background-suppression baseline is accepted and M13 is marked DONE before M13.2.

### 2026-07-14 — M13.1 floating-point multiplier assertion correction

The first Windows M13 test run built all projects and executed 700 cases. One settings test compared the derived segment multiplier `0.44999999999999996` with the mathematically equivalent literal `0.45` using exact `double` equality. M13.1 applies the established 12-decimal precision policy to all four derived painterly multipliers in that test. Production background-suppression logic, schemas and test count are unchanged.


### 2026-07-14 — M13 background suppression prepared

M13 adds an immutable signed `ArtisticDetailField` and a separate `BackgroundSuppressionComposer`. Background confidence is attenuated by manual focus, semantic subject/importance, silhouette and uncertainty protection. The result exposes suppression, protection and effective-detail diagnostics while retaining a signed field for the planner. Confident background receives fewer starts, longer/wider marks, fewer segments, freer curvature and deterministic colour simplification. Primitive and Hybrid modes reuse the same effective-detail policy. Project schema moves to 9 and preset schema to 7 with older files loading disabled defaults. Thirty-four focused cases increase the expected suite from 666 to 700.

The living roadmap, architecture, tests and ADR-0014 document the signed policy and retain M14-M16 as separate stages. Static packaging validation covers syntax structure, XAML/XML/JSON, named controls and handlers, schema literals, pure-layer dependencies, test counts and ZIP integrity. Executable validation remains assigned to the target Windows .NET 10 environment.

### 2026-07-14 — M12 validated on Windows

The user confirmed that M12 builds successfully and all 666 tests pass. Boundary-aware painting is accepted as the production baseline for M13.

### 2026-07-14 — M12 boundary-aware painting prepared

M12 converts the M11 diagnostic result into a deterministic Application-layer `BoundaryGuidanceField`. The field carries tangent direction, influence, hardness, subject-boundary confidence and corner strength. `FlowPainterPlanner` can now blend its artistic field toward the closest tangent orientation, sample proposed segments for crossing risk, deflect or terminate at hard boundaries, shorten marks near corners and reinforce contour detail without drawing an artificial outline. The same precomputed guidance is reused by both hybrid stroke layers.

Project schema moves to 8 and preset schema to 6. Previous files receive disabled compatibility defaults, preserving the validated pre-M12 plan and random sequence. Soft contour, Strong silhouette and Loose background demonstrate progressively stronger boundary policies. Thirty-five focused cases increase the expected suite from 631 to 666.

The living roadmap, architecture, test strategy and ADR-0013 document the implemented policy and retain M13-M16 as separate stages. Static preparation checks include syntax parsing of all non-legacy C# sources, XAML/XML/JSON parsing, named-control/event resolution, cancellation-token ordering, common .NET/xUnit analyzer scans, schema compatibility, pure-layer dependency scans, exact test-case counts and ZIP integrity. The packaging environment does not contain a usable .NET SDK, so executable validation remains assigned to the target Windows environment.

### 2026-07-14 — M11.1 SkiaSharp sampling-overload correction

The first Windows build of M11 completed Domain, Application, imaging and their tests, then stopped in `BoundaryDirectionOverlayRenderer` because SkiaSharp 4 marks `SKCanvas.DrawImage(SKImage, SKRect, SKPaint)` obsolete. M11.1 now supplies an explicit reusable `SKSamplingOptions` value, matching the already validated proxy, background and JPEG composition paths. No rendering semantics, schemas or test counts change; the expected suite remains 631 cases.

### 2026-07-14 — M11 scene-boundary analysis prepared

M11 adds a pure normalized boundary direction field, replaceable scene-boundary analyzer, deterministic multiscale luminance/colour analysis, contour-continuity and semantic-silhouette weighting, separate silhouette/internal-structure/texture maps, background confidence, uncertainty and a Skia tangent-direction diagnostic overlay. Project schema moves to 7 and preset schema to 5 while all previous schemas remain readable. Fifty-five focused cases increase the expected suite from 576 to 631.

The living roadmap, architecture, test strategy and ADR now contain the complete M11-M16 plan: diagnostic boundary separation, boundary-aware painting, background suppression, advanced manual editing, artistic hierarchy and release consolidation. Static preparation checks include C# syntax parsing, XAML/XML/JSON parsing, named-control/event resolution, project references, common analyzer patterns, pure-layer dependency scans, schema migration review, exact test-case counts and ZIP integrity. The packaging environment cannot resolve the official SDK host, so executable validation remains assigned to the target Windows environment.

### 2026-07-14 — M10 hybrid mode accepted

The user confirmed that the hybrid primitive/flow result works and is visually successful. The M10 baseline is accepted and marked DONE before M11 begins.

### 2026-07-14 — M10 hybrid primitive/flow engine prepared

M10 introduces the immutable `HybridPlan`, deterministic three-layer composition, primitive-derived axis/boundary/vortex/mixed flow deformation, configurable layer budgets, refinement controls, schema-6 project persistence and layered Skia preview/final rendering. Thirty-one focused cases increase the expected suite from 545 to 576.

Static preparation checks cover 235 non-legacy C# files, delimiter-balanced C# source, XAML/XML/JSON parsing, all 109 named controls, all 35 event handlers, solution/project references with platform-normalized paths, cancellation-token ordering, common .NET/xUnit analyzer patterns, pure-layer dependency boundaries, exact test-case counts and ZIP integrity. The packaging environment cannot resolve the official SDK download host, so executable build/test validation remains assigned to the target Windows environment.

### 2026-07-14 — M9.1 validated on Windows

The user confirmed that M9.1 compiles, all 545 tests pass and the geometric-primitive workflow functions correctly. M9 is marked DONE before M10 begins.


### 2026-07-13 — M9 geometric primitive engine prepared

M9 adds a second deterministic generative engine. Proxy-space candidate search, analytical colour estimation, weighted local error scoring and hill-climbing mutation produce a normalized immutable `PrimitivePlan`. The same plan drives synchronized preview, high-resolution PNG/JPEG rendering and SVG export. Detail guidance controls placement, size, error priority and local search effort. Project schema moves to version 5 while versions 1–4 remain readable. Forty-nine focused cases increase the expected suite from 496 to 545.

Static preparation checks cover 216 non-legacy C# files, C# syntax parsing, XAML/XML/JSON parsing, all 99 named controls, all 35 event handlers, solution/project references, cancellation-token ordering, common .NET/xUnit analyzer patterns, forbidden dependencies, deterministic SVG line endings and ZIP integrity. The packaging environment has no usable .NET SDK, so executable validation remains assigned to the target Windows environment.

### 2026-07-13 — M8.2 validated on Windows

The user confirmed that M8.2 builds successfully and all 496 tests pass. The semantic-importance baseline is accepted and M8 is marked DONE.

### 2026-07-13 — M8.2 xUnit analyzer correction

The next Windows build completed all production projects but stopped in `FlowPainter.Application.Tests` with `xUnit2031`. The semantic subject-count test used `Assert.Single(collection.Where(predicate))`; M8.2 now uses the dedicated `Assert.Single(collection, predicate)` overload. No production behavior changes, analyzer severity remains unchanged, and the expected suite remains 496 cases.

### 2026-07-13 — M8.1 semantic subject-kind analyzer correction

The first Windows build of M8 stopped in `FlowPainter.Domain` with `CA1720` because the public enum member `SemanticSubjectKind.Object` repeated the CLR type name `Object`. M8.1 renames that member to `SceneObject` while preserving its numeric value `3`, so serialized numeric compatibility and the semantic model remain unchanged. Analyzer severity is not reduced and the expected suite remains 496 cases.

### 2026-07-13 — M8 semantic importance prepared

M8 introduces a deterministic local semantic-importance provider that identifies generic salient subjects, silhouettes and focal points on the analysis proxy. Separate maps can be inspected in the desktop UI and detected regions can be promoted to persistent manual focus or critical-detail regions. The provider boundary allows future local ONNX/class-aware analyzers without adding a machine-learning runtime to Domain or Application today. Project and preset schemas move to version 4 with versions 1–3 remaining readable. Fifty-six focused cases increase the expected suite from 440 to 496.

Static preparation checks include C# syntax parsing, XAML/XML/JSON parsing, named-control/event resolution, schema migration review, forbidden-dependency scans and ZIP integrity. The packaging environment cannot obtain the .NET SDK, so executable validation remains assigned to the target Windows environment.

### 2026-07-13 — M7.1 validated on Windows

The user confirmed that M7.1 builds and works correctly. The 440-case brush-engine baseline is accepted and M7 is marked DONE.

### 2026-07-13 — M7.1 cancellation-token parameter ordering correction

The first Windows build of M7 reached the rendering project and reported `CA1068` because `SkiaStrokePlanRenderer.RenderAsync` placed the optional `BrushSettings` parameter after `CancellationToken`. M7.1 moves `CancellationToken` to the final position required by the analyzer and updates the two positional application call sites. Named-argument test calls and rendering behaviour remain unchanged; analyzer severity is not reduced.

### 2026-07-13 — M7 brush engine prepared

M7 introduces a pure brush configuration value and a Skia renderer strategy boundary. The same immutable `StrokePlan` can now be rasterized with SolidRound, SoftRound, Flat or Bristle materials. Size and opacity jitter are derived from the plan seed and stroke index, so preview and final export remain repeatable. Project and preset schemas move to version 3 while earlier files receive SolidRound defaults. Thirty focused cases increase the expected suite from 410 to 440.

### 2026-07-13 — M6.1 validated on Windows

The user confirmed that M6.1 builds successfully and all 410 automated tests pass. Synchronized wheel zoom, middle-button pan and source-region alignment are accepted; M6.1 is marked DONE.

### 2026-07-13 — M6.1 synchronized viewport prepared

The source/detail-map panel and rendered-preview panel now share a normalized viewport state. Wheel zoom is anchored at the pointer, middle-button pan is clamped to the image, and the same center/zoom is applied independently to both control sizes. Source-region selection inverse-maps pointer coordinates through the active transform. Ten focused tests increase the expected suite from 400 to 410 cases.

### 2026-07-13 — M6 validated on Windows

The user confirmed that M6 builds successfully and all 400 automated tests pass. M6 is marked DONE.

### 2026-07-13 — M6 prepared

M6 separates preview and final raster output. The immutable preview `StrokePlan` is retained and reused at an independently configured resolution up to 10,000 × 10,000 pixels. Final settings are persisted in project schema 2, with schema-1 migration to explicit defaults. PNG preserves alpha; JPEG composes transparency over white. A conservative estimate covers known source, proxy, preview, overlay and double final-output RGBA buffers.

The expected suite grows from 360 to 400 cases. Static preparation checks include C# syntax parsing, XAML/XML/JSON parsing, named-control/event resolution, project references, forbidden dependency scans and ZIP integrity.

### 2026-07-13 — M5.3 validated on Windows

The user confirmed that the corrected M5.3 package builds successfully and all 360 tests pass. M5 is marked DONE.

### 2026-07-13 — M5.3 project rectangle serialization correction

The M5.2 Windows test run built all projects and executed 357 tests. One project round-trip test failed because `NormalizedRect` was serialized as edge properties but deserialized as its default zero-valued struct. This was a persistence defect rather than a floating-point assertion issue.

M5.3 adds an Application-layer JSON converter that writes the four stable rectangle edges, reconstructs the validated immutable domain value, rejects invalid bounds and accepts earlier schema-1 payloads containing the derived `width` and `height` properties. Three focused tests cover the stable JSON shape, previous-payload compatibility and invalid-bounds rejection. The expected suite is now 360 cases.

### 2026-07-13 — M5.2 target build corrections

The first Windows build of M5.1 identified nine compile/analyzer findings unrelated to the scrollbar layout change:

- six ambiguous `Path` references caused by importing Avalonia shapes alongside `System.IO`;
- one `CA1859` finding on a private helper receiving arrays through an `IReadOnlyList<string>` parameter;
- two `CA1861` findings caused by repeated inline expected-order arrays in region-editor tests.

M5.2 introduces an explicit `IoPath` alias, uses the concrete `string[]` parameter already supplied by the UI, and moves the two expected orders to static readonly fields. Analyzer severity remains unchanged and production behavior is preserved.

### 2026-07-13 — M5.1 configuration scrollbar spacing

The configuration `ScrollViewer` now disables horizontal scrolling and adds a stable right margin to its settings panel. The 18-pixel gutter keeps the vertical overlay scrollbar separate from text boxes, combo boxes and buttons without changing the configuration column width or application behavior. The automated suite remains at 357 cases.

Static validation completed before packaging:

- Avalonia XAML parses successfully;
- all existing named controls and event handlers are unchanged;
- the modification is layout-only and does not affect project, rendering or persistence code;
- no generated build output is included.

### 2026-07-13 — M5 prepared

M5 introduces a schema-versioned project document, preview-quality settings, portable source references, a testable workspace and region editor, persistent recent items and the corresponding Avalonia workflow. The expected suite grows to 357 cases.

Static preparation checks completed before packaging:

- project XML, Avalonia XAML and JSON fixtures parse successfully;
- every named XAML control and event handler resolves to the code-behind;
- all solution and project-reference paths resolve;
- exposed project/workspace collections use read-only views;
- project and recent-item schemas are validated before full deserialization;
- Domain and Application contain no SkiaSharp, Avalonia, LibNoiseCore, `System.Random` or network-loading reference;
- source references round-trip through relative project paths;
- no `bin`, `obj` or IDE output is included.

The packaging environment does not contain the .NET SDK, so executable build/test validation remains assigned to the target Windows environment.

### 2026-07-13 — M4.1 validated on Windows

The user confirmed the corrected M4.1 package and continued development. M4 is marked DONE with all 249 cases expected to pass using tolerance-based floating-point assertions.

### 2026-07-13 — M4.1 floating-point assertion correction

The first Windows test run built all projects and executed all 249 cases. One viewport test failed because exact record equality compared `90` with the mathematically equivalent IEEE 754 result `90.00000000000001`. M4.1 replaces exact `ViewportRect` equality with component-wise comparisons at 12 decimal digits and documents the floating-point assertion policy. Production geometry code is unchanged.

### 2026-07-13 — M0 validated on Windows

The user confirmed that the M0.1 package restored, built in Release and passed all 37 test cases. M0 is marked DONE.

### 2026-07-13 — Initial M0 build findings

The first Release build exposed:

- 27 `CA1707` analyzer errors caused by underscore-based test names;
- one `CS0118` namespace collision between `FlowPainter.Application` and Avalonia `Application`.

M0.1 corrected both without suppressing analyzers and was subsequently validated.

### 2026-07-13 — Initial M1 build findings

The first target Windows build exposed two `CA1859` performance analyzer errors in `LegacyFlowPainterPlanner`. M1.1 corrected both without changing public contracts or generated plans.

### 2026-07-13 — M1 validated on Windows

The user confirmed that M1.1 compiled successfully and all 77 test cases passed. M1 is marked DONE.

### 2026-07-13 — M2 build corrections

Target builds identified and corrected:

- the sealed `InvalidDataException` base in .NET 10;
- narrow `CA1822` findings on replaceable stateless services;
- obsolete mutable `SKPath` calls in SkiaSharp 4.

The final M2.3 package uses `IOException`, documented service-level suppressions and `SKPathBuilder`.

### 2026-07-13 — M2 validated on Windows

The user confirmed that M2.3 compiled successfully and all 110 test cases passed. M2 is marked DONE.

### 2026-07-13 — M3 validated on Windows

The user confirmed that M3 compiled successfully and all 183 test cases passed. M3 is marked DONE.

### 2026-07-13 — M4 prepared

M4 adds structural detail analysis, heat-map visualization, normalized manual regions, detail-aware planning and schema-1 preset migration. The suite grows to 249 cases.

Static preparation checks completed before packaging:

- project XML and Avalonia XAML parse successfully;
- all solution and project-reference paths resolve;
- public test names comply with analyzer naming rules;
- Domain and Application contain no SkiaSharp, Avalonia, LibNoiseCore, `System.Random` or network-loading reference;
- `flow-field-v1` compatibility remains covered while detail-aware plans use `flow-field-detail-v1`;
- preset schema 2 accepts schema 1 and applies explicit M4 defaults;
- proxy detail maps never exceed the proxy image dimensions;
- overlay and native-image ownership have deterministic failure paths;
- no `bin`, `obj` or IDE output is included.

The packaging environment does not contain the .NET SDK, so executable build/test validation remains assigned to the target Windows environment.
