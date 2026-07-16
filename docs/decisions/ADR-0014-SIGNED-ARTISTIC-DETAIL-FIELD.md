# ADR-0014 — Signed artistic-detail field

## Status

Accepted for M13.

## Context

A normalized detail map can express importance but cannot distinguish neutral low detail from deliberate background suppression. Inverting subject confidence is unsafe because partially recognized silhouettes, hair, hands, associated objects and uncertain transitions may be damaged.

## Decision

Introduce a signed immutable `ArtisticDetailField` in Domain and compose it in Application from independent automatic detail, manual detail, semantic evidence, scene-boundary background confidence and uncertainty. Keep diagnostic suppression/protection maps and a normalized effective map alongside the signed field.

Protection priority is explicit: manual increases, subject/importance, silhouette and uncertainty precede background suppression. The M11 analyzer is not mutated. Flow planning interprets the negative sign; primitive and hybrid components reuse the normalized effective detail map.

## Consequences

- disabled suppression preserves the validated M12 path and random sequence;
- background treatment becomes measurable and deterministic;
- UI can inspect why an area is protected or suppressed;
- later freehand masks can override the same composer without changing planners;
- renderers remain ignorant of semantic policy;
- four maps require additional proxy-sized memory, bounded by the existing analysis proxy rather than final 10,000-pixel output.
