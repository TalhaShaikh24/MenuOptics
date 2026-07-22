from pathlib import Path
import re

root = Path('MenuOptics.API/Controllers')
pattern = re.compile(r'public\s+async\s+Task<IActionResult>\s+([A-Za-z0-9_]+)\s*\(')
service_call_pattern = re.compile(r'_\w+Service\.([A-Za-z0-9_]+)\s*\(')

for path in root.rglob('*.cs'):
    text = path.read_text(encoding='utf-8')
    matches = list(pattern.finditer(text))
    if not matches:
        continue

    new_text = text
    for m in reversed(matches):
        method_name = m.group(1)
        start = m.start()
        end = len(text)
        for next_m in matches:
            if next_m.start() > start:
                end = next_m.start()
                break
        body = text[start:end]
        service_call_match = service_call_pattern.search(body)
        if not service_call_match:
            continue
        target_name = service_call_match.group(1)
        if target_name == method_name or target_name == method_name + 'Async':
            continue
        if not target_name.endswith('Async'):
            continue
        new_text = new_text.replace(f'public async Task<IActionResult> {method_name}(', f'public async Task<IActionResult> {target_name}(', 1)

    if new_text != text:
        path.write_text(new_text, encoding='utf-8')
        print(path)
