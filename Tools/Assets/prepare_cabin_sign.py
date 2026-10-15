"""Retain the CC0 source archive and pack its existing maps for Unity URP."""
import urllib.request,zipfile
from pathlib import Path
from PIL import Image
root=Path(__file__).resolve().parents[2];vendor=root/'ArtSource/Vendor/wooden-sign-cc0';vendor.mkdir(parents=True,exist_ok=True)
archive=vendor/'medievalSign.zip';source=vendor/'source'
if not archive.exists():
    with urllib.request.urlopen('https://opengameart.org/sites/default/files/medievalSign.zip',timeout=40) as response:archive.write_bytes(response.read())
if not (source/'medievalSign.fbx').exists():
    with zipfile.ZipFile(archive) as package:
        for name in package.namelist():
            destination=(source/name).resolve()
            if not destination.is_relative_to(source.resolve()):raise ValueError('Source archive path outside vendor directory')
        package.extractall(source)
out=root/'Assets/_Game/Art/Textures';out.mkdir(parents=True,exist_ok=True)
base=Image.open(source/'medievalSign_col.jpg').convert('RGB');base.save(out/'CabinSignWood_BaseColor.png')
Image.open(source/'medievalSign_norm.jpg').convert('RGB').save(out/'CabinSignWood_Normal.png')
ao=Image.open(source/'medievalSign_AO.jpg').convert('L').resize(base.size)
spec=Image.open(source/'medievalSign_spec.jpg').convert('L').resize(base.size)
smooth=spec.point(lambda v:round(24+v*.20))
Image.merge('RGBA',(Image.new('L',base.size,0),ao,Image.new('L',base.size,0),smooth)).save(out/'CabinSignWood_Mask.png')
print('Prepared source wood albedo/normal and AO/smoothness mask:',base.size)
