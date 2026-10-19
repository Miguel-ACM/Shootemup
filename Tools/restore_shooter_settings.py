import pathlib
import re

root=pathlib.Path(__file__).resolve().parents[1]
backup=root/'MigrationBackup/BeforeIndustrialExpansion/Resources/Enemy'
current=root/'Assets/Resources/Enemy'
pattern=re.compile(r'(?m)^--- !u!114 &(-?\d+)\n.*?(?=^--- !u!|\Z)',re.S)
guids=('6c6231a2e9cfada459de06e919d06c32','246c6e9e082edbe45bb7c238ca0acff6')
for original in sorted(backup.rglob('*.prefab')):
    old=original.read_text(encoding='utf-8-sig')
    replacements={m[1]:m[0] for m in pattern.finditer(old) if any(guid in m[0] for guid in guids)}
    target=current/original.relative_to(backup)
    text=target.read_text(encoding='utf-8-sig')
    changes=[]
    def restore(match):
        if match[1] in replacements:
            changes.append(match[1])
            return replacements[match[1]]
        return match[0]
    updated=pattern.sub(restore,text)
    if updated!=text:
        target.write_text(updated,encoding='utf-8')
    print(str(original.relative_to(backup))+': '+str(len(changes))+' original shooter settings restored')
