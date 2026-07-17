# M16 — Advanced regional editing

**Status:** PLANNED  
**Dependency:** stable M15 artistic hierarchy and rendering contracts

## Persistence principle

Derived SLIC labels are recomputable and should not be duplicated wholesale in project files. Persist user intent as compact source-relative operations, role overrides, masks, barriers and stable region fingerprints. On load, rebuild the automatic segmentation and rebind edits deterministically; unresolved edits remain visible for user repair rather than being silently discarded.

## M16.1 — Hierarchy-aware selection

- click/select fine regions and cycle through ancestors;
- show descriptors, neighbours, hierarchy level and effective artistic role;
- preserve synchronized source/result navigation;
- no topology mutation yet.

## M16.2 — Role and topology overrides

- merge selected adjacent regions non-destructively;
- exclude a region from automatic artistic promotion;
- assign Background, Supporting, Protected, Focal or Critical roles;
- migrate remaining legacy correction terminology behind compatibility adapters.

## M16.3 — Local split and resegmentation

- split by user stroke/markers or bounded local SLIC;
- keep unrelated labels and hierarchy branches unchanged;
- rebuild descriptors, local RAG edges and affected ancestors only;
- enforce memory/work admission before local analysis.

## M16.4 — Freehand masks and barriers

- brush Increase detail / Reduce detail;
- polygonal and freehand protected masks;
- manual barriers with strength and transition width;
- false-boundary erasing as an override, never destructive source editing;
- continuous feathering compatible with M13.2 principles.

## M16.5 — Command history and partial regeneration

- command-based undo/redo with reversible payloads;
- dirty tracking and atomic persistence;
- precise invalidation of segmentation, hierarchy, analysis or plan stages;
- locked regions and partial regeneration;
- before/after comparison using accepted plan snapshots.

## Validation strategy

Core edit behaviour belongs in Domain/Application command tests. Avalonia tests remain limited to wiring, hit-testing and visual-state smoke checks. Required properties include reversibility, deterministic rebind, no loss of unresolved edits, bounded invalidation and schema migration.
