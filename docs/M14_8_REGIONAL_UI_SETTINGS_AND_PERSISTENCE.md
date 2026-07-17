# M14.8 — Regional UI, settings and persistence

**Status:** DONE — validated with 1,024 tests  
**Baseline:** M14.8 validated with 1,024 tests  
**Validated suite:** 1,024 tests

## Objective

Expose the validated SLIC regional pipeline without reintroducing semantic recognition. M14.8 adds editable SLIC and hierarchical-merge settings, diagnostic regional views, direct region inspection and versioned persistence. Derived labels, descriptors, graphs and hierarchy remain reproducible in-memory outputs and are not stored.

## Desktop controls

The analysis panel now exposes:

- enable/disable deterministic regional segmentation;
- target region size;
- compactness;
- Gaussian pre-smoothing sigma;
- maximum iterations and convergence tolerance;
- a simplified 0–100 merge-intensity control;
- fine mean-colour, fine-boundary, strong-boundary and hierarchy previews;
- fine, intermediate and broad-mass hierarchy selection;
- explicit SLIC reanalysis;
- diagnostics for region count, adjacency count, hierarchy sizes, convergence and connectivity repairs;
- click inspection of fine-region geometry, CIELAB colour, texture, edge density, compactness and orientation.

Obsolete semantic tuning widgets are hidden. Their values remain readable and round-trippable solely for compatibility with old project and preset documents. Compatibility overlays and manual role commands are derived from SLIC regional evidence, not from object classification.

## Persistence

Project schema advances from 11 to **12** and preset schema from 8 to **9**.

Project schema 12 persists:

- `RegionSegmentationSettings`, including the enable flag;
- `RegionMergeSettings`;
- generalized `RegionRoleOverride` values;
- legacy correction data while the compatibility editor remains present.

Preset schema 9 persists reusable SLIC and merge parameters. It never stores source-specific region-role overrides.

Schemas 1–11 projects and schemas 1–8 presets remain readable. Schema-11 corrections are converted deterministically when direct role overrides are absent. Older documents receive explicit default SLIC and merge settings.

## Diagnostics and ownership

Diagnostic maps are produced in Application as immutable `RgbaImage` values and converted to owned Skia images through `SkiaImageFactory`. Overlay replacement is transactional and disposes the previous Avalonia/Skia resources. Diagnostic allocations are admitted through the shared memory-budget policy.

## Automated coverage

M14.8 adds 26 cases:

- segmentation enable/default behaviour;
- merge-intensity mapping, limits and round trips;
- hierarchy and strong-boundary diagnostic rendering and validation;
- Skia diagnostic-image conversion, ownership, cancellation and null validation;
- project/preset round trips, schema migrations and current schema constants.

## Manual acceptance

1. Open an image and verify the SLIC settings are populated.
2. Reanalyze with visibly different target size and compactness values.
3. Switch through every regional overlay and hierarchy level.
4. Click a visible region and verify its diagnostics update.
5. Save/reopen a project and confirm SLIC, merge and role-override values survive.
6. Save/reopen a preset and confirm reusable SLIC/merge parameters survive without image-specific overrides.
7. Open schema-11 projects and schema-8 presets and confirm default migration without data loss.
8. Disable segmentation and verify the pipeline returns one valid full-image region.

## Exit criteria

- Release build completes with zero warnings and errors;
- all 1,024 tests pass with zero failures and skips;
- project schema 12 and preset schema 9 round-trip correctly;
- all older supported schemas remain readable;
- overlays and inspection never alter project dirty state;
- no external SLIC, ML or imaging dependency is introduced.
