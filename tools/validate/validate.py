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
    cap_data = {c["id"]: c for c in rules["model"]["capabilities"]}
    links = ids("transportLinks")
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
        for mode in v.get("lift", []):
            if mode not in {"airlift", "sealift"}: errs.append(f"vendor {v['id']}: lift mode {mode} is not airlift or sealift")
        for o in v["offers"]:
            if o["capability"] not in caps: errs.append(f"vendor {v['id']}: capability {o['capability']} not in rules")
            source = o.get("sourceIsland")
            if source is not None:
                if source not in islands: errs.append(f"vendor {v['id']} {o['capability']}: sourceIsland {source} not found")
                if cap_data.get(o["capability"], {}).get("delivery") != "consumable":
                    errs.append(f"vendor {v['id']} {o['capability']}: sourceIsland applies only to consumables")
    for funding in sc.get("fundingLines", []):
        for capability in funding.get("capabilities", []):
            if capability not in caps:
                errs.append(f"funding line {funding['id']}: capability {capability} not in rules")
        if funding["availableDay"] > funding["expiresDay"]:
            errs.append(f"funding line {funding['id']}: expires before it is released")
    for v in sc["vehicles"]:
        if v["vendor"] not in vendors: errs.append(f"vehicle {v['id']}: vendor {v['vendor']} not found")
        for isl in v.get("placeOfPerformance", []):
            if isl not in islands: errs.append(f"vehicle {v['id']}: island {isl} not found")
    # Mirror ScenarioBundle's M2R-01 people references; optional cast preserves legacy scenarios.
    people = {}
    roles = {"customer", "outgoingCco", "scoStaff", "finance", "commander", "vendorRep"}
    for person in sc.get("people", []):
        person_id = person["id"]
        if person_id in people: errs.append(f"person id {person_id} is not unique")
        else: people[person_id] = person
        if person["role"] not in roles: errs.append(f"person {person_id}: role {person['role']} not recognized")
        if not person["name"]: errs.append(f"person {person_id} has no name")
        vendor = person.get("vendor")
        if vendor is not None and vendor not in vendors:
            errs.append(f"person {person_id}: vendor {vendor} not found")
        if person["role"] == "vendorRep" and vendor is None:
            errs.append(f"person {person_id} is a vendorRep with no vendor")
        site = person.get("site")
        if site is not None and site not in sites:
            errs.append(f"person {person_id}: site {site} not found")
    for card in sc["requirementCards"]:
        person = card.get("person")
        if person is not None and person not in people:
            errs.append(f"card {card['id']}: person {person} not found")
        if person is None and not card.get("submittedBy", ""):
            errs.append(f"card {card['id']} names neither a person nor submittedBy")
        heard = card.get("heardAtStaffMeeting")
        if heard is not None:
            if not 1 <= heard["fromDay"] < card["submittedDay"]:
                errs.append(f"card {card['id']}: heardAtStaffMeeting.fromDay must be at least 1 and before submittedDay")
            if heard.get("by") is not None and heard["by"] not in people:
                errs.append(f"card {card['id']}: heardAtStaffMeeting.by {heard['by']} not found")
    for event in sc["events"]:
        speaker = event.get("from")
        if speaker is not None and speaker not in people:
            errs.append(f"event {event['id']}: from {speaker} not found")
    for actor, role in (("sco", "scoStaff"), ("fm", "finance"), ("commander", "commander")):
        person_id = sc["actors"][actor].get("person")
        if person_id is None: continue
        person = people.get(person_id)
        if person is None: errs.append(f"actors.{actor}: person {person_id} not found")
        elif person["role"] != role:
            errs.append(f"actors.{actor}: person {person_id} must have role {role}, has {person['role']}")
    # M2R-02's optional blocks use the published turnover/prep contract (PR 97).
    turnover = sc.get("turnover")
    if turnover is not None:
        person_id = turnover.get("from", "")
        person = people.get(person_id)
        if person is None: errs.append(f"turnover.from: person {person_id} not found")
        elif person["role"] != "outgoingCco":
            errs.append(f"turnover.from: person {person_id} must have role outgoingCco, has {person['role']}")
        for group, known in (("arrangements", ids("inheritedArrangements")), ("vendors", vendors), ("fundingLines", ids("fundingLines"))):
            for item in turnover.get("remarks", {}).get(group, {}):
                if item not in known: errs.append(f"turnover.remarks.{group}: {item} not found")
    prep = sc.get("prepBriefing")
    if prep is not None:
        person_id = prep.get("from", "")
        if person_id not in people: errs.append(f"prepBriefing.from: person {person_id} not found")
        choices = {c["id"]: c for c in rules.get("prepChoices", [])}
        for choice in list(prep.get("choices", {})) + prep.get("defaults", []):
            if choice not in choices: errs.append(f"prepBriefing choice {choice} is not in rules {rules['rulesVersion']}")
        used = sum(choices[choice]["prepDays"] for choice in set(prep.get("defaults", [])) if choice in choices)
        if used > sc["prepDays"]:
            errs.append(f"prepBriefing.defaults use {used} prep-days; the scenario allows {sc['prepDays']}")
    cors = ids("cors")
    if len(cors) != len(sc.get("cors", [])): errs.append("COR ids must be unique")
    cor_rules = rules.get("cor", {})
    qualities = set(cor_rules.get("qualities", {"good": {}, "fair": {}, "poor": {}}))
    for cor in sc.get("cors", []):
        if cor["quality"] not in qualities:
            errs.append(f"COR {cor['id']}: quality {cor['quality']} not in rules cor.qualities")
    relief = cor_rules.get("reliefQuality", "poor")
    if relief not in qualities: errs.append(f"rules cor.reliefQuality {relief} not in cor.qualities")
    for a in sc.get("inheritedArrangements", []):
        if a.get("cor") is not None and a["cor"] not in cors:
            errs.append(f"arrangement {a['id']}: COR {a['cor']} not found")
        if a["instrument"] not in instruments: errs.append(f"arrangement {a['id']}: instrument {a['instrument']} not in rules")
        if a["vendor"] not in vendors: errs.append(f"arrangement {a['id']}: vendor {a['vendor']} not found")
        if a["site"] not in sites: errs.append(f"arrangement {a['id']}: site {a['site']} not found")
        if a["capability"] not in caps: errs.append(f"arrangement {a['id']}: capability {a['capability']} not in rules")
    event_ids = ids("events")
    for e in sc["events"]:
        if e["window"]["latestDay"] < e["window"]["earliestDay"]: errs.append(f"event {e['id']}: window ends before it starts")
        if e["window"]["earliestDay"] > sc["days"]: errs.append(f"event {e['id']}: window starts after the scenario ends")
        for effect in e.get("effects", []):
            kind = effect["kind"]
            duration = effect.get("daysRange")
            if duration is not None and duration["min"] > duration["max"]:
                errs.append(f"event {e['id']}: {kind} daysRange max is below min")
            if effect.get("forecastDays") is not None and kind != "link.closed":
                errs.append(f"event {e['id']}: forecastDays applies only to link.closed")
            if kind == "link.closed" and effect.get("target") not in links:
                errs.append(f"event {e['id']}: link.closed link {effect.get('target')} not found")
            if kind == "cor.rotates" and effect.get("target") not in cors:
                errs.append(f"event {e['id']}: cor.rotates COR {effect.get('target')} not found")
            if kind in {"acsa.withdraw", "hostNation.withdraw"} and effect.get("target") is not None and effect["target"] not in caps:
                errs.append(f"event {e['id']}: {kind} capability {effect['target']} not in rules")
            if kind == "funding.late":
                if effect.get("target") not in ids("fundingLines"):
                    errs.append(f"event {e['id']}: funding.late funding line {effect.get('target')} not found")
                if (effect.get("days") or 0) <= 0 and (duration or {}).get("max", 0) <= 0:
                    errs.append(f"event {e['id']}: funding.late needs a positive days or daysRange")
            if kind == "site.populationJump":
                if effect.get("target") not in sites:
                    errs.append(f"event {e['id']}: site.populationJump site {effect.get('target')} not found")
                if (effect.get("fraction") or 0) <= 0:
                    errs.append(f"event {e['id']}: site.populationJump needs a positive fraction")
        for f in e.get("followOns", []):
            if f not in event_ids: errs.append(f"event {e['id']}: follow-on {f} not found")
    # M3-15 mirrors ScenarioBundle's support-provider references. Delivery rules stay in Sim.
    for actor, quantity, present, instrument in (("acsaPartner", "stock", "present", "acsa"),
                                                  ("hostNation", "capacity", "agreementExists", "hostNation")):
        provider = sc["actors"].get(actor)
        if provider is None: continue
        island = provider.get("island")
        if island is not None and island not in islands: errs.append(f"{actor} island {island} not found")
        quantities = provider.get(quantity, {})
        for capability in quantities:
            if capability not in caps: errs.append(f"{actor} {quantity} capability {capability} not in rules")
        for capability in provider.get("unitPriceCents", {}):
            if capability not in quantities: errs.append(f"{actor} price for {capability} has no {quantity}")
        for site in provider.get("sites", []):
            if site not in sites: errs.append(f"{actor} site {site} not found")
        if provider.get(present, False) and instrument not in instruments:
            errs.append(f"{actor} needs the {instrument} instrument in rules")
    external = sc["actors"].get("externalSupport")
    if external is not None:
        if "externalSupport" not in instruments: errs.append("actors.externalSupport needs the externalSupport instrument in rules")
        offers = external.get("offers", [])
        if len({o["capability"] for o in offers}) != len(offers):
            errs.append("actors.externalSupport offers must name each capability once")
        for offer in offers:
            capability = offer["capability"]; cap = cap_data.get(capability)
            if cap is None: errs.append(f"external support capability {capability} not in rules")
            else:
                lines = {line["id"]: line for line in cap["lines"]}
                for line in offer["lines"]:
                    if line not in lines: errs.append(f"external support line {line} is not a {capability} line")
                if not any(lines.get(line, {}).get("kind") == "core" for line in offer["lines"]):
                    errs.append(f"external support {capability} must offer the core line")
            for site in offer.get("sites", []):
                if site not in sites: errs.append(f"external support {capability} site {site} not found")
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
