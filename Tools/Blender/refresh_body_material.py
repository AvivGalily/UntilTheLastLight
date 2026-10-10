"""Refresh the skin preview nodes in existing .blend sources only."""
import argparse
from pathlib import Path
import runpy
import sys
import bpy

parser=argparse.ArgumentParser();parser.add_argument('--project',type=Path,required=True)
args=parser.parse_args(sys.argv[sys.argv.index('--')+1:]);root=args.project.resolve()
builder=runpy.run_path(str(Path(__file__).with_name('forest_bodies.py')))
builder['material'].__globals__['ROOT']=root
bpy.context.preferences.filepaths.save_version=0
for path in sorted((root/'ArtSource/Blender').glob('forest_body_*.blend')):
    bpy.ops.wm.open_mainfile(filepath=str(path))
    builder['material']('BodySkin',(.78,.82,.86))
    bpy.ops.wm.save_as_mainfile(filepath=str(path))
    print('Updated human skin preview',path.name,flush=True)
