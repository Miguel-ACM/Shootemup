import pathlib
import re
import difflib

root=pathlib.Path(__file__).resolve().parents[1]
backup=root/'MigrationBackup/BeforeIndustrialExpansion/Resources/Enemy'
current=root/'Assets/Resources/Enemy'

def gameplay_blocks(path):
    blocks={}
    for block in re.split(r'--- !u!',path.read_text(encoding='utf-8-sig'))[1:]:
        match=re.match(r'(\d+) &(-?\d+)',block)
        if match and match[1] in ('114','50','61','58','60'):
            lines=[line.rstrip() for line in block.splitlines() if not line.lstrip().startswith('m_EditorClassIdentifier:')]
            if match[1]!='114':
                keys=('m_Enabled:','m_Density:','m_IsTrigger:','m_Offset:','m_Size:','m_Radius:','m_EdgeRadius:','m_BodyType:','m_Mass:','m_GravityScale:','m_Constraints:','m_CollisionDetection:','m_LinearDamping:','m_AngularDamping:')
                lines=[line.replace('m_LinearDrag:','m_LinearDamping:').replace('m_AngularDrag:','m_AngularDamping:') for line in lines]
                lines=[line for line in lines if any(line.startswith('  '+key) for key in keys)]
            lines=[re.sub(r'^(\s+(?:minmax\w+|- arr): [0-9a-f]+)i$',r'\1',line) for line in lines]
            blocks[match[2]]='\n'.join(lines)
    return blocks

for original in sorted(backup.rglob('*.prefab')):
    path=current/original.relative_to(backup)
    before,after=gameplay_blocks(original),gameplay_blocks(path)
    changes=[]
    for key,value in before.items():
        if value!=after.get(key):
            changes.extend(difflib.unified_diff(value.splitlines(),after.get(key,'MISSING').splitlines(),lineterm=''))
    print(str(original.relative_to(backup))+(': gameplay components unchanged' if not changes else ': differences\n'+'\n'.join(changes)))
