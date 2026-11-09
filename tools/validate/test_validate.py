"""M2-23: schema-valid broken references must fail the Python validator too."""
import contextlib
import copy
import io
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

import validate as checker


class SeededReferenceTests(unittest.TestCase):
    def setUp(self):
        self.scenario = checker.load(checker.ROOT / "scenarios/spoke-b/scenario.json")
        self.rules = checker.load(checker.RULES_DIR / "rules-2025-10.json")

    def effect(self, kind):
        return next(f for e in self.scenario["events"] for f in e["effects"] if f["kind"] == kind)

    def food(self):
        return next(o for v in self.scenario["vendors"] for o in v["offers"] if "sourceIsland" in o)

    def check_error(self, fragment):
        self.assertTrue(any(fragment in e for e in checker.cross_checks(self.scenario, self.rules)))

    def test_authored_references_and_equal_duration_bounds_pass(self):
        self.assertEqual([], checker.cross_checks(self.scenario, self.rules))
        self.effect("link.closed")["daysRange"] = {"min": 2, "max": 2}
        self.assertEqual([], checker.cross_checks(self.scenario, self.rules))

    def test_source_must_name_an_island(self):
        self.food()["sourceIsland"] = "missing-island"
        self.check_error("sourceIsland missing-island not found")

    def test_source_is_only_for_consumables(self):
        offer = next(o for v in self.scenario["vendors"] for o in v["offers"] if o["capability"] == "power")
        offer["sourceIsland"] = "hub"
        self.check_error("sourceIsland applies only to consumables")

    def test_closed_link_must_exist(self):
        self.effect("link.closed")["target"] = "missing-link"
        self.check_error("link.closed link missing-link not found")

    def test_jump_site_must_exist(self):
        self.effect("site.populationJump")["target"] = "missing-site"
        self.check_error("site.populationJump site missing-site not found")

    def test_jump_fraction_must_be_present_and_positive(self):
        for fraction in [None, 0, -0.4]:
            with self.subTest(fraction=fraction):
                self.effect("site.populationJump")["fraction"] = fraction
                self.check_error("needs a positive fraction")

    def test_inverted_duration_is_rejected_for_each_chain(self):
        original = copy.deepcopy(self.scenario)
        for kind in ["link.closed", "site.populationJump"]:
            with self.subTest(kind=kind):
                self.scenario = copy.deepcopy(original)
                self.effect(kind)["daysRange"] = {"min": 5, "max": 2}
                self.check_error("daysRange max is below min")

    def test_forecast_only_applies_to_a_link_closure(self):
        self.effect("site.populationJump")["forecastDays"] = 1
        self.check_error("forecastDays applies only to link.closed")

    def test_cli_reports_schema_valid_reference_error(self):
        self.food()["sourceIsland"] = "missing-island"
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            folder = root / "scenarios/broken"
            folder.mkdir(parents=True)
            (folder / "scenario.json").write_text(json.dumps(self.scenario))
            output = io.StringIO()
            with patch.object(checker, "ROOT", root), contextlib.redirect_stdout(output):
                self.assertEqual(1, checker.main([str(folder)]))
            self.assertIn("sourceIsland missing-island not found", output.getvalue())


