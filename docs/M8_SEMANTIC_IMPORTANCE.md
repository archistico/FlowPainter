# M8 — Semantic importance and generic subject analysis

## Status

DONE — validated historical milestone with 496 passing tests.

**Future direction:** superseded for new automatic segmentation by ADR-0017 and the M14 SLIC roadmap. The implementation remains supported as part of the current M8–M13.3/schema-11 compatibility baseline until M14.7.

## Purpose

M8 adds the first subject-aware importance layer. The goal is not to classify every object or facial landmark yet. The built-in analyzer identifies visually salient subject-like regions, their silhouettes and internal focal points, then combines those signals with the existing structural detail map.

The resulting pipeline is:

```text
Structural edges and contrast
        +
Generic saliency and subject regions
        +
Subject silhouettes and focal points
        +
Manual increase/reduce regions
        ↓
Composed detail map
        ↓
Detail-aware stroke placement, length and width
```

This is the first step toward the project’s visual hierarchy: broad and painterly backgrounds, recognizable subjects, stronger focal areas and protected critical details.

## Built-in analyzer

`HeuristicSemanticImportanceAnalyzer` is local, deterministic and model-free. It operates on the selected analysis proxy and produces five normalized maps:

- saliency;
- generic subject occupancy;
- subject silhouettes;
- focal areas;
- combined semantic importance.

The implementation uses global/local colour contrast, luminance gradients, configurable centre bias, connected-component segmentation and focal peaks. It does not claim class labels such as person, animal or vehicle. Detected regions are intentionally labelled as generic subjects.

## Provider boundary — historical decision

`ISemanticImportanceAnalyzer` isolated the rest of the application from the M8 implementation and allowed M8–M13.3 to evolve without coupling Domain or renderers to the heuristic analyzer.

The provider boundary remains in the validated code for compatibility, but new class-aware, ONNX or model-backed providers are no longer planned. No machine-learning runtime or model file is bundled, and ADR-0017 explicitly selects deterministic SLIC regional segmentation for future development.

## Semantic regions

The Domain layer introduces normalized semantic regions with:

- stable identifier;
- bounds;
- confidence;
- artistic importance;
- role: background, supporting area, subject, focal area, critical detail or ignore;
- optional subject kind and provider metadata.

Detected subject and focal regions can be promoted to normal manual detail regions. Once promoted, the user can edit, reorder, persist or remove them using the existing M5 workflow.

## Desktop workflow

The configuration panel adds:

- semantic analysis enable/disable;
- overall influence;
- separate saliency, subject, silhouette and focal weights;
- subject threshold and minimum area;
- maximum subject count;
- centre bias;
- semantic smoothing and silhouette radius;
- selectable diagnostic overlay;
- detected-region list;
- promote-to-focus and critical-detail commands.

The overlay selector can display the final combined detail map or one semantic contribution at a time.

## Persistence

Project and preset schemas move to version 4. Semantic settings are serialized with all other FlowPainter settings. Schema versions 1–3 remain readable and receive explicit M8 defaults.

Detected regions themselves are analysis output and are not persisted. Promoted manual regions are persisted normally because they represent an intentional artistic decision tied to the source image.

## M13.3 evolution: persistent semantic corrections

M13.3 keeps M8 analyzer output immutable and adds a separate project-level correction layer. The user can select a detected rectangle and force it to primary subject, subject, background or ignored-detection status. Corrections are applied to copied semantic maps before scene-boundary analysis and background suppression, while the original detected-region list remains inspectable.

Semantic corrections are not painterly detail regions: they express scene role, not requested mark density. They use soft rectangular influence and are persisted in project schema 11, while detected analyzer output remains derived state. See [`M13_3_REGION_SELECTION_AND_SEMANTIC_CORRECTIONS.md`](M13_3_REGION_SELECTION_AND_SEMANTIC_CORRECTIONS.md) and ADR-0016.

## Deliberate limitations

M8 does not yet provide:

- reliable class-specific labels;
- complete instance masks from a trained model;
- eyes, mouth, hands or pose landmarks;
- non-rectangular editing masks;
- primitive generation.

These limitations are accepted as historical characteristics. They will not be addressed by adding a trained semantic provider; M14 instead replaces the active automatic path with class-agnostic SLIC regions, graph structure and manual artistic roles.

## M14 migration policy

M8 output is derived automatic evidence, not durable user intent. During M14.7:

- the active automatic contribution is replaced by SLIC regional segmentation;
- semantic settings remain readable for older projects but no longer drive new planning;
- M13.3 manual corrections are converted to generalized region-role overrides;
- existing generated plans are still derived and are rebuilt from the current pipeline;
- this document remains an accurate record of the implemented historical milestone.

## Validation

M8 adds tests for:

- semantic settings and progress validation;
- generic-subject detection on synthetic images;
- empty output for uniform or disabled analysis;
- deterministic maps and regions;
- subject-count limits;
- saliency, silhouette and focal-map separation;
- structural/semantic map composition;
- cancellation;
- semantic-region invariants;
- project and preset schema-4 round trips;
- schema-3 migration defaults.

Expected suite: 496 automated cases.

## M8.1 analyzer compatibility correction

The generic object category is named `SceneObject` rather than `Object`. This avoids the .NET `CA1720` type-name collision while preserving enum value `3` and the intended future provider contract.
## M8.2 xUnit analyzer correction

The subject-count regression test now uses the predicate overload of `Assert.Single`, as required by xUnit analyzer rule `xUnit2031`. This is a test-only compatibility correction and does not change semantic analysis behavior or the expected total of 496 cases.
