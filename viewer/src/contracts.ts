export type ViewerMessage =
  | { type: "loaded"; revision: string }
  | { type: "empty"; revision: string }
  | { type: "error"; revision: string; message: string };

export interface ViewerRequest {
  revision: string;
  modelUrl: URL | null;
}

export interface ViewerApp {
  load(modelUrl: URL, revision: string): Promise<void>;
  dispose(): void;
}

declare global {
  interface Window {
    chrome?: { webview?: { postMessage(message: ViewerMessage): void } };
  }
}

export function parseViewerRequest(url: URL): ViewerRequest {
  if (url.origin !== "https://rhino-ifc.local" || url.username || url.password ||
      url.pathname !== "/index.html" || url.hash) {
    throw new Error("Viewer request must use the local index page");
  }
  const query = url.searchParams;
  if (query.getAll("revision").length !== 1 || query.getAll("model").length > 1 ||
      [...query.keys()].some(key => key !== "revision" && key !== "model")) {
    throw new Error("Unexpected or duplicate viewer query");
  }
  const revision = query.get("revision")!;
  if (!/^[0-9a-f]{32}$/.test(revision)) throw new Error("Invalid viewer revision");
  const model = query.get("model");
  if (model !== null && model !== `models/model-${revision}.ifc`) {
    throw new Error("IFC path does not match the requested revision");
  }
  return { revision, modelUrl: model === null ? null : new URL(model, url) };
}
