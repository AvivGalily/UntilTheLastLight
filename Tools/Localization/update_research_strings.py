"""Research release, creature combat and the cabin gate lock, in both game languages."""
import json
from pathlib import Path
root=Path(__file__).resolve().parents[2]/'Assets/_Game/Data'
entries={
'release_specimen':('E — release R-09','E — שחרור דגימה R-09'),
'release_activated':('Containment release already activated','שחרור המיכל כבר הופעל'),
'containment_warning':('CONTAINMENT RELEASE: draining. Stand clear.','אזהרה: ניקוז המיכל הופעל. התרחקו מהדלתות.'),
'search_specimen':('E — take the key from the neck chain','E — לקיחת המפתח מהשרשרת'),
'specimen_searched':('The key has been recovered','המפתח כבר נאסף'),
'specimen_down':('R-09 is dead. The key on its neck chain is flashing. Take it.','היצור מת. המפתח שעל השרשרת בצווארו מהבהב. קח אותו.'),
'gate_key_acquired':('Gate key recovered. Return to the cabin and turn the lock beside the fuse panel.','מפתח השער נאסף. חזור לבקתה וסובב את המנעול שליד לוח הנתיך.'),
'gate_locked':('The gate has power, but the cabin key lock is still engaged.','לשער יש חשמל, אבל המנעול שבבקתה עדיין נעול.'),
'gate_unlocked':('Gate key lock released','מנעול השער שוחרר'),
'lock_needs_power':('Install the fuse in the adjacent panel first.','התקן קודם את הנתיך בלוח הסמוך.'),
'turn_gate_key':('E — turn the gate key','E — סיבוב מפתח השער'),
'lock_needs_key':('Gate key missing. Search the research station.','מפתח השער חסר. חפש בתחנת המחקר.'),
'gate_lock_opened':('Lock turned. The powered north gate is opening.','המנעול הסתובב. השער הצפוני נפתח.'),
'aquatic_hit':('R-09 struck you. Another strike within five seconds will kill you.','היצור פגע בך. פגיעה נוספת בתוך חמש שניות תהרוג אותך.'),
'specimen_death':('The released specimen killed you.','היצור ששוחרר מהמיכל הרג אותך.'),
'objective_research_key':('Recover the gate key from the creature\'s neck chain.','אסוף את מפתח השער מהשרשרת שעל צוואר היצור.'),
'objective_gate_unlock':('Turn the gate key beside the cabin fuse panel.','סובב את המפתח במנעול שליד לוח הנתיך בבקתה.'),
'inventory_gate_key':('Gate key: {0}','מפתח השער: {0}'),
'hint_research_key':('The gate key hangs from the specimen\'s neck chain. Release it with the red control. Four shotgun hits or axe strikes kill it; two of its strikes within five seconds kill you. Once it dies, collect the flashing key.','מפתח השער תלוי בשרשרת שעל צוואר היצור במיכל הכחול. הכפתור האדום ישחרר אותו. ארבע פגיעות רובה או גרזן יהרגו אותו; שתי פגיעות שלו בתוך חמש שניות יהרגו אותך. לאחר מותו, אסוף את המפתח המהבהב.'),
'hint_gate_unlock':('The key lock is beside the fuse panel in the starting cabin. With power restored, turn the key there, then follow the lamps to the gate.','המנעול נמצא ליד לוח הנתיך בבקתה הראשונה. אחרי חזרת החשמל, סובב שם את המפתח ועקוב אחר הפנסים אל השער.'),
'checkpoint_release':('Research containment released','מיכל המחקר שוחרר'),
'checkpoint_specimen_dead':('Research specimen neutralized','יצור המחקר חוסל'),
'checkpoint_specimen_key':('Gate key recovered','מפתח השער נאסף'),
'checkpoint_gate_unlocked':('Cabin gate lock released','מנעול השער בבקתה שוחרר'),
'power_restored':('Circuit powered. The cabin gate key lock is separate.','החשמל פועל. מנעול המפתח של השער נפרד מלוח הנתיך.'),
'circuit_alive':('Power restored. Recover the research key and turn the lock beside this panel.','החשמל חזר. אסוף את המפתח מתחנת המחקר וסובב את המנעול שליד הלוח.'),
'note_research':('STATION 03 / R-09\n\nThe archive holds eyes, embryos and animal heads in preservative. R-09 remains in the blue tank. The gate key hangs from the chain around its neck. Do not reach for it while the specimen is alive.\n\nThe red release control starts a five-second siren and drain. The doors then open. It does not fear the hand light. Four firearm hits or axe strikes stop it. Two strikes from it within five seconds are fatal. After death, recover the flashing key from the neck chain.\n\nRestore the fuse circuit, then turn the recovered key in the separate lock beside the starting cabin panel. The weak station bulbs offer no shelter.','תחנה 03 / דגימה R-09\n\nבארכיון שמורות עיניים, עוברים וראשי חיות בנוזל שימור. R-09 נמצא במיכל הכחול. מפתח השער תלוי בשרשרת שעל צווארו. אין לנסות לקחת אותו כשהיצור חי.\n\nהכפתור האדום מפעיל סירנה וניקוז במשך חמש שניות. אחר כך הדלתות נפתחות. היצור אינו מפחד מהפנס. ארבע פגיעות רובה או גרזן יעצרו אותו. שתי פגיעות שלו בתוך חמש שניות הן קטלניות. לאחר מותו, אספו את המפתח המהבהב מהשרשרת.\n\nהחזירו את החשמל עם הנתיך, ואז סובבו את המפתח במנעול הנפרד שליד הלוח בבקתה הראשונה. נורות התחנה החלשות אינן מחסה.')
}
for index,language in enumerate(['English','Hebrew']):
    path=root/(language+'.json');table=json.loads(path.read_text(encoding='utf-8'));lookup={e['id']:e for e in table['entries']}
    for key,pair in entries.items():
        if key in lookup:lookup[key]['value']=pair[index]
        else:table['entries'].append({'id':key,'value':pair[index]})
    path.write_text(json.dumps(table,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print('Research and gate lock strings synchronized in English and Hebrew.')
