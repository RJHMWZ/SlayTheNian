"""Prepare generated Spring Festival sprite sheets for the existing pixel renderer.

Only crops, nearest-neighbour scaling, palette reduction and atlas packing occur here.
The illustrations themselves come from imagegen, not procedural stand-ins.
"""
from pathlib import Path
from PIL import Image
import argparse
import json
import math
import re
import uuid

ROOT = Path(__file__).resolve().parents[1]
RES = ROOT / 'Assets/Sleigh/Resources/Sleigh'
OUT = RES / 'Art/Festival'

# Cell indexes in the generated 8x8 item sheet, in row-major order.
GIFT_ITEMS = dict(zip(
    'sock mitten snowball canejab icicle baubles sledsweep nutchomp tinselwhip bellslam crumbs scarf cocoasip offkey wrapspree stuffer eggshot snowfort frostbreath chimney stampede kiss bellybump flurry jingle doublewrap globe fireplace checktwice elfhelp spiked tinselarmor hollywreath candles snowfall rhythm midnight starfall birch bigbow miracle milkcookies guidingstar choir coal soot naughty'.split(),
    range(47)))
GIFT_ITEMS.update(atk=0, skl=1, pow=33, curse=44, friend=29)
RELIC_ITEMS = dict(zip(
    'toolbelt rednose cookietin jinglebell warmsocks candybowl snowglobe eggnogmug iceskates hollysprig nutjaw tinselgarland gingercharm lostmitten reindeerbell treestar coalscuttle gifttag ledger workbench sackpatch cocoapot elfcoupon pocketwatch fruitcake sugarplum snowshovel churchbell yulelog reindeerfeed wishlist treeangel'.split(),
    [35, 0, 52, 35, 49, 52, 26, 16, 49, 50, 7, 39, 1, 60, 20, 40, 53, 56, 54, 35, 15, 12, 59, 57, 41, 15, 51, 61, 27, 55, 54, 17]))
OTHER_ITEMS = dict(zip(
    'st_holly st_carol st_cocoa st_ribbon st_tinsel st_candles st_snowfall st_workshop st_star st_choir st_jolly st_drawnext part_musicbox part_ribbon part_bell part_paint part_battery part_spring part_windup part_glitter part_squeaker part_wheels hook gift_mitten stocking gift'.split(),
    [32, 9, 41, 39, 11, 33, 23, 35, 42, 43, 40, 56, 13, 39, 35, 29, 63, 38, 57, 37, 24, 20, 18, 1, 15, 52]))
ZODIAC_CELLS = dict(zip(
    'rat coalgolem yeti penguin alpha viper toysoldier creeper rogueelf angel grumpkin slime gingerling snowman ornament lights'.split(),
    range(16)))
SPIRIT_CELLS = dict(zip(
    'nutgrunt jack fruitcake bat krampimp general coalbaron witch jackfrost santa krampus naughtylist'.split(),
    range(12)))


def rgba(im):
    im = im.convert('RGBA')
    im.putalpha(im.getchannel('A').point(lambda a: 255 if a >= 160 else 0))
    return im


def cell(im, index, columns, rows):
    col, row = index % columns, index // columns
    return rgba(im.crop((round(col * im.width / columns), round(row * im.height / rows),
                        round((col + 1) * im.width / columns), round((row + 1) * im.height / rows))))


def fit(im, width, height, logical_size=None):
    if logical_size:
        im = rgba(im.resize((logical_size, logical_size), Image.Resampling.NEAREST))
    bounds = im.getchannel('A').getbbox()
    if not bounds:
        raise ValueError('Empty generated sprite')
    im = im.crop(bounds)
    scale = min(width / im.width, height / im.height)
    return rgba(im.resize((max(1, round(im.width * scale)), max(1, round(im.height * scale))), Image.Resampling.NEAREST))


