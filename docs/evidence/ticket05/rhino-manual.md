# Rhino host verification — Ticket 05 (2026-09-16)

Tested Ticket 05 full refresh lifecycle, UI refinement, and live geometry modification in Rhino 8.

- **PASS — Initial Auto-Refresh (A5):**
  - Launching `_OptiIfcViewer` automatically triggered initial export and render of `assessment.3dm` without manual click.
  - Initial load completed in 4063 ms.
  - Successfully exported 4 supported objects, 1 excluded curve.
  - Status displayed: `Loaded with exclusions`.
  - Exported IFC written to `%LOCALAPPDATA%\OptiDesign\RhinoIfcViewer\exports\`.

- **PASS — UI Aesthetics & Control Refinement:**
  - Modern `Export & Refresh` button with 1px border (`#1D4ED8`), vibrant blue `#2563EB`, hand cursor (`Cursors.Hand`), and hover transition to `#1D4ED8`.
  - Header subtitle rendered cleanly: *"Review the exported snapshot of your Rhino model."*.
  - Status bar shows color-coded status, summary counts (`Objects: 5 exported · 1 skipped`), and selectable file path text box.

- **PASS — Live Geometry Modification & Manual Refresh (A5/A6):**
  - Authored a new 3D Brep box into the active document.
  - Programmatically invoked `btn.PerformClick()`.
  - Button transitioned to `Working...` (disabled state with wait cursor).
  - Background `Task.Run` exported new IFC4 model (`model-90de870d7f8b4a9aae01509423f6a82e.ifc`).
  - WebView2 That Open Engine reloaded the scene and rendered all 5 objects in 3D.
  - Button re-enabled with text `Export & Refresh`.

- Recorded evidence in `docs/evidence/ticket05/refresh-checks.json` (Overall: PASS, 9/9 assertions passed).
