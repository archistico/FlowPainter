# M15.2 — High-detail local stroke policy

**Status:** DONE — validated with 1,071 tests  
**Baseline:** M15.1 validated with 1,049 tests  
**Validated suite:** 1,071 tests

## Objective

Use the continuous detail and regional-boundary fields to vary local stroke geometry without hard regional seams. Detailed areas receive shorter, thinner marks, more local segments, controlled curve freedom, stronger tangent alignment and greater resistance to crossing important boundaries.

## Policy

`HighDetailStrokePolicy` evaluates immutable `DetailInfluenceSettings` and returns continuous local geometry:

- length multiplier;
- width multiplier;
- segment-count multiplier;
- curve-freedom multiplier.

Boundary response is evaluated separately at each sampled point. Zero detail preserves the validated M12/M15.1 alignment and crossing behaviour. Increasing detail applies a SmoothStep-weighted boost using continuous scene/regional boundary evidence.

## Planner integration

- no-detail plans retain the original `flow-field-v1` sequence;
- detail, boundary, regional-boundary and background planner identities advance to version 2;
- local segment counts are rounded deterministically and capped at the supported maximum;
- length and segment changes combine to produce shorter local path increments in detailed areas;
- curve freedom remains bounded by one full turn;
- tangent and crossing boosts are clamped to the unit interval;
- Flow and both Hybrid stroke layers use the same policy.

## Settings and persistence

The Detail influence panel adds:

- detailed/background segment multipliers;
- detailed/background curve multipliers;
- detailed tangent-alignment boost;
- detailed crossing-resistance boost.

Project schema 13 and preset schema 10 persist these reusable values. Projects through schema 12 and presets through schema 9 remain readable and receive the painterly defaults.

## Work admission

`GenerationWorkEstimator` now reserves the largest possible local segment count from the detailed/background policy and caps it at `FlowPainterSettings.MaximumSegmentCount`. This keeps planner admission conservative before stroke collections are allocated.

## Automated coverage

M15.2 adds 22 Application cases covering settings, interpolation, boundary response, planner segment geometry, version identity, workload admission and project/preset migration.

Endpoint interpolation returns the configured background and detail values exactly at detail 0 and 1. Persistence tests bind to the current project and preset schema constants so future schema increments do not leave stale numeric expectations.

## Manual acceptance

1. Render the same image with low and high detailed-segment values.
2. Confirm detailed areas use visibly shorter local increments and thinner strokes.
3. Increase detailed curve freedom and verify curved contours are followed without random perpendicular cuts.
4. Increase tangent/crossing boosts and verify important boundaries are followed more strongly, with no visible rectangular or regional seam.
5. Set segment and curve multipliers to 100% and boosts to 0%; verify the previous policy is recovered.
6. Save/reopen a project and preset and confirm all six values round-trip.

## Validation result

The user confirmed that the solution compiles and all **1,071 tests pass**. M15.2 is therefore the accepted baseline for M15.3.

## Exit criteria

- Release build completes with zero warnings and errors;
- all 1,071 tests pass with zero failures and skips;
- no-detail plans remain compatible;
- local geometry varies continuously with detail;
- regional boundary response strengthens continuously rather than switching abruptly;
- project schema 13 and preset schema 10 migrate older documents safely;
- no external dependency is introduced.
