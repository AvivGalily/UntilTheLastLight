"""Read official asset pages and retain source download metadata."""
import json, urllib.request, sys
from html.parser import HTMLParser
from pathlib import Path

class Links(HTMLParser):
    def __init__(self): super().__init__(); self.links=[]
    def handle_starttag(self, tag, attrs):
        if tag=='a':
            href=dict(attrs).get('href','')
            if any(x in href.lower() for x in ['.zip','.blend','.7z','download','systemassets','system_assets']):self.links.append(href)

pages=[
 'https://static.makehumancommunity.org/assets/assetpacks/skins02.html',
 'https://static.makehumancommunity.org/assets/assetpacks.html',
 'https://opengameart.org/content/3d-wolf',
 'https://opengameart.org/content/wolf',
 'https://opengameart.org/content/wolf-low-poly-rigged',
 'https://opengameart.org/content/nightmare-hound',
]
for url in (sys.argv[1:] or pages):
    try:
        text=urllib.request.urlopen(url,timeout=35).read().decode(); parser=Links();parser.feed(text)
        print(url,parser.links,flush=True)
        Path('ArtSource/Vendor').mkdir(exist_ok=True)
        Path('ArtSource/Vendor/'+url.rstrip('/').split('/')[-1].replace('.html','')+'.html').write_text(text,encoding='utf-8')
    except Exception as ex:print(url,str(ex),flush=True)
