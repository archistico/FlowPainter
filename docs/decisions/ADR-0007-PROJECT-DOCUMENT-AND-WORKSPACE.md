# ADR-0007 — Project document and workspace boundary

## Status

Accepted for M5.

## Context

M4 stored image-specific regions only in the window code-behind. Presets deliberately contain reusable algorithm settings, not source-image references or manual annotations. Continuing to add save/load and editing behaviour directly to Avalonia controls would make persistence, validation and later high-resolution export difficult to test.

At the same time, native SkiaSharp images and Avalonia bitmaps require deterministic disposal tied to the desktop window and operation lifetime. Moving those resources into serializable view models would obscure ownership.

## Decision

Introduce two explicit Application concepts:

1. `FlowPainterProject`, a versioned serializable snapshot containing source reference, seed, settings, preview quality and ordered manual regions.
2. `FlowPainterWorkspace`, mutable logical editing state that owns no native or UI resources.

The Avalonia window remains the composition root and native-resource owner. It translates between controls and the Application models, resolves file dialogs and adopts/disposes images transactionally.

Project source references are relative to the project file whenever possible. Region coordinates remain normalized. Presets continue to exclude image-specific regions.

## Consequences

Positive:

- projects can be round-tripped without Avalonia or SkiaSharp;
- region mutation and dirty-state behaviour are testable;
- project files can move with their source images;
- preview quality becomes explicit and independent from final export resolution;
- M6 can consume the same project state for high-resolution export;
- native ownership remains clear.

Trade-offs:

- the composition root still contains event wiring and bitmap conversion;
- project schema changes require migrations;
- recent-item persistence is a separate non-critical document;
- automatic heavy recomputation remains command-driven rather than firing on every field edit.
