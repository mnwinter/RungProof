"""Read/hash the native-created private payload from Python's filesystem view."""
import argparse
import hashlib
import json
from datetime import datetime, timezone
from pathlib import Path


def sha(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


parser = argparse.ArgumentParser()
parser.add_argument('--receipt', type=Path, required=True)
parser.add_argument('--summary', type=Path, required=True)
args = parser.parse_args()
if args.summary.exists():
    raise SystemExit('Use a new summary path; prior proof is preserved')
receipt = json.loads(args.receipt.read_text(encoding='utf-8-sig'))
if receipt['result'] != 'PASS':
    raise SystemExit('Native source/copy proof did not pass')
expected = {row['relative']: row for row in receipt['source_before']['files']}
if len(expected) != len(receipt['source_before']['files']) or not expected:
    raise SystemExit('Native source pathset is empty or duplicated')
payload = args.receipt.parent / receipt['payload_relative']
actual = {}
for path in payload.rglob('*'):
    if path.is_symlink():
        raise SystemExit('Unexpected staged reparse/symlink')
    if path.is_file():
        relative = path.relative_to(payload).as_posix()
        actual[relative] = {'relative': relative, 'bytes': path.stat().st_size, 'sha256': sha(path)}
if set(actual) != set(expected):
    raise SystemExit('Packaged Python cannot see the full native staged pathset')
if any(actual[key]['bytes'] != expected[key]['bytes'] or actual[key]['sha256'] != expected[key]['sha256'] for key in expected):
    raise SystemExit('Packaged Python staged size/hash mismatch')
manifest = [actual[key] for key in sorted(actual)]
summary = {
    'result': 'PASS',
    'created_utc': datetime.now(timezone.utc).isoformat(),
    'native_files': len(expected),
    'packaged_python_staged_files': len(actual),
    'bytes': sum(row['bytes'] for row in manifest),
    'native_receipt_sha256': sha(args.receipt),
    'canonical_path_size_hash_manifest_sha256': hashlib.sha256(json.dumps(manifest, sort_keys=True, separators=(',', ':')).encode()).hexdigest(),
    'private_staging_child': args.receipt.parent.name,
    'checks': {'pathset_exact': True, 'sizes_exact': True, 'sha256_exact': True},
    'boundary': 'Native source -> fresh private C staging -> independent packaged-Python readback. Final immutable D copy/hash verification remains required; no public userdata publication or app acceptance.'
}
with args.summary.open('x', encoding='utf-8') as output:
    json.dump(summary, output, indent=2)
    output.write('\n')
print(json.dumps(summary))
