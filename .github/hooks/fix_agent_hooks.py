#!/usr/bin/env python3
import json, shutil, time, os, sys
root = os.path.dirname(__file__)
logdir = os.path.join(root, 'logs')
path = os.path.join(logdir, 'agent-hooks.jsonl')
if not os.path.exists(path):
    print('Log file not found:', path, file=sys.stderr); sys.exit(1)
backup = path + '.bak.' + time.strftime('%Y%m%d%H%M%S')
shutil.copy2(path, backup)
lines = open(path, 'r', encoding='utf-8', errors='replace').read().splitlines()
out_lines = []
fixed_count = 0
removed_count = 0
for i, line in enumerate(lines, start=1):
    s = line.strip()
    if not s:
        continue
    try:
        obj = json.loads(s)
    except Exception:
        # skip lines that are literal text like 'Invalid JSON input'
        if 'Invalid JSON input' in s:
            removed_count += 1
            continue
        # keep unparsable line unchanged
        out_lines.append(line)
        continue
    payload = obj.get('payload')
    if isinstance(payload, dict) and 'parseError' in payload:
        raw = payload.get('raw')
        if isinstance(raw, str):
            # raw may already be a JSON string (unescaped by outer loads)
            try:
                parsed_raw = json.loads(raw)
                obj['payload'] = parsed_raw
                fixed_count += 1
            except Exception:
                # unable to parse inner raw; just remove parseError
                obj['payload'].pop('parseError', None)
        else:
            obj['payload'].pop('parseError', None)
    out_lines.append(json.dumps(obj, ensure_ascii=False))
# write back
with open(path, 'w', encoding='utf-8') as f:
    f.write('\n'.join(out_lines) + '\n')
print(f'Backup: {backup}')
print(f'Lines processed: {len(lines)}, fixed payloads: {fixed_count}, removed lines: {removed_count}')
