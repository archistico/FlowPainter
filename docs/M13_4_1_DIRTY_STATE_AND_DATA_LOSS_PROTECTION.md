# M13.4.1 — Dirty state and data-loss protection

**Status: DONE**


## Validation result

The user validated the milestone on 2026-07-16:

- Release build completed successfully;
- all **765** automated tests passed;
- the guarded Save / Discard / Cancel workflow behaved correctly in manual use.

## Purpose

M13.4.1 closes audit finding F-02 before SLIC introduces additional project settings, label maps and derived resources. A user must never replace or close an edited session without an explicit Save, Discard or Cancel decision.

## Scope

The milestone adds:

- complete dirty tracking for persisted project controls after a source is active;
- a testable `ProjectSessionController` in Application;
- Save / Discard / Cancel protection before opening another image;
- the same protection before opening a project from the picker or recent-project list;
- a guarded window-closing workflow;
- a visible `*` suffix in the window title while the active project is dirty;
- preservation of the current session when Save is cancelled, invalid or unsuccessful;
- ten additional Application test cases, including value-equivalent settings checks that prevent rendering from creating false dirty state.

Transient UI state remains intentionally outside project dirty tracking:

- viewport zoom and pan;
- selected diagnostic overlay;
- overlay visibility;
- recent-item selection;
- preset name and recent-preset selection;
- uncommitted manual-region editor fields.

Committed detail-region and semantic-correction operations continue to mark the workspace dirty through `FlowPainterWorkspace`.

## Application contracts

### `UnsavedChangesDecision`

The destructive-navigation decision is explicit:

```text
Cancel
Discard
Save
```

### `ProjectSessionController`

`ProjectSessionController` depends only on `FlowPainterWorkspace` and callbacks supplied by the presentation layer. It:

- reports whether the active source has unsaved project changes;
- accepts presentation-level edit notifications;
- bypasses prompts for clean sessions;
- allows Discard immediately;
- blocks on Cancel;
- allows Save only when the supplied save workflow returns success;
- rejects unknown decision values.

No Avalonia or native image resource enters the Application contract.

## Desktop workflow

Before a destructive action:

```text
Clean session
    ↓
continue immediately

Dirty session
    ↓
Save / Discard / Cancel dialog
    ├── Save    → continue only after successful project save
    ├── Discard → continue without saving
    └── Cancel  → retain the current session
```

The protected actions are:

1. Open image;
2. Open project;
3. Open recent project;
4. Close main window.

If an operation is active while the window is being closed, FlowPainter first requests cancellation and keeps the window open. The user may close again after the operation has stopped.

## Dirty-tracked project controls

The presentation layer tracks changes to:

- project name and seed;
- selected generative mode;
- preview and final-output settings;
- Flow, brush, detail, semantic, boundary and background parameters;
- primitive and hybrid settings;
- persisted enable/disable switches.

Programmatic control population during project adoption is suppressed so a freshly loaded project remains clean. Applying a preset to an active source is a real project edit and therefore marks the session dirty.

## Transactional guarantees

M13.3 audit remediation already validates and analyzes a project into detached resources before adoption. M13.4.1 preserves that path and adds the navigation guard in front of it.

The following guarantees now apply:

- cancelling the decision dialog changes nothing;
- cancelling the Save picker changes nothing and blocks the destructive action;
- invalid project controls block Save and therefore block the destructive action;
- failed or cancelled project saving blocks the destructive action;
- cancelling the target Open picker leaves the existing dirty state intact;
- failed project/image loading retains the previously active session;
- project control population after a successful load does not create a false dirty state.

Atomic destination-file replacement is deliberately deferred to M13.4.3.

## Automated tests

`ProjectSessionControllerTests` covers:

- clean-session bypass;
- Cancel behaviour;
- Discard behaviour;
- successful Save;
- unsuccessful Save;
- invalid decision rejection;
- presentation edits with and without an active source.

`FlowPainterWorkspaceTests` additionally verifies that value-equivalent Flow, Primitive and Hybrid settings remain clean, preventing preview rendering from creating a false dirty state.

Expected suite after this milestone: **765 test cases**.

## Manual validation checklist

1. Open an image and save the project.
2. Change a persisted numeric parameter; confirm the title becomes `FlowPainter *`.
3. Choose Open image and verify Cancel retains the session.
4. Repeat and verify Discard opens the selected image without saving the old project.
5. Repeat and verify Save requires a successful project save before opening the image.
6. Repeat the same checks for Open project and Open recent project.
7. Edit a project and close the window; verify all three choices.
8. Cancel the Save picker from the close workflow; verify the application remains open.
9. Enter an invalid persisted value, choose Save during navigation, and verify navigation is blocked with the validation message visible.
10. Load a saved project and verify the title has no `*` until a real edit occurs.
11. Change only zoom, pan, overlay selection or overlay visibility and verify the project remains clean.
12. Start a long operation and close the window; verify cancellation is requested and the window remains open until the operation stops.

## Exit criteria

- all four destructive paths are guarded;
- all persisted presentation controls participate in dirty tracking;
- transient visual state does not mark the project dirty;
- Save failure or cancellation never permits destructive navigation;
- successfully loaded and saved projects are clean;
- build succeeds with zero warnings/errors;
- all 765 tests pass;
- the manual checklist passes.
