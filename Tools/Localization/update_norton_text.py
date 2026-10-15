"""Keep English and Hebrew story/UI entries together."""
import json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
TEXT={
 'note_norton_title':('A letter to Mr. Norton','מכתב למר נורטון'),
 'note_norton':('Mr. Norton,\n\nThey have crossed onto my land. Last night the lamps went out, but the things beneath them kept moving. Dead men\'s heads, crawling on six legs. One of them spoke in my brother\'s voice. He has been buried for nine years.\n\nYou are the finest hunter I know. I have left my shotgun beside the car. Light will not turn these creatures back. Use steel and shot. When their mouths begin to glow, get out of the way.\n\nPlease deal with them before they reach the houses. There is scratching under my door now.\n\nE. Mercer\nBlackpine ranger',
 'מר נורטון,\n\nהם חדרו לשטח שלי. אמש כבו המנורות, אבל הדברים שמתחתיהן המשיכו לנוע. ראשים של אנשים מתים, זוחלים על שש רגליים. אחד מהם דיבר בקולו של אחי. אחי קבור כבר תשע שנים.\n\nאתה הצייד הטוב ביותר שאני מכיר. השארתי את רובה הציד לצד הרכב. האור לא יבריח את היצורים האלה. השתמש בברזל ובעופרת. כשהפה שלהם מתחיל לזהור, זוז מהדרך.\n\nבבקשה, טפל בהם לפני שיגיעו לבתים. עכשיו אני שומע שריטות מתחת לדלת.\n\nא. מרסר\nשומר שמורת בלקפיין'),
 'take_shotgun':('E — take the shotgun','E — קח את רובה הציד'),
 'take_shells':('E — take cartridges ({0})','E — קח כדורי ציד ({0})'),
 'shotgun_acquired':('Shotgun acquired. Left click fires · R reloads · 1 axe / 2 shotgun · B flashlight battery.','רובה ציד נמצא. לחצן שמאלי לירי · R לטעינה · 1 גרזן / 2 רובה · B סוללת הפנס.'),
 'shells_acquired':('Cartridges added to your pack.','כדורי ציד נוספו לתיק.'),
 'shells_full':('Your cartridge pouch is full.','אין מקום נוסף לכדורי ציד.'),
 'shotgun_empty':('The barrels are empty. Press R to reload.','הרובה ריק. לחץ R לטעינה.'),
 'shotgun_hud':('Shotgun {0}/2 · spare {1} · R reload · 1 axe','רובה {0}/2 · במלאי {1} · R טעינה · 1 גרזן'),
 'shotgun_stowed':('2 — equip shotgun ({0}/2 · {1} spare)','2 — רובה ציד ({0}/2 · במלאי {1})'),
 'shotgun_reloading':('Reloading… {0:0.0}s','טוען… {0:0.0} שניות'),
 'head_arrival':('Something answers the letter. Light cannot stop it. Aim at the head — or use your axe.','משהו עונה למכתב. האור לא יעצור אותו. כוון לראש — או השתמש בגרזן.'),
 'fire_hit':('You are burned. Avoid another fireball for ten seconds.','נכווית. הימנע מכדור אש נוסף במשך עשר שניות.'),
 'fire_recovery':('Burn recovery: {0:0.0}s · another fireball is fatal','החלמה מכוויה: {0:0.0} שניות · כדור אש נוסף יהרוג אותך'),
 'fire_death':('A second flame took the last of your strength.','הלהבה השנייה כילתה את שארית כוחך.'),
 'checkpoint_wreck':('The shotgun by the wreck','רובה הציד שליד הרכב'),
 'checkpoint_norton':('Norton\'s letter','המכתב לנורטון'),
 'controls':('WASD move · Shift run · Ctrl crouch · E interact · F light · Right click boost · B battery · R reload · 1 axe / 2 shotgun · Left click attack · Tab pack · J notes · M map · Esc pause · F11 fullscreen','WASD תנועה · Shift ריצה · Ctrl כריעה · E פעולה · F פנס · לחצן ימני חיזוק · B סוללה · R טעינה · 1 גרזן / 2 רובה · לחצן שמאלי תקיפה · Tab תיק · J מסמכים · M מפה · Esc השהיה · F11 מסך מלא')
}
for index,language in enumerate(['English','Hebrew']):
    path=ROOT/'Assets/_Game/Data'/f'{language}.json';data=json.loads(path.read_text(encoding='utf-8'));lookup={e['id']:e for e in data['entries']}
    for key,values in TEXT.items():
        if key in lookup:lookup[key]['value']=values[index]
        else:data['entries'].append({'id':key,'value':values[index]})
    path.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
for name in ['CoreTests','CombatTests','ForestEncounterTests']:
    path=ROOT/'Assets/_Game/Tests/EditMode'/f'{name}.cs';s=path.read_text(encoding='utf-8');s=s.replace('loaded.schemaVersion,Is.EqualTo(4)','loaded.schemaVersion,Is.EqualTo(5)');path.write_text(s,encoding='utf-8')
print('Updated bilingual Norton story and weapon/condition controls.')
