"""Executed in forest_art.py's authoring context; original spider-human creature and props."""
clean()
mats.update({name:material(name,color) for name,color in [('Chitin',(.045,.055,.05)),('Mouth',(.035,.008,.009)),('Teeth',(.55,.52,.44)),('Coat',(.16,.19,.18)),('Skin',(.23,.25,.24))]})

def ellipsoid(name,position,scale,mat):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=24,ring_count=16,location=position)
    obj=bpy.context.object;obj.name=name;obj.scale=scale
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    obj.data.materials.append(mats[mat])
    for poly in obj.data.polygons:poly.use_smooth=True
    return obj

def lower(obj):
    group=obj.vertex_groups.new(name='JawLower');group.add(list(range(len(obj.data.vertices))),1,'REPLACE')

# Licensed MakeHuman anatomy, four dressed poses and three LODs.
import runpy
runpy.run_path(str(Path(__file__).parent/'licensed_humans.py'),run_name='__main__')

# Three rooms and a 3m central corridor; collider shells mirror every opening.
clean()
box('Foundation',(0,-.10,0),(18.2,.2,14.2),'Stone')
for n in range(24):box('Floorboards',(-8.65+n*.75,.035,0),(.72,.06,14),'Timber',.006)
box('Back wall',(0,1.6,-7),(18,3.2,.25),'Timber')
for x in (-9,9):box('Side wall',(x,1.6,0),(.25,3.2,14),'Timber')
for x in (-5.25,5.25):box('Front wall',(x,1.6,7),(7.5,3.2,.25),'Timber')
box('Entrance lintel',(0,2.98,7),(3,.44,.25),'Timber')
for x in (-1.5,1.5):
    for a,b in [(-7,-4.75),(-2.25,2.25),(4.75,7)]:box('Corridor wall',(x,1.6,(a+b)/2),(.20,3.2,b-a),'Timber')
    for z in (-3.5,3.5):box('Room lintel',(x,2.98,z),(.20,.44,2.5),'Timber')
box('East room divider',(5.25,1.6,0),(7.5,3.2,.20),'Timber')
# West room spans the building; two east rooms. One west opening is boarded off.
box('West closed doorway',(-1.5,1.6,-3.5),(.2,3.2,2.5),'Timber')
for x in (-5.8,5.8):
    box('Broken window glass',(x,1.8,7.16),(1.6,1.15,.04),'Glass',.005)
    for dx in (-.9,.9):box('Window trim',(x+dx,1.8,7.20),(.09,1.3,.08),'PaleBark')
    for y in (1.15,2.45):box('Window trim',(x,y,7.20),(1.9,.09,.08),'PaleBark')
for side in (-1,1):
    for n in range(20):box('Corrugated roof',(side*4.55,3.83,-7.35+n*.77),(9.4,.12,.74),'Steel',.015,(0,math.radians(side*8),0))
box('Roof ridge',(0,4.52,0),(.2,.15,15.5),'Steel')
for side in (-1,1):
    for n in range(13):box('Closed gable',(0,3.22+n*.1,side*7),(max(.18,18-n*1.35),.095,.25),'Timber',.008)
for n in range(4):
    box('Archive shelf',(-7.9,.35+n*.68,-3),(1.5,.08,6),'Timber')
for z in (-5.9,-.1):
    for x in (-8.6,-7.2):box('Archive upright',(x,1.4,z),(.1,2.8,.1),'Steel')
box('Research desk',(-4.6,.96,3),(3,.18,1.3),'Timber')
for x in (-5.8,-3.4):box('Desk legs',(x,.45,3),(.12,.9,1.1),'Timber')
for z in (2.2,4.9):
    box('Bunk frame',(6,.4,z),(1.4,.15,2.2),'Steel');box('Bunk mattress',(6,.55,z),(1.3,.15,2.1),'Fabric')
box('Observation table',(5.5,.95,-3.7),(2.8,.15,1.4),'Timber')
for x in (4.4,6.6):box('Table leg',(x,.45,-3.7),(.13,.9,1.2),'Steel')
finish('research_station_01')
print('Four CC0-derived human bodies and original three-room research station authored.',flush=True)

# Author the weighted reference crawler last, in an isolated Python namespace.
import runpy
runpy.run_path(str(Path(__file__).parent/'reference_creature.py'), run_name='__main__')