def save_atlas(im, name):
    path = OUT / name
    # One small shared palette and binary alpha keep every texel a real pixel.
    im = im.quantize(colors=64, method=Image.Quantize.FASTOCTREE, dither=Image.Dither.NONE).convert('RGBA')
    im = rgba(im)
    im.save(path, optimize=True)
    meta = Path(str(path) + '.meta')
    if not meta.exists():
        template = (RES / 'Art/Icons/FestivalHudAtlas.png.meta').read_text(encoding='utf-8')
        template = re.sub(r'^guid: .*$', 'guid: ' + uuid.uuid4().hex, template, flags=re.M)
        meta.write_text(template, encoding='utf-8')
    return 'Sleigh/Art/Festival/' + path.stem


def frame(kind, anim, path, x, y, width, height, display_width=None, display_height=None, pivot=(.5, .5), eyes=None):
    value = dict(type=kind, anim=anim, frame=0, path=path, sheet=Path(path).name + '.png',
                 width=display_width or width, height=display_height or height,
                 pivotX=pivot[0], pivotY=pivot[1], x=x, y=y, sourceWidth=width, sourceHeight=height, tint=0)
    if eyes is not None:
        value['eyes'] = eyes
    return value


def pixelize_existing_catalog(name):
    source = RES / (name + '.json')
    data = json.loads(source.read_text(encoding='utf-8'))
    frames = data['frames']
    images = {}
    native = Image.new('RGBA', (8 * 64, math.ceil(len(frames) / 8) * 64))
    for i, f in enumerate(frames):
        if f['path'] not in images:
            images[f['path']] = Image.open(ROOT / 'Assets/Sleigh/Resources' / (f['path'] + '.png')).convert('RGBA')
        original = images[f['path']]
        crop = original.crop((f['x'], f['y'], f['x'] + f['sourceWidth'], f['y'] + f['sourceHeight']))
        crop = rgba(crop.resize((f['width'], f['height']), Image.Resampling.NEAREST))
        x, y = i % 8 * 64, i // 8 * 64
        native.alpha_composite(crop, (x, y))
        f.update(x=x, y=y, sourceWidth=f['width'], sourceHeight=f['height'])
    path = ROOT / 'Assets/Sleigh/Resources' / (frames[0]['path'] + '.png')
    rgba(native.quantize(colors=64, method=Image.Quantize.FASTOCTREE, dither=Image.Dither.NONE).convert('RGBA')).save(path, optimize=True)
    source.write_text(json.dumps(data, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for key in ['items', 'zodiac', 'spirits', 'frames']:
        parser.add_argument('--' + key, required=True, type=Path)
    args = parser.parse_args()
    OUT.mkdir(parents=True, exist_ok=True)
    folder_meta = OUT.with_suffix('.meta')
    if not folder_meta.exists():
        folder_meta.write_text('fileFormatVersion: 2\nguid: ' + uuid.uuid4().hex + '\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n', encoding='utf-8')
    old = json.loads((RES / 'ArtIndex.json').read_text(encoding='utf-8'))['frames']
    definitions = json.loads((RES / 'Definitions.json').read_text(encoding='utf-8'))
    old_by_key = {(f['type'], f['anim']): f for f in old}

    items = Image.open(args.items)
    item_atlas = Image.new('RGBA', (256, 256))
    for i in range(64):
        sprite = fit(cell(items, i, 8, 8), 28, 28, 32)
        item_atlas.alpha_composite(sprite, (i % 8 * 32 + (32 - sprite.width) // 2, i // 8 * 32 + (32 - sprite.height) // 2))
    item_path = save_atlas(item_atlas, 'FestivalItemsAtlas.png')

    zodiac, spirits = Image.open(args.zodiac), Image.open(args.spirits)
    enemies = Image.new('RGBA', (1024, 512))
    final = []
    enemy_frames = {}
    for i, enemy in enumerate(definitions['enemies']):
        key = enemy['id']; old_frame = old_by_key[('Enemy', key)]
        if key in ZODIAC_CELLS:
            raw = cell(zodiac, ZODIAC_CELLS[key], 4, 4)
        else:
            raw = cell(spirits, SPIRIT_CELLS[key], 4, 4)
        w, h = old_frame['width'], old_frame['height']
        if key == 'santa': w, h = 118, 96
        if key == 'krampus': w, h = 126, 108
        logical = rgba(raw.resize((64, 64), Image.Resampling.NEAREST))
        bounds = logical.getchannel('A').getbbox()
        # A broad door-god must not be squeezed into the old thin Nutcracker canvas.
        w = min(126, max(w, round((h - 2) * (bounds[2] - bounds[0]) / (bounds[3] - bounds[1])) + 2))
        sprite = fit(logical, w - 2, h - 2)
        x, y = i % 8 * 128, i // 8 * 128
        enemies.alpha_composite(sprite, (x + (w - sprite.width) // 2, y + h - sprite.height))
        enemy_frames[key] = frame('Enemy', key, '', x, y, w, h, pivot=(.5, 1), eyes=[dict(x=.4, y=.27), dict(x=.6, y=.27)])
    enemy_path = save_atlas(enemies, 'FestivalEnemiesAtlas.png')
    for f in enemy_frames.values():
        f['path'] = enemy_path; f['sheet'] = 'FestivalEnemiesAtlas.png'; final.append(f)

    card_frames = Image.open(args.frames)
    cards = Image.new('RGBA', (264, 88))
    for i, kind in enumerate(['atk', 'skl', 'pow', 'curse']):
        raw = cell(card_frames, i, 4, 1)
        bounds = raw.getchannel('A').getbbox()
        cards.alpha_composite(rgba(raw.crop(bounds).resize((66, 88), Image.Resampling.NEAREST)), (i * 66, 0))
    cards_path = save_atlas(cards, 'FestivalCardFramesAtlas.png')
    for i, kind in enumerate(['atk', 'skl', 'pow', 'curse']):
        final.append(frame('CardFrame', kind, cards_path, i * 66, 0, 66, 88, pivot=(0, 0)))
    final.append(frame('CardFrame', 'friend', cards_path, 66, 0, 66, 88, pivot=(0, 0)))

    def item_frame(kind, key, index, w=32, h=32):
        return frame(kind, key, item_path, index % 8 * 32, index // 8 * 32, 32, 32, w, h)
    for key, index in GIFT_ITEMS.items():
        final.append(item_frame('Gift', key, index))
    for key, f in enemy_frames.items():
        final.append(dict(f, type='Gift'))

    hud = {f['anim'] for f in json.loads((RES / 'FestivalHudArt.json').read_text())['frames']}
    map_icons = {f['anim'] for f in json.loads((RES / 'FestivalMapArt.json').read_text())['frames']}
    seen = set()
    code = '\n'.join(p.read_text(encoding='utf-8') for p in (ROOT / 'Assets/Sleigh').rglob('*.cs'))
    literal_keys = set(re.findall(r'"([^"\\\r\n]*)"', code))
    for f in old:
        key = f['anim']
        if f['type'] == 'Icon' and key not in seen:
            seen.add(key)
            if key in hud or key in map_icons or key in ['Animation 2', 'new_arrow']:
                continue
            if key not in literal_keys and not key.startswith(('st_', 'relic_', 'potion_', 'part_', 'node_')):
                continue
            index = RELIC_ITEMS.get(key.removeprefix('relic_')) if key.startswith('relic_') else OTHER_ITEMS.get(key)
            if index is not None:
                final.append(item_frame('Icon', key, index, f['width'], f['height']))
            elif key.startswith('potion_') or key == 'potion':
                final.append(item_frame('Icon', key, 30, f['width'], f['height']))
            else:
                final.append(f)
        elif f['type'] == 'Claus':
            if key == 'tinsel_idle':
                f = dict(old_by_key[('Claus', 'rudolph_idle')], anim=key)
            final.append(f)
        elif f['type'] in ['HeartEye', 'Background']:
            final.append(f)

    # Old source-sheet coordinates have no effect for standalone retained sprites.
    # Keeping logical IDs preserves save compatibility while removing Christmas art paths.
    (RES / 'ArtIndex.json').write_text(json.dumps(dict(frames=final), ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    pixelize_existing_catalog('FestivalHudArt')
    pixelize_existing_catalog('FestivalMapArt')
    print(f'Prepared {len(definitions["enemies"])} Spring Festival enemies, {len(GIFT_ITEMS)} item/card symbols and five card categories; {len(final)} logical frames retained.')


if __name__ == '__main__':
    main()
