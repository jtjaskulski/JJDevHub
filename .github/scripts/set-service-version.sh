#!/usr/bin/env bash
set -euo pipefail

usage() {
  echo "użycie: set-service-version.sh <plik> <tag>" >&2
  exit 2
}

if [[ $# -ne 2 ]]; then
  usage
fi

file=$1
value=$2
pattern='^(identity|news|learning|cv|contact|gateway|web|mobile)-v[0-9]+\.[0-9]+\.[0-9]+$'

if [[ ! $value =~ $pattern ]]; then
  echo "zły tag" >&2
  exit 1
fi

if [[ ! -f $file ]]; then
  echo "brak pliku env" >&2
  exit 1
fi

python3 - "$file" "$value" <<'PY'
import pathlib
import sys

path = pathlib.Path(sys.argv[1])
value = sys.argv[2]
lines = path.read_text(encoding="utf-8").splitlines()
found = False
out = []
for line in lines:
    key = line.strip()
    if key.startswith("SERVICE_VERSION=") or key.startswith("export SERVICE_VERSION="):
        out.append(f"SERVICE_VERSION={value}")
        found = True
        continue
    out.append(line)
if not found:
    out.append(f"SERVICE_VERSION={value}")
temporary = path.with_name(path.name + ".tmp")
temporary.write_text("\n".join(out) + "\n", encoding="utf-8")
temporary.chmod(0o600)
temporary.replace(path)
PY
chmod 600 "$file"
