from pathlib import Path
import subprocess
import tempfile
import unittest

from conflicts import check


class ConflictMarkerTests(unittest.TestCase):
    def setUp(self):
        temporary = tempfile.TemporaryDirectory(prefix="los-conflicts-")
        self.addCleanup(temporary.cleanup)
        self.root = Path(temporary.name)
        subprocess.run(["git", "init", "--quiet", str(self.root)], check=True)

    def track(self, name, data):
        (self.root / name).write_bytes(data)
        subprocess.run(["git", "-c", "core.autocrlf=false", "add", "--", name], cwd=self.root, check=True)

    def test_each_marker_fails_with_path_and_line(self):
        self.track("space in name.md", b"title\r\n<<<<<<< HEAD\r\nours\r\n=======\r\ntheirs\r\n>>>>>>> origin/main\r\n")
        errors = check(self.root)
        self.assertEqual(3, len(errors))
        for line in (2, 4, 6):
            self.assertTrue(any(f"space in name.md:{line}:" in error for error in errors))

    def test_untracked_and_binary_files_do_not_fail(self):
        self.track("mesh.glb", b"glTF\0\n<<<<<<< binary\n=======\n")
        (self.root / "untracked.txt").write_text("<<<<<<< untracked\n", encoding="utf-8")
        self.assertEqual([], check(self.root))

    def test_similar_text_and_inline_examples_pass(self):
        self.track("examples.md", b'Example: "<<<<<<< HEAD"\n =======\n========\n=======text\n<<<<<<<no-space\n>>>>>>no-space\n')
        self.assertEqual([], check(self.root))

    def test_utf16_text_is_checked_instead_of_skipped_as_binary(self):
        self.track("encoded.txt", "title\n=======\n".encode("utf-16"))
        self.assertEqual(["encoded.txt:2: unresolved merge marker"], check(self.root))

    def test_non_utf8_text_still_rejects_ascii_markers(self):
        self.track("legacy.txt", b"caf\xe9\n>>>>>>> branch\n")
        self.assertEqual(["legacy.txt:2: unresolved merge marker"], check(self.root))


if __name__ == "__main__":
    unittest.main()
