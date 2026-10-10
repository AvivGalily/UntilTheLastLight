"""Assemble unmodified Unity capture frames into an animated review artifact."""
from pathlib import Path
from PIL import Image

root=Path(__file__).resolve().parents[1]
folder=root/'Docs/Screenshots/Creature'
paths=sorted((folder/'Crawl').glob('frame-*.png'))
if len(paths)!=24:raise RuntimeError('Capture the 24 Unity crawl frames first')
frames=[Image.open(p).convert('RGB') for p in paths]
frames[0].save(folder/'crawl-preview.gif',save_all=True,append_images=frames[1:],duration=50,loop=0,disposal=2)
print('Created actual 1.2-second Unity crawl preview:',folder/'crawl-preview.gif')
