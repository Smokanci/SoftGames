#!/usr/bin/env node
// gate-check.mjs — execute the CHECK lines in a gates file, record the deciding
// output as evidence, and report what is actually proven versus merely asserted.
//
// Zero dependencies, Node 16+. CHECK lines run under /bin/sh (not zsh), so a
// gate behaves the same however the checker was launched.
//
// Usage:
//   node gate-check.mjs                    GATES.md + gates/*.md in the cwd
//   node gate-check.mjs <file>...          only the named files
//   node gate-check.mjs --status [file..]  report only, never writes, never runs
//   node gate-check.mjs --fast [file..]    skip gates already verified (see below)
//   node gate-check.mjs --timeout 300      per-CHECK timeout in seconds (default 120)
//   node gate-check.mjs --strict [file..]  only verified gates count as success
//
// Every CHECK re-runs by default. A box that passed at minute 10 and broke at
// minute 60 is the failure this whole file exists to catch, so a stale pass is
// never trusted. Use --fast only when a slow gate (a Unity test run) is known
// good and you are iterating on a different one.
//
// Exit: 0 = nothing unmet, 1 = unmet gates remain, 2 = usage or read error.
//
// Plain exit 0 means "no gate is outstanding" — it counts an asserted or an
// abandoned gate as settled, because a human settled it. Anything reading the
// exit code to decide whether the work is proven (CI, a hook, another script)
// must pass --strict, which succeeds only when every gate was verified by a
// check that ran.

import { readFileSync, writeFileSync, existsSync, readdirSync } from "node:fs";
import { spawnSync } from "node:child_process";
import { join } from "node:path";

const argv = process.argv.slice(2);
const opts = { status: false, fast: false, strict: false, timeout: 120 };
const files = [];

for (let i = 0; i < argv.length; i++) {
  const a = argv[i];
  if (a === "--status") { opts.status = true; }
  else if (a === "--fast") { opts.fast = true; }
  else if (a === "--strict") { opts.strict = true; }
  else if (a === "--timeout") {
    const n = Number(argv[++i]);
    if (!Number.isFinite(n) || n <= 0) { die(`--timeout needs a positive number, got "${argv[i]}"`); }
    opts.timeout = n;
  }
  else if (a === "-h" || a === "--help") { console.log(help()); process.exit(0); }
  else if (a.startsWith("--")) { die(`unknown flag ${a}`); }
  else { files.push(a); }
}

// --fast and --status both decide a gate from evidence recorded by an earlier
// run. --strict exists to mean "a check ran and passed", so the combination
// would report proof for a command that never executed.
if (opts.strict && (opts.fast || opts.status)) {
  die("--strict cannot combine with --fast or --status; both trust a recorded pass instead of running the check");
}

function die(msg) { console.error(`gate-check: ${msg}`); process.exit(2); }
function help() {
  const out = [];
  for (const l of readFileSync(new URL(import.meta.url), "utf8").split("\n").slice(1)) {
    if (!l.startsWith("//")) { break; }
    out.push(l.replace(/^\/\/ ?/, ""));
  }
  return out.join("\n");
}

function discover(dir) {
  const found = [];
  if (existsSync(join(dir, "GATES.md"))) { found.push(join(dir, "GATES.md")); }
  const gdir = join(dir, "gates");
  if (existsSync(gdir)) {
    for (const f of readdirSync(gdir).sort()) {
      if (f.endsWith(".md")) { found.push(join(gdir, f)); }
    }
  }
  return found;
}

const targets = files.length ? files : discover(process.cwd());
if (!targets.length) { die("no gate files found (GATES.md or gates/*.md)"); }
for (const f of targets) {
  if (!existsSync(f)) { die(`no such file: ${f}`); }
}

const GATE_RE    = /^(\s*)- \[([ xX])\] (.*)$/;
const ATTR_RE    = /^(\s+)(CHECK|EXPECT|EVIDENCE):[ \t]?(.*)$/;
const ABANDON_RE = /^\s*ABANDON:\s*(\S+?):?(?:\s+(.*))?$/;

