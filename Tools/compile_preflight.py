"""Compile current C# with Unity's cached reference lists, outside its asset cache.

This checks compilation only. It does not replace Unity import, tests or a player build.
"""
from pathlib import Path
import subprocess,re,json
root=Path(__file__).resolve().parents[1]
editor=Path('C:/dev/UnityEditors/6000.3.24f1/Editor/Data')
output=root/'Logs/CompilePreflight';output.mkdir(parents=True,exist_ok=True)
assemblies={'Game.Runtime':'Assets/_Game/Scripts','Game.Editor':'Assets/_Game/Editor','Game.EditModeTests':'Assets/_Game/Tests/EditMode','Game.PlayModeTests':'Assets/_Game/Tests/PlayMode'}
reports=[]
for name,source in assemblies.items():
    cached=root/'Library/Bee/artifacts/1900b0aE.dag'/(name+'.rsp')
    lines=[]
    for line in cached.read_text(encoding='utf-8-sig').splitlines():
        if line.startswith(('-out:','-refout:')):continue
        if line.strip('"').endswith('.cs') and not line.startswith(('-','/')):continue
        for earlier in reports:
            if line.startswith(('-reference:','/reference:','-r:','/r:')) and earlier['assembly']+'.' in line:
                line='-reference:"'+str(output/(earlier['assembly']+'.dll'))+'"'
        lines.append(line)
    lines.append('-out:"'+str(output/(name+'.dll'))+'"')
    lines.extend('"'+str(p)+'"' for p in sorted((root/source).rglob('*.cs')))
    rsp=output/(name+'.rsp');rsp.write_text('\n'.join(lines),encoding='utf-8')
    result=subprocess.run([str(editor/'NetCoreRuntime/dotnet.exe'),str(editor/'DotNetSdkRoslyn/csc.dll'),'@'+str(rsp)],cwd=root,capture_output=True,text=True,encoding='utf-8')
    (output/(name+'.log')).write_text(result.stdout+result.stderr,encoding='utf-8')
    print(name,'OK' if result.returncode==0 else result.stdout+result.stderr,flush=True)
    reports.append({'assembly':name,'exitCode':result.returncode})
    if result.returncode:break
(output/'result.json').write_text(json.dumps({'scope':'compilation only; no Unity runtime tests','assemblies':reports},indent=2),encoding='utf-8')
raise SystemExit(reports[-1]['exitCode'])
