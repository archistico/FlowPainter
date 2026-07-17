# Validation checklist

## M15.2 High-detail local stroke policy

Status: **READY FOR VALIDATION**  
Validated baseline: **M15.1 — 1,049 tests**  
Expected automated suite: **1,071 cases** (1,049 validated baseline + 22 new Application cases).

Run:

```bash
dotnet build -c Release
dotnet test -c Release --no-build
```

Manual acceptance is defined in [`M15_2_HIGH_DETAIL_LOCAL_STROKE_POLICY.md`](M15_2_HIGH_DETAIL_LOCAL_STROKE_POLICY.md).

## Current validated baseline — M15.1

- restore succeeds;
- all nine projects build with zero warnings and zero errors;
- all **1,049** test cases pass with zero failures and zero skips;
- project schema 12 and preset schema 9 remain the M15.1 validated persistence baseline;
- M13.4 state, memory, persistence and analysis-orchestration safeguards remain validated;
- M14.1–M14.6 contracts, deterministic SLIC, connectivity, descriptors, RAG and hierarchy remain accepted;
- M14.7 active SLIC orchestration and generalized roles remain accepted;
- M14.8 regional controls, diagnostics, inspection and backward-compatible persistence are accepted;
- Domain and Application remain free of Avalonia, SkiaSharp, LibNoiseCore, machine-learning runtimes, external SLIC packages and model files.

M13.3 plus audit corrections established 755 cases. M13.4.1 reached 765, M13.4.2 reached 782, M13.4.3 reached 790 and M13.4.4 reached 804. M14.1 reached 863, M14.2 reached 882, M14.3 reached 907, M14.4 reached 920, M14.5 reached 940, M14.6 reached 964, M14.7 reached 998 and M14.8 was validated at **1,024** and M15.1 at **1,049**.

## Next validation target — M15.2 High-detail local stroke policy

M15.1 is fully validated. M15.2 exit checks are:

- detailed areas use shorter and thinner marks through continuous multipliers;
- local segment count increases deterministically and remains capped by the supported maximum;
- curve freedom interpolates continuously and remains bounded;
- zero detail preserves the validated M12/M15.1 boundary response;
- detailed areas progressively strengthen tangent alignment and crossing resistance;
- no-detail plans retain the original v1 identity and sequence;
- project schema 13 and preset schema 10 round-trip the new controls and migrate older documents;
- work estimation reserves the worst-case local segment count;
- build has zero warnings/errors and all 1,071 tests pass.

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
