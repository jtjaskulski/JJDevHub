#!/usr/bin/env bash
set -euo pipefail

mode=""
root=""
title=""
base_ref=""
head_ref=""
paths=()
saw_paths=0

usage() {
  echo "$1" >&2
  exit 2
}

fail() {
  echo "$1" >&2
  exit 1
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --check)
      if [[ -n $mode ]]; then
        usage "jeden tryb"
      fi
      mode=check
      shift
      ;;
    --candidates)
      if [[ -n $mode ]]; then
        usage "jeden tryb"
      fi
      mode=candidates
      shift
      ;;
    --root)
      if [[ $# -lt 2 || $2 == --* ]]; then
        usage "brak --root"
      fi
      root=$2
      shift 2
      ;;
    --title)
      if [[ $# -lt 2 || $2 == --* ]]; then
        usage "brak --title"
      fi
      title=$2
      shift 2
      ;;
    --base-ref)
      if [[ $# -lt 2 || $2 == --* ]]; then
        usage "brak --base-ref"
      fi
      base_ref=$2
      shift 2
      ;;
    --head-ref)
      if [[ $# -lt 2 || $2 == --* ]]; then
        usage "brak --head-ref"
      fi
      head_ref=$2
      shift 2
      ;;
    --paths)
      if [[ $# -lt 2 || $2 == --* ]]; then
        usage "brak --paths"
      fi
      paths+=("$2")
      saw_paths=1
      shift 2
      ;;
    *)
      usage "nieznany argument: $1"
      ;;
  esac
done

if [[ -z $mode ]]; then
  usage "brak --check albo --candidates"
fi

if [[ -z $root ]]; then
  script_dir=$(CDPATH='' cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)
  root=$(CDPATH='' cd -- "${script_dir}/../.." && pwd)
else
  script_dir=$(CDPATH='' cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)
fi

if [[ ! -d $root ]]; then
  usage "brak katalogu: ${root}"
fi

cd -- "$root"

if [[ $saw_paths -eq 0 && -n $base_ref ]]; then
  if [[ -z $head_ref ]]; then
    usage "brak --head-ref"
  fi
  if ! git rev-parse --is-inside-work-tree >/dev/null 2>&1; then
    fail "brak repozytorium gita"
  fi
  if ! git rev-parse --verify -q "${base_ref}^{commit}" >/dev/null; then
    fail "brak ${base_ref}"
  fi
  if ! git rev-parse --verify -q "${head_ref}^{commit}" >/dev/null; then
    fail "brak ${head_ref}"
  fi
  while IFS= read -r line; do
    [[ -z $line ]] && continue
    paths+=("$line")
  done < <(git diff --name-only "${base_ref}...${head_ref}")
fi

path_file=$(mktemp)
trap 'rm -f "$path_file"' EXIT
if [[ ${#paths[@]} -gt 0 ]]; then
  printf '%s\n' "${paths[@]}" > "$path_file"
else
  : > "$path_file"
fi

set +e
python3 - "$root" "$mode" "$path_file" "$title" <<'PY'
import json
import pathlib
import re
import sys

root = pathlib.Path(sys.argv[1])
mode = sys.argv[2]
path_file = pathlib.Path(sys.argv[3])
title = sys.argv[4]

dictionary = (
    "identity",
    "news",
    "learning",
    "cv",
    "contact",
    "gateway",
    "web",
    "mobile",
)
paths_by_name = {
    "identity": "src/services/Identity",
    "news": "src/services/News",
    "learning": "src/services/Learning",
    "cv": "src/services/Cv",
    "contact": "src/services/Contact",
    "gateway": "src/gateway",
    "web": "src/clients/web",
    "mobile": "src/clients/mobile",
}
semver_re = re.compile(r"^[0-9]+\.[0-9]+\.[0-9]+$")
title_re = re.compile(
    r"^(feat|fix|docs|chore|ci|refactor|test|perf|build)"
    r"(\(([^)]+)\))?(!)?: (.+)$"
)
kernel_root = "src/shared/JJDevHub.Shared.Kernel"
config_path = root / ".github" / "release-please-config.json"
manifest_path = root / ".release-please-manifest.json"


def die(message):
    print(message, file=sys.stderr)
    raise SystemExit(1)


def load_json(path):
    try:
        return json.loads(path.read_text(encoding="utf-8"))
    except (OSError, UnicodeError, json.JSONDecodeError):
        die(f"zły JSON {path.relative_to(root)}")


def contains_linked(value):
    if isinstance(value, dict):
        for key, item in value.items():
            if key == "linked-versions" or contains_linked(item):
                return True
        return False
    if isinstance(value, list):
        return any(contains_linked(item) for item in value)
    if isinstance(value, str):
        return "linked-versions" in value
    return False


def normalize(path):
    path = path.replace("\\", "/").rstrip("\r")
    if path.startswith("./"):
        path = path[2:]
    return path


def hits(path, root_path):
    return path == root_path or path.startswith(root_path + "/")


changed = [normalize(line) for line in path_file.read_text(encoding="utf-8").splitlines() if line.strip()]

if not config_path.is_file():
    die("brak .github/release-please-config.json")
config = load_json(config_path)
if contains_linked(config):
    die("linked-versions nie wchodzi")
if config.get("bump-minor-pre-major") is not True:
    die("brak bump-minor-pre-major")
if config.get("bump-patch-for-minor-pre-major") is True:
    die("feat przed 1.0.0 zostaje minorem")
if config.get("separate-pull-requests") is not True:
    die("brak osobnych pull requestów")
if config.get("include-component-in-tag") is not True or config.get("include-v-in-tag") is not True:
    die("tag bez prefiksu komponentu")
if config.get("tag-separator") != "-":
    die("zły separator tagu")
if config.get("initial-version") != "0.1.0":
    die("pierwsze wydanie nie jest 0.1.0")
if config.get("component-no-space") is not True:
    die("tytuł wydania ma spację w zakresie")
if config.get("pull-request-title-pattern") != "chore(${component}): release ${version}":
    die("tytuł wydania nie przejdzie pr-title")

packages = config.get("packages")
if not isinstance(packages, dict):
    die("zły JSON .github/release-please-config.json")

if set(packages) != set(paths_by_name.values()):
    die("manifest nie ma ośmiu ścieżek")
for name, package_path in paths_by_name.items():
    meta = packages[package_path]
    if not isinstance(meta, dict) or meta.get("component") != name:
        die(f"komponent nie zgadza się ze ścieżką: {name}")
    extra = meta.get("extra-files")
    expected = f"/versions/{name}.json"
    if not isinstance(extra, list) or not any(
        isinstance(item, dict) and item.get("path") == expected and item.get("jsonpath") == "$.version"
        for item in extra
    ):
        die(f"brak pliku wersji w manifeście: {name}")

if kernel_root in packages or any(hits(kernel_root, key) or hits(key, kernel_root) for key in packages):
    die("jądro nie jest komponentem")

if not manifest_path.is_file():
    die("brak .release-please-manifest.json")
manifest = load_json(manifest_path)
if not isinstance(manifest, dict) or set(manifest) != set(paths_by_name.values()):
    die("zły JSON .release-please-manifest.json")

versions = {}
for name, package_path in paths_by_name.items():
    file_path = root / "versions" / f"{name}.json"
    if not file_path.is_file():
        die(f"brak versions/{name}.json")
    payload = load_json(file_path)
    if not isinstance(payload, dict) or "version" not in payload:
        die(f"zły JSON versions/{name}.json")
    version = payload["version"]
    if not isinstance(version, str) or semver_re.fullmatch(version) is None:
        die(f"wersja nie pasuje: {name} {version}")
    manifest_version = manifest[package_path]
    if not isinstance(manifest_version, str) or semver_re.fullmatch(manifest_version) is None:
        die(f"wersja nie pasuje: {name} {manifest_version}")
    if manifest_version != version:
        die(f"manifest nie zgadza się z plikiem: {name}")
    versions[name] = version

hit = []
for name, package_path in paths_by_name.items():
    if any(hits(path, package_path) for path in changed):
        hit.append(name)

if mode == "candidates":
    for name in dictionary:
        if name in hit:
            print(name)
    raise SystemExit(0)

kernel = any(hits(path, kernel_root) for path in changed)

if title:
    if "\n" in title or "\r" in title or title_re.fullmatch(title) is None:
        die("tytuł nie jest Conventional Commits")
    match = title_re.fullmatch(title)
    kind = match.group(1)
    scope = match.group(3) or ""
    known = set(dictionary) | {"infra", "shared"}
    if scope and scope not in known:
        die(f"zakres spoza listy: {scope}")
    if kind in {"feat", "fix"} and not scope:
        die("feat i fix wymagają zakresu")
    if kind in {"feat", "fix"} and scope in dictionary:
        if scope not in hit:
            die(f"release-please nie podbiłby: {scope}")
    if kind in {"feat", "fix"} and scope == "shared" and not kernel:
        die("shared bez jądra")

if kernel:
    raise SystemExit(10)
raise SystemExit(0)
PY
status=$?
set -e
if [[ $status -eq 10 ]]; then
  bump_args=(--root "$root")
  if [[ ${#paths[@]} -gt 0 ]]; then
    for path in "${paths[@]}"; do
      bump_args+=(--paths "$path")
    done
  fi
  bash "${script_dir}/bump-shared.sh" "${bump_args[@]}"
  exit 0
fi
exit "$status"
