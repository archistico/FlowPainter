# Validation checklist

## Current validated baseline — M15.2

Status: **DONE**  
Validated automated suite: **1,071 cases**.

The user confirmed:

- restore and build complete successfully;
- all nine projects build with zero warnings and zero errors;
- all **1,071** tests pass with zero failures and zero skips;
- project schema 13 and preset schema 10 round-trip the M15.2 local-stroke policy;
- continuous length, width, segment, curvature, tangent-alignment and crossing-resistance policies are accepted;
- M13.4 safety work, the complete M14 SLIC pipeline and the M15.1 regional boundary field remain regression-covered;
- Domain and Application remain free of Avalonia, SkiaSharp, machine-learning runtimes and external SLIC packages.

M13.3 plus audit corrections established 755 cases. M13.4.1 reached 765, M13.4.2 reached 782, M13.4.3 reached 790 and M13.4.4 reached 804. M14.1 reached 863, M14.2 reached 882, M14.3 reached 907, M14.4 reached 920, M14.5 reached 940, M14.6 reached 964, M14.7 reached 998, M14.8 reached 1,024, M15.1 reached 1,049 and M15.2 reached **1,071**.

## Next validation target — M15.3 Staged Flow rendering

Detailed implementation and manual acceptance plan: [`M15_3_STAGED_FLOW_RENDERING.md`](M15_3_STAGED_FLOW_RENDERING.md).

The final test count will be set when the implementation is complete. Required exit checks are already fixed:

- one immutable staged Flow plan contains Broad mass, Regional structure, Boundary reinforcement and Fine detail passes in that order;
- the sum of per-pass stroke budgets equals the accepted total Flow budget;
- per-pass seeds are deterministic, independent and reproducible from the project seed;
- preview and final export rasterize the same staged plan rather than replanning;
- cancellation and progress are monotonic across pass planning and rendering;
- M13.4 work-admission limits account for total and per-pass work before allocation;
- disabling staged rendering preserves the accepted M15.2 single-pass plan and output identity;
- no pass creates hard regional seams or mechanical contour outlines;
- build has zero warnings/errors and the complete suite passes.

The first Windows build of M5.1 identified nine compile/analyzer findings unrelated to the scrollbar layout change:

- six ambiguous `Path` references caused by importing Avalonia shapes alongside `System.IO`;
- one `CA1859` finding on a private helper receiving arrays through an `IReadOnlyList<string>` parameter;
- two `CA1861` findings caused by repeated inline expected-order arrays in region-editor tests.

M5.2 introduces an explicit `IoPath` alias, uses the concrete `string[]` parameter already supplied by the UI, and moves the two expected orders to static readonly fields. Analyzer severity remains unchanged and production behavior is preserved.

### 2026-07-13 — M5.1 configuration scrollbar spacing

The configuration `ScrollViewer` now disables horizontal scrolling and adds a stable right margin to its settings panel. The 18-pixel gutter keeps the vertical overlay scrollbar separate from text boxes, combo boxes and buttons without changing the configuration column width or application behavior. The automated suite remains at 357 cases.

Static validation completed before packaging:

- Avalonia XAML parses successfully;
- all existing named controls and event handlers are unchanged;
- the modification is layout-only and does not affect project, rendering or persistence code;
- no generated build output is included.

### 2026-07-13 — M5 prepared

M5 introduces a schema-versioned project document, preview-quality settings, portable source references, a testable workspace and region editor, persistent recent items and the corresponding Avalonia workflow. The expected suite grows to 357 cases.

Static preparation checks completed before packaging:

- project XML, Avalonia XAML and JSON fixtures parse successfully;
- every named XAML control and event handler resolves to the code-behind;
- all solution and project-reference paths resolve;
- exposed project/workspace collections use read-only views;
- project and recent-item schemas are validated before full deserialization;
- Domain and Application contain no SkiaSharp, Avalonia, LibNoiseCore, `System.Random` or network-loading reference;
- source references round-trip through relative project paths;
- no `bin`, `obj` or IDE output is included.

The packaging environment does not contain the .NET SDK, so executable build/test validation remains assigned to the target Windows environment.

### 2026-07-13 — M4.1 validated on Windows

The user confirmed the corrected M4.1 package and continued development. M4 is marked DONE with all 249 cases expected to pass using tolerance-based floating-point assertions.

### 2026-07-13 — M4.1 floating-point assertion correction

The first Windows test run built all projects and executed all 249 cases. One viewport test failed because exact record equality compared `90` with the mathematically equivalent IEEE 754 result `90.00000000000001`. M4.1 replaces exact `ViewportRect` equality with component-wise comparisons at 12 decimal digits and documents the floating-point assertion policy. Production geometry code is unchanged.

### 2026-07-13 — M0 validated on Windows

The user confirmed that the M0.1 package restored, built in Release and passed all 37 test cases. M0 is marked DONE.

### 2026-07-13 — Initial M0 build findings

The first Release build exposed:

- 27 `CA1707` analyzer errors caused by underscore-based test names;
- one `CS0118` namespace collision between `FlowPainter.Application` and Avalonia `Application`.

M0.1 corrected both without suppressing analyzers and was subsequently validated.

### 2026-07-13 — Initial M1 build findings

The first target Windows build exposed two `CA1859` performance analyzer errors in `LegacyFlowPainterPlanner`. M1.1 corrected both without changing public contracts or generated plans.

### 2026-07-13 — M1 validated on Windows

The user confirmed that M1.1 compiled successfully and all 77 test cases passed. M1 is marked DONE.

### 2026-07-13 — M2 build corrections

Target builds identified and corrected:

- the sealed `InvalidDataException` base in .NET 10;
- narrow `CA1822` findings on replaceable stateless services;
- obsolete mutable `SKPath` calls in SkiaSharp 4.

The final M2.3 package uses `IOException`, documented service-level suppressions and `SKPathBuilder`.

### 2026-07-13 — M2 validated on Windows

The user confirmed that M2.3 compiled successfully and all 110 test cases passed. M2 is marked DONE.

### 2026-07-13 — M3 validated on Windows

The user confirmed that M3 compiled successfully and all 183 test cases passed. M3 is marked DONE.

### 2026-07-13 — M4 prepared

M4 adds structural detail analysis, heat-map visualization, normalized manual regions, detail-aware planning and schema-1 preset migration. The suite grows to 249 cases.

Static preparation checks completed before packaging:

- project XML and Avalonia XAML parse successfully;
- all solution and project-reference paths resolve;
- public test names comply with analyzer naming rules;
- Domain and Application contain no SkiaSharp, Avalonia, LibNoiseCore, `System.Random` or network-loading reference;
- `flow-field-v1` compatibility remains covered while detail-aware plans use `flow-field-detail-v1`;
- preset schema 2 accepts schema 1 and applies explicit M4 defaults;
- proxy detail maps never exceed the proxy image dimensions;
- overlay and native-image ownership have deterministic failure paths;
- no `bin`, `obj` or IDE output is included.

The packaging environment does not contain the .NET SDK, so executable build/test validation remains assigned to the target Windows environment.
