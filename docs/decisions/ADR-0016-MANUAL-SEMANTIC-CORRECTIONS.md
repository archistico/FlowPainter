# ADR-0016 — Non-destructive manual semantic corrections

**Status:** Accepted  
**Date:** 2026-07-14

## Context

The built-in semantic analyzer is intentionally generic and heuristic. It may rank a contrast-rich background component above the intended subject. Tuning thresholds alone cannot guarantee the correct artistic hierarchy for every image, so the user needs a persistent correction mechanism.

Using `DetailRegion` for this purpose would merge two independent decisions: semantic role and desired painterly detail. Deleting automatic detections from analyzer output would also be destructive, difficult to diagnose and hard to reverse.

## Decision

FlowPainter stores semantic corrections as separate immutable `SemanticCorrectionRegion` values in the project.

- Corrections use normalized rectangles and stable identifiers.
- The supported intentions are primary subject, subject, background and ignored detection.
- Only one correction is retained per automatic source-region identifier; selecting a new role replaces the existing correction in place.
- Only one forced primary subject is allowed; adding a new one demotes the previous primary to a normal subject.
- Automatic detected regions remain unchanged and inspectable.
- `SemanticCorrectionComposer` applies corrections to copied semantic maps before scene-boundary analysis.
- Corrections use the same soft transition width as manual detail regions.
- Project schema 11 persists the correction collection; presets do not.

## Consequences

Positive:

- wrong subject detections can be corrected immediately without waiting for a stronger analyzer;
- the original automatic result remains visible for diagnostics;
- corrections are reversible, deterministic and project-specific;
- semantic role remains independent from local detail strength;
- corrected maps influence boundaries, background suppression and all three generative engines consistently;
- soft masks prevent correction rectangles from appearing in the painting.

Costs:

- every correction change requires semantic, boundary and detail recomposition;
- rectangular masks cannot precisely isolate irregular shapes;
- the project schema gains another image-specific collection;
- overlapping corrections require a documented precedence rule.

## Precedence rule

Corrections are composed by kind rather than by arbitrary UI list order:

```text
ordinary subject promotion
→ background / ignore suppression
→ primary-subject promotion
```

Same-kind overlaps use maximum influence. This keeps results stable when correction order changes and ensures the explicit primary-subject command remains authoritative.

## Rejected alternatives

### Reuse detail regions

Rejected because semantic identity and painterly detail are orthogonal. A subject may need broad treatment, while a non-subject texture may intentionally receive high detail.

### Delete automatic regions

Rejected because analyzer output should remain reproducible and inspectable. A manual correction is a separate user decision, not a mutation of provider evidence.

### Store corrections in presets

Rejected because regions refer to the geometry and detections of one source image. Reusing them on another image would be meaningless or harmful.

### Hard correction masks

Rejected because abrupt semantic-map changes can propagate to density, boundaries and background suppression, recreating visible rectangular seams.

### Allow multiple primary subjects

Rejected for this milestone because downstream hierarchy currently expects one authoritative main subject. Additional important subjects remain representable with `ForceSubject`.
