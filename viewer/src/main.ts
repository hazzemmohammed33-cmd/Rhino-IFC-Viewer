import { parseViewerRequest } from "./contracts";
import type { ViewerApp, ViewerMessage } from "./contracts";
import { createViewer } from "./viewer";
import { createReporter } from "./reporting";
import "./style.css";

export function postViewerMessage(message: ViewerMessage): void {
  if (!window.chrome?.webview) throw new Error("WebView2 host bridge is unavailable");
  window.chrome.webview.postMessage(message);
}

export async function bootstrap(): Promise<void> {
  let app: ViewerApp | undefined;
  let terminal = false;
  let ended = false;
  let revision = "";
  const reporter = createReporter(postViewerMessage);
  const report = (message: ViewerMessage) => {
    reporter.report(message);
    terminal = true;
  };
  const fail = (error: unknown) => {
    const message = error instanceof Error ? error.message : String(error);
    console.error("IFC viewer", revision, message);
    try { report({ type: "error", revision, message }); }
    catch (bridgeError) { console.error(bridgeError); }
    app?.dispose();
    const container = document.getElementById("viewer");
    if (container) container.textContent = `Viewer failed: ${message}`;
  };
  const rejection = (event: PromiseRejectionEvent) => fail(event.reason);
  const runtimeError = (event: ErrorEvent) => fail(event.error ?? event.message);
  window.addEventListener("unhandledrejection", rejection);
  window.addEventListener("error", runtimeError);
  window.addEventListener("pagehide", () => {
    ended = true;
    reporter.end();
    app?.dispose();
    window.removeEventListener("unhandledrejection", rejection);
    window.removeEventListener("error", runtimeError);
  }, { once: true });
  try {
    const request = parseViewerRequest(new URL(location.href));
    revision = request.revision;
    if (!window.chrome?.webview) throw new Error("WebView2 host bridge is unavailable");
    if (!request.modelUrl) { report({ type: "empty", revision }); return; }
    const container = document.getElementById("viewer");
    if (!(container instanceof HTMLDivElement)) throw new Error("Viewer mount is missing");
    app = await createViewer(container);
    if (ended || terminal) { app.dispose(); return; }
    await app.load(request.modelUrl, revision);
    report({ type: "loaded", revision });
  } catch (error) { fail(error); }
}

void bootstrap();
