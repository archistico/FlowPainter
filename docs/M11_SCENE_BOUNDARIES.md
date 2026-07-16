# M11 — Scene separation and important boundaries

**Status:** DONE — diagnostic baseline used by M12  
**Expected automated suite:** 631 cases

## Purpose

M11 identifies the contours that make subjects, figures and objects recognizable. It does not yet force the brush planner to follow those contours; instead, it produces independent diagnostic maps and a tangent direction field that can be inspected and validated before M12 changes stroke behaviour.

This separation is intentional:

```text
M11: Where is the boundary, how important is it, and which way does it run?
M12: How should a stroke align, deflect, shorten or terminate near it?
M13: Which background areas may then be simplified safely?
```

## Artistic rationale

A painting may simplify much of the source while remaining recognizable if the principal separations are preserved. The most important boundaries are normally:

1. subject/background silhouettes;
2. boundaries between different subjects or objects;
3. internal structural edges that define form;
4. texture edges;
5. isolated microcontrast and noise.

The system therefore must not treat every pixel gradient equally. A coherent silhouette should outrank grass texture, fabric noise or tiny colour fluctuations in the background.

## Analysis output

`SceneBoundaryAnalysisResult` contains:

| Output | Meaning |
|---|---|
| `EdgeStrengthMap` | Raw normalized local luminance/colour discontinuity |
| `EdgeImportanceMap` | Multiscale, continuity- and semantic-weighted structural importance |
| `SubjectBoundaryMap` | Boundaries supported by the semantic silhouette |
| `InternalStructureMap` | Important edges inside a detected subject |
| `TextureEdgeMap` | Fine high-frequency edges likely to represent texture |
| `BackgroundConfidenceMap` | Confidence that an area is low-importance background |
| `UncertaintyMap` | Areas that should not yet be treated confidently as subject or background |
| `BoundaryDirectionField` | Unit tangent direction along the estimated contour |

The tangent is deliberately stored instead of only the gradient normal:

```text
normal  → crosses the edge
tangent → follows the edge
```

M12 uses the tangent to orient painterly flow along significant contours.

## Built-in analyzer

`HeuristicSceneBoundaryAnalyzer` is deterministic and local. It combines:

- luminance gradients;
- RGB colour gradients;
- fine and coarse analysis radii;
- persistence across scales;
- local directional continuity;
- semantic subject and silhouette confidence from M8;
- suppression of fine texture;
- distance from subjects;
- a configurable boundary-protection radius;
- optional smoothing of scalar output maps.

The provider identifier is:

```text
heuristic-scene-boundaries-v1
```

Future model-backed or class-aware providers can implement `ISceneBoundaryAnalyzer` without changing Domain, planners or rendering adapters.

## Background confidence

Background confidence is not calculated as a simple inverse of subject confidence. Such an inversion would incorrectly classify missed hair, hands, thin objects, soft silhouettes and associated objects as disposable background.

The built-in provider combines:

- low subject confidence;
- low focal confidence;
- reduced saliency;
- distance from detected subjects;
- absence of important structural edges.

A protected transition band lowers background confidence around the subject. The separate uncertainty map exposes areas that require conservative treatment or future manual correction.

## User interface

The analysis overlay selector now includes:

- combined detail;
- semantic importance;
- saliency;
- subjects;
- silhouettes;
- focal areas;
- edge strength;
- important edges;
- subject boundaries;
- internal structure;
- texture edges;
- background confidence;
- uncertainty;
- edge directions.

The edge-direction overlay draws sampled cyan tangent segments over the source. It is diagnostic only and follows the same synchronized source/result zoom and pan.

The Scene boundaries settings section exposes:

- luminance weight;
- colour weight;
- multiscale weight;
- continuity weight;
- semantic silhouette weight;
- texture suppression;
- general edge threshold;
- important-edge threshold;
- coarse radius;
- smoothing radius;
- subject-boundary protection radius.

## Persistence

M11 updates:

- project schema from 6 to 7;
- flow-preset schema from 4 to 5.

Earlier projects and presets remain readable and receive explicit default `SceneBoundaryAnalysisSettings`.

The generated maps and direction field are not persisted. They are reproducible derived data and are rebuilt from:

- source image;
- preview resolution;
- semantic settings;
- boundary settings.

## Deliberate limitations

M11 does **not** yet:

- redirect strokes along the tangent;
- prevent silhouette crossings;
- terminate a stroke at a boundary;
- sample colour separately on each side;
- reduce the background rendering budget;
- persist manual boundary edits.

These belong to M12–M14 and are documented in the living roadmap.

## Automated validation

The expected 631 cases include tests for:

- vector and field invariants;
- horizontal and vertical tangent estimation;
- equal-luminance chromatic boundaries;
- semantic silhouette promotion;
- internal-structure separation;
- texture classification;
- background confidence and subject protection;
- deterministic analysis;
- progress and cancellation;
- diagnostic overlay rendering;
- schema migration and round trips.

## Manual validation checklist

1. Open a portrait or a figure on a distinct background.
2. Inspect `ImportantEdges` and verify that the outer silhouette is stronger than minor background texture.
3. Inspect `SubjectBoundaries` and `InternalStructure` independently.
4. Select `EdgeDirections` and verify that cyan segments follow rather than cross visible contours.
5. Inspect `BackgroundConfidence`; the central subject and its immediate border band should remain protected.
6. Inspect `Uncertainty`; soft or ambiguous boundaries should not be classified aggressively as background.
7. Change multiscale, continuity and texture-suppression settings and reanalyze.
8. Save/reopen a project and preset and verify every boundary parameter.
9. Confirm synchronized zoom and pan for all overlays.
10. Confirm that generated Flow, Primitive and Hybrid images are unchanged by selecting a different diagnostic overlay.
## M11.1 compatibility correction

The boundary-direction overlay uses the SkiaSharp 4 `DrawImage` overload with explicit `SKSamplingOptions`. This preserves the original full-size source composition while avoiding the obsolete paint-based overload.

