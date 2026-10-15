"""Extract Unity's official TMP essentials with their original .meta files.
Useful for unattended first import: ImportPackage schedules work asynchronously.
"""
import argparse
import tarfile
from pathlib import Path

parser = argparse.ArgumentParser()
parser.add_argument('--package', required=True, type=Path)
parser.add_argument('--project', required=True, type=Path)
args = parser.parse_args()
root = args.project.resolve()
with tarfile.open(args.package) as archive:
    names = set(archive.getnames())
    count = 0
    for member in archive.getmembers():
        if not member.name.endswith('/pathname'):
            continue
        relative = archive.extractfile(member).read().decode('utf-8').strip()
        if not relative.startswith('Assets/TextMesh Pro/') and relative != 'Assets/TextMesh Pro':
            raise ValueError('Unexpected package path: ' + relative)
        target = (root / relative).resolve()
        if not target.is_relative_to(root):
            raise ValueError('Path escaped project')
        prefix = member.name.rsplit('/', 1)[0]
        asset = prefix + '/asset'
        meta = prefix + '/asset.meta'
        if asset in names:
            target.parent.mkdir(parents=True, exist_ok=True)
            if not target.exists():
                target.write_bytes(archive.extractfile(asset).read())
                count += 1
        else:
            target.mkdir(parents=True, exist_ok=True)
        meta_target = Path(str(target) + '.meta')
        if meta in names and not meta_target.exists():
            meta_target.write_bytes(archive.extractfile(meta).read())
print(f'Prepared {count} TMP resource files, retaining official asset IDs.')
