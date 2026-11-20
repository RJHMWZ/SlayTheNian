"""Read-only Unity asset/GUID/resource audit. No third-party dependencies."""
import argparse
from collections import defaultdict
import hashlib
import json
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
GUID = re.compile(r"^guid: ([0-9a-f]{32})", re.M)
REFERENCES = re.compile(r"guid: ([0-9a-f]{32})")
SERIALIZED = {".unity", ".prefab", ".asset", ".mat", ".controller", ".anim"}


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def audit(baseline=None, moves=None):
    errors, warnings = [], []
    guid_paths = defaultdict(list)
    files = [p for p in (ROOT / "Assets").rglob("*") if p.is_file()]
    for p in files:
        if p.suffix == ".meta":
            match = GUID.search(p.read_text(encoding="utf-8-sig"))
            if match:
                guid_paths[match[1]].append(p.relative_to(ROOT).as_posix()[:-5])
        elif not Path(str(p) + ".meta").is_file():
            errors.append("Missing meta: " + p.relative_to(ROOT).as_posix())
    for guid, paths in guid_paths.items():
        if len(paths) != 1:
            errors.append("Duplicate GUID " + guid + ": " + str(paths))

    # Package assets belong to Unity/third parties; inspect their identities without editing them.
    package_guids = set()
    for directory in (ROOT / "Packages", ROOT / "Library/PackageCache"):
        if directory.is_dir():
            for p in directory.rglob("*.meta"):
                match = GUID.search(p.read_text(encoding="utf-8-sig", errors="replace"))
                if match:
                    package_guids.add(match[1])
    unresolved = {}
    for p in files:
        if p.suffix not in SERIALIZED:
            continue
        missing = sorted({g for g in REFERENCES.findall(p.read_text(encoding="utf-8-sig", errors="replace"))
                          if g not in guid_paths and g not in package_guids and not g.startswith("0000000000000000")})
        if missing:
            unresolved[p.relative_to(ROOT).as_posix()] = missing
    if unresolved:
        errors.append("Unresolved serialized GUIDs: " + json.dumps(unresolved))

    resources = defaultdict(list)
    for p in files:
        parts = p.relative_to(ROOT / "Assets").parts
        if "Resources" in parts and p.suffix != ".meta":
            i = parts.index("Resources")
            key = Path(*parts[i + 1:]).with_suffix("").as_posix()
            resources[key].append(p)
    for key, paths in resources.items():
        # The font atlas deliberately has .png and .json with the same typed resource key.
        if len(paths) > 1 and {p.suffix for p in paths} != {".png", ".json"}:
            errors.append("Ambiguous resource key: " + key)
    frame_count = 0
    for name in ("ArtIndex", "FestivalHudArt", "FestivalMapArt"):
        path = ROOT / "Assets/Sleigh/Resources/Sleigh" / (name + ".json")
        for frame in json.loads(path.read_text(encoding="utf-8"))["frames"]:
            frame_count += 1
            if frame["path"] not in resources:
                errors.append("Missing sprite resource: " + frame["path"])
    for p in (ROOT / "Assets/Sleigh").rglob("*.cs"):
        for key in re.findall(r'Resources\.Load<[^>]+>\("([^"]+)"\)', p.read_text(encoding="utf-8")):
            if key not in resources:
                errors.append("Missing resource literal: " + key + " in " + p.name)
    build = (ROOT / "ProjectSettings/EditorBuildSettings.asset").read_text(encoding="utf-8")
    for path, guid in re.findall(r"path: ([^\n]+)\n\s+guid: (\w+)", build):
        if not (ROOT / path).is_file() or path not in guid_paths.get(guid, []):
            errors.append("Invalid build scene path/GUID: " + path)

    hashes = defaultdict(list)
    for p in files:
        if p.suffix != ".meta":
            hashes[digest(p)].append(p.relative_to(ROOT).as_posix())
    duplicates = [paths for paths in hashes.values() if len(paths) > 1]
    preserved = 0
    if baseline:
        state = json.loads(Path(baseline).read_text(encoding="utf-8"))
        mapping = json.loads(Path(moves).read_text(encoding="utf-8"))["files"] if moves else {}
        for old, expected in state["files"].items():
            new = mapping.get(old, old)
            path = ROOT / new
            if not path.is_file():
                errors.append("Unexpected removal: " + old + " -> " + new)
                continue
            if "guid" in expected:
                match = GUID.search(path.read_text(encoding="utf-8-sig"))
                if not match or match[1] != expected["guid"]:
                    errors.append("Changed GUID: " + old)
                else:
                    preserved += 1
            if path.suffix.lower() in {".png", ".wav", ".ogg", ".mp3", ".ttf", ".prefab", ".unity", ".mat", ".shader"}:
                if digest(path) != expected["sha256"]:
                    errors.append("Unexpected content change: " + new)
        for name in ("Definitions.json", "Localization/zh-CN.json"):
            key = "Assets/Sleigh/Resources/Sleigh/" + name
            if digest(ROOT / key) != state["files"][key]["sha256"]:
                errors.append("Gameplay/localization data changed: " + key)
        for key in ("Assets/Sleigh/Scripts/SleighBattle.cs", "Assets/Sleigh/Scripts/SleighRun.cs", "Assets/Sleigh/Scripts/SleighEvents.cs"):
            if digest(ROOT / mapping.get(key, key)) != state["files"][key]["sha256"]:
                errors.append("Gameplay rules changed: " + key)
        for key, value in state["files"].items():
            if key.startswith("Packages/") and digest(ROOT / key) != value["sha256"]:
                errors.append("Package configuration changed: " + key)
    return dict(errors=errors, warnings=warnings, asset_files=len(files), asset_guids=len(guid_paths),
                preserved_baseline_guids=preserved, package_guids=len(package_guids),
                sprite_manifest_frames=frame_count, duplicate_content_groups=duplicates,
                unresolved=unresolved)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--baseline")
    parser.add_argument("--moves")
    parser.add_argument("--output")
    args = parser.parse_args()
    result = audit(args.baseline, args.moves)
    if args.output:
        Path(args.output).write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps(result, ensure_ascii=False, indent=2))
    raise SystemExit(bool(result["errors"]))
