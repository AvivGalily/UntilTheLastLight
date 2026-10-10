import json
from pathlib import Path
root=Path(__file__).resolve().parents[1]/'Assets/_Game/Data'
updates={
'English':{
'intro':'Light drives it back while it is still distant.\nIf it reaches you, RUN.\n\nFollow the ranger’s trail. Keep a way back.\nListen before you look.',
'controls':'WASD Move · Shift Run · F Light · Right mouse Boost · Left mouse Axe · E Take / Read · M Map · Esc Pause',
'attack':'TOO CLOSE · RUN TO SHELTER',
'dead_body':'The Walker kills at close range. Your beam cannot repel it within 2.8 metres.\n\nThe axe can kill undead wolves in two strikes. A second wolf bite is fatal.\n\nOnly marked amber shelters offer safety.',
'new_hint':'Find the surveyor at the first bend. Take the axe beside the body and search for the map.',
'take_axe':'[E] Take field axe',
'axe_acquired':'Field axe acquired. Left mouse: swing. Two hits kill an undead wolf. Keep the Walker at a distance.',
'axe_control':'AXE · LEFT MOUSE',
'note_guard_rules':'Keep your distance. The beam repels the Walker only beyond 2.8 metres. When it is already beside you, run. Its attack is fatal.\n\nDry clicks are its footsteps: when they stop, it may be standing nearby. It returns from the trees.\n\nThe surveyor left an axe beside the first body. Use it against the diseased wolves: two strikes, with room to swing.\n\nMarked amber shelters are safe. The research station’s weak bulb is not.',
'note_cartographer':'The map is beside my coat. It marks the cabin, fuse storehouse, research station, old lantern, northern gate and the bodies we found. Distances are estimates.\n\nTake my axe. The wolves no longer behave like animals. The blade will stop them; the Walker is something else.\n\nDo not trust the research station’s dim lamp. It is too weak.'},
'Hebrew':{
'intro':'האור מרחיק אותה כל עוד היא רחוקה.\nאם היא כבר לידך — רוץ.\n\nעקוב אחרי שביל השומר. השאר דרך חזרה.\nהקשב לפני שתסתכל.',
'controls':'WASD תנועה · Shift ריצה · F פנס · עכבר ימני הגברה · עכבר שמאלי גרזן · E איסוף / קריאה · M מפה · Esc השהיה',
'attack':'היא קרובה מדי · רוץ למחסה',
'dead_body':'תקיפה מקרוב של המפלצת היא קטלנית. בטווח של פחות מ־2.8 מטרים הפנס לא מרחיק אותה.\n\nשתי מכות גרזן הורגות זאב נגוע. נשיכה שנייה של זאב היא קטלנית.\n\nרק מחסות המסומנים באור ענברי מגינים עליך.',
'new_hint':'מצא את המודד ליד העיקול הראשון. קח את הגרזן לצד הגופה וחפש את המפה.',
'take_axe':'[E] קח גרזן',
'axe_acquired':'הגרזן נאסף. לחצן עכבר שמאלי: מכה. שתי פגיעות הורגות זאב נגוע. מהמפלצת יש לשמור מרחק.',
'axe_control':'גרזן · לחצן עכבר שמאלי',
'note_guard_rules':'שמור מרחק. הפנס מרחיק את המפלצת רק מעבר ל־2.8 מטרים. כשהיא כבר לידך, רוץ. התקיפה שלה קטלנית.\n\nהנקישות היבשות הן הצעדים שלה: כשהן נפסקות, ייתכן שהיא עומדת לידך. היא חוזרת מתוך העצים.\n\nהמודד השאיר גרזן לצד הגופה הראשונה. השתמש בו נגד הזאבים הנגועים: שתי פגיעות, עם מרווח להנפה.\n\nמחסות המסומנים באור ענברי בטוחים. הנורה החלשה בתחנת המחקר אינה מחסה.',
'note_cartographer':'המפה לצד המעיל שלי. סימנתי את הבקתה, מחסן הנתיך, תחנת המחקר, הפנס הישן, השער הצפוני והגופות שמצאנו. המרחקים משוערים.\n\nקח את הגרזן שלי. הזאבים כבר לא מתנהגים כחיות. הלהב יעצור אותם; המפלצת היא משהו אחר.\n\nאל תסמוך על האור העמום בתחנת המחקר. הוא חלש מדי.'}}
for language,changes in updates.items():
    path=root/(language+'.json');doc=json.loads(path.read_text(encoding='utf-8-sig'));entries={e['id']:e for e in doc['entries']}
    for key,value in changes.items():
        if key in entries:entries[key]['value']=value
        else:doc['entries'].append({'id':key,'value':value})
    path.write_text(json.dumps(doc,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
