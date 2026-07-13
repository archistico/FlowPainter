# M2 — Imaging and SkiaSharp rendering

## Purpose

M2 connects the deterministic M1 planner to real local images and a real raster output while preserving the architectural boundaries established in M0.

It is the first milestone in which the application can perform a complete user-visible operation:

```text
local image → analysis proxy → StrokePlan → rendered preview → PNG
```

## Implemented components

### `FlowPainter.Imaging.Skia`

`SkiaImageLoader`

- reads an encoded stream asynchronously;
- inspects metadata with `SKCodec`;
- rejects invalid files;
- rejects decoded dimensions above 10,000 × 10,000 before allocating the target RGBA bitmap;
- decodes to RGBA8888 premultiplied pixels;
- reports progress and honors cancellation.

`SkiaImage`

- implements the domain `IRgbaPixelSource` contract;
- exposes only `ImageSize`, optional source name, nearest-neighbour RGBA sampling and PNG encoding;
- owns exactly one `SKBitmap`;
- transfers ownership only through its return value;
- rejects sampling and encoding after disposal.

`SkiaImageProxyGenerator`

- preserves aspect ratio;
- never upscales;
- returns an independent native image even when the source already fits;
- uses SkiaSharp 4 sampling APIs.

`SkiaPngEncoder`

- writes a valid PNG to a caller-owned stream;
- leaves stream ownership with the caller;
- reports progress and honors cancellation before expensive work and I/O.

### `FlowPainter.Rendering.Skia`

`SkiaStrokePlanRenderer`

- accepts immutable M1 `StrokePlan` data;
- supports source-image and transparent backgrounds;
- validates source-background aspect ratio;
- projects `RelativePoint` coordinates at the requested output size;
- scales width proportionally to the output maximum dimension;
- safely clips characterized legacy points that leave the canvas;
- reports batched progress and honors cancellation;
- returns an independently owned `SkiaImage`.

The renderer intentionally uses a solid antialiased path with round caps and joins. It is the compatibility renderer that will later become the first `IBrushRenderer` implementation.

### Application preview

The Avalonia window can:

- select a local PNG, JPEG, WebP or BMP image;
- show a maximum-512-pixel analysis proxy;
- create a deterministic preview plan with 12,000 strokes;
- render and display the result;
- cancel an active operation;
- save the rendered preview as PNG.

The density map is temporarily uniform. This avoids reintroducing the expensive and nondeterministic legacy density algorithm before M7 defines the production importance-analysis pipeline.

## Resource ownership

All native results use explicit transfer semantics:

- a successfully returned `SkiaImage` belongs to the caller;
- temporary native objects remain inside `using` scopes;
- if cancellation or a progress callback interrupts construction, the partially created result is disposed;
- the Avalonia window replaces source/result state transactionally;
- closing the window first cancels active work and releases owned images only after that operation has exited.

## Package alignment

M2 pins SkiaSharp managed and Linux native assets to the same 4.150.0 version. This prevents Avalonia's lower-bound dependency from resolving a 3.x Linux native binary alongside the directly referenced 4.x managed API. Windows and macOS native assets are transitively supplied by the SkiaSharp package.

## Deliberate limitations

M2 does not yet provide:

- editable parameters;
- automatic density, saliency or face analysis;
- manual detail-region selection;
- production flow-field composition;
- realistic brush stamping;
- full-resolution export separate from preview;
- saved project files.

Those remain assigned to M3–M7 in the living roadmap.

## Validation target

```bash
dotnet restore FlowPainter.sln
dotnet build FlowPainter.sln -c Release --no-restore
dotnet test FlowPainter.sln -c Release --no-build
dotnet run --project src/FlowPainter.App/FlowPainter.App.csproj
```

Expected automated result: zero warnings, zero errors and all 110 test cases passing.

Manual smoke test:

1. load a supported local image;
2. verify source and proxy dimensions;
3. render the preview;
4. cancel and rerun at least once;
5. save the PNG;
6. reopen the saved PNG and verify its dimensions and visible result;
7. close the window during a render and confirm clean termination.
