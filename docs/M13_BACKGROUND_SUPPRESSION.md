# M13 — Background suppression and painterly simplification

## Purpose

M13 introduces the negative counterpart to subject/detail recognition. A scene is no longer represented only by “more detail here”; confident background can receive an explicit, bounded simplification signal while subjects, important contours and uncertain transitions remain protected.

## Data model

`BackgroundSuppressionComposer` creates four immutable maps:

- `ProtectionMap`: confidence that detail must be preserved;
- `SuppressionMap`: confidence and strength of allowed simplification;
- `EffectiveDetailMap`: normalized map consumed by existing detail-aware engines;
- `ArtisticDetailField`: signed `[-1,+1]` policy consumed by FlowPainter.

Priority is manual detail increase, semantic subject/importance, silhouette, uncertainty and finally background confidence. Automatic suppression cannot silently override explicit user focus.

## Painterly policy

As local suppression increases, FlowPainter:

- allocates fewer stroke origins;
- increases stroke length and width;
- reduces the number of path segments;
- permits more curvature freedom;
- applies deterministic colour simplification.

The detail floor prevents empty or cut-out background. Transition smoothing avoids a technical seam around protected forms.

Primitive mode consumes the effective detail map so background forms become broader and less exact. Hybrid mode uses one immutable suppression result for primitive masses, flow and refinement.

## Persistence

Project schema 9 and preset schema 7 persist all settings. Older schemas remain readable and default to disabled suppression.

## Intentional limits

M13 does not yet provide freehand mask painting, local primitive-budget editing or the complete visual hierarchy budget. Those remain M14 and M15.

## M13.1 validation correction

The first executable Windows run exposed one exact IEEE 754 comparison in `BackgroundSuppressionSettingsTests`. The test now compares all derived painterly multipliers at 12 decimal digits, following the project-wide floating-point policy. No production behavior changed.

## M13.2 soft manual-region transitions

M13.2 removes a separate seam source that remained visible when manual detail rectangles caused strong local policy differences. `DetailMapComposer` now converts each rectangle into a SmoothStep feather field with a full-strength core and a transition both inside and outside its border. The transition radius is persisted in `DetailInfluenceSettings.RegionTransitionWidth` and defaults to 5% of the shorter analysis-map dimension.

Same-intent overlaps merge by maximum influence so overlapping rectangles do not create artificial peaks. Project schema advances to 10 and preset schema to 8; previous files receive the default transition. See [`M13_2_SOFT_DETAIL_REGIONS.md`](M13_2_SOFT_DETAIL_REGIONS.md) and ADR-0015.


## M13.3 semantic corrections before suppression

M13.3 applies persistent semantic corrections before scene-boundary analysis and before `BackgroundSuppressionComposer` builds protection. A forced subject therefore receives semantic protection, while a false detection marked as background or ignored no longer protects that area from painterly simplification. A forced primary subject has the highest local correction precedence.

Corrections reuse the same soft transition radius as manual detail regions, are stored only in project schema 11 and remain separate from reusable preset settings. See [`M13_3_REGION_SELECTION_AND_SEMANTIC_CORRECTIONS.md`](M13_3_REGION_SELECTION_AND_SEMANTIC_CORRECTIONS.md) and ADR-0016.
