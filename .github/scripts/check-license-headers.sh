#!/usr/bin/env bash
# Check the Apache 2.0 license header on every C# source file.
#
# The expected banner lives in .github/license-header.txt, which is the single source of
# truth. New files created in Rider get it from the file templates in
# CSharpDriver.sln.DotSettings, which must be kept in step by hand.
#
#     .github/scripts/check-license-headers.sh

set -uo pipefail

root=$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)
cd "$root" || exit 2

reference=.github/license-header.txt
search_roots=(src tests benchmarks)

# Dual-license attribution notice derived from tombentley/saslprep. Apache-2.0 section 4
# requires retaining it, so it is deliberately not normalized.
excluded=src/MongoDB.Driver/Authentication/SaslPrepHelper.cs

marker="Licensed under the Apache License"
bom=$(printf '\357\273\277')
[[ -f $reference ]] || { echo "error: reference banner not found at $reference"; exit 2; }

findings=$(mktemp) || exit 2
trap 'rm -f "$findings"' EXIT

# Violations are printed, never counted inside awk: xargs may split the file list into
# several awk invocations, so bash aggregates the lines instead.
find "${search_roots[@]}" -name '*.cs' -not -path '*/obj/*' -not -path '*/bin/*' -print0 |
xargs -0 awk -v ref="$reference" -v bom="$bom" -v marker="$marker" -v excluded="$excluded" '
  BEGIN { while ((getline line < ref) > 0) want[++n] = line }

  function report(f) {
    if (f == "" || f == excluded) return
    if (lines[f] < n || bad[f])
      print f "\t" (seen[f] ? "license header does not match" : "missing license header")
    else if (seen[f] != 1)
      print f "\t" seen[f] " license banners, expected 1"
  }

  FILENAME != current { report(current); current = FILENAME }
  { sub(/\r$/, "") }
  FNR == 1 && index($0, bom) == 1 { $0 = substr($0, 4) }
  { lines[FILENAME] = FNR; if (index($0, marker)) seen[FILENAME]++ }
  FNR <= n && $0 != want[FNR] { bad[FILENAME] = 1 }

  END { report(current) }
' >> "$findings" || { echo "error: the file scan failed to run"; exit 2; }

if [[ ! -s $findings ]]; then
  echo "all license headers are compliant"
  exit 0
fi

while IFS=$'\t' read -r file reason; do
  if [[ ${GITHUB_ACTIONS:-} == true ]]; then
    echo "::error file=$file,line=1::$reason"
  else
    echo "$file: $reason"
  fi
done < "$findings"

echo
echo "$(wc -l < "$findings" | tr -d ' ') license header violation(s)."
echo "Every .cs file under ${search_roots[*]} must start with the banner in $reference, byte for byte."
exit 1
