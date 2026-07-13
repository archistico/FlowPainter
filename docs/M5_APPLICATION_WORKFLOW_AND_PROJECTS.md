# M5 — Application workflow and project model

## Status

**READY FOR VALIDATION**

M5 moves image-specific editing state out of ad-hoc UI collections and introduces a versioned project workflow. Native SkiaSharp and Avalonia bitmap ownership remains in the desktop composition root; persisted state and region-editing rules live in `FlowPainter.Application` and are covered by automated tests.

## Goals

- save and reopen a FlowPainter working session;
- preserve the source-image reference, seed, complete M4 settings, preview quality and manual detail regions;
- keep region editing deterministic and independent from Avalonia controls;
- let the user relabel, resize, reorder and delete existing regions;
- make preview resolution an explicit project setting;
- retain recent project and preset paths between sessions;
- introduce structured workspace operation and validation models;
- keep Domain and Application independent from SkiaSharp and Avalonia.

## Project format

Project files use the suggested extension:

```text
*.flowpainter.json
```

Schema version 1 stores:

```text
schemaVersion
project
  name
  sourcePath
  seed
  settings
  preview
    quality
  detailRegions[]
```

`sourcePath` is written relative to the project directory whenever possible. An absolute reference is retained when a portable relative path cannot be produced. On loading, relative paths are resolved against the project file rather than the current working directory.

The project serializer:

- validates the schema before deserializing the complete model;
- rejects missing and unsupported schema versions explicitly;
- truncates existing seekable streams before writing;
- supports cancellation;
- copies exposed region collections into read-only views.

## Preview quality

M5 introduces three explicit preview qualities:

| Quality | Maximum side |
|---|---:|
| Draft | 256 px |
| Standard | 512 px |
| High | 1,024 px |

The preview remains an analysis/rendering proxy. It does not change normalized region geometry and does not define the future final export resolution. M6 will render independently up to 10,000 × 10,000.

Changing preview quality requires **Rebuild preview**. Heavy image analysis is intentionally not triggered on every text-field edit: explicit commands keep cancellation and resource ownership predictable.

## Workspace model

`FlowPainterWorkspace` owns logical editing state:

- current source and project paths;
- project name;
- seed;
- immutable `FlowPainterSettings`;
- `PreviewSettings`;
- ordered manual regions;
- dirty state;
- structured operation state;
- structured validation messages.

It does not own native images, UI controls or dialogs.

`DetailRegionEditor` owns region mutation rules:

- sequential stable identifiers;
- default or custom labels;
- atomic bounds/strength/intent updates;
- normalized movement and resizing;
- ordering;
- deletion and clearing;
- duplicate-identifier rejection when loading a project.

The desktop UI now exposes a region list with label, left, top, width and height editors expressed as percentages. Reordering changes the order in which overlapping manual adjustments are composed.

## Recent items

Recent projects and custom presets are persisted in:

```text
%LOCALAPPDATA%/FlowPainter/recent-items.json
```

or the equivalent local application-data folder on the current platform.

The list:

- stores normalized full paths;
- places the newest entry first;
- removes duplicates case-insensitively;
- is capped at ten entries per category;
- removes missing entries when the user attempts to open them;
- is non-critical state: corruption or write failure must not prevent normal use of the application.

## Presentation boundary

The Avalonia window remains the composition root for:

- file pickers;
- `SkiaImage` ownership;
- proxy generation;
- detail-overlay bitmap conversion;
- final preview bitmap replacement;
- cancellation-token lifetime.

Replacement remains transactional: the old source, proxy, result and Avalonia bitmaps are disposed only after the new resources have been successfully adopted.

M5 extracts durable project/workspace rules, but it does not claim a framework-heavy MVVM rewrite. Further view decomposition can occur incrementally without moving native ownership into view models.

## Manual validation

1. Open an image and create positive and negative regions.
2. Relabel a region and edit its percentage bounds.
3. Move regions earlier/later and verify that the heat map recomposes.
4. Save a `*.flowpainter.json` project next to or away from the source image.
5. Close and reopen the project; verify seed, settings, preview quality and regions.
6. Move the project and source together while preserving their relative layout; reopen it.
7. Switch Draft, Standard and High preview quality and rebuild.
8. Save/load a custom preset and verify both recent lists.
9. Remove a recent file from disk and attempt to open it; verify that the stale entry is removed.
10. Render and save a PNG after reopening a project.

## Automated validation target

```text
0 errors
0 warnings
360 test cases passing
```

M5 adds coverage for project schema validation, relative path handling, preview sizing, project immutability, region editing, dirty-state transitions, operation/validation state and recent-item persistence.

## M5.2 build correction

The first target build of M5.1 exposed namespace/analyzer issues that did not affect the project model or scrollbar layout. M5.2 resolves the `System.IO.Path` versus Avalonia shape-name ambiguity, uses the concrete array type in the private recent-item helper and reuses static expected-order arrays in tests. No schema, workflow or UI behavior changed.


## M5.3 project rectangle serialization correction

The Windows M5.2 test run built all projects but exposed a real project-persistence defect: immutable `NormalizedRect` values were serialized with their edge properties, then deserialized as the default zero rectangle because `System.Text.Json` could not reconstruct the readonly domain struct through the generic object contract.

M5.3 registers an Application-layer `JsonConverter<NormalizedRect>` for project documents. The converter:

- writes only the stable `left`, `top`, `right` and `bottom` edges;
- reconstructs the validated domain value through its public constructor;
- rejects missing or invalid bounds with `JsonException`;
- ignores the previously emitted derived `width` and `height` properties, preserving compatibility with M5/M5.1/M5.2 project files;
- keeps serialization concerns out of the Domain assembly.

Three focused project-serialization tests raise the suite from 357 to 360 cases.
