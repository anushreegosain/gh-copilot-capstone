#!/usr/bin/env bash
set -euo pipefail

HOOK_NAME="${1:-unknown}"
LOG_DIR=".github/hooks/logs"
LOG_FILE="${LOG_DIR}/agent-hooks.jsonl"

mkdir -p "${LOG_DIR}"

RAW_INPUT=""
if [ ! -t 0 ]; then
  RAW_INPUT="$(cat)"
fi

if command -v jq >/dev/null 2>&1; then
  PAYLOAD="null"
  if [ -n "${RAW_INPUT}" ]; then
    if echo "${RAW_INPUT}" | jq -c . >/dev/null 2>&1; then
      PAYLOAD="$(echo "${RAW_INPUT}" | jq -c .)"
    else
      PAYLOAD="$(jq -nc --arg raw "${RAW_INPUT}" '{parseError:"Invalid JSON input", raw:$raw}')"
    fi
  fi

  jq -nc \
    --arg ts "$(date -u +%Y-%m-%dT%H:%M:%SZ)" \
    --arg hook "${HOOK_NAME}" \
    --argjson payload "${PAYLOAD}" \
    '{timestampUtc:$ts,hook:$hook,payload:$payload}' >> "${LOG_FILE}"
else
  # Fallback if jq is unavailable.
  printf '{"timestampUtc":"%s","hook":"%s"}\n' "$(date -u +%Y-%m-%dT%H:%M:%SZ)" "${HOOK_NAME}" >> "${LOG_FILE}"
fi

# Exit 0 so this hook does not block agent execution.
exit 0
