import { test } from "node:test";
import assert from "node:assert/strict";
import { createReporter } from "../src/reporting.ts";

test("a displayed scene failure is reported once after loaded", () => {
  const sent: unknown[] = [];
  const reporter = createReporter(message => sent.push(message));
  reporter.report({ type: "loaded", revision: "a" });
  reporter.report({ type: "loaded", revision: "a" });
  reporter.report({ type: "error", revision: "a", message: "renderer lost" });
  reporter.report({ type: "error", revision: "a", message: "duplicate" });
  assert.deepEqual(sent, [{ type: "loaded", revision: "a" }, { type: "error", revision: "a", message: "renderer lost" }]);
});

test("page exit and failed loads cannot report delayed success", () => {
  const sent: unknown[] = [];
  const reporter = createReporter(message => sent.push(message));
  reporter.report({ type: "error", revision: "a", message: "missing asset" });
  reporter.report({ type: "loaded", revision: "a" });
  reporter.end();
  reporter.report({ type: "error", revision: "a", message: "late" });
  assert.equal(sent.length, 1);
});
