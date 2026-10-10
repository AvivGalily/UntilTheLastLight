"""Generate the original Blender forest assets and PBR textures."""
from pathlib import Path
import runpy

runpy.run_path(str(Path(__file__).with_name("forest_art.py")), run_name="__main__")
runpy.run_path(str(Path(__file__).with_name("combat_assets.py")), run_name="__main__")
runpy.run_path(str(Path(__file__).with_name("encounter_assets.py")), run_name="__main__")
runpy.run_path(str(Path(__file__).with_name("licensed_forest.py")), run_name="__main__")
runpy.run_path(str(Path(__file__).with_name("cabin_sign.py")), run_name="__main__")
runpy.run_path(str(Path(__file__).with_name("victim_web.py")), run_name="__main__")
runpy.run_path(str(Path(__file__).with_name("player_presence.py")), run_name="__main__")
runpy.run_path(str(Path(__file__).with_name("norton_assets.py")), run_name="__main__")
