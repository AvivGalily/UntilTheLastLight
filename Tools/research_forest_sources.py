import urllib.request,json
from pathlib import Path
from concurrent.futures import ThreadPoolExecutor
ROOT=Path(__file__).resolve().parents[1]/'ArtSource/Vendor/polyhaven';ROOT.mkdir(parents=True,exist_ok=True)
def fetch(slug):
    try:
        request=urllib.request.Request('https://api.polyhaven.com/files/'+slug,headers={'User-Agent':'UntilTheLastLightAssetPreparation/0.6 (Powered by Poly Haven)'})
        with urllib.request.urlopen(request,timeout=35) as r:data=json.load(r)
        (ROOT/(slug+'.files.json')).write_text(json.dumps(data,indent=2))
        if 'blend' in data:
            variants={k:{fmt:{'bytes':v.get('size'),'url':v.get('url'),'includes':len(v.get('include',{}))} for fmt,v in formats.items()} for k,formats in data['blend'].items()}
            return {'id':slug,'blend':variants,'maps':list(data)}
        return {'id':slug,'maps':list(data)}
    except Exception as e:return {'id':slug,'error':str(e)}
if __name__=='__main__':
    with ThreadPoolExecutor(max_workers=3) as pool:
        for r in pool.map(fetch,['pine_tree_01','fir_tree_01','rock_moss_set_01','rock_moss_set_02','dead_tree_trunk','tree_stump_02','pine_bark','forest_ground_04','rocky_trail','wood_planks_dirt','rust_coarse_01']):print(json.dumps(r),flush=True)
