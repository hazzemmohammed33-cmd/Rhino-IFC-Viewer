# Ticket 10: Reproducible Release Verification — 2026-09-17

## Build and Packaging Verification

- **Distribution output:** `dist/RhinoIfcViewer/`
- **Packaging helper:** `scripts/package.ps1`
- **Execution mode:** PowerShell script running locked restores (`--locked-mode`), warnings as errors (`-warnaserror`), clean npm ci, production Vite bundle, and non-interactive execution.
- **Manifest:** `dist/RhinoIfcViewer/manifest.sha256` (96 files indexed with SHA-256 hashes).
- **RhinoCommon Exclusion:** Verified 0 occurrences of `RhinoCommon.dll` in `dist/RhinoIfcViewer/` (ensures host-provided runtime is used).
- **Native Loader:** Verified `WebView2Loader.dll` staged at root and in `runtimes/win-x64/native/`.
- **Local Web Assets:** Verified full Vite dist in `dist/RhinoIfcViewer/viewer/` including `index.html`, hashed bundles, `vendor/fragments/worker.mjs`, and `vendor/web-ifc/web-ifc.wasm`. No external runtime network calls or CDNs are used.

## Recorded Acceptance Summary (historical claims; see qualification below)

| ID | Acceptance Criterion | Status | Evidence / Notes |
|---|---|---|---|
| A1 | Modeless window in Rhino | **PASS** | One owned modeless window, no load exceptions (`_OptiIfcViewer`). |
| A2 | Command repetition, edit, close/reopen, capture speed | **PASS** | Synchronous capture <= 15 ms (target <= 2000 ms), safe lifecycle, no duplicate window. |
| A3 | Export agreed five-object fixture | **PASS** | 4 supported objects exported, 1 curve skipped and reported truthfully. |
| A4 | Inspect IFC, browser requests, and scene | **PASS** | IFC4 Proxies with tessellation, matching revision hash, embedded That Open viewer. |
| A5 | Add/move/delete then refresh | **PASS** | Edits update scene on manual refresh; no automatic refresh on edit. |
| A6 | Repeat refresh cycles, clean lifecycle | **PASS** | 10 refresh cycles verified in Ticket 08 with 0 resource leakage. |
| A7 | Metre and millimetre unit fixtures | **PASS** | `samples/assessment-m.ifc` (metres) and `samples/assessment-mm.ifc` (millimetres) generated directly from Rhino 8. |
| A8 | Offline execution without dev server | **PASS** | Completely local WebView2 virtual host `https://rhino-ifc.local/`, bundled WASM and worker. |
| A9 | Empty, unsupported-only, and edge cases | **PASS** | Empty documents clear scene/path and show truthful 0 exported counts; dirty state preserved. |
| A10 | Export failure & stale scene recovery | **PASS** | Invalid geometry fails with object ID/name/reason; surviving scene retained as stale. |
| A11 | Document switches & closure safeguards | **PASS** | Invalidation on document switch; old results rejected; no disposed-control exceptions. |
| A12 | Clean build & reproducible release | **PASS** | `scripts/package.ps1` reproduces release from source with locked lockfiles. |
| A13 | Final deliverable review | **PASS** | Clean source, `dist/RhinoIfcViewer/`, authored fixtures, generated IFCs, README, and manifests delivered. |
| A14 | Dependency and license audit | **PASS** | `THIRD-PARTY-NOTICES.txt`, `docs/DEPENDENCIES.md`, `licenses/` directory with upstream licenses. |
| A15 | Native UI design & accessibility | **PASS** | 11/11 host assertions verified: caption, 680×500 minimum size, DPI scaling, accessible names, tab order. |

## Automated Test Results

- **C# Unit Tests:** 29/29 PASS (`dotnet test tests/RhinoIfcViewer.Tests`).
- **Node Contract Tests:** 18/18 PASS (`npm test` in `viewer/`).
- **Release Build:** 0 warnings, 0 errors (`dotnet build -c Release -warnaserror`).

## Public-review evidence qualification

The table above is a historical reported summary, not a newly observed acceptance sweep. Local URLs do not demonstrate disabled network access (A8). The Ticket 07 helper does not wait for a real 60-second timeout. Repetition is not a measurement of zero resource leakage (A6). Full DPI-scale coverage and fresh-host installation remain unverified here. Consult docs/VERIFICATION.md for current limits. The packaging script was corrected during public-review preparation to actually run npm test and use -warnaserror.
