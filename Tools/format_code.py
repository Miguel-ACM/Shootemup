import sys, glob, os, re, subprocess

# Formats the game's own C# code: run from the repository root with `python Tools/format_code.py`.
# 1. `dotnet format whitespace` applies .editorconfig (Allman braces, spacing, one statement per line).
# 2. A blank line is put between members that end with a closing brace, which the formatter leaves out.
# Third-party code (PathCreator, EZ Camera Shake) and the generated Inputs.cs are not touched.
DIRS = ["Assets/Scripts", "Assets/Editor", "Assets/Resources", "Assets/Tests"]
root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
dirs = [d for d in DIRS if os.path.isdir(os.path.join(root, d))]
# A throwaway project under Temp/ defines the editor symbols, so code inside #if UNITY_EDITOR
# is formatted too (formatting a bare folder treats those blocks as inactive text).
work = os.path.join(root, "Temp", "FormatCode")
os.makedirs(work, exist_ok=True)
items = "\n".join('    <Compile Include="../../%s/**/*.cs" />' % d for d in dirs)
with open(os.path.join(work, "FormatCode.csproj"), "w") as f:
    f.write("""<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.1</TargetFramework>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
    <DefineConstants>UNITY_EDITOR;UNITY_EDITOR_WIN;UNITY_STANDALONE;UNITY_64;ENABLE_INPUT_SYSTEM;UNITY_6000_0_OR_NEWER</DefineConstants>
  </PropertyGroup>
  <ItemGroup>
%s
  </ItemGroup>
</Project>
""" % items)
# A second pass settles lambda bodies that the first pass only partly re-indents.
for _ in range(2):
    subprocess.run(["dotnet", "format", "whitespace", os.path.join(work, "FormatCode.csproj"), "-v", "q"], check=True)
changed = 0
for d in dirs:
    for p in glob.glob(os.path.join(root, d, "**/*.cs"), recursive=True):
        raw = open(p, "rb").read()
        crlf = b"\r\n" in raw
        text = raw.decode("utf-8-sig") if raw.startswith(b"\xef\xbb\xbf") else raw.decode("utf-8")
        bom = raw.startswith(b"\xef\xbb\xbf")
        lines = text.replace("\r\n", "\n").split("\n")
        # Track which open braces belong to type declarations, to know member depth.
        out, stack = [], []  # stack of booleans: True if the brace opened a type body
        pending_type = False
        for i, line in enumerate(lines):
            s = line.strip()
            if out and stack and stack[-1] and s and s != "}" and not s.startswith("//") and not s.startswith("#") \
                    and out[-1].strip() == "}" and not s.startswith("else") and not s.startswith("catch") and not s.startswith("finally"):
                # previous line closed a member of the current type: separate the next member
                out.append("")
            out.append(line)
            if re.match(r"^(\[.*\]\s*)*((public|private|protected|internal|static|sealed|abstract|partial|readonly|new|unsafe)\s+)*(class|struct|interface|enum|record)\b", s):
                pending_type = True
            if s.startswith("//"):
                continue
            code = re.sub(r'"(\\.|[^"\\])*"', '""', re.sub(r"'(\\.|[^'\\])'", "''", s))
            code = code.split("//")[0]
            for ch in code:
                if ch == "{":
                    stack.append(pending_type)
                    pending_type = False
                elif ch == "}":
                    if stack:
                        stack.pop()
            if pending_type and code.endswith(";"):
                pending_type = False
        new = "\n".join(out)
        if new != text.replace("\r\n", "\n"):
            changed += 1
            data = new.replace("\n", "\r\n") if crlf else new
            open(p, "wb").write((b"\xef\xbb\xbf" if bom else b"") + data.encode("utf-8"))
print("files changed", changed)
