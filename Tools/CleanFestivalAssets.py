"""Remove superseded artwork/audio only after atlas and serialized-reference checks.

No backup or archive is created. Logical save IDs are intentionally unaffected.
"""
from pathlib import Path
import json
import re

ROOT = Path(__file__).resolve().parents[1]
ASSETS = (ROOT / 'Assets').resolve()
RES = ASSETS / 'Sleigh/Resources/Sleigh'
GUID = re.compile(r'^guid: ([a-f0-9]{32})', re.M)
REF = re.compile(r'guid: ([a-f0-9]{32})')


def main():
    files = [p for p in ASSETS.rglob('*') if p.is_file()]
    guid_of = {}
    incoming = {}
    for p in files:
        if p.suffix == '.meta':
            match = GUID.search(p.read_text(encoding='utf-8-sig', errors='replace'))
            if match:
                guid_of[Path(str(p)[:-5])] = match[1]
        if p.suffix.lower() in {'.meta', '.unity', '.prefab', '.asset', '.mat', '.controller', '.anim', '.spriteatlas', '.json'}:
            for guid in REF.findall(p.read_text(encoding='utf-8-sig', errors='replace')):
                incoming.setdefault(guid, set()).add(p)

    indexed = set()
    for name in ['ArtIndex', 'FestivalHudArt', 'FestivalMapArt']:
        for frame in json.loads((RES / (name + '.json')).read_text(encoding='utf-8'))['frames']:
            path = ASSETS / 'Sleigh/Resources' / (frame['path'] + '.png')
            if not path.is_file():
                raise RuntimeError('Missing indexed image: ' + str(path))
            indexed.add(path)

    candidates = set(p for p in (RES / 'Art').rglob('*.png') if p not in indexed)
    candidates.update(p for p in (RES / 'UI').glob('*.png') if p not in indexed)
    # Old music, old generated sounds and unused source variants are removable only
    # when no bank, prefab, scene or other serialized asset references their GUID.
    candidates.update(p for p in (ASSETS / 'Sleigh/Audio').rglob('*') if p.suffix.lower() in {'.wav', '.mp3', '.ogg'})
    removed = []; blocked = []
    for p in sorted(candidates):
        resolved = p.resolve()
        if not resolved.is_relative_to(ASSETS):
            raise RuntimeError('Deletion escaped Assets: ' + str(p))
        references = incoming.get(guid_of.get(p, ''), set()) - {Path(str(p) + '.meta')}
        if references:
            if p.suffix.lower() == '.png':
                blocked.append((str(p.relative_to(ROOT)), [str(x.relative_to(ROOT)) for x in references]))
            continue
        size = p.stat().st_size
        p.unlink(); Path(str(p) + '.meta').unlink(missing_ok=True)
        removed.append((str(p.relative_to(ROOT)), size))

    # Delete now-empty art/audio directories and their metadata, never source-code directories.
    for base in [RES / 'Art', RES / 'UI', ASSETS / 'Sleigh/Audio']:
        if not base.exists():
            continue
        for directory in sorted([p for p in base.rglob('*') if p.is_dir()], key=lambda p: len(p.parts), reverse=True):
            if not any(directory.iterdir()):
                directory.rmdir(); Path(str(directory) + '.meta').unlink(missing_ok=True)
        if not any(base.iterdir()):
            base.rmdir(); Path(str(base) + '.meta').unlink(missing_ok=True)
    print(f'Deleted {len(removed)} superseded or unused image/audio files and matching metas; {sum(s for _, s in removed) / 1024**2:.2f} MiB removed. No backup created.')
    if blocked:
        print('Remaining unindexed images with serialized references:', json.dumps(blocked, ensure_ascii=False))
        raise SystemExit(1)


if __name__ == '__main__':
    main()
