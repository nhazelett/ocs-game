"""M2R-18: mirror the engine's people references, including legacy no-cast scenarios."""
import contextlib
import copy
import io
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

import validate as checker


class PeopleReferenceTests(unittest.TestCase):
    def setUp(self):
        self.scenario = checker.load(checker.ROOT / "scenarios/spoke-b/scenario.json")
        self.rules = checker.load(checker.RULES_DIR / "rules-2025-10.json")

    def person(self, role):
        return next(p for p in self.scenario["people"] if p["role"] == role)

    def check_error(self, fragment):
        errors = checker.cross_checks(self.scenario, self.rules)
        self.assertTrue(any(fragment in error for error in errors), errors)

    def test_every_authored_cast_and_legacy_scenario_passes(self):
        for path in (checker.ROOT / "scenarios").glob("*/scenario.json"):
            with self.subTest(scenario=path.parent.name):
                self.assertEqual([], checker.cross_checks(checker.load(path), self.rules))

    def test_duplicate_person_ids_fail_even_with_different_content(self):
        duplicate = copy.deepcopy(self.person("customer"))
        duplicate["name"] = "Another name"
        self.scenario["people"].append(duplicate)
        self.check_error(f"person id {duplicate['id']} is not unique")

    def test_vendor_reference_must_exist_for_any_role(self):
        for role in ("vendorRep", "customer"):
            with self.subTest(role=role):
                person = self.person(role)
                person["vendor"] = "missingVendor"
                self.check_error(f"person {person['id']}: vendor missingVendor not found")

    def test_vendor_rep_needs_a_vendor(self):
        person = self.person("vendorRep")
        person.pop("vendor", None)
        self.check_error(f"person {person['id']} is a vendorRep with no vendor")

    def test_site_reference_must_exist(self):
        person = self.person("customer")
        person["site"] = "missingSite"
        self.check_error(f"person {person['id']}: site missingSite not found")

    def test_card_person_must_exist(self):
        self.scenario["requirementCards"][0]["person"] = "missingPerson"
        self.check_error("person missingPerson not found")

    def test_card_can_use_legacy_submitted_by_without_cast(self):
        self.scenario.pop("people")
        self.scenario.pop("turnover", None)
        self.scenario.pop("prepBriefing", None)
        for card in self.scenario["requirementCards"]:
            card.pop("person", None)
            card["submittedBy"] = "Legacy customer"
        for event in self.scenario["events"]:
            event.pop("from", None)
        for actor in ("sco", "fm", "commander"):
            self.scenario["actors"][actor].pop("person", None)
        self.assertEqual([], checker.cross_checks(self.scenario, self.rules))

    def test_card_must_have_a_person_or_nonempty_submitted_by(self):
        card = self.scenario["requirementCards"][0]
        card.pop("person", None)
        for submitted_by in (None, ""):
            with self.subTest(submitted_by=submitted_by):
                if submitted_by is None: card.pop("submittedBy", None)
                else: card["submittedBy"] = submitted_by
                self.check_error(f"card {card['id']} names neither a person nor submittedBy")

    def test_event_speaker_must_exist_but_is_optional(self):
        event = self.scenario["events"][0]
        event["from"] = "missingPerson"
        self.check_error(f"event {event['id']}: from missingPerson not found")
        event.pop("from")
        self.assertEqual([], checker.cross_checks(self.scenario, self.rules))

    def test_each_actor_needs_the_correct_person_role(self):
        original = copy.deepcopy(self.scenario)
        for actor, expected in (("sco", "scoStaff"), ("fm", "finance"), ("commander", "commander")):
            for person_id, fragment in (("missingPerson", "person missingPerson not found"),
                                        (self.person("customer")["id"], f"person {self.person('customer')['id']} must have role {expected}")):
                with self.subTest(actor=actor, person=person_id):
                    self.scenario = copy.deepcopy(original)
                    self.scenario["actors"][actor]["person"] = person_id
                    self.check_error(f"actors.{actor}: {fragment}")

    def test_optional_vendor_and_site_do_not_require_a_value(self):
        person = self.person("customer")
        person.pop("site", None)
        person.pop("vendor", None)
        self.assertEqual([], checker.cross_checks(self.scenario, self.rules))

    def test_unknown_role_and_empty_name_match_engine_rejections(self):
        person = self.person("customer")
        person["role"] = "unknownRole"
        person["name"] = ""
        self.check_error("role unknownRole not recognized")
        self.check_error(f"person {person['id']} has no name")

    def test_cli_rejects_schema_valid_broken_person_reference(self):
        self.scenario["requirementCards"][0]["person"] = "missingPerson"
        self.assertEqual([], list(checker.validator("scenario").iter_errors(self.scenario)))
        with tempfile.TemporaryDirectory(prefix="ocs-people-check-") as temporary:
            root = Path(temporary)
            folder = root / "scenarios/broken"
            folder.mkdir(parents=True)
            (folder / "scenario.json").write_text(json.dumps(self.scenario), encoding="utf-8")
            output = io.StringIO()
            with patch.object(checker, "ROOT", root), contextlib.redirect_stdout(output):
                self.assertEqual(1, checker.main([str(folder)]))
            self.assertIn("person missingPerson not found", output.getvalue())

    def turnover(self):
        self.scenario["turnover"] = {"from": self.person("outgoingCco")["id"], "remarks": {
            "arrangements": {self.scenario["inheritedArrangements"][0]["id"]: "The contract is in the file."},
            "vendors": {self.scenario["vendors"][0]["id"]: "They have worked with the shop."},
            "fundingLines": {self.scenario["fundingLines"][0]["id"]: "This is the money I leave you."},
        }}
        return self.scenario["turnover"]

    def prep(self):
        choice = next(c for c in self.rules["prepChoices"] if c["prepDays"] <= self.scenario["prepDays"])
        self.scenario["prepBriefing"] = {"from": self.person("scoStaff")["id"], "intro": "You have time before you leave.",
                                        "choices": {choice["id"]: "This can be done before you go."}, "defaults": [choice["id"]]}
        return self.scenario["prepBriefing"]

    def test_valid_turnover_and_prep_match_the_published_contract(self):
        self.turnover()
        self.prep()
        self.assertEqual([], checker.cross_checks(self.scenario, self.rules))

    def test_turnover_sender_exists_and_is_the_outgoing_cco(self):
        turnover = self.turnover()
        turnover["from"] = "missingPerson"
        self.check_error("turnover.from: person missingPerson not found")
        turnover["from"] = self.person("customer")["id"]
        self.check_error("must have role outgoingCco")

    def test_each_remark_names_an_item_of_the_correct_kind(self):
        for group in ("arrangements", "vendors", "fundingLines"):
            with self.subTest(group=group):
                turnover = self.turnover()
                turnover["remarks"][group]["missingItem"] = "An authoring mistake."
                self.check_error(f"turnover.remarks.{group}: missingItem not found")
        turnover = self.turnover()
        vendor = self.scenario["vendors"][0]["id"]
        turnover["remarks"]["arrangements"][vendor] = "Wrong kind of item."
        self.check_error(f"turnover.remarks.arrangements: {vendor} not found")

    def test_prep_sender_must_exist(self):
        self.prep()["from"] = "missingPerson"
        self.check_error("prepBriefing.from: person missingPerson not found")

    def test_prep_message_and_default_choices_must_exist(self):
        for field, value in (("choices", {"missingChoice": "An unknown preparation."}), ("defaults", ["missingChoice"])):
            with self.subTest(field=field):
                self.prep()[field] = value
                self.check_error("prepBriefing choice missingChoice is not in rules")

    def test_prep_defaults_over_budget_fail_and_equal_budget_passes(self):
        prep = self.prep()
        prep["defaults"] = [c["id"] for c in self.rules["prepChoices"]]
        self.check_error("prepBriefing.defaults use")
        self.scenario["prepDays"] = sum(c["prepDays"] for c in self.rules["prepChoices"])
        self.assertEqual([], checker.cross_checks(self.scenario, self.rules))

    def test_prep_defaults_use_distinct_costs_like_the_engine(self):
        prep = self.prep()
        choice = next(c for c in self.rules["prepChoices"] if c["id"] == prep["defaults"][0])
        self.scenario["prepDays"] = choice["prepDays"]
        prep["defaults"] *= 2
        self.assertEqual([], checker.cross_checks(self.scenario, self.rules))


if __name__ == "__main__":
    unittest.main()
