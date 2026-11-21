#!/usr/bin/env bash
set -euo pipefail

root=$(cd "$(dirname "$0")/../.." && pwd)
tags_sh=$root/.github/scripts/release-tags.sh
bump_sh=$root/.github/scripts/bump-shared.sh
title_sh=$root/.github/scripts/pr-title.sh

expect_code() {
  local expected=$1
  shift
  set +e
  "$@"
  local code=$?
  set -e
  if [[ $code -ne $expected ]]; then
    echo "oczekiwano ${expected}, jest ${code}: $*" >&2
    exit 1
  fi
}

expect_out() {
  local expected=$1
  shift
  local actual
  actual=$("$@")
  if [[ $actual != "$expected" ]]; then
    echo "oczekiwano [${expected}], jest [${actual}]: $*" >&2
    exit 1
  fi
}

snapshot() {
  local dir=$1
  mkdir -p "$dir/.github" "$dir/versions"
  cp "$root/.github/release-please-config.json" "$dir/.github/release-please-config.json"
  cp "$root/.release-please-manifest.json" "$dir/.release-please-manifest.json"
  cp "$root"/versions/*.json "$dir/versions/"
}

before=$(git -C "$root" tag --list | sort)
stdout=$(bash "$tags_sh" --check)
after=$(git -C "$root" tag --list | sort)
if [[ $before != "$after" ]]; then
  echo "check utworzył tag" >&2
  exit 1
fi
if [[ -n $stdout ]]; then
  echo "check wypisał tag albo podbicie: ${stdout}" >&2
  exit 1
fi

expect_code 1 bash "$tags_sh" --check --title 'feat(cicd): x'
expect_code 1 bash "$tags_sh" --check --title 'feat: add lesson'
expect_code 0 bash "$tags_sh" --check --title 'feat(infra): add pipeline' --paths .github/workflows/versions.yml
expect_code 0 bash "$title_sh" 'chore(news): release 0.1.0'
expect_code 1 bash "$title_sh" 'chore(main): release news 0.1.0'
expect_out 'news' bash "$tags_sh" --candidates --paths src/services/News/Foo.cs
expect_out $'identity\nnews' bash "$tags_sh" --candidates --paths src/services/News/Foo.cs --paths src/services/Identity/A.cs
expect_out '' bash "$tags_sh" --candidates --paths src/shared/JJDevHub.Shared.Kernel/Result.cs --paths README.md
expect_code 0 bash "$tags_sh" --check --title 'feat(news): add lesson' --paths src/services/News/Foo.cs
expect_code 1 bash "$tags_sh" --check --title 'feat(news): add lesson' --paths docs/README.md
expect_code 1 bash "$tags_sh" --check --title 'feat(shared): bump kernel' --paths docs/README.md

shared_out=$(bash "$tags_sh" --check --title 'feat(shared): bump kernel' --paths src/shared/JJDevHub.Shared.Kernel/Result.cs)
if [[ $shared_out != 'nie ma kogo podbijać' ]]; then
  echo "oczekiwano pustej listy jądra, jest [${shared_out}]" >&2
  exit 1
fi

expect_out 'nie ma kogo podbijać' bash "$bump_sh" --paths src/shared/JJDevHub.Shared.Kernel/Result.cs
expect_out '' bash "$bump_sh" --paths src/services/News/Foo.cs

python3 - "$root/.github/workflows/versions.yml" <<'PY'
import pathlib
import sys

text = pathlib.Path(sys.argv[1]).read_text(encoding="utf-8")
job = text.index("  release-please:\n")
head = text[:job]
if "googleapis/release-please-action@" in head:
    raise SystemExit("release-please jest w jobie release-tags")
if "contents: write" in head:
    raise SystemExit("release-tags ma zapis")
if "git tag" in text or "push-tags" in text:
    raise SystemExit("workflow wycina tag sam")
tail = text[job:]
if "github.event_name == 'push' && github.ref == 'refs/heads/main'" not in tail:
    raise SystemExit("wydanie nie jest ograniczone do pusha na main")
if "googleapis/release-please-action@45996ed1f6d02564a971a2fa1b5860e934307cf7 # v5.0.0" not in tail:
    raise SystemExit("brak pinu release-please")
PY

python3 - <<'PY'
import re

pattern = re.compile(r"^chore\(?(?P<component>@?[\w\-./]*)?\): release v?(?P<version>[0-9].*)$")
match = pattern.fullmatch("chore(news): release 0.1.0")
if match is None or match.group("component") != "news" or match.group("version") != "0.1.0":
    raise SystemExit("tytuł wydania nie wraca do release-please")
PY

for name in identity news learning cv contact gateway web mobile; do
  python3 -c 'import json,sys; data=json.load(open(sys.argv[1])); raise SystemExit(0 if data.get("version")=="0.0.0" else 1)' "$root/versions/${name}.json"
done

python3 - "$root" <<'PY'
import json, pathlib, sys
root = pathlib.Path(sys.argv[1])
for name in ("develop", "release", "main"):
    data = json.loads((root / ".github" / "rulesets" / f"{name}.json").read_text(encoding="utf-8"))
    checks = []
    for rule in data["rules"]:
        if rule["type"] == "required_status_checks":
            checks = [item["context"] for item in rule["parameters"]["required_status_checks"]]
    if "release-tags" not in checks:
        raise SystemExit(f"brak release-tags w {name}")
config = json.loads((root / ".github" / "release-please-config.json").read_text(encoding="utf-8"))
if "linked-versions" in json.dumps(config):
    raise SystemExit("linked-versions w manifeście")
PY

dir=$(mktemp -d)
trap 'rm -rf "$dir"' EXIT
snapshot "$dir"
python3 - "$dir/versions/news.json" <<'PY'
import json, pathlib, sys
path = pathlib.Path(sys.argv[1])
path.write_text('{"version":"nope"}\n', encoding="utf-8")
PY
expect_code 1 bash "$tags_sh" --check --root "$dir"

snapshot "$dir"
rm "$dir/versions/learning.json"
expect_code 1 bash "$tags_sh" --check --root "$dir" --title 'feat(learning): add lesson'

snapshot "$dir"
python3 - "$dir/.github/release-please-config.json" <<'PY'
import json, pathlib, sys
path = pathlib.Path(sys.argv[1])
data = json.loads(path.read_text(encoding="utf-8"))
data["plugins"] = ["linked-versions"]
path.write_text(json.dumps(data), encoding="utf-8")
PY
expect_code 1 bash "$tags_sh" --check --root "$dir"

snapshot "$dir"
git -C "$dir" init -q -b main
git -C "$dir" config user.email "ci@example.com"
git -C "$dir" config user.name "ci"
git -C "$dir" config commit.gpgsign false
git -C "$dir" add .
git -C "$dir" commit -q -m base
base=$(git -C "$dir" rev-parse HEAD)
mkdir -p "$dir/src/services/News"
printf 'class A {}\n' > "$dir/src/services/News/A.cs"
git -C "$dir" add src/services/News/A.cs
git -C "$dir" commit -q -m 'feat(news): add lesson'
expect_code 0 bash "$tags_sh" --check --root "$dir" --title 'feat(news): add lesson' --base-ref "$base" --head-ref HEAD
expect_code 1 bash "$tags_sh" --check --root "$dir" --title 'feat(identity): add lesson' --base-ref "$base" --head-ref HEAD

dep=$(mktemp -d)
mkdir -p "$dep/src/services/News/JJDevHub.News.Api" "$dep/src/services/Identity/JJDevHub.Identity.Api" "$dep/src/shared/JJDevHub.Shared.Kernel"
cat > "$dep/src/services/News/JJDevHub.News.Api/JJDevHub.News.Api.csproj" <<'EOF'
<Project Sdk="Microsoft.NET.Sdk.Web">
  <ItemGroup>
    <ProjectReference Include="..\..\..\shared\JJDevHub.Shared.Kernel\JJDevHub.Shared.Kernel.csproj" />
  </ItemGroup>
</Project>
EOF
cat > "$dep/src/services/Identity/JJDevHub.Identity.Api/JJDevHub.Identity.Api.csproj" <<'EOF'
<Project Sdk="Microsoft.NET.Sdk.Web">
</Project>
EOF
expect_out 'news' bash "$bump_sh" --root "$dep" --paths src/shared/JJDevHub.Shared.Kernel/Result.cs
rm -rf "$dep"

echo "versions: ok"
