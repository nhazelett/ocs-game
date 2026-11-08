"""Reject unresolved merge markers in tracked text files (no third-party packages)."""

from pathlib import Path
import codecs
import subprocess
import sys


def text_content(data: bytes) -> str | None:
    if data.startswith((codecs.BOM_UTF32_LE, codecs.BOM_UTF32_BE)):
        return data.decode("utf-32")
    if data.startswith((codecs.BOM_UTF16_LE, codecs.BOM_UTF16_BE)):
        return data.decode("utf-16")
    if b"\0" in data:
        return None
    # A byte-preserving fallback also catches ASCII markers in legacy text encodings.
    return data.decode("utf-8-sig", errors="surrogateescape")


def check(root: Path) -> list[str]:
    tracked = subprocess.run(
        ["git", "ls-files", "-z"], cwd=root, check=True, capture_output=True
    ).stdout
    errors = []
    for raw_path in tracked.split(b"\0"):
        if not raw_path:
            continue
        relative = raw_path.decode("utf-8", errors="surrogateescape")
        path = root / relative
        if not path.is_file():
            continue  # Deleted working-tree entries and submodule directories are not text.
        try:
            content = text_content(path.read_bytes())
        except (OSError, UnicodeError) as error:
            errors.append(f"{relative}: cannot inspect text: {error}")
            continue
        if content is None:
            continue
        for line_number, line in enumerate(content.split("\n"), start=1):
            line = line.removesuffix("\r")
            if line.startswith(("<<<<<<< ", ">>>>>>> ")) or line == "=======":
                errors.append(f"{relative}:{line_number}: unresolved merge marker")
    return errors


def main() -> int:
    try:
        errors = check(Path(__file__).resolve().parents[2])
    except (OSError, subprocess.CalledProcessError) as error:
        print(f"Cannot list tracked files: {error}", file=sys.stderr)
        return 1
    if errors:
        print("\n".join(errors))
        return 1
    print("Tracked text passes: no unresolved merge markers.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
