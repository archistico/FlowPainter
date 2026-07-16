# M6 — High-resolution final rendering

## Status

**DONE — validated on Windows with 400 passing tests**

M6 separates the interactive preview from the final raster output. A preview creates and displays one deterministic, resolution-independent `StrokePlan`; final export reuses that exact plan and rasterizes it against the original source image at a separately configured size.

## Goals

- render final images independently from preview quality;
- preserve the exact preview plan, seed, geometry, colours and stroke order;
- support output dimensions up to 10,000 × 10,000 pixels;
- preserve source aspect ratio;
- estimate known RGBA working memory before allocation;
- export PNG and JPEG;
- retain cancellation and progress through rendering and encoding;
- persist final-output settings in the project document;
- migrate M5 schema-1 projects without data loss.

## Final-output settings

`FinalRenderSettings` stores:

- maximum output dimension, from 1 to 10,000 pixels;
- PNG or JPEG format;
- JPEG quality from 1 to 100.

The exact width and height are derived from the original source aspect ratio. Upscaling and downscaling are both allowed because the output is a new rasterization of normalized generative geometry, not a resize of the preview bitmap.

The default final maximum dimension is 4,096 pixels. PNG is the default lossless format. JPEG defaults to quality 92.

## Preview/final plan contract

The workflow is deliberately two-stage:

```text
Proxy + detail map + seed + settings
                ↓
         immutable StrokePlan
          ↙              ↘
preview raster          final raster
proxy dimensions        configured dimensions
```

Final export is enabled only after a preview has been rendered. This ensures that the exported artwork is the same plan currently shown in the result pane. Editing controls does not silently mutate the cached plan; the user renders a new preview to produce a new final plan.

The source-image background is taken from the original decoded image during final export. Compatibility is verified by recreating the expected proxy dimensions from the original source, avoiding false failures caused by integer aspect-ratio rounding.

## Memory estimate

M6 reports a conservative estimate for known RGBA buffers:

- decoded source;
- analysis proxy;
- rendered preview;
- detail-map overlay when present;
- final Skia surface;
- copied final bitmap returned by the renderer.

The encoded file, Avalonia display copies, native allocator overhead and codec scratch memory are not included, so the displayed value is a lower bound for process memory rather than a guarantee.

Risk bands are informational:

- Normal: below 768 MiB known buffers;
- Elevated: 768 MiB to below 1.5 GiB;
- High: 1.5 GiB or more.

No full-size floating-point detail map is allocated.

## Encoding

### PNG

- preserves alpha;
- uses lossless SkiaSharp encoding;
- truncates an existing seekable output stream before writing.

### JPEG

- uses the configured quality;
- JPEG has no alpha channel, so transparent pixels are composed over white before encoding;
- output remains fully opaque when decoded.

## Project schema

The project schema advances from version 1 to version 2 and adds:

```text
project.finalRender
  maximumDimension
  format
  jpegQuality
```

Schema-1 M5 projects remain readable. Missing final-render data is migrated to the M6 defaults. New saves always write schema 2.

## Automated validation target

```text
0 errors
0 warnings
400 test cases passing
```

M6 adds tests for:

- final-output validation and aspect-ratio fitting;
- upscaling and downscaling;
- PNG/JPEG extensions;
- known-memory estimates and risk bands;
- schema-1 project migration and schema-2 round trips;
- workspace dirty-state transitions;
- PNG and JPEG decoding after export;
- JPEG transparency flattening;
- quality and format validation;
- cancellation, truncation and progress;
- reuse of an original source whose integer-rounded proxy matches the plan.

## Manual validation

1. Open a landscape image and render a preview.
2. Set final maximum dimension to 4,096 and update the estimate.
3. Export PNG; verify exact reported dimensions and visual correspondence with the preview.
4. Export JPEG at quality 92 and reopen it.
5. Use a transparent-background plan and verify PNG transparency and JPEG white background.
6. Change preview quality, rebuild and render a new preview; export again.
7. Set a final size larger than the source and confirm that normalized strokes remain proportionally identical.
8. Set 10,000 as maximum dimension on a representative image and inspect the memory warning before deciding whether to export.
9. Cancel during final rendering and confirm that the UI becomes usable again.
10. Save the project, reopen it and verify final dimension, format and JPEG quality.
11. Open an M5 schema-1 project and verify default M6 output settings.