class FundingReferenceTests(unittest.TestCase):
    def setUp(self):
        self.scenario = checker.load(checker.ROOT / "scenarios/spoke-b-90/scenario.json")
        self.rules = checker.load(checker.RULES_DIR / "rules-2025-10.json")
        self.effect = next(f for e in self.scenario["events"] for f in e["effects"] if f["kind"] == "funding.late")

    def test_authored_funding_and_valid_purposes_pass(self):
        self.scenario["fundingLines"][0]["capabilities"] = ["power", "fuel"]
        self.assertEqual([], checker.cross_checks(self.scenario, self.rules))
        self.scenario["fundingLines"][0]["capabilities"] = []
        self.assertEqual([], checker.cross_checks(self.scenario, self.rules))

    def test_capability_must_exist_in_rules(self):
        self.scenario["fundingLines"][0]["capabilities"] = ["missing-capability"]
        self.assertTrue(any("capability missing-capability not in rules" in e for e in checker.cross_checks(self.scenario, self.rules)))

    def test_line_must_release_before_or_on_expiry(self):
        line = self.scenario["fundingLines"][0]
        line["availableDay"] = line["expiresDay"]
        self.assertEqual([], checker.cross_checks(self.scenario, self.rules))
        line["availableDay"] += 1
        self.assertTrue(any("expires before it is released" in e for e in checker.cross_checks(self.scenario, self.rules)))

    def test_late_effect_target_must_be_a_funding_line(self):
        self.effect["target"] = "missing-line"
        self.assertTrue(any("funding.late funding line missing-line not found" in e for e in checker.cross_checks(self.scenario, self.rules)))

    def test_late_effect_needs_positive_days_or_range(self):
        for days, duration, accepted in [(None, None, False), (0, None, False), (-1, None, False), (1, None, True),
                                         (None, {"min": 0, "max": 0}, False), (None, {"min": 1, "max": 1}, True),
                                         (1, {"min": 0, "max": 0}, True)]:
            with self.subTest(days=days, duration=duration):
                self.effect.pop("days", None); self.effect.pop("daysRange", None)
                if days is not None: self.effect["days"] = days
                if duration is not None: self.effect["daysRange"] = duration
                failures = checker.cross_checks(self.scenario, self.rules)
                self.assertEqual(not accepted, any("needs a positive days or daysRange" in e for e in failures))

    def test_cli_rejects_schema_valid_bad_funding_reference(self):
        self.effect["target"] = "missing-line"
        self.assertEqual([], checker.errors(checker.validator("scenario"), self.scenario, "fixture"))
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary); folder = root / "scenarios/broken"; folder.mkdir(parents=True)
            (folder / "scenario.json").write_text(json.dumps(self.scenario))
            output = io.StringIO()
            with patch.object(checker, "ROOT", root), contextlib.redirect_stdout(output):
                self.assertEqual(1, checker.main([str(folder)]))
            self.assertIn("funding.late funding line missing-line not found", output.getvalue())


class CorReferenceTests(unittest.TestCase):
    def setUp(self):
        self.scenario = checker.load(checker.ROOT / "scenarios/spoke-b-90/scenario.json")
        self.rules = checker.load(checker.RULES_DIR / "rules-2025-10.json")

    def failures(self): return checker.cross_checks(self.scenario, self.rules)

    def test_authored_roster_and_valid_inherited_cor_pass(self):
        self.scenario["inheritedArrangements"][0]["cor"] = self.scenario["cors"][0]["id"]
        self.assertEqual([], self.failures())

    def test_duplicate_ids_are_rejected(self):
        self.scenario["cors"].append(copy.deepcopy(self.scenario["cors"][0]))
        self.assertIn("COR ids must be unique", self.failures())

    def test_quality_must_name_a_rules_key(self):
        self.scenario["cors"][0]["quality"] = "missing-quality"
        self.assertTrue(any("quality missing-quality not in rules cor.qualities" in e for e in self.failures()))

    def test_inherited_cor_must_be_on_roster(self):
        self.scenario["inheritedArrangements"][0]["cor"] = "missing-cor"
        self.assertTrue(any("COR missing-cor not found" in e for e in self.failures()))

    def test_rotation_target_must_be_on_roster(self):
        effect = next(f for e in self.scenario["events"] for f in e["effects"] if f["kind"] == "cor.rotates")
        effect["target"] = "missing-cor"
        self.assertTrue(any("cor.rotates COR missing-cor not found" in e for e in self.failures()))

    def test_relief_quality_must_name_a_rules_key(self):
        self.rules["cor"]["reliefQuality"] = "missing-quality"
        self.assertTrue(any("cor.reliefQuality missing-quality not in cor.qualities" in e for e in self.failures()))

    def test_optional_cor_rules_use_the_same_defaults_as_sim(self):
        self.rules.pop("cor")
        self.assertEqual([], self.failures())

    def test_cli_rejects_schema_valid_bad_cor_reference(self):
        self.scenario["inheritedArrangements"][0]["cor"] = "missing-cor"
        self.assertEqual([], checker.errors(checker.validator("scenario"), self.scenario, "fixture"))
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary); folder = root / "scenarios/broken"; folder.mkdir(parents=True)
            (folder / "scenario.json").write_text(json.dumps(self.scenario))
            output = io.StringIO()
            with patch.object(checker, "ROOT", root), contextlib.redirect_stdout(output):
                self.assertEqual(1, checker.main([str(folder)]))
            self.assertIn("COR missing-cor not found", output.getvalue())


if __name__ == "__main__":
    unittest.main()
