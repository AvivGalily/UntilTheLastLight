"""Fetch pinned, openly licensed character-authoring inputs into the project."""
from pathlib import Path
import hashlib, json, urllib.request, zipfile
from concurrent.futures import ThreadPoolExecutor

ROOT=Path(__file__).resolve().parents[1]/'ArtSource'/'Vendor'
SOURCES={
 'mpfb-2.0.17': 'https://api.github.com/repos/makehumancommunity/mpfb2/zipball/v2.0.17',
 'male-skins-cc0': 'https://files.makehumancommunity.org/asset_packs/skins02/skins02_cc0.zip',
 'newdlc-wolf-cc0': 'https://opengameart.org/sites/default/files/3dwolf_blend.zip',
 'human-system-cc0': 'https://files.makehumancommunity.org/asset_packs/makehuman_system_assets/makehuman_system_assets_cc0.zip',
 'yells-cc0': 'https://opengameart.org/sites/default/files/yelling%20sounds.zip',
 'steps-cc0': 'https://opengameart.org/sites/default/files/%5Bkdd%5DDifferentSteps_0.zip',
}
def fetch(item):
    name,url=item; target=ROOT/(name+'.zip'); ROOT.mkdir(parents=True,exist_ok=True)
    if not target.exists() or not zipfile.is_zipfile(target):
        with urllib.request.urlopen(url,timeout=90) as response, target.open('wb') as output:
            while chunk:=response.read(64*1024): output.write(chunk)
    destination=(ROOT/name).resolve();destination.mkdir(exist_ok=True)
    with zipfile.ZipFile(target) as archive:
        for member in archive.infolist():
            if not (destination/member.filename).resolve().is_relative_to(destination):raise ValueError('Unsafe archive path')
        archive.extractall(destination)
    record={'name':name,'url':url,'sha256':hashlib.sha256(target.read_bytes()).hexdigest(),'bytes':target.stat().st_size}
    (ROOT/(name+'.source.json')).write_text(json.dumps(record,indent=2))
    print(record,flush=True)
if __name__=='__main__':
    with ThreadPoolExecutor(max_workers=2) as pool:list(pool.map(fetch,SOURCES.items()))
