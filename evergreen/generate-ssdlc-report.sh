#!/usr/bin/env bash
set -o errexit # Exit the script with error if any of the commands fail

# Environment variables used as input:
# NUGET_SIGN_CERTIFICATE_FINGERPRINT
# PRODUCT_NAME
# PACKAGE_VERSION
# task_id

echo "$PRODUCT_NAME"
echo "$PACKAGE_VERSION"

echo "Creating SSDLC reports"

declare -r SSDLC_PATH="./artifacts/ssdlc"
mkdir -p "${SSDLC_PATH}"

echo "Creating SSDLC compliance report"
declare -r TEMPLATE_SSDLC_REPORT_PATH="./evergreen/template_ssdlc_compliance_report.md"
declare -r SSDLC_REPORT_PATH="${SSDLC_PATH}/ssdlc_compliance_report.md"
cp "${TEMPLATE_SSDLC_REPORT_PATH}" "${SSDLC_REPORT_PATH}"

declare -a SED_EDIT_IN_PLACE_OPTION
if [[ "$OSTYPE" == "darwin"* ]]; then
  SED_EDIT_IN_PLACE_OPTION=(-i '')
else
  SED_EDIT_IN_PLACE_OPTION=(-i)
fi
sed "${SED_EDIT_IN_PLACE_OPTION[@]}" \
    -e "s/\${PRODUCT_NAME}/${PRODUCT_NAME}/g" \
    -e "s/\${PACKAGE_VERSION}/$PACKAGE_VERSION/g" \
    -e "s/\${task_id}/$task_id/g" \
    -e "s/\${REPORT_DATE_UTC}/$(date -u +%Y-%m-%d)/g" \
    -e "s/\${NUGET_SIGN_CERTIFICATE_FINGERPRINT}/${NUGET_SIGN_CERTIFICATE_FINGERPRINT}/g" \
    "${SSDLC_REPORT_PATH}"
ls "${SSDLC_REPORT_PATH}"