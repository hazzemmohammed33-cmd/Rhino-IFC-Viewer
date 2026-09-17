import { test } from "node:test";
import assert from "node:assert/strict";
import { parseViewerRequest } from "../src/contracts.ts";

const revision = "0123456789abcdef0123456789abcdef";
const base = `https://rhino-ifc.local/index.html?revision=${revision}`;
test("loads only the IFC belonging to the requested revision", () => {
  const request = parseViewerRequest(new URL(`${base}&model=models%2Fmodel-${revision}.ifc`));
  assert.equal(request.revision, revision);
  assert.equal(request.modelUrl?.href, `https://rhino-ifc.local/models/model-${revision}.ifc`);
});
test("missing model requests an empty page", () => {
  assert.equal(parseViewerRequest(new URL(base)).modelUrl, null);
});
for (const url of [
  base.replace("https:", "http:"), base.replace("rhino-ifc.local", "example.com"),
  base.replace("rhino-ifc.local", "user@rhino-ifc.local"),
  base.replace("rhino-ifc.local", "rhino-ifc.local:444"),
  base.replace("index.html", "other.html"), base.replace(revision, "ABC"),
  `${base}&revision=${revision}`, `${base}&unexpected=1`, `${base}#fragment`,
  `${base}&model=`, `${base}&model=../secret.ifc`, `${base}&model=https://example.com/a.ifc`,
  `${base}&model=models/model-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.ifc`,
  `${base}&model=models/model-${revision}.ifc&model=models/model-${revision}.ifc`,
]) {
  test(`rejects invalid request ${url}`, () => assert.throws(() => parseViewerRequest(new URL(url))));
}
