"""Read-only project/GUID/path inventory; does not delete unused-looking assets.

Usage: python Tools/Maintenance/audit_project.py
Pair this with Unity's Last Light/Maintenance/Audit asset usage menu. Unity
reachability alone cannot establish use by Blender or string-based asset loads.
"""
from pathlib import Path
from collections import defaultdict
import argparse,ast,hashlib,json,re

ROOT=Path(__file__).resolve().parents[2]
parser=argparse.ArgumentParser()
parser.add_argument('--report',default='Logs/ProjectAudit.json')
args=parser.parse_args()
assert (ROOT/'ProjectSettings/ProjectVersion.txt').exists()
sizes=defaultdict(int);guids={};duplicates=[];missing_meta=[];syntax=[];paths=[];code_paths=[]
for base in ['Assets','ArtSource','Tools','Docs']:
    for path in (ROOT/base).rglob('*'):
        if not path.is_file():continue
        rel=path.relative_to(ROOT).as_posix();sizes[base]+=path.stat().st_size
        if base=='Assets' and path.suffix!='.meta' and not Path(str(path)+'.meta').exists():missing_meta.append(rel)
        if path.suffix=='.meta':
            match=re.search(r'(?m)^guid: ([0-9a-f]{32})$',path.read_text(encoding='utf-8-sig'))
            if match:
                if match[1] in guids:duplicates.append([guids[match[1]],rel])
                guids[match[1]]=rel
        if base=='Tools' and path.suffix=='.py':
            try:ast.parse(path.read_text(encoding='utf-8-sig'))
            except SyntaxError as error:syntax.append({'file':rel,'error':str(error)})
        if base in ['Tools','Docs'] and path.suffix in ['.cs','.py','.ps1','.md','.json']:
            text=path.read_text(encoding='utf-8-sig')
            if path.suffix=='.json':
                value=json.loads(text)
                if isinstance(value,dict) and isinstance(value.get('moves'),list):
                    # Migration history records deleted old paths intentionally.
                    # Its destinations, and the rest of the receipt, must exist.
                    history=value.pop('moves');text=json.dumps(value)+'\n'+'\n'.join(item['to'] for item in history)
            for candidate in re.findall(r'Tools/[A-Za-z0-9_./-]+\.(?:py|ps1|txt)',text):
                if not (ROOT/candidate).is_file():paths.append({'from':rel,'missing':candidate})
            for candidate in re.findall(r'Assets/_Game/[A-Za-z0-9_./-]+\.cs',text):
                if not (ROOT/candidate).is_file():code_paths.append({'from':rel,'missing':candidate})
report={'scope':'source folders; Library, Logs, Builds and Git history excluded','bytes':dict(sizes),'duplicateGuids':duplicates,'missingMeta':missing_meta,'pythonSyntaxErrors':syntax,'missingToolPaths':paths,'missingCodePaths':code_paths}
output=(ROOT/args.report).resolve();assert output.is_relative_to(ROOT);output.parent.mkdir(parents=True,exist_ok=True);output.write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
print(json.dumps(report,indent=2))
raise SystemExit(bool(duplicates or missing_meta or syntax or paths or code_paths))
