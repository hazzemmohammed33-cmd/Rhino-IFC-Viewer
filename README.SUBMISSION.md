# Rhino IFC Viewer

A Rhino 8 add-in that exports the active model to IFC and displays it inside Rhino using the That Open Company viewer.

## Implementation

- **C# / .NET 8 and RhinoCommon:** Capture supported geometry from the active document without modifying it.
- **xBIM Essentials:** Write a geometry-focused IFC4 file using triangulated `IfcBuildingElementProxy` objects, with coordinates in metres.
- **WinForms and WebView2:** Host a simple modeless window inside Rhino.
- **TypeScript, That Open Components, Fragments and web-ifc:** Load the exported IFC into the embedded 3D viewer. Viewer assets are bundled locally; no server or API key is needed.

Geometry capture runs on Rhino's UI thread; IFC writing and viewer loading run asynchronously. Each refresh displays a new snapshot of the model.

## Requirements

- Windows x64, licensed Rhino 8 running .NET 8, and Microsoft Edge WebView2 Runtime.
- To build: .NET SDK 10.0.400, Node.js 24 and npm 11. Package restore requires internet access; the packaged viewer runs offline.

## Build from source

From the source root, run:

```powershell
powershell.exe -NoProfile -File .\scripts\package.ps1
```

The script restores locked dependencies, builds the plug-in and viewer, runs automated tests, and creates `dist/RhinoIfcViewer/`.

## Run

A prebuilt package ready for direct testing is available on the [GitHub Releases page (v0.1.0)](https://github.com/hazzemmohammed33-cmd/Rhino-IFC-Viewer/releases/tag/v0.1.0).

1. Keep the complete `dist/RhinoIfcViewer/` folder together, including its DLLs and `viewer` folder.
2. In Rhino, run `PlugInManager`, choose **Install**, and select `RhinoIfcViewer.rhp` from that folder.
3. Open a model and run `OptiIfcViewer`. The window exports the active model and displays the IFC automatically.
4. Edit the Rhino model, then click **Export & Refresh** to update the preview. The UI shows the exported file path and exported/skipped counts.

## Scope and errors

Supports Breps, surfaces, extrusions and meshes, including hidden and locked objects. Unsupported types, including curves and block instances, are skipped and reported. Curved geometry is meshed; elements use generic IFC types without semantic BIM classification. Updates are manual.

If a supported object cannot be converted, the entire new export fails with object details. An available previous preview is marked stale. Viewer errors retain the valid export path and allow retry. Switching documents clears the previous preview and requires manual refresh.

If the window or model fails to load, check Rhino's .NET runtime, WebView2 installation and that the complete release folder is present. For full technical details see [README.DETAILED.md](README.DETAILED.md), for recorded checks see [docs/VERIFICATION.md](docs/VERIFICATION.md), and see `THIRD-PARTY-NOTICES.txt` for dependency notices.
