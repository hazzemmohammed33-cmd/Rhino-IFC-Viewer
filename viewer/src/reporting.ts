import type { ViewerMessage } from "./contracts.ts";

export function createReporter(send: (message: ViewerMessage) => void) {
  let terminal = false;
  let failed = false;
  let ended = false;
  return {
    report(message: ViewerMessage) {
      if (ended || failed || (terminal && message.type !== "error")) return;
      send(message);
      terminal = true;
      failed = message.type === "error";
    },
    end() { ended = true; }
  };
}
