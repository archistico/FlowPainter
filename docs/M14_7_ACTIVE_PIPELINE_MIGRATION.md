# M14.7 — Active regional-pipeline migration

**Status:** DONE — validated with 998 tests  
**Baseline:** M14.6 validated with 964 tests  
**Validated suite:** 998 tests

## Purpose

M14.7 makes the deterministic SLIC result the active automatic regional representation used by every FlowPainter generative mode. It retires automatic semantic recognition from planning without deleting the M8–M13.3 compatibility code or breaking schema-11 projects.

The milestone does not add segmentation controls or persist new SLIC settings. Those responsibilities remain in M14.8.

## Active analysis path

The Application-level `AnalysisCoordinator` now executes:

1. structural detail analysis;
2. deterministic SLIC segmentation;
3. connectivity repair, regional descriptors, RAG and hierarchy construction;
4. migration of persisted schema-11 semantic corrections into generalized region-role overrides;
5. composition of regional saliency, protection, focus, importance, explicit background and ignore maps;
6. region-aware scene-boundary analysis;
7. fixed-policy structural/regional detail composition;
8. manual detail-region composition;
9. background suppression and artistic-detail publication.

`ISemanticImportanceAnalyzer` is no longer injected into or called by the active coordinator. Its implementation and tests remain available only as historical compatibility code.

## Generalized region roles

M14.7 introduces:

- `RegionRole`;
- `RegionRoleOverride`;
- `LegacySemanticCorrectionAdapter`.

The runtime migration is deterministic:

| Persisted schema-11 correction | Active regional role |
|---|---|
| `ForcePrimarySubject` | `Focal` |
| `ForceSubject` | `Subject` |
| `ForceBackground` | `Background` |
| `IgnoreAutomaticDetection` | `Ignore` |

Identifiers, bounds, labels and old source-region identifiers are retained. At the validated M14.7 baseline, project schema remained 11 and preset schema remained 8. M14.8 subsequently advances those schemas and persists generalized role overrides directly; the M14.7 runtime conversion remains the compatibility fallback for older documents.

## Regional structure composition

`RegionalStructureAnalysisComposer` derives automatic evidence only from:

- the structural detail map;
- fine-region descriptors;
- RAG boundary strengths;
- hierarchy scale/specificity;
- manual region-role overrides.

It does not classify objects, people or subjects and does not rank a primary subject.

Published maps are:

- structural saliency;
- protection;
- shared-boundary evidence;
- focus;
- importance;
- explicit background role;
- ignore influence.

Fine RAG boundaries are rasterized with their normalized strength. Stronger regional boundaries therefore reach the validated M11 boundary analyzer through `RegionalSceneBoundaryAnalyzerAdapter` without model-backed or class-aware evidence.

## Compatibility envelope

The desktop application still exposes some M8/M13 controls and overlay types until M14.8. `RegionalSemanticCompatibilityAdapter` provides a read-only compatibility envelope for those legacy consumers:

- regional saliency → legacy saliency map;
- regional protection → legacy subject map;
- RAG boundary evidence → legacy silhouette map;
- regional focus → legacy focal map;
- regional importance → legacy importance map.

This adapter is not automatic semantic recognition. It contains no inferred class label, subject kind or primary-subject detector. Compatibility regions are created only from intentional manual role overrides and use `Unknown` subject kind.

## Cache identity

`AnalysisCacheKey` now includes:

- SLIC target region size;
- compactness;
- pre-blur sigma;
- iteration and convergence settings;
- intermediate/broad merge ratios;
- merge-cost thresholds;
- strong-boundary and maximum-parent-area policies.

Legacy semantic-analysis settings are deliberately excluded because they no longer affect active analysis or generated plans. Their persisted values remain readable until M14.8 removes or relabels the old controls.

## Rendering-mode consistency

Flow, Primitive and Hybrid already consume the shared effective detail field and boundary result. Because both are now produced by the regional path, all three modes switch together. No mode retains an alternate automatic semantic route.

Manual detail regions remain a late composition layer and `RecomposeAsync` reuses accepted SLIC, regional and boundary results when only those regions change.

## Automated validation

M14.7 adds 34 tests:

- 4 Domain tests for generalized role overrides;
- 6 migration-adapter cases covering all schema-11 correction kinds and snapshot semantics;
- 4 regional-result invariant cases;
- 10 regional-composer cases for RAG boundaries, structural focus, role precedence, soft transitions, determinism and validation;
- 3 compatibility-envelope cases;
- 3 fixed regional-detail composition cases;
- 2 regional-boundary adapter cases;
- 2 additional coordinator/cache cases proving semantic-setting retirement and SLIC/merge identity.

Existing coordinator tests now verify detached segmentation, regional analysis, region-aware boundaries and recomposition reuse.

## Manual validation

1. Open an existing schema-11 project containing subject/background corrections.
2. Reanalyze and verify that the project opens without migration prompts or data loss.
3. Confirm that subject/focal corrections remain protected and background corrections remain simplified.
4. Change legacy semantic sliders and verify that the generated plan does not change.
5. Change structural or boundary controls and verify that analysis invalidates normally.
6. Render Flow, Primitive and Hybrid previews and confirm that all use the same regional analysis result.
7. Confirm that analysis progress reports SLIC segmentation and regional roles rather than automatic semantic recognition.

## Exit criteria

- build completes with zero warnings and zero errors;
- all 998 tests pass;
- `AnalysisCoordinator` has no automatic semantic-analyzer dependency;
- the accepted result contains SLIC labels, descriptors, RAG, hierarchy and regional role maps;
- scene-boundary and background composition are invoked from regional evidence;
- legacy semantic settings do not alter cache identity or new plans;
- schema-11 manual intent survives deterministic runtime migration;
- the M14.7 package did not advance project or preset schemas prematurely.
