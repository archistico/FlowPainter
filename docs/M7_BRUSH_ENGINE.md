# M7 — Deterministic brush engine

## Status

**DONE — M7.1 validated on Windows with 454 passing tests**

M7 separates stroke planning from the material mark left by a stroke. `StrokePlan` remains immutable and resolution-independent; a `BrushSettings` value selects how the same plan is rasterized for preview and final export.

M7.1 also aligns the public renderer signature with .NET analyzer rule `CA1068`: `CancellationToken` is the final optional parameter. This is an API-ordering correction only and does not alter brush output or determinism.

## Implemented brush kinds

### SolidRound

The compatibility renderer. It uses a round cap and round join and preserves the visual behaviour of the M6 renderer when brush jitter is zero.

### SoftRound

A layered procedural brush. Hardness controls the width and opacity of outer feathered passes while preserving a solid central trace.

### Flat

A square-cap, bevel-join brush that produces more graphic and angular marks than the compatibility renderer.

### Bristle

A deterministic group of parallel offset traces. Bristle count and spread control material density; hardness controls transverse irregularity. An odd bristle count preserves one central trace.

## Brush settings

`BrushSettings` is a pure Domain value and contains:

- brush kind;
- hardness;
- deterministic size jitter;
- deterministic opacity jitter;
- bristle count;
- bristle spread.

Every setting has a validated range. Jitter is derived from the plan seed and stroke index, not from global or runtime randomness. Therefore:

- equal plan + equal brush settings = equal rasterization;
- preview and final export use the same brush realization;
- changing output resolution scales marks without changing their identity;
- changing the plan seed changes local brush variation predictably.

## Rendering boundary

```text
FlowPainterPlanner
    ↓
StrokePlan
    ↓
SkiaStrokePlanRenderer
    ↓
ISkiaBrushRenderer
    ├── SolidRoundBrushRenderer
    ├── SoftRoundBrushRenderer
    ├── FlatBrushRenderer
    └── BristleBrushRenderer
```

The planner does not reference SkiaSharp or a brush implementation. The renderer owns Skia paths and paints, and all native objects use deterministic disposal.

## Persistence

Preset and project schemas move to version 3. Existing schema-1 and schema-2 files remain readable. When a previous document has no brush payload it receives the compatibility default:

```text
SolidRound
hardness 80%
size jitter 0%
opacity jitter 0%
bristle count 7
bristle spread 75%
```

## Built-in presets

The built-in catalog now demonstrates every brush family:

- Balanced — SoftRound;
- Fine detail — SolidRound;
- Expressive — Flat;
- Legacy comparison — SolidRound without jitter;
- Bristle study — Bristle.

## Deliberate scope boundary

M7 establishes a deterministic procedural brush architecture. Raster texture masks, user-loaded brush tips, pressure curves, stamp spacing and per-point rotation remain future extensions of this engine. They must be added without changing `StrokePlan` geometry or introducing randomness outside the versioned renderer policy.

## Validation

Expected automated suite: **440 cases**.

Manual smoke test:

1. load one image and render all five built-in presets;
2. compare SolidRound, SoftRound, Flat and Bristle without changing the seed;
3. render Bristle twice and verify identical output;
4. change size/opacity jitter and verify a repeatable visual change;
5. export the approved preview at a larger resolution and verify the same brush character;
6. save/reopen a project and a preset and verify all brush settings;
7. open an M6 schema-2 project and verify it defaults to SolidRound.
