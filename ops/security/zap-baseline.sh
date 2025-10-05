#!/usr/bin/env bash
set -euo pipefail

BASE_URL=${BASE_URL:-"http://localhost:5000"}
TARGET_ROLE=${TARGET_ROLE:-"participant"}
DEBUG_MODE=${DEBUG_MODE:-""}
OUTPUT_DIR=${OUTPUT_DIR:-"artifacts/security"}

mkdir -p "$OUTPUT_DIR"

echo "Running ZAP baseline scan against ${BASE_URL}"

ADDITIONAL_ARGS=()

if [[ -n "$DEBUG_MODE" ]]; then
  ADDITIONAL_ARGS+=("-z" "-config replacer.full_list(0).description=debug-user")
  ADDITIONAL_ARGS+=("-z" "-config replacer.full_list(0).enabled=true")
  ADDITIONAL_ARGS+=("-z" "-config replacer.full_list(0).matchtype=REQ_HEADER")
  ADDITIONAL_ARGS+=("-z" "-config replacer.full_list(0).matchstr=X-Debug-User")
  ADDITIONAL_ARGS+=("-z" "-config replacer.full_list(0).regex=false")
  ADDITIONAL_ARGS+=("-z" "-config replacer.full_list(0).replacement=zap-${TARGET_ROLE}")

  ADDITIONAL_ARGS+=("-z" "-config replacer.full_list(1).description=debug-email")
  ADDITIONAL_ARGS+=("-z" "-config replacer.full_list(1).enabled=true")
  ADDITIONAL_ARGS+=("-z" "-config replacer.full_list(1).matchtype=REQ_HEADER")
  ADDITIONAL_ARGS+=("-z" "-config replacer.full_list(1).matchstr=X-Debug-Email")
  ADDITIONAL_ARGS+=("-z" "-config replacer.full_list(1).regex=false")
  ADDITIONAL_ARGS+=("-z" "-config replacer.full_list(1).replacement=zap-${TARGET_ROLE}@example.com")

  ADDITIONAL_ARGS+=("-z" "-config replacer.full_list(2).description=debug-roles")
  ADDITIONAL_ARGS+=("-z" "-config replacer.full_list(2).enabled=true")
  ADDITIONAL_ARGS+=("-z" "-config replacer.full_list(2).matchtype=REQ_HEADER")
  ADDITIONAL_ARGS+=("-z" "-config replacer.full_list(2).matchstr=X-Debug-Roles")
  ADDITIONAL_ARGS+=("-z" "-config replacer.full_list(2).regex=false")
  ADDITIONAL_ARGS+=("-z" "-config replacer.full_list(2).replacement=${TARGET_ROLE}")
fi

docker run --rm -v "$(pwd)/${OUTPUT_DIR}:/zap/wrk" owasp/zap2docker-stable \
  zap-baseline.py -t "${BASE_URL}" -r "zap-baseline.html" "${ADDITIONAL_ARGS[@]}"

echo "Baseline scan complete. Report saved to ${OUTPUT_DIR}/zap-baseline.html"
