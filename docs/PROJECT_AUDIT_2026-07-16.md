# FlowPainter project audit

Date: 2026-07-16

## Executive summary

The current codebase has a strong non-UI foundation: the Release build succeeds with warnings treated as errors, all 755 tests pass, the documented dependency rule is respected, and the NuGet vulnerability scan reports no known vulnerable direct or transitive packages.

The audit nevertheless found correctness and reliability risks concentrated in the desktop orchestration layer. The most urgent problems are state/cache divergence after failed or cancelled edits, unused dirty-state tracking that permits silent data loss, configuration limits that admit workloads requiring tens of gigabytes or impractical execution times, and an underestimated Hybrid export memory peak.

Recommended order:

1. make editing and project loading transactional;
2. wire dirty-state and close/open confirmation into the UI;
3. add workload and mode-aware memory budgets;
4. extract testable application workflows from `MainWindow`;
5. harden file persistence and CI hygiene.

## Implementation progress

Remediation batches completed on 2026-07-16:

- F-01 resolved within the current desktop architecture: workspace region/correction revisions invalidate cached analysis after every successful edit, and all region/correction add, update, reorder, delete and clear workflows restore their logical state, dirty flag, selection and revisions when recomposition fails or is cancelled.
- F-05 resolved: changing `RegionTransitionWidth` now forces semantic-correction and boundary reanalysis when corrections exist.
- F-06 resolved: project path and edit collections are validated into an immutable `WorkspaceProjectCandidate`; decoding, proxy generation, analysis and overlay rendering complete without changing the active controls or session, followed by a final cancellation check and synchronous adoption.
- operation wrappers now return an explicit success/failure result instead of hiding cancellation and exceptions from edit commands.
- seven workspace revision/transaction/project-load tests added; the complete suite now contains 755 passing cases. Avalonia-level failure injection remains part of the broader F-09 testability work.

## Scope and constraints

Reviewed areas:

- Domain invariants and immutable plans;
- Application analysis, planning, persistence, workspace and cache behavior;
- Skia image ownership, rendering and encoding;
- Avalonia desktop workflows and resource lifetime;
- build, tests, coverage, formatting, CI and dependency health;
- architecture, ADRs, validation notes and milestone documentation.

Constraint: the supplied directory does not contain readable Git repository metadata. This audit therefore evaluates the current snapshot and its documented contracts, but cannot compare behavior against previous commits or identify the commit that introduced a regression.

## Verification baseline

| Check | Result |
| --- | --- |
| SDK | .NET SDK 10.0.203 selected through `global.json` |
| Release build | Passed, zero warnings/errors |
| Tests | 755 passed, 0 failed, 0 skipped |
| Domain coverage | 90.72% lines, 85.71% branches |
| Application coverage | 95.87% lines, 86.48% branches |
| Imaging coverage | 92.17% lines, 76.04% branches |
| Rendering coverage | 95.50% lines, 82.92% branches |
| Desktop UI coverage | None: `FlowPainter.App` is not referenced by a test project |
| NuGet vulnerability scan | No known vulnerable direct or transitive packages |
| `dotnet format --verify-no-changes` | Failed due to repository-wide line-ending mismatch |

Coverage files were generated under `artifacts/audit-coverage/`. Coverage percentages above are package rates from the test project primarily responsible for each package, rather than a misleading aggregate of all instrumented dependencies.

## Findings

### F-01 - High - Edits can diverge from cached analysis after failure or cancellation

Status: resolved on 2026-07-16. The text below records the original finding and remediation rationale.

