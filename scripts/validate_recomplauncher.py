#!/usr/bin/env python3
"""Validate the shipped RecompLauncher manifest against the official SDK schema."""
import json
import sys
from pathlib import Path
from urllib.request import urlopen
from urllib.error import HTTPError, URLError

from jsonschema import Draft7Validator

ROOT = Path(__file__).resolve().parent.parent
MANIFEST = ROOT / "recomplauncher.json"
SCHEMA_URL = "https://recomplauncher.com/schema/recomplauncher.json"


def main():
    manifest = json.loads(MANIFEST.read_text(encoding="utf-8"))
    # The official endpoint may reject CI clients (HTTP 403). Keep local checks
    # available instead of blocking every launcher build on a remote service.
    try:
        with urlopen(SCHEMA_URL, timeout=15) as response:
            schema = json.load(response)
    except (HTTPError, URLError, TimeoutError) as exc:
        print(f"WARNING: official schema unavailable ({exc}); full schema validation skipped")
        schema = {
            "type": "object",
            "required": ["format", "repo", "start"],
            "properties": {
                "format": {"const": 1},
                "repo": {"type": "string", "minLength": 3},
                "start": {
                    "type": "object",
                    "required": ["program"],
                    "properties": {
                        "program": {"type": "array", "minItems": 1, "items": {"type": "string"}}
                    }
                },
                "config": {"type": "array"},
                "settings": {"type": "array"}
            }
        }
    else:
        print("Official RecompLauncher schema downloaded")
    Draft7Validator.check_schema(schema)
    errors = sorted(Draft7Validator(schema).iter_errors(manifest), key=lambda e: str(e.path))
    for error in errors:
        print(f"ERROR at {list(error.path)}: {error.message}", file=sys.stderr)
    if errors:
        return 1
    if manifest.get("repo") != "VeladeLibra/clock-tower-pc-enhanced":
        print("ERROR: wrong repository", file=sys.stderr)
        return 1
    if manifest.get("start", {}).get("program") != ["ClockTower_PC_Enhanced.exe"]:
        print("ERROR: start.program differs from the released launcher", file=sys.stderr)
        return 1
    print("RecompLauncher manifest: available schema and project checks passed")
    return 0


if __name__ == "__main__":
    sys.exit(main())
