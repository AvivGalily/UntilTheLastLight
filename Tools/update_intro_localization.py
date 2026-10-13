"""Idempotent opening subtitles; Hebrew reference text and natural English."""
import json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
he=["תגידו, רק אני עדיין לא מאמין כמה הם משלמים לנו?","אם אתה מרגיש לא נעים, אני מוכן לקחת את החלק שלך.","חלום שלך. עבודה של לילה, משכורת של חודש. מה כבר יכול להשתבש?","מוזר לכם שהם לא אמרו בכלל מה אנחנו אמורים לצוד?","חיות בר. ככה היה כתוב.","גם 'לא להגיע עם טלפונים' היה כתוב. זה לא מפריע לכם?","הם שילמו חצי מראש. מבחינתי שיבקשו גם שלא נביא מכנסיים.","תסתכלו על השער הזה... מאז מתי צריך גדר כזאת בשביל חזירי בר?","אולי החזירים פה ממש לא אוהבים אורחים.","רגע... אתם שומעים את זה?","זה... זה לא נשמע כמו חיה.","סע! סע, עכשיו!"]
en=["Am I the only one who still can't believe what they're paying us?","If you feel guilty, I'll happily take your cut.","Not a chance. One night's work, a month's pay. What could go wrong?","Doesn't it bother you they never said what we're hunting?","Wildlife. That's what the contract said.","It also said no phones. You're all fine with that?","They paid half up front. For that money, I'd leave my pants at home.","Look at that gate... Since when do you need a fence like that for boars?","Maybe the boars around here hate visitors.","Wait... do you hear that?","That's... that's not an animal.","Drive! Drive, now!"]
for lang,lines,extras in [
 ('Hebrew',he,{'intro_jack':"ג'ק",'intro_tyler':'טיילר','intro_ethan':'איתן','intro_steer':'A / D או ← / → — היגוי  |  בקר: ציר שמאל','intro_skip':'החזק רווח / A בבקר במשך 2 שניות לדילוג','intro_paused':'הנסיעה מושהית. השמירה תתבצע לאחר ההתעוררות בבקתה.','intro_subtitles':'כתוביות בפתיחה: {0}','intro_flash':'הבזק בפתיחה: {0}'}),
 ('English',en,{'intro_jack':'Jack','intro_tyler':'Tyler','intro_ethan':'Ethan','intro_steer':'A / D or ← / → — Steer  |  Controller: left stick','intro_skip':'Hold SPACE / controller A for 2 seconds to skip','intro_paused':'Drive paused. A checkpoint is saved after waking in the cabin.','intro_subtitles':'Opening subtitles: {0}','intro_flash':'Opening flash: {0}'})]:
    path=ROOT/'Assets/_Game/Data'/f'{lang}.json'
    table=json.loads(path.read_text(encoding='utf-8-sig'));values={e['id']:e['value'] for e in table['entries']}
    extras['intro_steer']='Mouse — Look  |  A/D / arrows — Steer  |  W — Accelerate  |  Controller: sticks + RT' if lang=='English' else 'עכבר — מבט  |  A/D / חצים — היגוי  |  W — האצה  |  בקר: צירים + RT'
    values.update({f'intro_L{n+1:02}':text for n,text in enumerate(lines)});values.update(extras)
    table['entries']=[{'id':key,'value':value} for key,value in values.items()]
    path.write_text(json.dumps(table,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print('Opening: 12 timed lines, speaker names, steering/skip/pause/accessibility in both existing tables.')
