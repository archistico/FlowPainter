# M3 — Parameters, presets and production flow field

## Purpose

M3 replaces the fixed M2 preview constants with a validated, deterministic production planning path. The application can now change the artistic flow without changing source code.

```text
local image
    ↓
analysis proxy
    ↓
validated FlowPainterSettings + seed
    ↓
internal deterministic IFlowField
    ↓
flow-field-v1 StrokePlan
    ↓
SkiaSharp preview / PNG
```

## Production flow field

`DefaultFlowFieldFactory` provides two selectable implementations:

- `CoherentNoise` — the new production default;
- `LegacyTrigonometric` — a comparison mode retained while visual migration is evaluated.

The coherent field is implemented entirely in the repository. It uses deterministic lattice hashing, smooth interpolation and configurable fractal octaves. Its output is an angle in canonical radians. Numerical golden tests protect representative samples against accidental algorithm changes.

The field parameters are:

- scale;
- octave count from 1 to 8;
- persistence;
- lacunarity;
- global angular rotation.

The angle-offset seam is also the first field-composition operation. Later primitive, saliency and manual-region influences can be introduced as explicit field transforms rather than embedded in the noise implementation.

## Production planner

`FlowPainterPlanner` creates `flow-field-v1` plans and is independent of Avalonia, SkiaSharp and LibNoiseCore.

It controls:

- deterministic field seed derivation;
- stroke starting points;
- density-controlled path length;
- curvature rejection using circular angle distance;
- width selection;
- source colour sampling;
- configured opacity;
- source or transparent background mode;
- progress and cancellation.

Unlike the characterized legacy planner, production paths stop at the image boundary. The last point is clamped to normalized canvas coordinates and no production point is emitted outside `[0, 1]`.

## Editable parameters

The Avalonia window now exposes:

- field kind;
- seed and secure new-seed generation;
- stroke and segment counts;
- field scale, octaves, persistence and lacunarity;
- field rotation;
- uniform density;
- stroke length scale;
- maximum curve angle;
- minimum and maximum width;
- opacity;
- source-image or transparent background.

Values are converted into immutable application settings before work starts. Invalid values do not reach the planner.

## Presets

Built-in presets provide useful starting points while leaving all values visible:

- Balanced;
- Fine detail;
- Expressive;
- Legacy comparison.

Custom settings can be saved and loaded as `*.flowpreset.json`. The file contains a schema version, preset name and complete settings. The serializer truncates existing seekable output streams and validates the immutable model during deserialization.

## LibNoiseCore decision

LibNoiseCore is not referenced by any shipping project and will not be introduced during migration. The legacy source remains available under `legacy/original` only as a behavioural reference.

The internal field is preferred because it provides:

- a stable persistence contract;
- explicit numerical tests;
- cross-platform determinism under project control;
- no dependency-specific field model;
- a direct path to later primitive-driven field transformations.

## Deliberate limitations

M3 still uses a uniform density map. Automatic structural importance and manual mouse regions enter in M4; semantic face and landmark analysis remains assigned to a later milestone.

The UI still renders and saves the 512-pixel analysis preview. Independent high-resolution export remains assigned to M5.

The presentation workflow remains in code-behind for this milestone. The presentation workflow remains in code-behind through M4; view-model extraction and a saved project model are assigned to M5 after the settings and detail-map contracts are validated.

## Validation target

```bash
dotnet restore FlowPainter.sln
dotnet build FlowPainter.sln -c Release --no-restore
dotnet test FlowPainter.sln -c Release --no-build
dotnet run --project src/FlowPainter.App/FlowPainter.App.csproj
```

Expected automated result: zero warnings, zero errors and all 183 test cases passing.
