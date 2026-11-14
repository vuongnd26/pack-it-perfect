#!/usr/bin/env python3
"""Import existing licensed artwork. No illustration generation, drawing, or executable archive extraction.
Run with python3 -I tools/asset-pipeline/import-sourced-art.py from the project root.
Only alpha-crops downloaded PNGs (preserving aspect ratio) and copies named archive entries.
"""
import concurrent.futures
import hashlib
import io
import json
from pathlib import Path
import urllib.parse
import urllib.request
import zipfile
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
STAGE = ROOT / 'Library/SourcedArtStaging/fluent'
COMMIT = '1ffb34c752ecf5d402f04cfb4b392c77f57c54bc'
RAW = f'https://raw.githubusercontent.com/microsoft/fluentui-emoji/{COMMIT}/'
CATALOG = ROOT / 'Assets/Resources/Art/item_catalog.json'
ART = ROOT / 'Assets/Resources/Art/Sourced'
# These are identity matches, including variants of the same object (e.g. folded socks).
ITEMS = {
    **{key: 'Socks' for key in ['sock_long','sock_short','sock_sport','sock_travel','sock_folded','sock_scrunched','sock_t','sock_straight']},
    **{key: 'Lotion bottle' for key in ['shampoo_pump','shampoo_w','pump_long','pump_t']},
    'toothbrush': 'Toothbrush', 'jar_t': 'Jar', 'milk_carton': 'Glass of milk',
    'wine_glass': 'Wine glass', 'tea_set': 'Teacup without handle', 'teapot': 'Teapot',
    'scarf_s': 'Scarf', 'scarf_w': 'Scarf', 'plush': 'Teddy bear', 'plush_long': 'Teddy bear',
    'trainer': 'Running shoe', 'sneaker': 'Running shoe', 'hiking_boot': 'Hiking boot',
    'cap': 'Billed cap', 'headphones': 'Headphone', 'umbrella': 'Umbrella',
    'sandwich': 'Sandwich', 'croissant': 'Croissant', 'prawn': 'Shrimp',
    'broccoli': 'Broccoli', 'baguette_v': 'Bread', 'baguette': 'Baguette bread',
    'carrot': 'Carrot', 'pepper': 'Bell pepper', 'cheese_u': 'Cheese wedge',
    'potato_sack': 'Potato', 'sushi_roll': 'Sushi', 'biscuit': 'Cookie',
    'lunchbox': 'Bento box', 'book': 'Closed book', 'book_stack': 'Books',
    'telescope': 'Telescope', 'wrench_z': 'Wrench', 'toolbox': 'Toolbox',
    'toy_dinosaur': 'T-rex', 'hand_mirror': 'Mirror', 'poster_tube': 'Scroll',
}
# Do not silently substitute milk in a glass for a carton, a telescope for a drill, etc.
for unsupported in ['milk_carton', 'tea_set', 'poster_tube']:
    ITEMS.pop(unsupported)
UI = {'ui_hint':'Light bulb', 'ui_autopack':'Luggage', 'ui_freeze':'Snowflake',
      'ui_star':'Star', 'ui_lock':'Locked', 'ui_home':'House', 'ui_chest':'Wrapped gift',
      'ui_clock':'Stopwatch', 'ui_heavy':'Balance scale', 'ui_upright':'Up arrow',
      'ui_pair':'Link', 'ui_fragile':'Wine glass', 'logo_luggage':'Luggage'}
PACKS = {
    'ui-pack': [('PNG/Grey/Default/button_square_depth_gloss.png','UI/piece_tile.png'),
                ('PNG/Grey/Default/button_rectangle_depth_gloss.png','UI/button.png'),
                ('PNG/Grey/Default/button_rectangle_flat.png','UI/panel.png')],
    'particle-pack': [('PNG (Transparent)/star_01.png','VFX/star.png'),
                      ('PNG (Transparent)/smoke_01.png','VFX/puff.png'),
                      ('PNG (Transparent)/spark_01.png','VFX/spark.png')],
}
AUDIO = {'UI':'click1', 'Pickup':'rollover2', 'Place':'click3', 'Rotate':'switch2',
         'Return':'switch1', 'Undo':'switch3', 'Locked':'click5'}


def download(url):
    request = urllib.request.Request(url, headers={'User-Agent':'licensed-asset-import'})
    with urllib.request.urlopen(request, timeout=45) as response:
        return response.read(32_000_000)


def digest(data):
    return hashlib.sha256(data).hexdigest()


def write_asset(destination, data):
    destination.parent.mkdir(parents=True, exist_ok=True)
    destination.write_bytes(data)


