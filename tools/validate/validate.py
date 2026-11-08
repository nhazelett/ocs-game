#!/usr/bin/env python3
"""Validate scenario folders against the v0 schemas.

Usage: python3 tools/validate/validate.py [scenario-dir ...]
With no arguments, validates every folder under scenarios/ that holds a scenario.json, plus every rules file.
Exit code 1 on any error. Requires: pip install jsonschema
Codex: the Harness `validate` verb should do the same checks in C#; this script is the reference.
"""
import json, sys, pathlib
from jsonschema import Draft202012Validator, FormatChecker

ROOT = pathlib.Path(__file__).resolve().parents[2]
SCHEMA = ROOT / "scenarios" / "schema"
RULES_DIR = ROOT / "scenarios" / "rules"

def load(p): return json.loads(pathlib.Path(p).read_text(encoding="utf-8"))

def validator(name):
    s = load(SCHEMA / f"{name}.schema.json")
    Draft202012Validator.check_schema(s)
    return Draft202012Validator(s, format_checker=FormatChecker())

def errors(v, doc, label):
    out = []
    for e in sorted(v.iter_errors(doc), key=lambda e: list(e.path)):
        out.append(f"{label}: /{'/'.join(str(x) for x in e.path)}: {e.message}")
    return out

def cross_checks(sc, rules):
    errs = []
    ids = lambda key: {x["id"] for x in sc.get(key, [])}
    islands, sites, vendors, vehicles = ids("islands"), ids("sites"), ids("vendors"), ids("vehicles")
    caps = {c["id"] for c in rules["model"]["capabilities"]}
    instruments = set(rules["instruments"].keys())
    if sc["rulesVersion"] != rules["rulesVersion"]:
        errs.append(f"rulesVersion {sc['rulesVersion']} does not match rules file {rules['rulesVersion']}")
    for l in sc["transportLinks"]:
        for end in ("from", "to"):
            if l[end] not in islands: errs.append(f"link {l['id']}: {end} island {l[end]} not found")
    for s in sc["sites"]:
        if s["island"] not in islands: errs.append(f"site {s['id']}: island {s['island']} not found")
        for c in s["capabilities"]:
            if c["id"] not in caps: errs.append(f"site {s['id']}: capability {c['id']} not in rules")
    for r in sc["requirementCards"]:
        if r["site"] not in sites: errs.append(f"card {r['id']}: site {r['site']} not found")
        if r["capability"] not in caps: errs.append(f"card {r['id']}: capability {r['capability']} not in rules")
        if r["requestedStartDay"] < r["submittedDay"]: errs.append(f"card {r['id']}: requested start before submission")
    for v in sc["vendors"]:
        if v["island"] not in islands: errs.append(f"vendor {v['id']}: island {v['island']} not found")
        for o in v["offers"]:
            if o["capability"] not in caps: errs.append(f"vendor {v['id']}: capability {o['capability']} not in rules")
    for v in sc["vehicles"]:
        if v["vendor"] not in vendors: errs.append(f"vehicle {v['id']}: vendor {v['vendor']} not found")
        for isl in v.get("placeOfPerformance", []):
            if isl not in islands: errs.append(f"vehicle {v['id']}: island {isl} not found")
    for a in sc.get("inheritedArrangements", []):
        if a["instrument"] not in instruments: errs.append(f"arrangement {a['id']}: instrument {a['instrument']} not in rules")
        if a["vendor"] not in vendors: errs.append(f"arrangement {a['id']}: vendor {a['vendor']} not found")
        if a["site"] not in sites: errs.append(f"arrangement {a['id']}: site {a['site']} not found")
        if a["capability"] not in caps: errs.append(f"arrangement {a['id']}: capability {a['capability']} not in rules")
    event_ids = ids("events")
    for e in sc["events"]:
        if e["window"]["latestDay"] < e["window"]["earliestDay"]: errs.append(f"event {e['id']}: window ends before it starts")
        if e["window"]["earliestDay"] > sc["days"]: errs.append(f"event {e['id']}: window starts after the scenario ends")
        for f in e.get("followOns", []):
            if f not in event_ids: errs.append(f"event {e['id']}: follow-on {f} not found")
    all_ids = islands | sites | vendors | vehicles | event_ids | ids("requirementCards")
    for f in sc["fixedSituations"]:
        if f not in all_ids: errs.append(f"fixedSituations: {f} not found")
    for p in sc["actors"]["commander"]["priorityOrder"]:
        if p not in sites: errs.append(f"commander priorityOrder: site {p} not found")
    for s in sc["sites"]:
        if s["opensDay"] > sc["days"]: errs.append(f"site {s['id']}: opens after the scenario ends")
    return errs

def main(argv):
    errs = []
    vs, vr, vt = validator("scenario"), validator("rules"), validator("theme")
    rules_by_version = {}
    for rp in sorted(RULES_DIR.glob("*.json")):
        r = load(rp); errs += errors(vr, r, rp.name)
        rules_by_version[r.get("rulesVersion")] = r
    dirs = [pathlib.Path(a) for a in argv] or [p.parent for p in (ROOT / "scenarios").glob("*/scenario.json")]
    for d in dirs:
        sp = d / "scenario.json"
        if not sp.exists(): errs.append(f"{d}: no scenario.json"); continue
        sc = load(sp); e = errors(vs, sc, str(sp.relative_to(ROOT))); errs += e
        if not e:
            r = rules_by_version.get(sc["rulesVersion"])
            if r is None: errs.append(f"{sp.name}: rulesVersion {sc['rulesVersion']} has no rules file")
            else: errs += [f"{sp.relative_to(ROOT)}: {x}" for x in cross_checks(sc, r)]
        tp = d / "theme.json"
        if tp.exists(): errs += errors(vt, load(tp), str(tp.relative_to(ROOT)))
    for x in errs: print("ERROR", x)
    print(f"{len(dirs)} scenario folder(s), {len(rules_by_version)} rules file(s), {len(errs)} error(s)")
    return 1 if errs else 0

if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
