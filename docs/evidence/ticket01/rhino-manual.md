# Rhino host verification — 2026-09-16

Tested existing Ticket 01 production build, without production-code changes.

- **PASS — launch/license:** Rhino 8.29.26063.11001 opened an empty workspace. Title showed Evaluation, 89 days remaining; this is an active evaluation license, not a perpetual-license claim.
- **PASS — actual runtime:** Process modules showed coreclr/System.Private.CoreLib from Microsoft.NETCore.App/8.0.25 and System.Windows.Forms from Microsoft.WindowsDesktop.App/8.0.25. The live script independently reported System.Environment.Version 8.0.25. SystemInfo was invoked but no usable report appeared; runtime evidence comes from the process and live script instead.
- **PASS — install/load:** PlugInManager Install opened the complete build folder's RhinoIfcViewer.rhp. Manager showed RhinoIfcViewer, Loaded Yes, enabled, with command OptiIfcViewer.
- **PASS — visible shell:** OptiIfcViewer opened the native preview with neutral text, disabled export action, no fake model/counts/path. WebView2 remains uninitialized as required for Ticket 01.
- **PASS — live assertions:** rhino-checks.json records 27 passing assertions, including repeated command results, same form/browser references, minimized-form restoration, modeless state, moving a box while preview remains visible, actual changed bounds, disposed form/browser on closure, three additional close/reopen cycles and preservation of object ID, geometry CRC, name/layer/mode, selection and document dirty state. Save followed by activation also retained Modified=False.
- **PASS — native ownership:** rhino-owner.json records GW_OWNER=68342, matching RhinoApp.MainWindowHandle=68342 for preview handle 115146946. This directly verifies native ownership.
- **PASS — shutdown:** With the preview visible and the authored scratch model saved, clicked Rhino's native Close button. No exception or save/discard prompt appeared; subsequent window enumeration contained no Rhino windows and process inventory contained zero Rhino processes. No unrelated document was discarded.

The authored box is retained in `.scratch/ticket01/host-check-box.3dm`; it is not the later five-object assessment fixture. The two Python helpers live under `.scratch/ticket01/` and do not ship in the plugin. They execute in real Rhino; they are not standalone unit tests or mocked-host tests.

Test-helper failures were corrected before the final results: the first script attempted ObjectAttributes.DataCRC, which is unavailable; it now compares explicit attributes and geometry CRC. Initial failure evidence is retained in rhino-checks-initial.json. The owner helper initially failed JSON serialization of a CLR integer; explicit Python integer conversion fixed it. Neither failure was a production-plugin defect.

Remaining **NOT RUN**: IFC export/rendering, capture performance, viewer timeout, full DPI/visual matrix and later document-operation safeguards. These belong to later tickets. No final distribution has been packaged or accepted.
