"""M3-15: schema-valid support references must also pass Sim's cross checks."""
import contextlib
import copy
import io
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

import validate as checker


class SupportReferenceTests(unittest.TestCase):
    def setUp(self):
        self.scenario = checker.load(checker.ROOT / "scenarios/spoke-b-90/scenario.json")
        self.rules = checker.load(checker.RULES_DIR / "rules-2025-10.json")
        self.scenario["actors"]["hostNation"] = {
            "agreementExists": True, "island": "hub", "capacity": {"fuel": 100},
            "unitPriceCents": {"fuel": 20}, "sites": ["hubSite"]}

    def failures(self):
        return checker.cross_checks(self.scenario, self.rules)

    def error(self, fragment):
        self.assertTrue(any(fragment in error for error in self.failures()), fragment)

    def external(self):
        return self.scenario["actors"]["externalSupport"]

    def test_valid_support_and_optional_blocks(self):
        self.assertEqual([], checker.errors(checker.validator("scenario"), self.scenario, "fixture"))
        self.assertEqual([], self.failures())
        for actor in ["hostNation", "acsaPartner", "externalSupport"]:
            self.scenario["actors"].pop(actor)
        self.assertEqual([], self.failures())

    def test_external_capability_must_exist_without_crashing(self):
        self.external()["offers"][0]["capability"] = "missing"
        self.error("external support capability missing not in rules")

    def test_external_capabilities_must_be_distinct(self):
        self.external()["offers"].append(copy.deepcopy(self.external()["offers"][0]))
        self.error("offers must name each capability once")

    def test_external_line_must_belong_to_its_capability(self):
        self.external()["offers"][0]["lines"].append("missingLine")
        self.error("line missingLine is not a power line")

    def test_external_must_include_core_line(self):
        self.external()["offers"][0]["lines"] = ["fuelSupply"]
        self.error("power must offer the core line")

    def test_external_sites_must_exist(self):
        self.external()["offers"][0]["sites"] = ["missingSite"]
        self.error("power site missingSite not found")

    def test_external_needs_instrument_even_without_offers(self):
        self.external()["offers"] = []
        self.rules["instruments"].pop("externalSupport")
        self.error("needs the externalSupport instrument")

    def test_provider_islands_must_exist(self):
        for actor in ["acsaPartner", "hostNation"]:
            with self.subTest(actor=actor):
                self.scenario["actors"][actor]["island"] = "missingIsland"
                self.error(f"{actor} island missingIsland not found")

    def test_provider_quantity_keys_must_be_capabilities(self):
        for actor, field in [("acsaPartner", "stock"), ("hostNation", "capacity")]:
            with self.subTest(actor=actor):
                self.scenario["actors"][actor][field]["missing"] = 1
                self.error(f"{actor} {field} capability missing not in rules")

    def test_provider_prices_need_matching_supply(self):
        for actor, field in [("acsaPartner", "stock"), ("hostNation", "capacity")]:
            with self.subTest(actor=actor):
                self.scenario["actors"][actor]["unitPriceCents"]["water"] = 1
                self.error(f"{actor} price for water has no {field}")

    def test_provider_instrument_required_only_when_present(self):
        for actor, flag, instrument in [("acsaPartner", "present", "acsa"),
                                        ("hostNation", "agreementExists", "hostNation")]:
            with self.subTest(actor=actor):
                self.setUp()
                self.rules["instruments"].pop(instrument)
                # Remove inherited uses to isolate the actor's conditional rule.
                self.scenario["inheritedArrangements"] = []
                self.scenario.get("turnover", {}).get("remarks", {})["arrangements"] = {}
                self.error(f"{actor} needs the {instrument} instrument")
                self.scenario["actors"][actor][flag] = False
                self.assertEqual([], self.failures())

    def test_host_nation_sites_must_exist(self):
        self.scenario["actors"]["hostNation"]["sites"] = ["missingSite"]
        self.error("hostNation site missingSite not found")

    def test_withdrawal_targets_allow_null_or_known_capability(self):
        for kind in ["acsa.withdraw", "hostNation.withdraw"]:
            for target in [None, "fuel", "missing"]:
                with self.subTest(kind=kind, target=target):
                    self.scenario["events"][0]["effects"] = [{"kind": kind, "target": target}]
                    if target == "missing": self.error(f"{kind} capability missing not in rules")
                    else: self.assertEqual([], self.failures())

    def test_vendor_lift_modes(self):
        vendor = self.scenario["vendors"][0]
        vendor["lift"] = ["airlift", "sealift"]
        self.assertEqual([], self.failures())
        vendor["lift"].append("teleport")
        self.error("lift mode teleport is not airlift or sealift")

    def test_rules_schema_parses_lift_and_rejects_bad_values(self):
        self.rules["lift"] = {"airliftQueueDays": {"min": 1, "max": 3},
                              "sealiftTransitDays": {"min": 3, "max": 9}, "customsHoldChance": 0.2}
        self.assertEqual([], checker.errors(checker.validator("rules"), self.rules, "rules"))
        self.rules["lift"]["customsHoldChance"] = 2
        self.assertTrue(checker.errors(checker.validator("rules"), self.rules, "rules"))

    def test_cli_rejects_schema_valid_broken_support_reference(self):
        self.external()["offers"][0]["sites"] = ["missingSite"]
        self.assertEqual([], checker.errors(checker.validator("scenario"), self.scenario, "fixture"))
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary); folder = root / "scenarios/broken"; folder.mkdir(parents=True)
            (folder / "scenario.json").write_text(json.dumps(self.scenario))
            output = io.StringIO()
            with patch.object(checker, "ROOT", root), contextlib.redirect_stdout(output):
                self.assertEqual(1, checker.main([str(folder)]))
            self.assertIn("power site missingSite not found", output.getvalue())


if __name__ == "__main__":
    unittest.main()
