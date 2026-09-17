# Ticket 08: Observed Rhino Host Checks — 2026-09-17

## Build identity

- Loaded assembly: `src/RhinoIfcViewer/bin/Release/net8.0-windows/RhinoIfcViewer.rhp`.
- Actual host: Rhino 8 evaluation, Windows x64 running .NET 8.
- Test helper: `.scratch/ticket08/verify-host-ticket08.py`.
- Results JSON: [host-checks.json](host-checks.json) — **11/11 PASS**.
- Operates only on private scratch fixtures. The original sample fixtures were not modified.

## Verified A2, A5, A6 & A11 Assertions (11/11 PASS)

1. **Baseline Load:** 4 exported objects, 1 skipped curve, scene available, valid IFC file on disk.
2. **No Auto-Refresh on Edit:** Adding a box to the active document does NOT trigger automatic refresh; scene revision and object count remain unchanged after 1.5 seconds.
3. **Manual Refresh After Edit:** Clicking Export & Refresh updates to 5 exported objects with a new revision.
4. **Delete + Refresh:** Deleting the added box and refreshing returns to 4 exported objects with a new revision.
5. **Move + Refresh:** Moving an object and refreshing produces the same count but a new revision (geometry changed).
6. **Idle Document Switch:** Switching from fixture A to fixture B clears scene availability and all displayed metadata (path, summary, stale badge).
7. **Refresh on New Document:** Refreshing on fixture B loads its geometry cleanly (4 exported, 1 skipped).
8. **Switch-Back Permanent Invalidation:** Switching back to fixture A clears scene and metadata permanently; no old preview resurfaces.
9. **Close/Reopen Viewer Form:** Closing the viewer form and reopening via `_OptiIfcViewer` command triggers exactly one new initial capture with clean state.
10. **Three Repeat Refresh Cycles:** Three consecutive refreshes produce 4 unique revisions, consistent object counts, and no resource accumulation.
11. **Busy-Switch Rejection:** Starting a refresh then switching documents during export causes the old result to be rejected; scene and path are cleared on the new document.

## Automated Test Suites

- C# Unit Tests: **PASS 29/29** (Snapshot validation, object conversion, IFC export, viewer session, viewer operation).
- Node Contract Tests: **PASS 18/18** (URL routing, parameters, terminal error reporting).
- Release Build: **PASS 0 warnings, 0 errors** (`dotnet build -c Release -warnaserror`).
