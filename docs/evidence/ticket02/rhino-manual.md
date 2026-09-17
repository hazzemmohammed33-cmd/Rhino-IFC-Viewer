# Rhino host verification — Ticket 02 (2026-09-16)

Tested Ticket 02 live IFC rendering inside Rhino 8 embedded WebView2 using bundled local assets.

- **PASS — WebView2 Environment and Local Virtual Host Mapping:**
  - Initialized WebView2 with separate user data directory at `%LOCALAPPDATA%\OptiDesign\RhinoIfcViewer\WebView2`.
  - Mapped local virtual host `https://rhino-ifc.local` to session site folder using `SetVirtualHostNameToFolderMapping` with `CoreWebView2HostResourceAccessKind.DenyCors`.
  - Unrelated origins and external navigations are blocked; assets are served strictly same-origin.

- **PASS — WebView2 Assembly Conflict Isolation:**
  - Rhino 8.29.26063.11001 loads `Microsoft.Web.WebView2.Core.dll` v1.0.1938.49 in the default context.
  - The plugin pins `Microsoft.Web.WebView2` v1.0.4191.47 in its dedicated `ViewerLoadContext` (`RhinoIfcViewer.WebView2`).
  - Added contextual reflection scope (`EnterContextualReflection`) in `ViewerForm.Integration.cs` so COM interop and managed wrapper types resolve within the plugin ALC.
  - Verified in live Rhino: Form ALC is `RhinoIfcViewer.WebView2`, WebView2 in Form ALC is `1.0.4191.47`, Rhino's own WebView2 is `1.0.1938.49`, with zero `MissingMethodException`.

- **PASS — Bundled Engine, Worker, and WASM Loading (A4 Loader-Path Proof):**
  - Staged all required assets beside the plugin at `viewer/`: `index.html`, hashed Vite bundles, `vendor/fragments/worker.mjs`, and `vendor/web-ifc/web-ifc.wasm`.
  - Session site copies all assets into `%LOCALAPPDATA%\OptiDesign\RhinoIfcViewer\sessions\{session-id}\site\`.
  - The That Open Fragments manager loads the local worker from `https://rhino-ifc.local/vendor/fragments/worker.mjs`.
  - The IfcLoader loads single-threaded WebAssembly from `https://rhino-ifc.local/vendor/web-ifc/web-ifc.wasm`.
  - No runtime CDN, no development server, and no remote dependencies are used.

- **PASS — Live 3D IFC Geometry Rendering:**
  - Diagnostic smoke runner loaded `samples/viewer-smoke.ifc` (8.5 MB school structural IFC) into the active session.
  - That Open Components, World, SimpleScene, SimpleRenderer, OrthoPerspectiveCamera, and IfcLoader initialized.
  - Geometry imported, bounding box calculated, camera fitted to model, and rendered frame confirmed with `renderer.three.info.render.triangles > 0`.
  - Status label updated to `"Temporary IFC loaded - not a Rhino export."`.
  - Verified visually by user screenshot and programmatically via `docs/evidence/ticket02/rhino-smoke.json` with `"result": "PASS"`.

- **PASS — Complete Offline Operation (A8 Initial Offline Proof):**
  - All web requests remain strictly on `https://rhino-ifc.local`.
  - Fully functional without internet connectivity or external network servers.
