#!/usr/bin/env bash
set -euo pipefail

usage() {
  echo "użycie: verify-attestation.sh <właściciel> <komponent> <digest>" >&2
  exit 2
}

if [[ $# -ne 3 ]]; then
  usage
fi

owner=$1
component=$2
digest=$3

if [[ ! $owner =~ ^[a-z0-9][a-z0-9-]*$ ]]; then
  echo "zły właściciel" >&2
  exit 2
fi

if [[ ! $component =~ ^(identity|news|learning|cv|contact|gateway|web|mobile)$ ]]; then
  echo "zły komponent" >&2
  exit 2
fi

if [[ ! $digest =~ ^sha256:[0-9a-f]{64}$ ]]; then
  echo "zły digest" >&2
  exit 2
fi

if ! command -v gh >/dev/null 2>&1; then
  echo "brak gh" >&2
  exit 2
fi

image="oci://ghcr.io/${owner}/jjdevhub-${component}@${digest}"
if gh attestation verify "$image" --owner "$owner"; then
  exit 0
fi
exit 1
