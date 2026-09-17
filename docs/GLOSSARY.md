# Rhino IFC Viewer - glossary

- **Capture-time snapshot:** Detached geometry representing the source at capture; later edits do not update it.
- **Operation revision:** Identifier pairing an export/load attempt with browser acknowledgements; not a document-edit detector.
- **Enumeration boundary:** Current active-document objects, including hidden/locked objects; excluding deleted objects, instance-definition members and linked/reference-document geometry.
- **Supported object:** An in-boundary mesh, Brep, surface or extrusion covered by the conversion baseline.
- **Skipped object:** An unsupported object inside the enumeration boundary. Outside-boundary objects are not skips.
- **Invalid supported geometry:** A covered object that fails meshing/validation. It fails the current export with the offending object name/ID and reason; it is not an unsupported skip.
- **Valid export metadata:** Source, counts and path from an actual successful IFC export; these do not prove display success.
- **Displayed snapshot:** A revision acknowledged as loaded, with a scene still available for display.
- **Retained scene:** A previous scene that still exists and may remain visible after export failure with a warning.
- **Stale warning:** Warning after a failed refresh when retaining a scene; not automatic detection of later edits.
- **Loaded:** The matching capture-time revision completed viewer integration.
- **Embedded-rendering gate:** Actual IFC renders inside Rhino WebView2 with bundled viewer, worker and WASM.
- **Assessment scheduling:** Owned by Hazem. The agent does not track project effort or stop based on a time budget; this does not remove runtime performance measurements or load timeout behavior.

- **Capture pause target:** At most two seconds for synchronous geometry capture/meshing on the agreed small fixture, measured until UI control returns; an acceptance target without a larger-model guarantee. Asynchronous IFC writing/loading are excluded from this interval.

- **Document switch:** Change of active document identity; clears the previous preview and displayed metadata and requests manual refresh. This is distinct from editing geometry inside the same document.
