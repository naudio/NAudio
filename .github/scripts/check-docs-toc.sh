#!/usr/bin/env bash
# Verifies every tutorial in Docs/ is referenced by Docs/toc.yml, so that a
# newly added tutorial can't silently become an orphan page (DocFX still
# builds it and it's reachable by URL/search, but it's missing from the
# sidebar navigation, and --warningsAsErrors does not catch that).
#
# Docs/Architecture/ is contributor-facing and excluded from the docs site,
# so it is excluded from this check too.
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
docs_dir="$repo_root/Docs"
toc="$docs_dir/toc.yml"

# Tutorial files: *.md directly under Docs/ (not recursing into Architecture/).
mapfile -t tutorials < <(find "$docs_dir" -maxdepth 1 -name '*.md' -printf '%f\n' | sort)

# href targets referenced by the TOC.
mapfile -t referenced < <(grep -oE 'href:[[:space:]]*[^[:space:]]+\.md' "$toc" \
  | sed -E 's/href:[[:space:]]*//' | sort -u)

status=0

for md in "${tutorials[@]}"; do
  if ! printf '%s\n' "${referenced[@]}" | grep -qxF "$md"; then
    echo "::error::Docs/$md is not listed in Docs/toc.yml"
    status=1
  fi
done

for ref in "${referenced[@]}"; do
  if [[ ! -f "$docs_dir/$ref" ]]; then
    echo "::error::Docs/toc.yml references Docs/$ref which does not exist"
    status=1
  fi
done

if [[ $status -ne 0 ]]; then
  echo ""
  echo "Update Docs/toc.yml: add entries for new tutorials (or remove stale ones)."
  exit 1
fi

echo "All ${#tutorials[@]} tutorials in Docs/ are referenced by Docs/toc.yml."
