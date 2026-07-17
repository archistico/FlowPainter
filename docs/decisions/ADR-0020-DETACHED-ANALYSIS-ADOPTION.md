# ADR-0020 — Detached analysis and transactional adoption

## Status

Accepted and validated by M13.4.4 with 804 tests. The active analysis contents were subsequently extended by M14.7, while the detached/adoption transaction remains authoritative.

## Context

The desktop window previously invoked structural, semantic and boundary analyzers directly, composed every derived map, tracked cache validity through unrelated mutable fields and immediately replaced active maps after each operation.

That concentration made cancellation, failure and stale-result behaviour difficult to test outside Avalonia. SLIC will introduce additional long-running stages and larger derived resources, so the lifecycle must be explicit before M14.

## Decision

FlowPainter uses one Application-level `AnalysisCoordinator`.

A request contains an immutable cache key, source, settings, manual regions and semantic corrections. Execution produces a detached `PendingAnalysis`; it does not mutate current session state.

Each request receives a monotonically increasing generation. Adoption succeeds only when the pending generation is the latest generation and its key matches the key expected by the caller.

The caller supplies a synchronous adoption callback. The coordinator publishes `CurrentKey` and `CurrentResult` only after that callback completes successfully. If the callback throws, the previous current result is preserved.

The desktop shell remains responsible for Skia/Avalonia resources. It renders temporary overlays before adoption and replaces native resources inside the adoption callback.

## Cache-key contents

The current key includes:

- per-source identity;
- proxy size;
- detail-region revision;
- semantic-correction revision;
- all settings that affect structural, semantic, boundary, transition and background-analysis output.

The key excludes renderer-only settings.

## Recomposition

The coordinator supports a reduced recomposition path that reuses an accepted structural/semantic/boundary basis and recomputes manual detail regions plus background suppression. Full analysis is required when the basis-affecting inputs change.

## Alternatives considered

### Keep the lifecycle in `MainWindow`

Rejected. It leaves cancellation, cache invalidation and stale-result behaviour coupled to Avalonia and untested.

### Put Skia images and Avalonia bitmaps inside the coordinator

Rejected. Application must remain independent of native rendering and UI-framework ownership.

### Publish each stage incrementally

Rejected. Partial publication would allow a failed later stage to leave structural, semantic, boundary and detail maps from different requests active together.

### Cancel the previous task and assume it cannot complete

Rejected. Cancellation is cooperative. A previous task may complete after a newer request and must still be rejected by generation/key validation.

### Hash only the settings object references

Rejected. Settings are immutable but independently reconstructed from UI/project data. Cache identity must be value-based.

## Consequences

Positive:

- analysis lifecycle is covered in Application tests;
- failed/cancelled analysis preserves the active result;
- older results cannot overwrite newer requests;
- project/image/proxy adoption uses one transaction boundary;
- manual region edits can recompose without rerunning expensive analyzers;
- M14 can add SLIC stages and regional resources behind the same contract.

Costs:

- the desktop shell still translates controls into an `AnalysisRequest`;
- a source identity must be maintained for the active session;
- cache-key construction must be updated whenever an analysis-affecting setting is added;
- adoption callbacks are synchronous and must remain short and non-reentrant;
- old semantic stages remain present until M14.7 replaces their active contribution.

These costs are accepted because they create a deterministic, testable lifecycle without a framework-wide UI rewrite.
