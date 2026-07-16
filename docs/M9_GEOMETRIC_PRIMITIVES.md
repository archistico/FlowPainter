# M9 — Geometric primitive engine

## Status

DONE

## Purpose

M9 adds a second generative engine that reconstructs the source image with optimized geometric forms. It is conceptually inspired by `fogleman/primitive`, but the implementation is native C# and follows FlowPainter's existing separation between pure planning, rasterization and UI.

The primitive engine is independent from flow painting. M10 will combine the two engines.

## Pipeline

```text
Source image
    ↓
Analysis proxy + composed detail map
    ↓
Average-colour initial canvas
    ↓
Deterministic candidate search
    ↓
Optimal colour estimation + local weighted error
    ↓
Hill-climbing mutation
    ↓
Immutable PrimitivePlan
    ├── Skia raster preview/final export
    └── SVG vector export
```

The optimizer never searches directly on the full 10,000 × 10,000 output. It works on the selected analysis proxy. Primitive centers and dimensions are normalized, so the accepted plan can be rerendered at any supported resolution.

## Supported forms

- triangle;
- rectangle;
- rotated rectangle;
- circle;
- ellipse;
- any non-empty combination of the supported forms, including mixed-all mode.

Each `GeometricPrimitive` stores kind, normalized center, normalized width/height, rotation and RGBA colour. A `PrimitivePlan` stores source proxy size, seed, average background colour, planner version and an ordered immutable primitive collection.

## Optimization

For every requested form, the optimizer:

1. generates a configurable number of deterministic candidates;
2. rasterizes only each candidate's affected proxy pixels;
3. analytically estimates a colour suited to those pixels and the configured opacity;
4. scores the local weighted reduction in RGB squared error;
5. hill-climbs the best candidate through deterministic geometry mutations;
6. accepts the form only when it improves the current canvas;
7. updates only the pixels covered by the accepted mask.

Factory, mutator, mask rasterizer and scorer are replaceable Application contracts.

## Detail guidance

The composed detail map affects primitive generation in four independent ways:

- placement bias sends more candidates toward important regions;
- size influence makes forms smaller in detailed regions;
- error weighting prioritizes fidelity in important regions;
- search influence allocates more mutation attempts to detailed regions.

Backgrounds consequently receive broader synthesis, while subjects, silhouettes and promoted focal regions receive smaller forms and more optimization effort.

## UI and persistence

The application exposes a generative-engine selector with Flow painting and Geometric primitives. Primitive controls include count, candidates, mutations, allowed forms, size range, opacity and detail influences.

Project schema 5 persists:

- selected generative mode;
- complete primitive-generation settings;
- all prior flow, brush, semantic, detail, preview and final-output settings.

Schemas 1–4 remain readable and default to Flow painting with standard primitive settings.

## Output

The same accepted `PrimitivePlan` drives:

- synchronized preview;
- PNG/JPEG final raster output up to 10,000 × 10,000;
- SVG vector export.

Raster and SVG output preserve primitive order and alpha. SVG uses invariant numeric formatting, UTF-8 without BOM and stable LF line endings.

## Non-goals

M9 does not yet:

- deform flow fields with primitive geometry;
- interleave primitive and brush passes;
- edit individual generated forms on the canvas;
- save generated plans inside project files;
- use polygons, Bézier paths or painterly stroke primitives.

Those belong to M10 and M11.

## Automated validation

M9 expects 545 automated cases:

```text
Domain                  88
Application            394
Imaging.Skia            24
Rendering.Skia          39
Total                   545
```

The new coverage includes primitive invariants, all mask families, analytical colour scoring, deterministic optimization, detail-guided search, schema-5 migration, workspace state, Skia raster output and SVG export.

M9.1 also includes the shared test progress recorders required by the primitive optimizer and primitive renderer progress tests; this is a test-infrastructure correction only.

## Manual validation

1. Open a source image and inspect or edit the detail map.
2. Select **GeometricPrimitives**.
3. Render each individual primitive family.
4. Render a mixed combination of forms.
5. Change the seed and verify that the composition changes.
6. Restore the previous seed and settings and verify that the plan is reproducible.
7. Compare source and preview with synchronized zoom and pan.
8. Export PNG and JPEG at a resolution larger than the preview.
9. Export SVG and open it in a browser or vector editor.
10. Save and reopen the project and verify mode and primitive settings.
11. Reopen an M8 project and verify that it defaults to Flow painting.


## Validation result

The target Windows environment compiled M9.1 successfully, passed all 545 automated tests and confirmed the primitive workflow operational before M10 development began.
