"""Install the revised continuous-light and close-contact player instructions."""
import json
from pathlib import Path
root=Path(__file__).resolve().parents[2]
translations={
    'English':{
        'intro':'Keep the light on it for 4-7 continuous seconds. It keeps coming until then.\nIf it reaches you, RUN.\n\nFollow the ranger’s trail. Keep a way back.\nListen before you look.',
        'dead_body':'The Walker kills after 3 continuous seconds closer than 1.5 metres, even while you walk. Run to break contact.\n\nKeep the beam on it for 4-7 continuous seconds to drive it back. It keeps approaching during that time.\n\nThe axe kills undead wolves in two strikes. Only marked amber shelters offer safety.',
        'note_guard_rules':'Keep your distance. Hold the beam on the Walker for 4-7 uninterrupted seconds. It continues approaching until the light drives it away. Losing the beam resets the exposure. Inside 1.5 metres, run: the beam cannot repel it.\n\nIf it stays closer than 1.5 metres for 3 continuous seconds, you die, even while walking. It moves faster than a walk but slower than a sprint.\n\nDry clicks are its footsteps. The surveyor left an axe beside the first body; two strikes kill a diseased wolf.\n\nMarked amber shelters are safe. The research station’s bulbs and blue containment light are not.'},
    'Hebrew':{
        'intro':'כוון אליה את הפנס במשך 4–7 שניות רצופות. עד אז היא ממשיכה להתקרב.\nאם היא מגיעה אליך — רוץ.\n\nעקוב אחרי שביל השומר. השאר דרך חזרה.\nהקשב לפני שתסתכל.',
        'dead_body':'אם המפלצת נמצאת במרחק קטן מ־1.5 מטר במשך 3 שניות רצופות, תמות גם בזמן הליכה. רוץ כדי להתרחק.\n\nהאר עליה במשך 4–7 שניות רצופות כדי להרחיק אותה. בזמן הזה היא ממשיכה להתקרב.\n\nשתי מכות גרזן הורגות זאב נגוע. רק מחסות המסומנים באור ענברי בטוחים.',
        'note_guard_rules':'שמור מרחק. כוון את הפנס אל המפלצת במשך 4–7 שניות רצופות. היא ממשיכה להתקדם עד שהאור מרחיק אותה. הפסקת ההארה מאפסת את הספירה. בטווח קטן מ־1.5 מטר הפנס לא מרחיק אותה — רוץ.\n\nאם היא נשארת במרחק קטן מ־1.5 מטר במשך 3 שניות רצופות, תמות גם בזמן הליכה. היא מהירה מהליכה ואיטית מריצה.\n\nהנקישות היבשות הן צעדיה. המודד השאיר גרזן לצד הגופה הראשונה; שתי מכות הורגות זאב נגוע.\n\nמחסות המסומנים באור ענברי בטוחים. הנורות בתחנת המחקר והאור הכחול במיכל אינם מחסה.'}}
for language,values in translations.items():
    path=root/'Assets/_Game/Data'/(language+'.json')
    data=json.loads(path.read_text(encoding='utf-8'))
    for entry in data['entries']:
        if entry['id'] in values:entry['value']=values[entry['id']]
    path.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print('Research/Walker instructions updated in English and Hebrew.')
