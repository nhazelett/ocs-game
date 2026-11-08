"""Require an exact provenance-table row with Origin and License for each asset file."""
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[2]


def cells(line):
    """Split our Markdown tables, preserving escaped pipes inside prompts and sources."""
    text = line.strip().strip("|")
    result, value = [], []
    index = 0
    while index < len(text):
        if text[index:index + 2] == "\\|":
            value.append("|")
            index += 2
            continue
        if text[index] == "|":
            result.append("".join(value).strip())
            value = []
        else:
            value.append(text[index])
        index += 1
    return result + ["".join(value).strip()]


def check(root):
    root = Path(root)
    ledger = root / "assets" / "PROVENANCE.md"
    errors, entries = [], {}
    try:
        lines = ledger.read_text(encoding="utf-8-sig").splitlines()
    except OSError as error:
        return [f"Cannot read assets/PROVENANCE.md: {error}"]
    columns = None
    for number, line in enumerate(lines, 1):
        if not line.lstrip().startswith("|"):
            continue
        row = cells(line)
        if row and row[0] == "Asset path":
            columns = row
            if "License" not in row or not any(x.startswith("Origin") for x in row):
                errors.append(f"PROVENANCE.md:{number}: table needs Origin and License columns.")
                columns = None
            continue
        if columns is None or all(re.fullmatch(r":?-{3,}:?", value) for value in row):
            continue
        if len(row) != len(columns):
            errors.append(f"PROVENANCE.md:{number}: expected {len(columns)} cells, found {len(row)}.")
            continue
        path = row[0].strip("`")
        if not path:
            errors.append(f"PROVENANCE.md:{number}: empty Asset path.")
            continue
        if path in entries:
            errors.append(f"PROVENANCE.md:{number}: duplicate row for {path}.")
        entries[path] = number
        origin = row[next(i for i, name in enumerate(columns) if name.startswith("Origin"))]
        license_value = row[columns.index("License")]
        for name, value in (("Origin", origin), ("License", license_value)):
            if not value.strip().strip("`").strip():
                errors.append(f"PROVENANCE.md:{number}: {path} has an empty {name}.")
    if columns is None:
        errors.append("No valid Asset path / Origin / License provenance table found.")
    for file in sorted((root / "assets").rglob("*")):
        if file.is_file() and file != ledger:
            path = file.relative_to(root).as_posix()
            if path not in entries:
                errors.append(f"Missing provenance row: {path}")
    return errors


def main():
    errors = check(Path(sys.argv[1]) if len(sys.argv) > 1 else ROOT)
    for error in errors:
        print("ERROR", error, file=sys.stderr)
    if not errors:
        print("Asset provenance passes: exact paths and non-empty Origin/License.")
    return 1 if errors else 0


if __name__ == "__main__":
    sys.exit(main())
