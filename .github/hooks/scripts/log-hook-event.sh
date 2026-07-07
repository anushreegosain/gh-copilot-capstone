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

# Sanitize input to valid UTF-8 (drop invalid bytes) when iconv is available
RAW_CLEAN="$RAW_INPUT"
if command -v iconv >/dev/null 2>&1; then
  # -c drops invalid characters
  RAW_CLEAN="$(printf '%s' "$RAW_INPUT" | iconv -f utf-8 -t utf-8 -c 2>/dev/null || printf '%s' "$RAW_INPUT")"
fi

if command -v jq >/dev/null 2>&1; then
  PAYLOAD="null"
  if [ -n "${RAW_CLEAN}" ]; then
    # If it's valid JSON, use it. Otherwise, try to unescape a JSON string; fall back to raw payload.
    if echo "${RAW_CLEAN}" | jq -c . >/dev/null 2>&1; then
      PAYLOAD="$(echo "${RAW_CLEAN}" | jq -c .)"
    else
      # capture jq stderr for diagnostics; try unescaping JSON string separately
      JQ_ERR="$(echo "${RAW_CLEAN}" | jq -c . 2>&1 >/dev/null || true)"
      if echo "${RAW_CLEAN}" | jq -R 'fromjson? // empty' >/dev/null 2>&1; then
        PAYLOAD="$(echo "${RAW_CLEAN}" | jq -R 'fromjson' -c)"
      else
        # include a short parse error detail to help debugging, but always keep raw
        ERR_SHORT="${JQ_ERR//\"/\\\"}"
        PAYLOAD="$(jq -nc --arg raw "${RAW_CLEAN}" --arg err "$ERR_SHORT" '{raw:$raw, parseErrorDetail:$err}')"
      fi
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
