# Rhino IFC Viewer

A Rhino 8 plug-in that exports the active 3D model to IFC4 and displays it inside an owned, modeless preview window using the That Open Company (formerly IFC.js) 3D viewer.

## 1. Purpose and Tested Platform

- **Purpose:** Fast, offline, geometry-accurate IFC4 snapshot preview directly inside Rhino.
- **Tested Environment:** Rhino 8 Evaluation (v8.29.26063.11001) on Windows 11 x64.
- **Runtimes:** .NET 8 (Windows Desktop runtime 8.0.25) and Microsoft Edge WebView2 Runtime.
- **Output:** Packaged standalone distribution in `dist/RhinoIfcViewer/`.

## 2. Run the Built Plug-in

1. Keep the complete `dist/RhinoIfcViewer/` folder together (do not separate DLLs, `viewer/`, or native assets).
2. Launch Rhino 8 in .NET 8 mode (verify via `SystemInfo` if needed).
3. In Rhino, run the `PlugInManager` command, click **Install**, and select `RhinoIfcViewer.rhp` from the release folder.
4. Open any Rhino model and run the command:
   ```text
   OptiIfcViewer
   ```
   *(Do not copy dependencies into Rhino's program files directory; the plug-in resolves dependencies from its own folder).*

## 3. Usage and Snapshot Semantics

- **Initial Export:** When `OptiIfcViewer` opens for the first time, it automatically captures the active model, exports an IFC4 file, and displays the 3D preview.
- **Manual Refresh:** Editing the Rhino model does *not* trigger automatic re-exports. Click **Export & Refresh** to capture and display current geometry.
- **Truthful Exclusions:** Unsupported types (such as curves, text, or block instances) are safely skipped and reported in the status bar (e.g. `Objects: 4 exported · 1 skipped`).
- **Failure Recovery:** If an export fails, an available previous scene is preserved as **stale** with matching metadata. If that scene no longer exists, the viewer shows an error and retains eligible valid export-file metadata.
- **Selectable Path:** The read-only path box displays the full `.ifc` path on disk and supports keyboard/mouse selection for inspection in external BIM software.

## 4. Rebuild from Source

### Prerequisites
- .NET SDK 10.0.400 (or an allowed patch under global.json)
- Node.js 24 and npm 11
- Windows PowerShell

### Exact Build and Packaging Command
From the repository root, run:

```powershell
powershell.exe -NoProfile -File .\scripts\package.ps1
```

This automated script:
1. Restores npm dependencies with `npm ci` and builds the production web bundle with Vite.
2. Restores NuGet packages using `--locked-mode` to ensure reproducible binaries.
3. Builds `RhinoIfcViewer.csproj` in `Release` configuration with warnings as errors.
4. Executes the full C# unit test suite (29 tests) and Node test suite (18 tests).
5. Stages the self-contained output into `dist/RhinoIfcViewer/` and writes `manifest.sha256`.

## 5. Architecture

```text
Rhino Document (UI Thread)
       │
       ▼ (Capture target <= 2000 ms on the small fixture)
ModelSnapshot (Detached immutable geometry in metres)
       │
       ▼ (Async background task)
IfcExporter (xBIM Essentials) ──► Valid IFC4 file on disk
       │
       ▼ (Local virtual host https://rhino-ifc.local/)
WebView2 Form ──► That Open Components + WebAssembly (web-ifc.wasm)
```

- **Zero Cloud / Zero CDN:** All web scripts, workers, WASM files, and IFC models are loaded strictly from the local filesystem via WebView2 custom virtual hostname mappings.
- **No API Keys or Servers:** Completely offline execution.

## 6. Geometry Scope and Limitations

- **Supported Geometry:** Breps, Surfaces, Extrusions, and Meshes (including hidden and locked objects).
- **Unsupported Types:** Curves, point clouds, text, annotations, block instances, SubD, and clipping planes are skipped and truthfully reported in export counts.
- **Units:** Coordinates are normalized to metres in the IFC4 output, preserving world coordinate placements and offsets.
- **BIM Classification:** Elements are exported as generic `IfcBuildingElementProxy` objects with tessellated faceted geometry; semantic BIM classification and property editing are out of scope.

## 7. Troubleshooting

- **Plug-in does not load:** Ensure Rhino 8 is configured to use the .NET 8 runtime (run `SetDotNetRuntime` if required).
- **Missing WebView2:** Ensure the Microsoft Edge WebView2 Runtime is installed.
- **Viewer Error / Failed to Fetch:** Ensure the complete `dist/RhinoIfcViewer/` folder was copied intact, including `viewer/vendor/web-ifc/web-ifc.wasm` and `viewer/vendor/fragments/worker.mjs`.

## 8. Verification and Acceptance Matrix

Implementation and recorded checks are summarized in [docs/VERIFICATION.md](docs/VERIFICATION.md). Recorded host assertions cover rendering, geometry, refresh, selected lifecycle and recovery scenarios. They do not establish a complete final acceptance sweep: actual 60-second timeout, network-disabled operation, comprehensive DPI/resource checks and a fresh installation require separate evidence.

## 9. Dependencies and Notices

- **xBIM Essentials:** CDDL-1.0 (Open-source IFC geometry and STEP engine).
- **That Open Components / Fragments / Three.js:** MIT License (Local 3D web viewer).
- **web-ifc:** MPL-2.0 (Local WebAssembly IFC parser).
- **Microsoft.Web.WebView2:** Microsoft SDK terms (redistributable native and managed wrappers).

Full dependency inventory and licenses are located in [`docs/DEPENDENCIES.md`](docs/DEPENDENCIES.md), [`THIRD-PARTY-NOTICES.txt`](THIRD-PARTY-NOTICES.txt), and the [`licenses/`](licenses/) directory.
