"""Offline authoring downloads. Powered by Poly Haven (https://polyhaven.com).

CC0 sources with published MD5 verification; no network access in the game.
"""
import hashlib,json,urllib.request,shutil
from pathlib import Path
from concurrent.futures import ThreadPoolExecutor
from PIL import Image,ImageOps
from research_forest_sources import fetch,ROOT
HEADERS={'User-Agent':'UntilTheLastLightAssetPreparation/0.6 (Powered by Poly Haven)'}
MODELS=['fir_tree_01','rock_moss_set_01','dead_tree_trunk','tree_stump_02']
SURFACES={'pine_bark':'Bark','forest_ground_04':'ForestFloor','rocky_trail':'Trail','wood_planks_dirt':'Timber','rust_coarse_01':'Steel'}
TEX=ROOT.parents[2]/'Assets/_Game/Art/Textures'
def download(job):
    path,record=job;path.parent.mkdir(parents=True,exist_ok=True)
    if path.exists() and hashlib.md5(path.read_bytes()).hexdigest()==record['md5']:return
    temp=path.with_suffix(path.suffix+'.partial')
    with urllib.request.urlopen(urllib.request.Request(record['url'],headers=HEADERS),timeout=120) as r,temp.open('wb') as f:shutil.copyfileobj(r,f)
    if hashlib.md5(temp.read_bytes()).hexdigest()!=record['md5']:raise ValueError('Checksum failed: '+str(path))
    temp.replace(path);print('Downloaded',path.name,flush=True)
def metadata(slug):
    path=ROOT/(slug+'.files.json')
    if not path.exists():fetch(slug)
    return json.loads(path.read_text())
def jobs():
    for slug in MODELS:
        res='1k' if slug=='fir_tree_01' else '2k';record=metadata(slug)['blend'][res]['blend']
        yield ROOT/slug/(slug+'.blend'),record
        for name,item in record['include'].items():yield ROOT/slug/name,item
    for slug in SURFACES:
        data=metadata(slug)
        for key in ['Diffuse','nor_gl','Rough','AO']:
            formats=data[key]['2k'];ext='png' if 'png' in formats else 'jpg'
            yield ROOT/slug/(key+'.'+ext),formats[ext]
def convert_surfaces():
    for slug,key in SURFACES.items():
        def load(name):return Image.open(next((ROOT/slug).glob(name+'.*'))).resize((2048,2048),Image.Resampling.LANCZOS)
        load('Diffuse').convert('RGBA').save(TEX/(key+'_BaseColor.png'))
        load('nor_gl').convert('RGB').save(TEX/(key+'_Normal.png'))
        ao=load('AO').convert('L');rough=load('Rough').convert('L')
        Image.merge('RGBA',(Image.new('L',ao.size,0),ao,Image.new('L',ao.size,128),ImageOps.invert(rough))).save(TEX/(key+'_Mask.png'))
    notices={'provider':'Powered by Poly Haven','license':'CC0-1.0','assets':[{'id':s,'source':'https://polyhaven.com/a/'+s} for s in MODELS+list(SURFACES)],'changes':'Unity packed maps; original UVs retained on scans; optimized mesh LODs'}
    (ROOT/'sources.json').write_text(json.dumps(notices,indent=2))
if __name__=='__main__':
    with ThreadPoolExecutor(max_workers=3) as pool:list(pool.map(download,jobs()))
    convert_surfaces()
