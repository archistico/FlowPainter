# Validation checklist

Run from the repository root with the .NET 10 SDK installed.

```bash
dotnet --info
dotnet restore FlowPainter.sln
dotnet build FlowPainter.sln -c Release --no-restore
dotnet test FlowPainter.sln -c Release --no-build --logger "console;verbosity=normal"
dotnet run --project src/FlowPainter.App/FlowPainter.App.csproj
```

## M8 expected result

- restore succeeds;
- all nine projects build;
- build emits zero warnings and zero errors;
- all 496 test cases pass;
- the Avalonia window opens;
- source/result synchronized zoom and pan remain operational;
- source/result synchronized zoom and all M7 brush families remain operational;
- the combined detail overlay includes structural and semantic importance;
- saliency, subject, silhouette and focal diagnostic overlays can be selected;
- detected semantic regions can be promoted to editable manual regions;
- equal source and semantic settings produce equal maps and regions;
- project and preset schema 4 round-trip every brush and semantic-analysis parameter;
- schema-1 through schema-3 documents load with explicit compatibility defaults;
- Domain and Application contain no Avalonia, SkiaSharp or LibNoiseCore dependency.

After successful validation, update M8 in `PROJECT_VISION_AND_ROADMAP.md` from `READY FOR VALIDATION` to `DONE` and record the result below.

## Validation history

### 2026-07-13 — M8.2 xUnit analyzer correction

The next Windows build completed all production projects but stopped in `FlowPainter.Application.Tests` with `xUnit2031`. The semantic subject-count test used `Assert.Single(collection.Where(predicate))`; M8.2 now uses the dedicated `Assert.Single(collection, predicate)` overload. No production behavior changes, analyzer severity remains unchanged, and the expected suite remains 496 cases.

### 2026-07-13 — M8.1 semantic subject-kind analyzer correction

The first Windows build of M8 stopped in `FlowPainter.Domain` with `CA1720` because the public enum member `SemanticSubjectKind.Object` repeated the CLR type name `Object`. M8.1 renames that member to `SceneObject` while preserving its numeric value `3`, so serialized numeric compatibility and the semantic model remain unchanged. Analyzer severity is not reduced and the expected suite remains 496 cases.

### 2026-07-13 — M8 semantic importance prepared

M8 introduces a deterministic local semantic-importance provider that identifies generic salient subjects, silhouettes and focal points on the analysis proxy. Separate maps can be inspected in the desktop UI and detected regions can be promoted to persistent manual focus or critical-detail regions. The provider boundary allows future local ONNX/class-aware analyzers without adding a machine-learning runtime to Domain or Application today. Project and preset schemas move to version 4 with versions 1–3 remaining readable. Fifty-six focused cases increase the expected suite from 440 to 496.

Static preparation checks include C# syntax parsing, XAML/XML/JSON parsing, named-control/event resolution, schema migration review, forbidden-dependency scans and ZIP integrity. The packaging environment cannot obtain the .NET SDK, so executable validation remains assigned to the target Windows environment.

### 2026-07-13 — M7.1 validated on Windows

The user confirmed that M7.1 builds and works correctly. The 440-case brush-engine baseline is accepted and M7 is marked DONE.

### 2026-07-13 — M7.1 cancellation-token parameter ordering correction

The first Windows build of M7 reached the rendering project and reported `CA1068` because `SkiaStrokePlanRenderer.RenderAsync` placed the optional `BrushSettings` parameter after `CancellationToken`. M7.1 moves `CancellationToken` to the final position required by the analyzer and updates the two positional application call sites. Named-argument test calls and rendering behaviour remain unchanged; analyzer severity is not reduced.

### 2026-07-13 — M7 brush engine prepared

M7 introduces a pure brush configuration value and a Skia renderer strategy boundary. The same immutable `StrokePlan` can now be rasterized with SolidRound, SoftRound, Flat or Bristle materials. Size and opacity jitter are derived from the plan seed and stroke index, so preview and final export remain repeatable. Project and preset schemas move to version 3 while earlier files receive SolidRound defaults. Thirty focused cases increase the expected suite from 410 to 440.

### 2026-07-13 — M6.1 validated on Windows

The user confirmed that M6.1 builds successfully and all 410 automated tests pass. Synchronized wheel zoom, middle-button pan and source-region alignment are accepted; M6.1 is marked DONE.

### 2026-07-13 — M6.1 synchronized viewport prepared

The source/detail-map panel and rendered-preview panel now share a normalized viewport state. Wheel zoom is anchored at the pointer, middle-button pan is clamped to the image, and the same center/zoom is applied independently to both control sizes. Source-region selection inverse-maps pointer coordinates through the active transform. Ten focused tests increase the expected suite from 400 to 410 cases.

### 2026-07-13 — M6 validated on Windows

The user confirmed that M6 builds successfully and all 400 automated tests pass. M6 is marked DONE.

### 2026-07-13 — M6 prepared

M6 separates preview and final raster output. The immutable preview `StrokePlan` is retained and reused at an independently configured resolution up to 10,000 × 10,000 pixels. Final settings are persisted in project schema 2, with schema-1 migration to explicit defaults. PNG preserves alpha; JPEG composes transparency over white. A conservative estimate covers known source, proxy, preview, overlay and double final-output RGBA buffers.

The expected suite grows from 360 to 400 cases. Static preparation checks include C# syntax parsing, XAML/XML/JSON parsing, named-control/event resolution, project references, forbidden dependency scans and ZIP integrity.

### 2026-07-13 — M5.3 validated on Windows

The user confirmed that the corrected M5.3 package builds successfully and all 360 tests pass. M5 is marked DONE.

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
