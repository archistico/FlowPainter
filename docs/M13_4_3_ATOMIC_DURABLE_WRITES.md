# M13.4.3 — Atomic durable writes

**Status: DONE — validated with 790 tests**

## Validation result

The user validated the milestone on 2026-07-16:

- Release build completed successfully;
- all **790** automated tests passed;
- atomic project, preset, preview and export writes are accepted as the baseline for M13.4.4.

## Purpose

M13.4.3 prevents a cancelled, failed or interrupted serialization/encoding operation from replacing a previously valid FlowPainter file with a partial or empty destination.

The milestone addresses persistence risk before SLIC introduces larger project state, regional caches and additional export diagnostics. It does not change project schema, preset schema or artistic output.

The cross-cutting decision is recorded in [`ADR-0019 — Atomic local-file commits`](decisions/ADR-0019-ATOMIC-LOCAL-FILE-COMMITS.md).

## Scope

Atomic local-file commits now cover:

- `*.flowpainter.json` projects;
- `*.flowpreset.json` presets;
- rendered preview PNG files;
- final Flow, Primitive and Hybrid PNG/JPEG exports;
- primitive SVG exports;
- the best-effort recent-project/recent-preset document.

All production destination writes are routed through the Application-level `AtomicFileWriter`. Existing stream serializers and image/vector encoders remain independently testable and do not need filesystem knowledge.

## Commit protocol

For every local destination, `AtomicFileWriter`:

1. resolves the full destination path;
2. creates the destination directory when necessary;
3. creates a uniquely named temporary sibling file with `FileMode.CreateNew` and exclusive sharing;
4. invokes the supplied serializer or encoder against that temporary stream;
5. flushes asynchronously, then requests a flush to durable storage;
6. closes the temporary stream;
7. checks cancellation once more before commit;
8. replaces or creates the destination with a same-directory replace/move;
9. removes the temporary file on every non-committed path.

The temporary file is created in the destination directory so the final move remains on the same filesystem. The existing destination is never opened, truncated or deleted before the new content has been written, flushed and closed successfully.

## Failure semantics

If serialization, encoding, flushing, cancellation or the final move fails:

- an existing destination remains unchanged;
- a destination that did not previously exist remains absent;
- the workspace is not marked saved;
- recent-item state is not updated as though the primary save succeeded;
- temporary cleanup is attempted without replacing the original exception.

The final move is the only operation allowed to change the visible destination path.

## Local-storage contract

The current Avalonia desktop workflow saves to local filesystem paths exposed by `IStorageFile.Path.LocalPath`. Atomic commit requires a local path because the temporary sibling and final destination must share a filesystem.

A future non-local storage provider must supply its own transactional commit contract. FlowPainter must not silently fall back to direct destination streaming, because that would weaken the preservation guarantee.

## Layering

`AtomicFileWriter` belongs to `FlowPainter.Application.Persistence`:

- it depends only on `System.IO`;
- serializers and encoders continue to accept generic writable streams;
- Avalonia remains responsible only for choosing the destination path;
- no Domain type depends on filesystem persistence.

This keeps atomicity reusable by the current desktop shell and future non-UI callers.

## Behavioural compatibility

For successful writes:

- serialized JSON content is unchanged;
- PNG/JPEG/SVG content is unchanged;
- project schema remains 11;
- preset schema remains 8;
- deterministic rendering and plan generation are unchanged;
- destination filenames and picker behaviour are unchanged.

The only visible difference is that incomplete output is no longer published at the selected destination.

## Automated coverage

The validated M13.4.2 baseline contains **782** cases. M13.4.3 adds **8** Application cases, for an expected total of **790**.

The new tests verify:

- creation of a new destination;
- replacement of an existing destination;
- preservation of an existing file after writer failure;
- preservation of an existing file after cancellation;
- absence of a new destination after failed serialization;
- creation of a missing destination directory;
- validation of an empty destination path;
- pre-cancelled requests leaving the destination untouched;
- cleanup of temporary sibling files on success and failure.

## Manual validation checklist

1. Build the full solution in Release with zero warnings and errors.
2. Run all **790** tests with zero failures and zero skips.
3. Save and reopen a project over an existing project file.
4. Save and reload a preset over an existing preset file.
5. Export preview PNG, final Flow PNG/JPEG, final Primitive PNG/JPEG, final Hybrid PNG/JPEG and primitive SVG.
6. Confirm every successful file opens normally and has the selected filename.
7. Start a sufficiently long final export over an existing file, cancel during rendering or encoding and confirm the previous destination remains valid and unchanged.
8. Confirm no `.*.<guid>.tmp` sibling remains after success or cancellation.
9. Confirm cancelling the file picker creates or changes no file.
10. Confirm a successful project save clears the dirty indicator only after the atomic commit completes.

## Residual work

The following remain outside M13.4.3:

- crash-consistent directory-entry flushing across every operating system/filesystem combination;
- transactional multi-file bundles;
- cloud or virtual storage-provider commits;
- autosave, backup rotation and recovery journals;
- detached analysis/session adoption, which belongs to M13.4.4.

The milestone provides the strongest practical single-file replacement guarantee available to the current local desktop architecture without changing formats or introducing a database.

## Exit criteria

M13.4.3 is complete when:

- all production local destination writes use `AtomicFileWriter`;
- no serializer or encoder writes directly to the final local path;
- an existing destination survives cancellation and writer failure unchanged;
- failed new writes publish no destination;
- temporary files are cleaned on non-committed paths;
- successful output remains byte-compatible with the existing serializers/encoders;
- build and all **790** tests pass locally.
