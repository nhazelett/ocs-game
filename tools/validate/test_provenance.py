from pathlib import Path
import tempfile
import unittest

from provenance import check

HEADER = """Every file under assets/ must have a row here.

| Asset path | Type | Origin (generated / authored / licensed) | Tool or author | Prompt or source | License | Date added |
| --- | --- | --- | --- | --- | --- | --- |
"""


class ProvenanceTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory(prefix="los-provenance-")
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)
        (self.root / "assets").mkdir()
        self.ledger = self.root / "assets" / "PROVENANCE.md"
        self.ledger.write_text(HEADER, encoding="utf-8")

    def asset(self, name):
        path = self.root / "assets" / name
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(b"fixture")
        return "assets/" + name

    def row(self, path, origin="generated", license_value="Project license TBD", source="fixture"):
        with self.ledger.open("a", encoding="utf-8") as file:
            file.write(f"| {path} | mesh | {origin} | test | {source} | {license_value} | 2026-11-08 |\n")

    def test_empty_inventory_passes_without_a_self_row(self):
        self.assertEqual([], check(self.root))

    def test_glb_audio_and_spaced_paths_all_require_rows(self):
        for name in ["kit/tent.glb", "audio/alert.ogg", "audio/click.wav", "with spaces/image.png"]:
            self.row(self.asset(name))
        self.assertEqual([], check(self.root))

    def test_missing_row_is_rejected_even_when_path_appears_in_prose(self):
        path = self.asset("missing.glb")
        with self.ledger.open("a", encoding="utf-8") as file:
            file.write("Remember to document " + path + " later.\n")
        self.assertTrue(any("Missing provenance row" in error for error in check(self.root)))

    def test_partial_path_match_does_not_count(self):
        path = self.asset("mesh.glb")
        self.row(path + ".backup")
        self.assertTrue(any("Missing provenance row" in error for error in check(self.root)))

    def test_empty_origin_or_license_is_rejected(self):
        path = self.asset("mesh.glb")
        for origin, license_value, field in [(" ", "TBD", "Origin"), ("generated", " ", "License"), ("` `", "TBD", "Origin")]:
            with self.subTest(field=field, origin=origin):
                self.ledger.write_text(HEADER, encoding="utf-8")
                self.row(path, origin, license_value)
                self.assertTrue(any("empty " + field in error for error in check(self.root)))

    def test_escaped_pipe_and_code_formatted_path_are_valid(self):
        self.row("`" + self.asset("kit.glb") + "`", source="box \\| prism")
        self.assertEqual([], check(self.root))

    def test_duplicate_rows_and_bad_column_counts_fail(self):
        path = self.asset("mesh.glb")
        self.row(path)
        self.row(path)
        with self.ledger.open("a", encoding="utf-8") as file:
            file.write("| wrong | count |\n")
        errors = check(self.root)
        self.assertTrue(any("duplicate row" in error for error in errors))
        self.assertTrue(any("expected 7 cells" in error for error in errors))

    def test_missing_ledger_fails(self):
        self.ledger.unlink()
        self.assertTrue(check(self.root))


if __name__ == "__main__":
    unittest.main()
