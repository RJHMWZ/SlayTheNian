from pathlib import Path
import json, shutil, uuid, struct, hashlib

root = Path(r'D:/workspace/SlayTheNian')
source_list = root / '.codex-build/app-icons/sources.json'
destination = root / 'Assets/Sleigh/Art/AppIcons'
destination.mkdir(parents=True, exist_ok=True)
folder_meta = Path(str(destination) + '.meta')
if not folder_meta.exists():
    folder_meta.write_text('fileFormatVersion: 2\nguid: ' + uuid.uuid4().hex + '\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n', encoding='utf-8')

entries = json.loads(source_list.read_text(encoding='utf-8'))
report = []
for entry in entries:
    src = Path(entry['path'])
    target = destination / ('AppIcon_' + entry['name'] + '.png')
    if target.exists():
        raise RuntimeError('Refusing to overwrite ' + str(target))
    shutil.copyfile(src, target)
    digest = hashlib.sha256(target.read_bytes()).hexdigest()
    assert digest == hashlib.sha256(src.read_bytes()).hexdigest()
    with target.open('rb') as stream:
        header = stream.read(33)
    assert header[:8] == b'\x89PNG\r\n\x1a\n'
    width, height = struct.unpack('>II', header[16:24])
    assert width == height
    meta = Path(str(target) + '.meta')
    if not meta.exists():
        template = (root / 'Assets/Sleigh/Resources/Sleigh/Art/Characters/yierIdle0.png.meta').read_text(encoding='utf-8')
        import re
        template = re.sub(r'(?m)^guid: .*$', 'guid: ' + uuid.uuid4().hex, template)
        replacements = {'textureType': '0', 'spriteMode': '0', 'spritePixelsToUnits': '100', 'alignment': '0', 'alphaIsTransparency': '0', 'spriteGenerateFallbackPhysicsShape': '0', 'spritePivot': '{x: 0.5, y: 0.5}'}
        for key, value in replacements.items():
            template = re.sub(r'(?m)^  ' + key + r': .*$', '  ' + key + ': ' + value, template)
        template = re.sub(r'(?m)^    spriteID: .*$', '    spriteID: ' + '0' * 32, template)
        template = re.sub(r'(?m)^    internalID: .*$', '    internalID: 0', template)
        meta.write_text(template, encoding='utf-8')
    report.append({'file': str(target), 'theme': entry['brief'], 'width': width, 'height': height, 'sha256': digest})

records = root / 'Deliverables/AppIconGeneration'
records.mkdir(parents=True, exist_ok=True)
(records / 'prompts.json').write_text(json.dumps({'mode': 'built-in image_gen', 'prompts': [{'name': e['name'], 'theme': e['brief'], 'prompt': e['prompt']} for e in entries]}, ensure_ascii=False, indent=2), encoding='utf-8')
(records / 'files.json').write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
print(json.dumps(report, ensure_ascii=False, indent=2))
