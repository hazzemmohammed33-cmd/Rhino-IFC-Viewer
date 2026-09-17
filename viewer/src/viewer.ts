import * as OBC from "@thatopen/components";
import * as THREE from "three";
import { IFCBUILDINGELEMENTPROXY } from "web-ifc";
import type { ViewerApp } from "./contracts";

export async function createViewer(container: HTMLDivElement): Promise<ViewerApp> {
  // Fail at the missing asset itself, before third-party initialization can hang.
  for (const asset of ["/vendor/fragments/worker.mjs", "/vendor/web-ifc/web-ifc.wasm"]) {
    const response = await fetch(new URL(asset, location.origin));
    if (!response.ok) throw new Error(`Viewer asset ${asset}: HTTP ${response.status}`);
    await response.arrayBuffer();
  }
  const components = new OBC.Components();
  let disposed = false;
  const abort = new AbortController();
  const dispose = () => {
    if (disposed) return;
    disposed = true;
    abort.abort();
    components.dispose();
  };
  try {
    const world = components.get(OBC.Worlds).create<OBC.SimpleScene, OBC.OrthoPerspectiveCamera, OBC.SimpleRenderer>();
    world.scene = new OBC.SimpleScene(components);
    world.scene.setup();
    world.scene.three.background = new THREE.Color("#EBEEF2");
    world.renderer = new OBC.SimpleRenderer(components, container);
    world.camera = new OBC.OrthoPerspectiveCamera(components);
    const fragments = components.get(OBC.FragmentsManager);
    fragments.init(new URL("/vendor/fragments/worker.mjs", location.origin).href);
    world.camera.controls.addEventListener("update", () => {
      if (!disposed) void fragments.core.update();
    });
    const loader = components.get(OBC.IfcLoader);
    loader.onIfcImporterInitialized.add(importer => importer.classes.elements.add(IFCBUILDINGELEMENTPROXY));
    await loader.setup({
      autoSetWasm: false,
      wasm: { path: new URL("/vendor/web-ifc/", location.origin).href, absolute: true }
    });
    components.init();
    return {
      async load(modelUrl, revision) {
        if (disposed) throw new Error("Viewer was disposed");
        const response = await fetch(modelUrl, { signal: abort.signal });
        if (!response.ok) throw new Error(`IFC fetch: HTTP ${response.status}`);
        const model = await loader.load(new Uint8Array(await response.arrayBuffer()), false, revision);
        if (disposed) throw new Error("Viewer was disposed during IFC import");
        if ((await model.getItemsIdsWithGeometry()).length === 0 || model.box.isEmpty())
          throw new Error("IFC contains no renderable geometry");
        model.useCamera(world.camera.three);
        world.scene.three.add(model.object);
        const center = model.box.getCenter(new THREE.Vector3());
        const size = model.box.getSize(new THREE.Vector3()).length();
        if (!Number.isFinite(size) || size <= 0) throw new Error("IFC has invalid model bounds");
        await world.camera.controls.setLookAt(center.x + size, center.y + size, center.z + size,
          center.x, center.y, center.z, false);
        await world.camera.controls.fitToBox(model.box, false);
        await fragments.core.update(true);
        // A renderer update with actual triangles is the success gate, never a timer or fetch alone.
        await new Promise<void>((resolve, reject) => {
          const afterFrame = () => {
            if (disposed) return finish(new Error("Viewer disposed before its rendered frame"));
            if (world.renderer!.three.info.render.triangles > 0) finish();
          };
          const canceled = () => finish(new Error("Viewer disposed before its rendered frame"));
          const finish = (error?: Error) => {
            world.renderer!.onAfterUpdate.remove(afterFrame);
            abort.signal.removeEventListener("abort", canceled);
            if (error) reject(error); else resolve();
          };
          world.renderer!.onAfterUpdate.add(afterFrame);
          abort.signal.addEventListener("abort", canceled, { once: true });
          if (disposed) canceled();
        });
      },
      dispose
    };
  } catch (error) {
    dispose();
    throw error;
  }
}