Manual regions and semantic corrections are committed to `FlowPainterWorkspace` before their derived maps are recomputed. Examples include adding a semantic correction in [`MainWindow.axaml.cs`](../src/FlowPainter.App/MainWindow.axaml.cs#L1822), adding a dragged region at [line 2129](../src/FlowPainter.App/MainWindow.axaml.cs#L2129), and updating a region at [line 2315](../src/FlowPainter.App/MainWindow.axaml.cs#L2315).

The asynchronous operation boundary catches cancellation and all other exceptions without rolling back the workspace mutation ([lines 3469-3485](../src/FlowPainter.App/MainWindow.axaml.cs#L3469)). The detail-map cache key checks analysis settings and transition width, but does not include region/correction identity or a revision token ([lines 3294-3302](../src/FlowPainter.App/MainWindow.axaml.cs#L3294)).

Consequences:

- a correction can be visible in the list and persisted but not affect overlays or rendering;
- cancelling recomposition can leave the old output cache valid;
- saving and reopening the same project can unexpectedly change the image because reload applies the persisted edit correctly;
- an invalid unrelated analysis field can prevent recomposition after the edit has already been accepted.

Recommended fix:

- add monotonically increasing region and correction revisions to the workspace;
- include those revisions in a single analysis-cache key;
- invalidate the cache synchronously when an edit is committed;
- preferably stage an edit, compute derived state, then atomically commit both logical and derived state;
- add integration tests for add/update/delete followed by cancellation, validation failure and retry.

### F-02 - High - Unsaved project changes are silently discarded

`FlowPainterWorkspace.IsDirty` is maintained and unit-tested, but `MainWindow` never reads it. Opening another image/project replaces the session directly, and the close handler only cancels work and disposes resources ([`MainWindow.axaml.cs`](../src/FlowPainter.App/MainWindow.axaml.cs#L4587)). There is no `Closing` guard or save/discard/cancel workflow.

The dirty model is also incomplete at the presentation boundary: most text and selection controls have no change binding/event that updates workspace state. A user can edit parameters and close the application without `IsDirty` reflecting those changes.

Recommended fix:

- introduce a presentation/session state object that owns editable values and dirty tracking;
- guard Open image, Open project, recent-item open and window closing;
- offer Save / Discard / Cancel;
- distinguish project changes from transient viewport and overlay selection changes;
- add UI-workflow tests for each destructive navigation path.

### F-03 - High - Valid settings admit impossible memory and compute workloads

[`FlowPainterSettings`](../src/FlowPainter.Application/FlowPainting/Planning/FlowPainterSettings.cs#L15) independently allows up to 1,000,000 strokes and 1,024 segments. The planner retains every point in the immutable plan ([`FlowPainterPlanner.cs`](../src/FlowPainter.Application/FlowPainting/Planning/FlowPainterPlanner.cs#L271)). At the maxima, point storage alone is roughly 1.025 billion `RelativePoint` values, or at least 15.3 GiB before arrays, strokes, read-only wrappers and rendering state.

Primitive limits have the same composition problem. Up to 20,000 primitives, 512 candidates and 2,048 mutations are independently valid ([`PrimitiveGenerationSettings.cs`](../src/FlowPainter.Application/PrimitiveGeneration/PrimitiveGenerationSettings.cs#L7)); the optimizer can therefore request about 51.2 million candidate scores before pixel work is considered ([`PrimitivePlanOptimizer.cs`](../src/FlowPainter.Application/PrimitiveGeneration/PrimitivePlanOptimizer.cs#L71)).

These are not useful supported maxima: they permit configurations that can hang for hours or terminate the process with `OutOfMemoryException`.

Recommended fix:

- validate composite work units, not only individual fields;
- estimate plan bytes from stroke count and segments before allocation;
- estimate primitive score work from count, candidates, mutations, proxy size and shape size;
- provide hard safety limits plus an explicit advanced override if needed;
- prevent rendering when the estimate exceeds a configurable process budget;
- add boundary tests proving accepted maxima fit the supported memory envelope.

### F-04 - High - Hybrid final-export memory is materially underestimated

The estimator always counts two output-size RGBA buffers ([`FinalRenderMemoryEstimator.cs`](../src/FlowPainter.Application/Images/FinalRenderMemoryEstimator.cs#L20)). Hybrid rendering retains the primitive layer while creating the flow layer, then retains both while creating the refinement result ([`SkiaHybridPlanRenderer.cs`](../src/FlowPainter.Rendering.Skia/Hybrid/SkiaHybridPlanRenderer.cs#L49)). Each nested renderer also owns an output surface and creates a bitmap copy.

For a 10,000 x 10,000 output, one RGBA buffer is about 381 MiB. During the refinement pass, multiple output-sized layers, the active surface/copy, the source image and preview resources can coexist. The UI can therefore report the documented 1.2 GiB `Elevated` estimate while the real Hybrid peak crosses the 1.5 GiB `High` threshold and can exceed 2 GiB.

Recommended fix:

- make estimation depend on `GenerativeMode` and output format;
- model renderer ownership phases explicitly;
- dispose `primitiveLayer` as soon as `flowLayer` is complete;
- investigate direct layer composition or reusable surfaces to reduce copies;
- include encoder byte arrays and JPEG flattening surfaces in the estimate;
- add measured 4K/8K/10K stress tests and compare estimates against process/native memory.

### F-05 - Medium - Region transition changes can leave semantic corrections stale

Semantic correction feathering explicitly consumes `RegionTransitionWidth` ([`SemanticCorrectionComposer.cs`](../src/FlowPainter.Application/Semantics/SemanticCorrectionComposer.cs#L8)). A manual-region recomposition reuses the existing corrected semantic and boundary maps, then records the new transition width as active ([`MainWindow.axaml.cs`](../src/FlowPainter.App/MainWindow.axaml.cs#L3364)). The next render sees matching cache metadata and does not reapply semantic corrections.

Scenario:

1. create a semantic correction;
2. change Region transition;
3. add or edit a manual detail region;
4. render.

The manual region uses the new feather width, while the semantic correction and boundaries can still use the old width.

Recommended fix: split `DetailInfluenceSettings` into planning-only and analysis-affecting cache keys, or force full semantic/boundary recomputation when the shared transition width changes. Add an end-to-end cache invalidation test for this sequence.

### F-06 - Medium - Failed project loading can partially overwrite the current UI

Status: resolved on 2026-07-16. The text below records the original finding and remediation rationale; automated coverage currently exercises the detached workspace candidate, while decode/analysis/cancellation UI injection remains a residual F-09 test gap.

Project controls are applied before source decoding, proxy creation and analysis complete ([`MainWindow.axaml.cs`](../src/FlowPainter.App/MainWindow.axaml.cs#L516)). Workspace and native images are adopted only later, after successful analysis ([line 568](../src/FlowPainter.App/MainWindow.axaml.cs#L568)). If decoding, allocation, cancellation or analysis fails, the old image/workspace survives but the controls show values from the failed project.

Recommended fix:

- deserialize and validate into a detached session candidate;
- load source and all derived resources without mutating the active UI;
- atomically swap session state and resources only after success;
- add failure-injection tests for decode, analysis and cancellation stages.

### F-07 - Medium - Durable saves and exports are not atomic

Project/preset serializers correctly truncate seekable streams, but the application writes directly to the selected destination ([project save](../src/FlowPainter.App/MainWindow.axaml.cs#L700), [preset save](../src/FlowPainter.App/MainWindow.axaml.cs#L2508), [final export](../src/FlowPainter.App/MainWindow.axaml.cs#L1387)). Cancellation, disk-full, codec or I/O failure can destroy a previously valid file and leave a partial replacement.

Recommended fix for local files:

- write to a temporary sibling file;
- flush and close it;
- atomically replace/move to the destination;
- preserve the original on failure;
- use an explicit fallback contract for non-local `IStorageFile` providers where atomic replacement is unavailable.

### F-08 - Medium - Encoded input is buffered without a size limit and copied repeatedly

The loader copies the entire source stream into a `MemoryStream`, converts it to another byte array, then creates another Skia data copy before metadata validation ([`SkiaImageLoader.cs`](../src/FlowPainter.Imaging.Skia/Images/SkiaImageLoader.cs#L28)). The 10,000 x 10,000 decoded-size limit is useful, but it does not bound the encoded input or temporary compressed-data memory.

Recommended fix:

- impose a documented encoded-file limit while streaming;
- avoid `MemoryStream.ToArray()` plus `SKData.CreateCopy()` duplication;
- inspect metadata through a bounded/seekable stream where supported;
- add tests for oversized encoded streams and cancellation during copy.

### F-09 - Medium - Desktop orchestration is a single untested 4,633-line class

`MainWindow.axaml.cs` owns dialogs, persistence, parsing, cache invalidation, analysis orchestration, native resources, rendering, interaction, operation state and view updates. This is the common root of F-01, F-02, F-05 and F-06. `FlowPainter.App` has no automated tests, so the most stateful layer has zero regression protection despite excellent lower-layer coverage.

Recommended extraction boundaries:

- `ProjectSessionController`: open/save/dirty/transactional replacement;
- `AnalysisCoordinator`: cache key, revisions and derived-map lifecycle;
- `RenderCoordinator`: preview/final plan ownership and memory checks;
- `OverlayInteractionController`: hit testing, selection and edit commands;
- a thin Avalonia window that binds controls and owns native visual resources.

Avoid a framework-wide rewrite. Extract one workflow at a time behind existing Application contracts and add tests before moving the next workflow.

### F-10 - Low - EditorConfig and repository line endings disagree

`.editorconfig` requires CRLF for all files, but 281 of 355 inspected source/test/project/documentation files use LF; only 74 use CRLF. Consequently `dotnet format --verify-no-changes` fails with thousands of `ENDOFLINE` errors. CI does not run the formatting check.

Recommended fix: choose one policy deliberately (LF is simpler for the existing Ubuntu CI), update `.editorconfig`, normalize once in an isolated mechanical commit, and add `dotnet format --verify-no-changes` to CI.

### F-11 - Low - Build reproducibility and platform validation are weaker than claimed support

`global.json` requests 10.0.100 with `rollForward: latestFeature`, and CI installs `10.0.x`; analyzer/compiler behavior can therefore change across feature bands. CI runs only on Ubuntu and only builds/tests, while the README claims Windows, Linux and macOS desktop support. No UI startup smoke test or packaging check exists.

Recommended fix:

- decide whether feature-band floating is intentional; pin the SDK if repeatability is more important;
- add at least Windows and Ubuntu build jobs;
- add a minimal application-startup/composition smoke test that does not require interactive dialogs;
- defer full packaging matrices to M16 as already planned.

## Positive observations

- Layer dependencies match `docs/ARCHITECTURE.md`; Domain and Application remain free of Avalonia/Skia coupling.
- Immutable plan and normalized geometry invariants are consistently defensive.
- Native Skia ownership is explicit, with transactional replacement in the successful preview paths.
- Cancellation and progress are propagated through the expensive algorithms.
- Project and preset schema compatibility has extensive automated coverage.
- Warning-as-error and current .NET analyzers provide a useful baseline.
- No TODO/FIXME/HACK markers or `NotImplementedException` paths were found in active source.
- No known vulnerable NuGet dependencies were reported on 2026-07-16.

## Proposed implementation plan

### Phase 1 - State correctness and data-loss prevention

Targets: F-01, F-02, F-05, F-06.

1. Introduce immutable `AnalysisCacheKey` containing source/proxy identity, all analysis-affecting settings, region revision and correction revision.
2. Invalidate derived state immediately on every edit; never mark a new key active until replacement succeeds.
3. Add failure/cancellation integration tests around edit and reanalysis workflows.
4. Build a detached `ProjectSessionCandidate` and make project adoption atomic.
5. Wire complete dirty tracking and Save / Discard / Cancel guards.

Exit criteria: no edit can be persisted while stale derived maps are considered valid; failed open leaves every active control/resource unchanged; destructive navigation cannot silently discard changes.

### Phase 2 - Resource safety

Targets: F-03, F-04, F-08.

1. Define supported memory and work budgets.
2. Add composite plan/work estimators and reject unsafe settings before allocation.
3. Make final-memory estimation mode-aware and reduce Hybrid layer lifetimes.
4. Bound and streamline encoded input buffering.
5. Add stress tests at representative proxy/output sizes, with 10K tests opt-in for capable CI agents.

Exit criteria: every accepted configuration has a defensible upper bound; estimate bands conservatively match measured peaks; cancellation latency remains bounded.

### Phase 3 - Persistence hardening

Target: F-07.

1. Add an atomic local-file writer abstraction.
2. Route project, preset, preview, SVG and final-image writes through it.
3. Test cancellation, disk-full simulation, replacement failure and preservation of the original file.

Exit criteria: failed local writes never corrupt an existing destination.

### Phase 4 - Presentation decomposition and regression coverage

Target: F-09.

1. Extract `AnalysisCoordinator` first because it resolves several correctness findings.
2. Extract project/session lifecycle second.
3. Extract render/export orchestration third.
4. Keep Avalonia controls and disposable bitmap ownership in the window.
5. Add headless controller tests plus a small Avalonia startup/binding smoke suite.

Exit criteria: `MainWindow` is primarily event wiring and visual resource adoption; critical open/edit/render/save state transitions are automated.

### Phase 5 - Repository hygiene and CI

Targets: F-10, F-11.

1. Normalize line endings in a dedicated change.
2. Add format verification and vulnerability scanning to CI.
3. Add Windows build coverage and an application composition smoke check.
4. Document SDK roll-forward policy.

## Suggested issue breakdown

| Order | Issue | Priority | Depends on |
| --- | --- | --- | --- |
| 1 | Add revisioned `AnalysisCacheKey` and invalidation tests | P0 | None |
| 2 | Make region/correction edits transactional on failure | P0 | 1 |
| 3 | Make project loading transactional | P0 | None |
| 4 | Implement complete dirty tracking and navigation guards | P0 | 3 |
| 5 | Add composite plan/work safety budgets | P0 | None |
| 6 | Make final-memory estimate mode-aware | P0 | 5 |
| 7 | Reduce Hybrid output-layer lifetime | P1 | 6 |
| 8 | Bound encoded image input | P1 | None |
| 9 | Add atomic local-file writes | P1 | None |
| 10 | Extract analysis/session coordinators with tests | P1 | 1-4 |
| 11 | Normalize line endings and enforce format | P2 | None |
| 12 | Expand CI platform/smoke coverage | P2 | 10-11 |

## Revalidation checklist

After the changes:

- run `dotnet restore FlowPainter.sln`;
- run `dotnet build FlowPainter.sln -c Release --no-restore`;
- run `dotnet test FlowPainter.sln -c Release --no-build`;
- run `dotnet format FlowPainter.sln --verify-no-changes --no-restore`;
- rerun package vulnerability scanning;
- collect coverage and ensure extracted workflow code is covered;
- manually test cancel/failure paths for image open, project open, edit recomposition and all export modes;
- measure memory for Flow, Primitive and Hybrid final exports at 4K, 8K and 10K where hardware permits.
