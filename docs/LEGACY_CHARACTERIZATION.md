# Legacy FlowPainter characterization

## Scope

Milestone M1 extracts the observable planning behaviour of the original WPF `FlowPainter` example into pure .NET domain and application components. It deliberately does not migrate image decoding, Skia rasterization or Avalonia interaction; those enter in M2 and later milestones.

The original source remains unchanged under `legacy/original` as a read-only reference.

## Behaviour retained

The M1 planner retains these legacy rules:

- 50,000 strokes by default;
- 20 path segments by default;
- flow direction derived from a scalar field as `angle = value × 2π`;
- path length equal to `0.005 × legacy density` by default;
- stroke colour sampled from the source pixel at the initial point;
- stroke width selected between 5 and 10 legacy pixels;
- source image retained as the plan background;
- one field seed drawn before stroke generation;
- one unused random draw for the legacy `darkness` variable retained per stroke;
- the unused post-path scalar-field sample retained as part of the characterized call sequence;
- paths allowed to extend outside the canvas during M1.

## Intentional deviations

### Versioned random generator

The original used `System.Random`, whose implementation is not accepted as a persisted project-format contract. M1 uses the existing SplitMix64-based `DeterministicRandom` through `IRandomSource`. The generated artwork is therefore not bit-identical to the historical demo, but it is stable across runs and protected by golden tests.

### Circular angle correction

The original compared angles with an absolute modulo expression that treated values near `0` and `2π` as far apart. M1 uses the true shortest circular distance. A characterization test protects this correction.

### Resolution-independent width

The original stored width only as 5–10 output pixels. M1 stores width as a ratio to an explicit 512-pixel reference dimension. Rendering at another resolution can therefore scale the same plan consistently.

### No network or native renderer

The original constructor downloaded a hard-coded avatar and immediately allocated Skia objects. M1 uses a repository-owned JSON fixture containing a tiny RGBA source and legacy density values. No network request or Skia native allocation occurs in Domain or Application.

### Scalar-field seam

M1 introduces `ILegacyScalarField` and `ILegacyScalarFieldFactory`. It does not yet ship a LibNoiseCore adapter or a replacement production field. M3 will compare a temporary compatibility adapter with an internal deterministic field before removing LibNoiseCore.

## Characterization fixture

`tests/FlowPainter.Application.Tests/Fixtures/legacy-flow-fixture.json` contains:

- fixture version;
- a 4 × 3 RGBA image;
- row-major legacy density values.

The golden test fixes:

- source fixture version;
- request seed;
- derived field seed;
- first three stroke colours;
- first three widths;
- every normalized path point for those strokes.

## Deferred corrections

The following legacy behaviours remain intentionally unchanged in M1:

- paths are not clipped or stopped at image boundaries;
- density values are supplied rather than recalculated;
- no cancellation or progress reporting;
- no renderer or final bitmap;
- no visual comparison with LibNoiseCore output.

These items belong to M2 and M3 so that each change is isolated and testable.