// The words a report reaches for when it has nothing to show. An EVIDENCE line
// that says one of these proves exactly as much as an empty one.
const NON_EVIDENCE = new Set([
  "pending", "tbd", "todo", "done", "ok", "okay", "yes", "y", "true",
  "verified", "complete", "completed", "confirmed", "checked", "passes",
  "passed", "pass", "works", "working", "fixed", "tested", "good",
  "all good", "looks good", "lgtm", "n/a", "na", "none", "-",
]);

function isRealEvidence(text) {
  if (text === null || text === undefined) { return false; }
  const norm = String(text).trim().toLowerCase().replace(/[.!]+$/, "");
  if (!norm) { return false; }
  return !NON_EVIDENCE.has(norm);
}

// A check that cannot fail is worse than no check, because it looks like proof.
// These shapes succeed whatever the repo does, so they prove the shell works and
// nothing else. Anything reading a variable or a subshell can vary, so it is not
// inert — the detector would rather miss one than condemn a real check.
const INERT_SEGMENT = /^(true|:|exit\s+0|echo(\s|$)|printf(\s|$))/;

function isInertCheck(cmd) {
  if (/[$`]/.test(cmd)) { return false; }
  const segments = cmd.split(/\|\||&&|;|\|/).map(x => x.trim()).filter(Boolean);
  return segments.length > 0 && segments.every(x => INERT_SEGMENT.test(x));
}

function parse(lines) {
  const gates = [];
  const abandoned = new Map();
  let cur = null;

  lines.forEach((line, i) => {
    const g = line.match(GATE_RE);
    if (g) {
      const body = g[3].trim();
      const idMatch = body.match(/^(\S+?):/);
      cur = {
        lineNo: i,
        indent: g[1],
        checked: g[2].toLowerCase() === "x",
        id: idMatch ? idMatch[1] : `line${i + 1}`,
        title: body.replace(/^\S+?:\s*/, ""),
        check: null,
        expect: null,
        evidence: null,
        evidenceLine: -1,
        lastAttrLine: i,
        lastField: "title",
        evidenceCont: [],
        malformed: null,
      };
      gates.push(cur);
      return;
    }

    const ab = line.match(ABANDON_RE);
    if (ab) { abandoned.set(ab[1], (ab[2] || "").trim()); cur = null; return; }

    const a = cur && line.match(ATTR_RE);
    if (a) {
      const key = a[2].toLowerCase();
      cur[key] = a[3].trim();
      cur.lastAttrLine = i;
      cur.lastField = key;
      if (key === "evidence") { cur.evidenceLine = i; }
      return;
    }

    // A deeper-indented line that is not an attribute continues the field above
    // it. A title wrapped onto a second line used to end the gate block right
    // here, which discarded the CHECK below it and reported the gate as
    // unchecked with no hint that the file, not the work, was the problem.
    if (cur && line.trim() && line.length - line.trimStart().length > cur.indent.length) {
      const text = line.trim();
      if (cur.lastField === "title" || cur.lastField === "evidence") {
        cur[cur.lastField] = `${cur[cur.lastField]} ${text}`.trim();
        if (cur.lastField === "evidence") { cur.evidenceCont.push(i); }
        cur.lastAttrLine = i;
        return;
      }
      // Joining a wrapped CHECK changes the command; joining a wrapped EXPECT
      // changes the pattern. Both are silent corruptions, so refuse to guess.
      if (!cur.malformed) { cur.malformed = { line: i + 1, field: cur.lastField.toUpperCase() }; }
      return;
    }

    // Anything else ends the gate block. Keeps a stray bullet in a description
    // from swallowing the next gate's attributes.
    cur = null;
  });

  return { gates, abandoned };
}

function expectMatches(expect, output) {
  // Trailing newlines must never decide a gate: /^done$/ on "done\n" fails in
  // JS without the m flag, which is a footgun, not a result.
  const text = output.trim();
  const rx = expect.match(/^\/(.+)\/([gimsuy]*)$/);
  if (rx) {
    try { return new RegExp(rx[1], rx[2]).test(text); }
    catch { return false; }
  }
  return text.includes(expect);
}

function deciding(output, max = 200) {
  const lines = output.split(/\r?\n/).map(s => s.trim()).filter(Boolean);
  const text = lines.slice(-2).join(" | ") || "(no output)";
  return text.length > max ? `${text.slice(0, max - 1)}…` : text;
}

const totals = { verified: 0, asserted: 0, abandoned: 0, unmet: 0 };
const unmetIds = [];
const assertedIds = [];
const abandonedList = [];

for (const file of targets) {
  let text;
  try { text = readFileSync(file, "utf8"); }
  catch (e) { die(`cannot read ${file}: ${e.message}`); }

  const lines = text.split(/\r?\n/);
  const { gates, abandoned } = parse(lines);
  if (!gates.length) { console.log(`${file}: no gates found`); continue; }

  const inserts = [];
  let changed = false;

  for (const gate of gates) {
    if (abandoned.has(gate.id)) {
      // Dropping a gate is allowed; dropping it silently is not. The reason is
      // the whole cost of abandoning, so a bare ABANDON leaves the gate open.
      const reason = abandoned.get(gate.id);
      if (!reason) {
        totals.unmet++;
        unmetIds.push(gate.id);
        console.log(`  UNMET ${gate.id} (ABANDON gives no reason): ${gate.title}`);
        continue;
      }
      totals.abandoned++;
      abandonedList.push(`${gate.id} — ${reason}`);
      console.log(`  ABANDONED ${gate.id}: ${gate.title} — ${reason}`);
      continue;
    }

    // A wrapped CHECK or EXPECT cannot be joined without changing what the gate
    // does, so the gate stays open until the file says plainly what it meant.
    if (gate.malformed) {
      totals.unmet++;
      unmetIds.push(gate.id);
      console.log(`  MALFORMED ${gate.id}: ${gate.title}`);
      console.log(`       line ${gate.malformed.line} continues ${gate.malformed.field}: put it on one line — joining it would change what the gate ${gate.malformed.field === "CHECK" ? "runs" : "matches"}.`);
      if (gate.checked && !opts.status) {
        lines[gate.lineNo] = lines[gate.lineNo].replace(/- \[[xX]\]/, "- [ ]");
        changed = true;
      }
      continue;
    }

    // `CHECK:` with nothing after it is almost always a command that wrapped to
    // the next line. Treating it as an absent check would quietly demote the gate
    // from verified to asserted, which is the downgrade the ledger must show.
    if (gate.check !== null && !gate.check.trim()) {
      totals.unmet++;
      unmetIds.push(gate.id);
      console.log(`  UNMET ${gate.id} (CHECK: line is empty): ${gate.title}`);
      continue;
    }

    if (gate.check && isInertCheck(gate.check)) {
      totals.unmet++;
      unmetIds.push(gate.id);
      console.log(`  SUSPECT ${gate.id}: ${gate.title}`);
      console.log(`       CHECK cannot fail, so it proves nothing: ${gate.check}`);
      if (gate.checked && !opts.status) {
        lines[gate.lineNo] = lines[gate.lineNo].replace(/- \[[xX]\]/, "- [ ]");
        changed = true;
      }
      continue;
    }

    const alreadyVerified = gate.check && gate.checked && isRealEvidence(gate.evidence);
    const shouldRun = gate.check && !opts.status && !(opts.fast && alreadyVerified);

    if (shouldRun) {
      const res = spawnSync(gate.check, {
        shell: "/bin/sh",
        encoding: "utf8",
        timeout: opts.timeout * 1000,
        maxBuffer: 8 * 1024 * 1024,
      });
      const output = `${res.stdout || ""}\n${res.stderr || ""}`;
      // EXPECT decides when present — a check may exit non-zero by design
      // (grep finding nothing is a pass for an absence gate). Otherwise the
      // exit code decides.
      const ok = res.error
        ? false
        : gate.expect ? expectMatches(gate.expect, output) : res.status === 0;
      const raw = res.error
        ? `check did not run: ${res.error.message}`
        : output.trim() ? deciding(output) : `no output, exit status ${res.status}`;
      // A real run can print something that reads like a hand-wave ("ok", "none").
      // Qualify it, or the stoplist below rejects the checker's own result and the
      // gate becomes unsatisfiable: PASS on screen, UNMET in the ledger, [x] on disk.
      const proof = isRealEvidence(raw) ? raw : `${raw} (exit status ${res.status})`;

      if (ok) {
        lines[gate.lineNo] = lines[gate.lineNo].replace("- [ ]", "- [x]");
        gate.checked = true;
        gate.evidence = proof;
        if (gate.evidenceLine !== -1) {
          const indent = lines[gate.evidenceLine].match(/^\s*/)[0];
          lines[gate.evidenceLine] = `${indent}EVIDENCE: ${proof}`;
          // The fresh proof is one line. Leaving the old wrapped remainder behind
          // would read as evidence on the next run and grow the block each time.
          for (const c of gate.evidenceCont) { lines[c] = null; }
        } else {
          inserts.push({ after: gate.lastAttrLine, text: `${gate.indent}  EVIDENCE: ${proof}` });
        }
        changed = true;
        console.log(`  PASS ${gate.id}: ${gate.title}`);
      } else {
        // A gate that used to pass and now fails must lose its box, or the
        // ledger keeps reporting a result that is no longer true.
        if (gate.checked) {
          lines[gate.lineNo] = lines[gate.lineNo].replace(/- \[[xX]\]/, "- [ ]");
          gate.checked = false;
          changed = true;
          console.log(`  REGRESSED ${gate.id}: ${gate.title}`);
        } else {
          console.log(`  FAIL ${gate.id}: ${gate.title}`);
        }
        console.log(`       ${proof}`);
      }
    }

    const proven = isRealEvidence(gate.evidence);
    if (!gate.checked || !proven) {
      totals.unmet++;
      unmetIds.push(gate.id);
      if (opts.status || !shouldRun) {
        const why = !gate.checked ? "unchecked"
          : !gate.evidence ? "no EVIDENCE line"
          : `EVIDENCE says "${gate.evidence}", which proves nothing`;
        console.log(`  UNMET ${gate.id} (${why}): ${gate.title}`);
      }
    } else if (gate.check) {
      totals.verified++;
    } else {
      totals.asserted++;
      assertedIds.push(gate.id);
    }
  }

  if (changed || inserts.length) {
    for (const ins of inserts.sort((a, b) => b.after - a.after)) {
      lines.splice(ins.after + 1, 0, ins.text);
    }
    if (!opts.status) { writeFileSync(file, lines.filter(l => l !== null).join("\n")); }
  }
  console.log(`${file}: ${gates.length} gates`);
}

const total = totals.verified + totals.asserted + totals.unmet + totals.abandoned;
const parts = [`${totals.verified} verified`];
if (totals.asserted) { parts.push(`${totals.asserted} asserted (unproven)`); }
if (totals.abandoned) { parts.push(`${totals.abandoned} abandoned`); }
parts.push(`${totals.unmet} unmet`);

// Every gate is accounted for exactly once. A ledger that does not add up is
// not a ledger.
console.log(`\nLEDGER ${total} gates — ${parts.join(", ")}`);
if (assertedIds.length) {
  console.log(`ASSERTED, no runnable check: ${assertedIds.join(", ")} — these rest on a claim, not a result.`);
}
if (abandonedList.length) {
  console.log(`ABANDONED, never proven: ${abandonedList.join("; ")}`);
}
if (totals.unmet) {
  console.log(`UNMET ${totals.unmet}: ${unmetIds.join(", ")}`);
  process.exit(1);
}
if (opts.strict && (totals.asserted || totals.abandoned)) {
  console.log(`STRICT: ${totals.asserted + totals.abandoned} gates were settled by hand, not proven.`);
  process.exit(1);
}
process.exit(0);
