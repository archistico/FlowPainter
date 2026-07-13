# ADR-0008 — Separate stroke plans from brush rasterization

**Status:** Accepted  
**Date:** 2026-07-13

## Context

FlowPainter originally represented every mark as a wide Skia path. Future painting requires materially different brushes while preview and final output must remain reproducible and must reuse the same approved plan.

## Decision

`StrokePlan` continues to describe geometry, sampled colour and relative width only. Brush selection and material parameters live in immutable `BrushSettings`. Skia-specific implementations are selected inside the rendering adapter through `ISkiaBrushRenderer`.

Local variation is derived deterministically from `StrokePlan.Seed` and `FlowStroke.Index`. Brush renderers may not call `System.Random`, share mutable global state or alter the plan.

The desktop application retains the brush settings used for the approved preview and uses that exact value for final export, even when controls are edited afterward.

## Consequences

- one plan can be compared with multiple brush materials;
- preview/final consistency is explicit;
- brush evolution does not contaminate the planner or Domain with SkiaSharp;
- project and preset schemas must persist brush settings;
- visual brush algorithms require renderer integration tests in addition to plan golden tests;
- future texture and pressure features extend the renderer contract rather than the stroke planner.
