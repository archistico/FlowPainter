# ADR-0009 — Semantic analysis provider boundary

## Status

Accepted for M8–M13.3; **superseded for future development by ADR-0017**.

## Context

FlowPainter must allocate more artistic detail to complete subjects, silhouettes, focal areas and critical details, not only to high-frequency texture. The project will eventually benefit from trained local models, but model size, runtime packaging, supported classes, licensing and hardware behaviour must not become dependencies of the core planner.

## Decision

Define semantic analysis through `ISemanticImportanceAnalyzer` in Application and normalized `SemanticRegion` values in Domain.

M8 ships a deterministic model-free provider that produces saliency, subject, silhouette, focal and combined importance maps. It is useful immediately and establishes the full integration path without adding a machine-learning runtime.

At the time of M8, class-aware or model-backed providers were permitted as a future extension and were required to:

- remain local/offline by default;
- consume `IRgbaPixelSource` or an explicitly versioned model input;
- return the same `SemanticAnalysisResult` contract;
- document model provenance, licence and expected input normalization;
- own and dispose native inference resources outside Domain;
- preserve manual override and provider-independent project semantics.

## Consequences

- Domain and Application remain independent of ONNX Runtime and model files.
- The UI, detail-map composer and planner do not depend on a specific detector.
- M8 can improve paintings immediately with generic subject hierarchy.
- Exact person/animal/object recognition is deferred and must not be implied by the heuristic provider.
- The boundary successfully prevented a model dependency from entering Domain or rendering.
- The project has since decided not to add a model provider. ADR-0017 selects deterministic SLIC regional segmentation instead.
- Existing M8–M13.3 code and project schemas remain supported until the active-path migration in M14.7.
