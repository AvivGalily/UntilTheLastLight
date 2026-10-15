"""Generate the original Blender forest assets and PBR textures."""
from pathlib import Path
import runpy
import sys

runpy.run_path(str(Path(__file__).with_name("forest_art.py")), run_name="__main__")
runpy.run_path(str(Path(__file__).with_name("combat_assets.py")), run_name="__main__")
runpy.run_path(str(Path(__file__).with_name("encounter_assets.py")), run_name="__main__")
runpy.run_path(str(Path(__file__).with_name("licensed_forest.py")), run_name="__main__")
runpy.run_path(str(Path(__file__).with_name("cabin_sign.py")), run_name="__main__")
runpy.run_path(str(Path(__file__).with_name("victim_web.py")), run_name="__main__")
runpy.run_path(str(Path(__file__).with_name("norton_assets.py")), run_name="__main__")
# These focused authors take their project root from their source location and
# must not inherit forest_art's --project arguments as an asset selector.
original_argv=sys.argv[:];sys.argv=[sys.argv[0]]
runpy.run_path(str(Path(__file__).with_name("forest_intro.py")), run_name="__main__")
runpy.run_path(str(Path(__file__).with_name("intro_driver.py")), run_name="__main__")
runpy.run_path(str(Path(__file__).with_name("arrival_cast.py")), run_name="__main__")
runpy.run_path(str(Path(__file__).with_name("encounter_cast.py")), run_name="__main__")
runpy.run_path(str(Path(__file__).with_name("player_presence.py")), run_name="__main__")
runpy.run_path(str(Path(__file__).with_name("first_person_arms.py")), run_name="__main__")
runpy.run_path(str(Path(__file__).with_name("research_specimen.py")), run_name="__main__")
runpy.run_path(str(Path(__file__).with_name("research_creature.py")), run_name="__main__")
runpy.run_path(str(Path(__file__).with_name("research_preserved_heads.py")), run_name="__main__")
sys.argv=original_argv
