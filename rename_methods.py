from pathlib import Path
import re

root = Path('MenuOptics.API/Controllers')
pattern = re.compile(r'public\s+async\s+Task<IActionResult>\s+([A-Za-z0-9_]+)\s*\(')

for path in root.rglob('*Controller.cs'):
    text = path.read_text(encoding='utf-8')

    def repl(match):
        name = match.group(1)
        if name.endswith('Async'):
            return match.group(0)
        return match.group(0).replace(name, name + 'Async', 1)

    new_text = pattern.sub(repl, text)
    if new_text != text:
        path.write_text(new_text, encoding='utf-8')
        print(path)
