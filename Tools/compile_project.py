"""Compile current owned Unity scripts without editing IDE-generated project files."""
from pathlib import Path
import re
import subprocess

root = Path(__file__).resolve().parents[1]
out = root / "Temp/ProjectOrganizationAudit/Compile"
out.mkdir(parents=True, exist_ok=True)

for original, destination, editor in (
    ("Assembly-CSharp.csproj", "Runtime.csproj", False),
    ("Assembly-CSharp-Editor.csproj", "Editor.csproj", True),
):
    source = root / original
    if not source.is_file():
        raise SystemExit("Generate Unity IDE project files before compiling: " + original)
    text = source.read_text(encoding="utf-8-sig")
    text = re.sub(r'\s*<Compile Include="[^"]+"\s*/>', "", text)
    files = [p for p in (root / "Assets").rglob("*.cs")
             if ("Editor" in p.relative_to(root / "Assets").parts) == editor]
    includes = "\n  <ItemGroup>\n" + "".join(
        '    <Compile Include="' + str(p) + '" />\n' for p in sorted(files)) + "  </ItemGroup>\n"
    text = text.replace("</Project>", includes + "</Project>")
    def project_reference(match):
        name = match[2]
        path = out / "Runtime.csproj" if name == "Assembly-CSharp.csproj" else root / name
        return match[1] + str(path) + match[3]
    text = re.sub(r'(<ProjectReference Include=")([^"]+)(")', project_reference, text)
    def hint_path(match):
        path = Path(match[1])
        return "<HintPath>" + str(path if path.is_absolute() else root / path) + "</HintPath>"
    text = re.sub(r'<HintPath>([^<]+)</HintPath>', hint_path, text)
    (out / destination).write_text(text, encoding="utf-8")

raise SystemExit(subprocess.call(["dotnet", "build", str(out / "Editor.csproj"), "-v", "quiet"], cwd=root))
