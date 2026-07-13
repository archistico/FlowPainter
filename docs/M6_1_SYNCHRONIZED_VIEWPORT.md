# M6.1 — Synchronized source and preview viewport

## Purpose

M6.1 improves visual comparison between the source/detail-map panel and the rendered-preview panel without changing rendering output or project persistence.

Both panels now share one normalized viewport state:

- mouse wheel zooms around the pointer position;
- the middle mouse button pans;
- zoom and pan performed in either panel are applied to both panels;
- the same normalized image center and zoom factor are preserved even if the two controls have different pixel sizes;
- a newly opened source resets the viewport;
- rebuilding the proxy or rendering a new preview preserves the current view.

## Selection compatibility

Manual detail-region selection continues to use the left mouse button on the source panel. Pointer coordinates are inverse-transformed through the shared viewport before being converted to normalized image coordinates. Existing region rectangles are transformed together with the source image and therefore remain aligned while zooming and panning.

The source and result use `RenderTransform` only. Layout size, image pixels, `StrokePlan`, project files and exported images are unaffected.

## Application boundary

`SynchronizedImageViewportState` belongs to `FlowPainter.Application.Interaction` and contains no Avalonia dependency. It owns:

- zoom limits;
- normalized center;
- cursor-anchored zoom mathematics;
- pan clamping;
- viewport-to-image coordinate conversion;
- per-control affine transform calculation.

The Avalonia window only translates that result into `MatrixTransform` instances and handles pointer capture.

## Automated validation

M6.1 adds ten Application tests covering:

- identity state;
- center zoom;
- cursor-anchored zoom;
- pan direction;
- pan disabled while the whole image is visible;
- edge clamping;
- synchronization across differently sized viewports;
- coordinate conversion after zoom and pan;
- reset;
- minimum and maximum zoom limits.

Expected suite: 410 tests.
