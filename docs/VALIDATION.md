# Validation checklist

Run from the repository root with the .NET 10 SDK installed.

```bash
dotnet --info
dotnet restore FlowPainter.sln
dotnet build FlowPainter.sln -c Release --no-restore
dotnet test FlowPainter.sln -c Release --no-build --logger "console;verbosity=normal"
dotnet run --project src/FlowPainter.App/FlowPainter.App.csproj
```

## M5.3 expected result

- restore succeeds;
- all nine projects build;
- build emits zero warnings and zero errors;
- all 360 test cases pass;
- the Avalonia window opens;
- a new image can be opened and saved as a `*.flowpainter.json` project;
- reopening the project restores source, seed, all settings, preview quality and ordered manual regions;
- project-relative source references resolve independently of the process working directory;
- Draft, Standard and High rebuild to maximum sides of 256, 512 and 1,024 pixels;
- existing regions can be relabelled, resized by normalized percentages, reordered and deleted;
- source and result remain visible side by side;
- recent project and preset entries survive application restart;
- missing recent files are removed on attempted load;
- planning, analysis and rendering can still be cancelled and rerun;
- Domain and Application contain no Avalonia, SkiaSharp or LibNoiseCore dependency.

After successful validation, update M5 in `PROJECT_VISION_AND_ROADMAP.md` from `READY FOR VALIDATION` to `DONE` and record the result below.

## Validation history

### 2026-07-13 — M5.3 project rectangle serialization correction

The M5.2 Windows test run built all projects and executed 357 tests. One project round-trip test failed because `NormalizedRect` was serialized as edge properties but deserialized as its default zero-valued struct. This was a persistence defect rather than a floating-point assertion issue.

M5.3 adds an Application-layer JSON converter that writes the four stable rectangle edges, reconstructs the validated immutable domain value, rejects invalid bounds and accepts earlier schema-1 payloads containing the derived `width` and `height` properties. Three focused tests cover the stable JSON shape, previous-payload compatibility and invalid-bounds rejection. The expected suite is now 360 cases.

### 2026-07-13 — M5.2 target build corrections

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