def main():
    STAGE.mkdir(parents=True, exist_ok=True)
    tree = json.loads(download(f'https://api.github.com/repos/microsoft/fluentui-emoji/git/trees/{COMMIT}?recursive=1'))
    if tree.get('truncated'):
        raise RuntimeError('Source tree truncated; refusing incomplete manifest.')
    pngs = {p['path'].split('/')[1]:p['path'] for p in tree['tree']
            if p['path'].startswith('assets/') and '/3D/' in p['path'] and p['path'].endswith('.png')
            and p['path'].count('/') == 3}
    catalog = json.loads(CATALOG.read_text())
    rows = []
    names = sorted(set(ITEMS.values()) | set(UI.values()))
    def fetch(name):
        path = pngs.get(name)
        if path is None:
            return name, None, None
        data = download(RAW + urllib.parse.quote(path, safe='/'))
        image = Image.open(io.BytesIO(data)).convert('RGBA')
        box = image.getchannel('A').getbbox()
        if not box:
            raise ValueError('Empty source alpha: '+path)
        image = image.crop(box)
        output = io.BytesIO(); image.save(output, format='PNG')
        (STAGE/(name.replace('/','_')+'.png')).write_bytes(data)
        return name, output.getvalue(), {'source':RAW+urllib.parse.quote(path, safe='/'),
                    'source_sha256':digest(data), 'license':'MIT', 'source_revision':COMMIT,
                    'transform':'alpha bounding-box crop only; no resampling or redrawing',
                    'dimensions':[image.width,image.height]}
    sources = {name:(data,info) for name,data,info in concurrent.futures.ThreadPoolExecutor(max_workers=6).map(fetch,names)}
    for item in catalog['objects']:
        source_name = ITEMS.get(item['id'])
        data, info = sources.get(source_name,(None,None))
        if data is None:
            rows.append({'item_id':item['id'],'status':'unresolved','reason':'No same-identity art selected; legacy image retained.'})
            continue
        rel = 'Items/'+source_name.lower().replace(' ','_')+'.png'
        write_asset(ART/rel,data)
        item['sourced_sprite'] = 'Sourced/'+rel[:-4]
        rows.append(dict(info, item_id=item['id'], status='sourced', destination=str((ART/rel).relative_to(ROOT)), sha256=digest(data)))
    for name, source_name in UI.items():
        data,info=sources.get(source_name,(None,None))
        if data is None: raise ValueError('Missing UI source '+source_name)
        rel='UI/'+name+'.png'; write_asset(ART/rel,data)
        rows.append(dict(info,status='sourced',destination=str((ART/rel).relative_to(ROOT)),sha256=digest(data)))
    notices = ['Third-party artwork and sounds. No endorsement by their authors is implied.\n',
               'Fluent Emoji\nhttps://github.com/microsoft/fluentui-emoji\nRevision '+COMMIT+'\n',
               download(RAW+'LICENSE').decode()]
    for pack,entries in PACKS.items():
        archive=ROOT/'Library/SourcedArtStaging'/pack/'source.zip'
        with zipfile.ZipFile(archive) as z:
            license_path=next(n for n in z.namelist() if n.lower().endswith('.txt') and 'license' in n.lower())
            notices.append(z.read(license_path).decode())
            for path,dest in entries:
                data=z.read(path); Image.open(io.BytesIO(data)).verify()
                write_asset(ART/dest,data)
                rows.append({'status':'sourced','source':'https://kenney.nl/assets/'+pack,'entry':path,
                             'archive_sha256':digest(archive.read_bytes()),'license':'CC0-1.0',
                             'destination':str((ART/dest).relative_to(ROOT)),'sha256':digest(data)})
    archive=ROOT/'Library/SourcedArtStaging/ui-audio/source.zip'
    with zipfile.ZipFile(archive) as z:
        license_path=next(n for n in z.namelist() if n.lower().endswith('.txt') and 'license' in n.lower())
        notices.append(z.read(license_path).decode())
        for event,source in AUDIO.items():
            entry='Audio/'+source+'.ogg';data=z.read(entry)
            dest=ROOT/'Assets/Resources/Audio/Sourced'/f'{event}.ogg';write_asset(dest,data)
            rows.append({'event':event,'status':'sourced','source':'https://kenney.nl/assets/ui-audio',
                         'entry':entry,'archive_sha256':digest(archive.read_bytes()),'license':'CC0-1.0',
                         'destination':str(dest.relative_to(ROOT)),'sha256':digest(data)})
    (ROOT/'Assets/Resources/Art/THIRD_PARTY_NOTICES.txt').write_text('\n\n'.join(notices))
    (ROOT/'tools/asset-pipeline/sourced-assets.json').write_text(json.dumps({'schema_version':1,'assets':rows},ensure_ascii=False,indent=2)+'\n')
    CATALOG.write_text(json.dumps(catalog,ensure_ascii=False,indent=2)+'\n')
    print('Sourced items:',sum(r.get('item_id') is not None and r['status']=='sourced' for r in rows))
    print('Unresolved:',', '.join(r['item_id'] for r in rows if r['status']=='unresolved'))

if __name__ == '__main__':
    main()
