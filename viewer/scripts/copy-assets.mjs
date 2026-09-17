import { mkdir, copyFile } from "node:fs/promises";
import { fileURLToPath } from "node:url";
import path from "node:path";

const viewerRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const copies = [
  ["node_modules/@thatopen/fragments/dist/Worker/worker.mjs", "public/vendor/fragments/worker.mjs"],
  ["node_modules/web-ifc/web-ifc.wasm", "public/vendor/web-ifc/web-ifc.wasm"]
];
for (const [source, destination] of copies) {
  const target = path.join(viewerRoot, destination);
  await mkdir(path.dirname(target), { recursive: true });
  await copyFile(path.join(viewerRoot, source), target);
}
