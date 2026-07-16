# M13.4.4 — Analysis orchestration extraction

**Status: DONE — validated with 804 tests**

## Purpose

M13.4.4 removes the derived-map lifecycle from the Avalonia window before SLIC adds label maps, descriptors, adjacency graphs and hierarchical merge levels.

Analysis must complete into a detached result. The active session is changed only when the source identity, proxy dimensions, settings and workspace revisions still match the request that produced that result.

The architectural decision is recorded in [`ADR-0020 — Detached analysis and transactional adoption`](decisions/ADR-0020-DETACHED-ANALYSIS-ADOPTION.md).

## Application boundary

`FlowPainter.Application.Analysis` now contains:

- `AnalysisCoordinator`;
- `AnalysisRequest`;
- immutable `AnalysisCacheKey`;
- detached `AnalysisResult`;
- `PendingAnalysis` generation token;
- `AnalysisPipelineStage` and `AnalysisPipelineProgress`.

The coordinator depends on the existing pure Application contracts:

```text
IDetailMapAnalyzer
ISemanticImportanceAnalyzer
ISceneBoundaryAnalyzer
DetailMapComposer
SemanticCorrectionComposer
SemanticDetailMapComposer
BackgroundSuppressionComposer
```

It has no Avalonia, SkiaSharp or filesystem dependency.

## Detached pipeline

A full request executes:

```text
Structural detail analysis
    ↓
Automatic semantic analysis
    ↓
Persistent semantic corrections
    ↓
Scene-boundary analysis
    ↓
Automatic detail composition
    ↓
Manual detail regions
    ↓
Background suppression / artistic detail
    ↓
Detached AnalysisResult
```

`AnalysisResult` contains:

- structural detail map;
- corrected semantic result;
- scene-boundary result;
- automatic detail map;
- manually composed detail map;
- background-suppression result and effective artistic-detail map.

None of these values is published to the active desktop session while analysis is running.

## Immutable cache key

`AnalysisCacheKey` identifies the exact derived-map state through:

- a per-source session identity;
- proxy dimensions;
- detail-region revision;
- semantic-correction revision;
- invariant fingerprints of structural, transition, semantic, boundary and background settings.

The key deliberately includes only settings that affect analysis output. Stroke, primitive, brush and final-render settings remain outside it.

Project/image loading initially analyzes against a detached candidate source identity. After successful workspace adoption, the current result is retagged to the committed workspace revisions without recomputation.

## Generation and stale-result rejection

Every full analysis or manual-region recomposition receives a monotonically increasing generation.

A pending result may be adopted only when:

```text
pending generation == latest requested generation
AND
pending key == current expected key
```

Therefore:

- an older run cannot overwrite a newer run;
- edits or corrections made after a request invalidate its result;
- proxy/source changes reject earlier results;
- a failed or cancelled run leaves the previously adopted result available;
- explicit invalidation clears the current cache and rejects outstanding pending results.

## Transactional desktop adoption

Avalonia remains responsible for native resources and visual ownership:

1. run the Application analysis into a detached result;
2. render the overlay into a temporary Skia image;
3. rebuild the expected cache key from the currently active source/settings/revisions;
4. call `AnalysisCoordinator.TryAdopt`;
5. inside the successful adoption callback, replace UI maps and previews transactionally;
6. publish the result as current only after the callback completes successfully.

If overlay rendering, key validation or UI replacement fails, the coordinator does not publish the pending result.

The same boundary is used for:

- opening a source image;
- opening a project;
- changing preview quality;
- explicit full reanalysis;
- semantic-correction add/delete/clear;
- render-time cache validation.

## Efficient recomposition

Manual detail-region edits and background-only recomposition do not require structural, semantic and boundary analyzers to run again when their basis is still valid.

`RecomposeAsync` reuses the adopted structural/semantic/boundary/automatic maps and rebuilds only:

- manual-region composition;
- background suppression;
- effective artistic-detail field.

A full analysis is still required when structural, semantic or boundary settings change, or when the transition width affects existing semantic corrections.

## Cancellation and failure guarantees

- cancellation is checked before and between all pipeline stages;
- analyser exceptions propagate without mutating `CurrentKey` or `CurrentResult`;
- a failed adoption callback leaves the coordinator cache unchanged;
- workspace edits are rolled back by the existing editor transaction when recomposition fails;
- rollback revisions retag the preserved current result to the restored workspace state;
- closing/disposal invalidates the coordinator and rejects pending results.

## Build-analyzer follow-up

The Windows validation pass required analyzer-only source corrections: concrete return types for private forwarding-progress factories, PascalCase test method names without underscores, a static request factory and a directly null-guarded recomposition branch. The subsequent test pass also corrected an invalid zero-width test rectangle and made completed phase endpoints exact, preventing a floating-point epsilon from appearing as a progress regression. These corrections do not change coordinator behaviour, schemas or the expected 804-case suite.

## Automated tests

Fourteen new Application tests cover:

- value-based cache-key equality;
- source, setting and revision invalidation;
- defensive copying of region/correction collections;
- complete detached pipeline output;
- monotonic progress and completion;
- cancellation preserving the current result;
- failure preserving the current result;
- successful transactional adoption;
- older-generation rejection;
- expected-key mismatch rejection;
- adoption-callback failure;
- recomposition without analyzer reruns;
- cache-key retagging;
- explicit invalidation and pending-result rejection.

Validated suite after this milestone: **804 test cases**.

## Manual validation checklist

1. Open an image and verify analysis completes and the preview appears as before.
2. Cancel image analysis; verify the previously active image and overlays remain unchanged.
3. Open a valid project and verify regions, corrections and analysis overlays are adopted together.
4. Attempt to open a project with an invalid/missing source and verify the active session remains unchanged.
5. Change preview quality and rebuild; cancel midway and verify the old proxy remains active.
6. Add, remove and clear manual detail regions; verify successful recomposition and rollback after cancellation.
7. Add and remove semantic corrections; verify full reanalysis and preserved prior state after cancellation.
8. Change an analysis setting and render; verify analysis is recomputed instead of using the previous cache key.
9. Render repeatedly without changing analysis inputs; verify the adopted derived maps are reused.
10. Close during analysis; verify cancellation and no late result adoption.

## Exit criteria

- `MainWindow` no longer calls the three analyzers or map composers directly;
- the complete derived-map pipeline is testable without Avalonia;
- analysis outputs remain detached until successful adoption;
- cache keys include source identity, dimensions, settings and workspace revisions;
- stale, failed and cancelled results cannot replace active maps;
- manual-region recomposition reuses valid automatic maps;
- project schema remains 11 and preset schema remains 8;
- build succeeds with zero warnings/errors;
- all **804** tests pass;
- the manual checklist passes.
