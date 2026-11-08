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


if __name__ == "__main__":
    unittest.main()
