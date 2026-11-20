#!/usr/bin/env bash
set -euo pipefail

root=$(cd "$(dirname "$0")/../.." && pwd)

sarif=""
seen_sarif=0
baseline=""
seen_baseline=0
baseline_rules=()
baseline_rule=""
baseline_reason=""
baseline_have_rule=0

usage() {
  echo "$1" >&2
  exit 2
}

reject() {
  echo "$1" >&2
  exit 1
}

trim() {
  local value=$1
  value=${value#"${value%%[![:space:]]*}"}
  value=${value%"${value##*[![:space:]]}"}
  printf '%s' "$value"
}

unquote() {
  local value=$1
  if [[ $value == \"*\" && ${#value} -ge 2 ]]; then
    value=${value#\"}
    value=${value%\"}
  elif [[ $value == \'*\' && ${#value} -ge 2 ]]; then
    value=${value#\'}
    value=${value%\'}
  fi
  printf '%s' "$value"
}

flush_baseline() {
  if [[ $baseline_have_rule -eq 0 ]]; then
    return 0
  fi
  if [[ -z $baseline_reason ]]; then
    reject "baseline bez powodu: ${baseline_rule}"
  fi
  baseline_rules+=("$baseline_rule")
  baseline_have_rule=0
  baseline_rule=""
  baseline_reason=""
}

load_baseline() {
  local file=$1
  local line trimmed
  while IFS= read -r line || [[ -n $line ]]; do
    line=${line%$'\r'}
    trimmed=$(trim "$line")
    if [[ -z $trimmed || $trimmed == '#'* || $trimmed == '---' ]]; then
      continue
    fi
    if [[ $trimmed =~ ^-[[:space:]]*rule:[[:space:]]*(.*)$ ]]; then
      flush_baseline
      baseline_rule=$(unquote "$(trim "${BASH_REMATCH[1]}")")
      if [[ -z $baseline_rule ]]; then
        reject "baseline bez reguły"
      fi
      baseline_reason=""
      baseline_have_rule=1
    elif [[ $trimmed =~ ^reason:[[:space:]]*(.*)$ ]]; then
      if [[ $baseline_have_rule -eq 0 ]]; then
        reject "baseline bez reguły"
      fi
      baseline_reason=$(unquote "$(trim "${BASH_REMATCH[1]}")")
    else
      reject "baseline: nieznana linia"
    fi
  done <"$file"
  flush_baseline
}

is_baselined() {
  local id=$1
  local known
  if [[ ${#baseline_rules[@]} -eq 0 ]]; then
    return 1
  fi
  for known in "${baseline_rules[@]}"; do
    if [[ $known == "$id" ]]; then
      return 0
    fi
  done
  return 1
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --sarif)
      if [[ $# -lt 2 || $2 == --* ]]; then
        usage "brak --sarif"
      fi
      sarif=$2
      seen_sarif=1
      shift 2
      ;;
    --baseline)
      if [[ $# -lt 2 || $2 == --* ]]; then
        usage "brak --baseline"
      fi
      baseline=$2
      seen_baseline=1
      shift 2
      ;;
    *)
      usage "nieznany argument: $1"
      ;;
  esac
done

if [[ $seen_sarif -eq 0 || -z $sarif ]]; then
  usage "brak --sarif"
fi

if [[ ! -f $sarif ]]; then
  usage "brak pliku SARIF"
fi

if ! command -v jq >/dev/null 2>&1; then
  usage "brak jq"
fi

if ! jq empty "$sarif" >/dev/null 2>&1; then
  usage "zły JSON"
fi

if [[ $seen_baseline -eq 1 ]]; then
  if [[ ! -f $baseline ]]; then
    usage "brak pliku baseline"
  fi
  load_baseline "$baseline"
elif [[ -f $root/.github/codeql/baseline.yml ]]; then
  load_baseline "$root/.github/codeql/baseline.yml"
fi

rows=$(jq -r '
  def security_tag($props):
    (($props // {}).tags // []) | any(. == "security");
  def severity_of($props):
    ($props // {})["security-severity"];
  def rule_for($run; $result):
    [ $run.tool.driver.rules[]?, $run.tool.extensions[]?.rules[]? ] as $rules
    | (
        [ $rules[]
          | select(.id != null and (.id == $result.ruleId or .id == $result.rule.id))
        ][0]
      ) as $by_id
    | (
        if ($result.ruleIndex | type) == "number" then
          $run.tool.driver.rules[$result.ruleIndex]
        elif ($result.rule.index | type) == "number" then
          $run.tool.driver.rules[$result.rule.index]
        else
          null
        end
      ) as $by_index
    | $by_id // $by_index // {};
  def score_text($raw):
    if $raw == null or $raw == "" then
      "missing"
    elif ($raw | type) == "number" then
      ($raw | tostring)
    elif (($raw | tostring | tonumber?) != null) then
      ($raw | tostring)
    else
      "missing"
    end;
  .runs[]? as $run
  | $run.results[]? as $result
  | ($result.properties // {}) as $result_props
  | (rule_for($run; $result).properties // {}) as $rule_props
  | select(security_tag($result_props) or security_tag($rule_props))
  | score_text(severity_of($result_props) // severity_of($rule_props)) as $score
  | [
      ($result.ruleId // $result.rule.id // "-"),
      ($result.locations[0].physicalLocation.artifactLocation.uri // "-"),
      ($result.locations[0].physicalLocation.region.startLine // "-"),
      $score,
      (if $score == "missing" or (($score | tonumber) >= 7.0) then "1" else "0" end)
    ]
  | @tsv
' "$sarif")

status=0
if [[ -n $rows ]]; then
  while IFS=$'\t' read -r rule file line score block; do
    [[ -z $rule && -z $file ]] && continue
    if is_baselined "$rule"; then
      printf 'baseline %s %s %s\n' "$rule" "$file" "$line"
      continue
    fi
    printf '%s %s %s security-severity %s\n' "$rule" "$file" "$line" "$score"
    if [[ $block == 1 ]]; then
      status=1
    fi
  done <<<"$rows"
fi

exit "$status"
