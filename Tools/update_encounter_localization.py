import json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]/'Assets/_Game/Data'
values={
 'display':('Display: {0} · F11','תצוגה: {0} · F11'),
 'fullscreen':('Full screen','מסך מלא'),
 'windowed':('Window','חלון'),
 'objective_escape':('RUN to an amber-lit house. The flashlight circuit has failed.','לרוץ לבית עם אור ענברי. מעגל הפנס הפסיק לעבוד.'),
 'objective_repair':('Hold E at the indoor charger to repair the flashlight.','החזק E בעמדת הטעינה שבתוך הבית כדי לתקן את הפנס.'),
 'survivor_help':('"Help! Please, help me!"','״הצילו! בבקשה, תעזרו לי!״'),
 'blackout_run':('The light is dead. RUN to the ranger cabin or east storehouse! Hold E at the indoor charger.','הפנס נשרף. לרוץ לבקתת השומר או למחסן המזרחי! יש להחזיק E בעמדת הטעינה שבתוך הבית.'),
 'blackout_refuge':('Inside, at last. Hold E at the charger for 12 seconds to restore the flashlight.','הגעת למחסה. החזק E בעמדת הטעינה במשך 12 שניות כדי להחזיר את הפנס לפעולה.'),
 'repair_hold':('[Hold E] Reset circuit and recharge · 12 seconds','[החזק E] תיקון המעגל וטעינה · 12 שניות'),
 'repair_house_only':('Circuit failure. Use a charger inside the ranger cabin or east storehouse.','תקלה במעגל הפנס. דרושה עמדת טעינה בתוך בקתת השומר או המחסן המזרחי.'),
 'checkpoint_blackout':('The forest ambush','המארב ביער'),
 'checkpoint_refuge':('Reached shelter','הגעה למחסה'),
 'checkpoint_repaired':('Flashlight restored','הפנס חזר לפעולה')}
for n,name in enumerate(['English','Hebrew']):
    path=ROOT/(name+'.json');data=json.loads(path.read_text(encoding='utf-8'))
    # String-table format remains identical to the existing localization files.
    entries={item['id']:item for item in data['entries']}
    for key,pair in values.items():entries[key]={'id':key,'value':pair[n]}
    data['entries']=list(entries.values());path.write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
