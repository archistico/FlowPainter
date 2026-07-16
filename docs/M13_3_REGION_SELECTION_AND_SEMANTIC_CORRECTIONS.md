# M13.3 — Region selection and semantic corrections

**Status:** DONE — validated baseline  
**Prepared:** 2026-07-14  
**Validated:** 2026-07-16

## Purpose

M13.3 gives the user direct control over regions displayed on the source image and provides a reversible way to correct wrong automatic subject detections.

The milestone separates two concepts that must not be conflated:

- a `DetailRegion` changes how much painterly detail is requested locally;
- a `SemanticCorrectionRegion` changes how the automatic semantic maps are interpreted.

A region can therefore be the main subject without automatically becoming a maximum-detail rectangle, and a detail rectangle does not automatically become a semantic subject.

## Direct selection

The source viewport now distinguishes a click from a drag in display pixels:

```text
movement below 6 px  -> select an existing overlay
movement of 6 px+    -> create a manual detail region
```

Selection priority at the clicked point is deterministic:

1. manual detail regions, latest first;
2. semantic corrections, latest first;
3. automatic semantic regions, smallest containing region first.

Repeated clicks cycle through overlapping regions of the same category. The selected overlay is drawn with a thicker, more visible border and the corresponding list item is selected.

`Delete` removes the selected manual detail region or semantic correction, except while a text box is being edited or another operation is active. Existing list buttons remain available.

## Semantic correction model

`SemanticCorrectionRegion` is an immutable Domain value containing:

- stable identifier;
- normalized rectangular bounds;
- correction kind;
- optional label;
- optional source automatic-region identifier.

Supported correction kinds are:

| Kind | Effect |
|---|---|
| `ForcePrimarySubject` | promotes subject, focal and importance maps; highest precedence |
| `ForceSubject` | promotes subject and importance without creating a focal area |
| `ForceBackground` | suppresses saliency, subject, silhouette, focal and importance |
| `IgnoreAutomaticDetection` | removes semantic subject/focal influence while preserving raw saliency |

Only one correction is retained for each automatic source-region identifier. Choosing a different role updates that correction in place instead of stacking contradictory instructions. Only one `ForcePrimarySubject` correction may exist globally; adding another automatically demotes the previous primary correction to `ForceSubject` instead of deleting it.

## Non-destructive composition

The automatic analyzer output is retained as the source result. `SemanticCorrectionComposer` creates corrected immutable maps before boundary analysis and detail-map composition:

```text
automatic semantic maps
        + persisted semantic corrections
        + RegionTransitionWidth
        ↓
corrected semantic maps
        ↓
scene-boundary analysis
        ↓
automatic detail + background suppression
        ↓
Flow / Primitive / Hybrid planning
```

The analyzer's detected-region list is preserved for inspection. Manual correction overlays remain separate, so a correction can be selected, deleted and recomputed without losing the original detection.

## Soft correction borders

Semantic corrections reuse the M13.2 transition radius:

```text
DetailInfluenceSettings.RegionTransitionWidth
```

Each correction receives:

- a full-strength core;
- 50% influence at its geometric border;
- `SmoothStep` transition inside and outside;
- Euclidean exterior distance around corners;
- maximum merging among corrections of the same kind.

This prevents a semantic correction from introducing the same rectangular seam that M13.2 removed from manual detail regions.

## Precedence

Local correction precedence is explicit and independent of list order:

```text
ForceSubject
    ↓
ForceBackground / IgnoreAutomaticDetection
    ↓
ForcePrimarySubject
```

Consequences:

- background or ignore can cancel an ordinary forced subject;
- a forced primary subject wins over an overlapping background/ignore correction;
- same-kind overlaps never accumulate above full influence.

## UI workflow

1. Run semantic analysis.
2. Click an automatic semantic rectangle in the source viewport or select it from the detected-region list.
3. Choose **Set primary subject**, **Mark as subject**, **Mark as background** or **Ignore detection**.
4. FlowPainter stores the correction, reruns semantic/boundary/detail composition and invalidates the rendered plan.
5. Select a correction from the source overlay or correction list and use **Delete selected**, `Delete` or **Clear corrections** to reverse it.
6. Save the project to preserve all corrections.

The existing **Promote to focus** and **Critical detail** commands continue to create manual positive detail regions. They are deliberately separate from semantic correction commands.

## Persistence

Project schema advances from **10** to **11** and stores `semanticCorrections` with the image-specific project state.

Schema-1 through schema-10 projects remain readable. A schema-10 document without the new collection receives an empty correction list. Preset schema remains **8** because semantic corrections belong to a source image and must not be copied into reusable painter settings.

## Automated coverage

M13.3 adds 35 cases, increasing the milestone suite from **713** to **748**. Seven audit-remediation Application cases were added on 2026-07-16, bringing the validated repository baseline to **755**. Coverage includes:

- semantic-correction value validation and normalization;
- editor identifiers, per-source replacement, read-only state and primary-subject uniqueness;
- hit-testing priority, cycling and outside clicks;
- all four correction-map transformations;
- soft correction transitions and precedence;
- project collection immutability and validation;
- schema-11 round trip and schema-10 compatibility;
- workspace dirty state, source reset, project creation and loading.

## Manual validation

1. Open an image containing at least one automatic semantic rectangle.
2. Click without dragging and verify that the rectangle/list selection changes without creating a detail region.
3. Drag more than 6 pixels and verify that a detail region is created.
4. Select overlapping rectangles repeatedly and verify deterministic cycling.
5. Select a detected false subject and choose **Mark as background** or **Ignore detection**.
6. Verify that the semantic/detail overlays are recomputed and that the correction border blends gradually.
7. Select the intended subject and choose **Set primary subject**.
8. Set a second primary subject and verify that only one correction remains primary.
9. Delete a selected correction with `Delete`, then test **Clear corrections**.
10. Save and reopen the project and verify that corrections and their effects are retained.
11. Open a schema-10 project and verify that it loads with zero semantic corrections.

## Intentional limits

M13.3 intentionally does not:

- provide freehand or polygonal masks;
- expose per-correction transition widths;
- add detail-specific segment, curvature and boundary-alignment multipliers;
- guarantee that detailed marks are painted after all broad base marks.

The former plan to improve heuristic primary-subject ranking or add model-backed providers has been withdrawn. M13.3 remains the schema-11 compatibility baseline; its manual decisions will later migrate to generalized region-role overrides.

## Roadmap supersession

ADR-0017 changes the future automatic-analysis direction to deterministic SLIC regional segmentation. The current semantic analyzer and correction pipeline remain operational until M14.7, when:

- SLIC regions become the active automatic partition;
- no automatic class label or primary-subject recognizer is required;
- manual primary-subject, subject, background and ignore decisions are preserved as generalized region roles;
- schema-1 through schema-11 projects remain readable;
- the derived automatic semantic-region list is not treated as durable user intent.
