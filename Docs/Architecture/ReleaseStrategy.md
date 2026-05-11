# NAudio 3 — Release strategy

**Goal:** Replace the current manual `publish.ps1` flow with an automated GitHub Actions pipeline that publishes both regular pre-release builds (cheap, frequent, on demand) and final releases (tag-driven, with GitHub Releases attached). Rebrand `naudio3dev` as the default branch, retire NAudio 2 to a maintenance branch, and put a lightweight discipline around release notes that keeps the maintainer in control without blocking PRs.

**Branch:** work continues on `main` (renamed from `naudio3dev` in step 8). NAudio 2 maintenance lives on `release/2.x` (renamed from `master`). Both are protected; all changes via PR.

## Status

| Phase | State | Notes |
| --- | --- | --- |
| 0. Validate GitHub Actions | ✅ done | Green build on `windows-latest`. 2028 passing / 0 failed / 7 skipped, 1m49s (Azure was 2m51s). Draft PR closed unmerged; throwaway branch `ci/validate-github-actions` retained for reference. |
| 1. Merge `origin/master` into `naudio3dev` | ✅ done | Auto-merged via ORT strategy with no conflicts. The expected `Mp3FileReader` clash didn't materialise — master's MP3 sample-rate fix touched different lines from the lazy-TOC work. Local tests post-merge: 2037/0/6. Pushed. |
| 2. Land build workflow on `naudio3dev` | ✅ done | Workflow merged. Azure Pipelines kept running in parallel as a safety net per step 12. |
| 3. Centralize version in `Directory.Build.props` | ✅ done | `<VersionPrefix>3.0.0</VersionPrefix>` set at root, `<Version>2.3.0</Version>` removed from all 8 NAudio package csprojs. All NAudio packages now build as `*.3.0.0.nupkg`. Tool/sample apps keep their own explicit `<Version>`. |
| 4. Release-notes plumbing + labels + `CLAUDE.md` | ✅ done | PR #1269 merged. `.github/release.yml`, `.github/PULL_REQUEST_TEMPLATE.md`, `CLAUDE.md`, and `### Unreleased` placeholder all live. `breaking` and `release-notes-skip` labels added in the UI. |
| 5. Backfill `RELEASE_NOTES.md` | ✅ done | PR #1270 merged. ~60 categorised bullets across six sub-sections under `### Unreleased`, ready for the maintainer to curate further as PRs land. |
| 6. Release workflow | ✅ done | PR #1271 merged. Two triggers (`workflow_dispatch` for previews, `push tags v*` for finals), version resolution from `<VersionPrefix>`, validation guards, pack of all 8 NAudio packages, NuGet trusted-publishing push, and a final-only GitHub Release with body extracted from `RELEASE_NOTES.md`. |
| 7. NuGet trusted publishing + smoke test | ✅ done | Trusted publisher configured on NuGet.org; `vars.NUGET_USER` set in repo. First dispatch (`preview.1`) burned at the push step on a PowerShell glob-expansion bug, fixed in PR #1273. Re-dispatch shipped all 8 packages as `3.0.0-preview.2`. |
| 8. Branch flip + protection | ✅ done | `master` → `release/2.x` and `naudio3dev` → `main`. `main` is default. Ruleset "Protected branches" on default + `release/*` (require PR, require `build` status check, require linear history, block force-push, block deletion). Cleanup PR #1272 followed up workflow files and doc/README links. |
| 9. First public preview + retire Azure Pipelines | ✅ done | `3.0.0-preview.2` shipped to NuGet via the new flow. PR #1274 deleted `azure-pipelines.yml` and `publish.ps1`, dropped `<GeneratePackageOnBuild>` from all 8 NAudio packages, refreshed badges and READMEs, and consolidated duplicated csproj boilerplate into `Directory.Build.props` and `Directory.Build.targets`. Contributor announcement and stale-PR triage remain as non-blocking manual steps (see §"Communication"). Azure DevOps cleanup is in §"Retiring Azure DevOps". |
| 10. SourceLink + symbol packages + deterministic CI | ✅ done | PR #1275 merged. `.NET 8+` SDK SourceLink activated via `<PublishRepositoryUrl>` (no PackageReference needed). `.snupkg` symbol packages produced via `<IncludeSymbols>` + `<SymbolPackageFormat>snupkg`; pushed alongside the `.nupkg` by `dotnet nuget push`. `<ContinuousIntegrationBuild>` gated on `GITHUB_ACTIONS=true` for reproducible CI binaries. `3.0.0-preview.3` shipped with all three active. |
| 11. Embed `PackageReleaseNotes` in the nupkg | ⏳ in progress | The release workflow now extracts the appropriate `RELEASE_NOTES.md` section (`### Unreleased` for previews, `### <version>` for finals) into a file before pack runs, then passes the file path via `-p:PackageReleaseNotesFile=...`. A small MSBuild target in `Directory.Build.targets` reads the file and sets `<PackageReleaseNotes>` so NuGet displays the content on the package page. The same file is reused for the GitHub Release body on finals. |
| 12. First final 3.0.0 release | future | |

### Findings carried forward from Phase 0

- **`actions/setup-dotnet` is not required on `windows-latest`** — .NET 10 SDK (currently `10.0.203`) and runtime are pre-installed. Removing the explicit setup step shaved ~10s and removed dead config. **Trade-off:** when the runner image eventually rotates and drops .NET 10, the build will silently break. If determinism becomes important before then, add `setup-dotnet` back with a pinned version. Deferred for now.
- **`dotnet test` in .NET 10 requires `--project`** when invoking with a project path. The Azure `DotNetCoreCLI@2` task papers over this; raw `dotnet test path/to.csproj` now errors with *"Specifying a project for 'dotnet test' should be via '--project'."* The validated workflow uses `--project NAudioTests/NAudioTests.csproj`. Same applies to any future test invocations.
- **Build runs ~40% faster on GitHub Actions** than on Azure Pipelines for the same commit. Not a decision criterion, but a nice confirmation that the migration isn't a regression.

## Why we're changing the flow

The current process — bump `<Version>` in eight `.csproj` files, build locally, run `publish.ps1` against a hand-set `$apiKey` — works but does not scale to the NAudio 3 cadence:

- NAudio 3 adds three more packages (`NAudio.MediaFoundation`, `NAudio.Dmo`, `NAudio.DirectSound`), so the per-csproj `<Version>` duplication grows from 8 to 11.
- We want frequent pre-releases on the path to 3.0.0 without ceremony.
- A long-lived NuGet API key in a developer's environment is not where the industry has settled — NuGet trusted publishing via OIDC is now GA.
- Final releases should produce a GitHub Release with notes attached, not just a NuGet package.
- Azure DevOps Pipelines builds and tests but does not publish; introducing publishing in two places is worse than consolidating in one.

## Key decisions (with rationale)

### CI host: migrate to GitHub Actions

Azure Pipelines builds and tests reliably today, but every reason to keep it is operational inertia. GitHub Actions wins on three points specific to this work:

1. **NuGet trusted publishing** is supported via OIDC — no long-lived API key in repository secrets. Azure Pipelines is supported too, but the GitHub flow is simpler.
2. **GitHub Releases** can be created in one CLI call (`gh release create --generate-notes`) directly from the same workflow that publishes packages.
3. The repo, issues, PRs, releases, and CI live in one place.

GitHub-hosted `windows-latest` is the same Windows Server 2022 image family Azure Pipelines uses for `windows-latest`, with the same .NET SDKs and the same lack of real audio hardware (which is fine — the existing pipeline already filters `TestCategory!=IntegrationTest`). No regression risk on the build/test side.

### Versioning: lockstep, single source

All packages share one version, declared once in `Directory.Build.props` as `<VersionPrefix>`. The per-csproj `<Version>` lines are deleted. Rationale:

- The `NAudio` meta-package transitively pins specific versions; partial updates produce confusing combinations.
- Even if some packages have no functional changes between releases, republishing them is harmless and keeps users on a self-consistent set.
- One file to edit on every bump.

CI sets `<VersionSuffix>` for pre-release builds via `dotnet pack -p:VersionSuffix=preview.NN`; final-release builds set neither suffix.

### Pre-release: manual trigger, auto-incrementing counter

Pre-releases are triggered manually via `workflow_dispatch`. The workflow uses `github.run_number` as the auto-incrementing preview counter, producing versions like `3.0.0-preview.142`. A `milestone` input on the dispatch lets the maintainer override the suffix when cutting a named milestone (`alpha.1`, `beta.2`, `rc.1`).

NuGet's SemVer 2 ordering means the resulting versions sort as expected:

```
3.0.0-alpha.1 < 3.0.0-beta.1 < 3.0.0-preview.142 < 3.0.0-rc.1 < 3.0.0
```

(`preview` is between `beta` and `rc` alphabetically; numeric identifiers like `142` compare numerically.)

We considered "publish on every push" and rejected it: NuGet version sprawl, reviewer confusion about which preview to test, and pressure to treat every commit as releasable. Manual is the right starting point and can be upgraded later if needed.

### Final release: tag-driven

Pushing a tag of the form `v3.0.0` triggers the release workflow, which:

1. Validates the tag matches `<VersionPrefix>` in `Directory.Build.props`.
2. Validates `RELEASE_NOTES.md` has a matching `### 3.0.0 (date)` section — fails the build if missing.
3. Builds, tests, packs all packages.
4. Pushes packages to NuGet via trusted publishing.
5. Creates a GitHub Release with the body extracted from the matching `RELEASE_NOTES.md` section and the auto-generated PR list appended.

Tags are explicit, auditable, and don't fire on every typo-fix to `Directory.Build.props`.

### Branching: rename, don't fork

`naudio3dev` becomes `main`; current `master` becomes `release/2.x`. Both renames are done via the GitHub UI's "Rename branch" feature, which preserves history, redirects URLs, and re-targets open PRs without any `git push --force`.

NAudio 2 stable consumers are unaffected by `main` representing NAudio 3, because `Install-Package NAudio` requires `-IncludePrerelease` to pick up `3.0.0-preview.*`. New PRs default to `main` (NAudio 3), which matches where contributor effort should be aimed.

### Branch protection: PRs only, CI green

Both `main` and `release/2.x` are protected:

- Direct pushes blocked (including from the maintainer).
- All changes via PR.
- Required status check: the `build` workflow must pass.
- Optional but recommended: require linear history (no merge commits — squash or rebase only). This keeps `git log main` readable.

This is what gets us reliable release notes coverage: every change goes through a PR, every PR can be labelled, every label feeds the auto-generated changelog.

### Release notes: layered, lightweight, maintainer-curated

Four complementary layers, none of them blocking:

1. **PR labels feed an auto-generated changelog** via `.github/release.yml`. `gh release create --generate-notes` produces a categorized PR list (Breaking, Features, Bug fixes, Docs, Other) — a safety net that catches anything missed elsewhere.
2. **`RELEASE_NOTES.md` is hand-curated** with an `### Unreleased` section at the top. Contributors and agents add bullets as PRs land; the maintainer edits prose and renames `Unreleased` → `3.0.0 (date)` at release time. This is what NuGet displays via `<PackageReleaseNotes>`.
3. **A PR template** has a checkbox "I added a release notes entry" with explicit escape hatches for "doesn't need one" and "leave for maintainer to write." Visible to reviewers, not enforced by CI.
4. **A `CLAUDE.md` instruction** tells AI agents to add an `Unreleased` bullet for any user-visible change. This automates most of the load.

Why no CI block on release notes? Because the maintainer often wants to write the entry anyway, so a blocking check produces friction without value. Layer 1 (auto-generated PR list) means nothing is ever lost; layer 2 gives the maintainer a draft to curate.

#### Labels needed

GitHub provides `bug`, `enhancement`, `documentation` by default. We add two new labels:

| Label | Purpose | Suggested colour |
|---|---|---|
| `breaking` | Breaking change — top of the changelog | red |
| `release-notes-skip` | Internal/refactor PR — exclude from changelog | grey |

Labels are applied by the maintainer (or any collaborator) at PR review/merge time. Untagged PRs fall into the `Other` catch-all section, which is acceptable.

## Step-by-step plan

The plan is sequenced so that the highest-uncertainty item (CI migration) is validated first, on a throwaway PR, before any irreversible changes (branch renames, label changes, doc edits in `main`). Each phase is independently committable and reversible.

### Phase 0 — Validate GitHub Actions on a throwaway PR ✅

Goal: prove that GitHub Actions can build and test NAudio with the same coverage as Azure Pipelines, before committing to anything else. This phase produces no merged change.

1. On a throwaway branch off `naudio3dev`, add `.github/workflows/build.yml` that mirrors the current Azure pipeline: `dotnet restore`, `dotnet build`, `dotnet test --filter "TestCategory!=IntegrationTest"`.
2. Push the branch, open a draft PR, observe the workflow run on `windows-latest`.
3. Confirm test results match what Azure Pipelines reports for the same commit.
4. If tests fail in ways that don't fail on Azure: triage. Likely culprits are missing audio-stack dependencies; the fix is usually a `TestCategory` exclusion. Stop here and resolve before proceeding.
5. If tests pass: close the draft PR. Do not merge yet — the workflow will be reintroduced as part of phase 2.

**Outcome:** green build on `windows-latest`. 2028 passing / 0 failed / 7 skipped, 1m49s (Azure baseline was 2m51s). Two YAML adjustments made during validation: dropped the now-redundant `setup-dotnet` step (.NET 10 is preinstalled), and switched to `dotnet test --project ...` to match the .NET 10 CLI's stricter argument parsing — see "Findings carried forward from Phase 0" above. Draft PR closed unmerged.

### Phase 1 — Bring `naudio3dev` up to date with `master` ✅

6. `git fetch origin`.
7. `git checkout naudio3dev`, `git merge origin/master`. Resolve conflicts. Likely affected files: `Mp3FileReader.cs` (overlaps with the lazy-TOC work), possibly `WaveStream.cs` (block-alignment fix), and MIDI files (running-status fix). Conflicts in `RELEASE_NOTES.md` are unlikely because `master` hasn't touched it since 2.3.0 shipped.
8. Run the full test suite locally to confirm the merge is sound.
9. Push `naudio3dev`.

**Outcome:** clean ORT-strategy merge with zero conflicts. Auto-merged files: `BiQuadFilter.cs`, `WaveStream.cs`, `MidiEvent.cs`, `MidiFile.cs`. New files: `WaveStreamTests.cs`, additions to `MidiFileTests.cs`. Local test run post-merge: 2037 passing / 0 failed / 6 skipped — the +9 vs Phase 0 corresponds to the new regression tests from master.

### Phase 2 — Land the build workflow on `naudio3dev` ✅

10. Open a real PR adding `.github/workflows/build.yml` (the validated version from phase 0, with `--project` arg fix and no `setup-dotnet`). Add `push: branches: [naudio3dev]` to the existing `pull_request` and `workflow_dispatch` triggers so post-merge pushes also run CI.
11. Merge.
12. Optional: leave Azure Pipelines running in parallel for one or two more PRs as a safety net before deleting in phase 9.

**Outcome:** PR opened, CI green on the PR's own diff, merged. Azure Pipelines kept running in parallel as the safety net per step 12; will be retired in phase 9.

### Phase 3 — Centralize the version ✅

13. Add `<VersionPrefix>3.0.0</VersionPrefix>` to `Directory.Build.props`.
14. Remove `<Version>2.3.0</Version>` from all 8 `.csproj` files (and the 3 NAudio 3 packages once they exist).
15. Build locally, confirm all packages produce `3.0.0.nupkg` filenames.
16. PR + merge.

**Outcome:** PR #1268 merged. All 8 NAudio packages now produce `*.3.0.0.nupkg` from a single `<VersionPrefix>` declaration. Tool/sample apps left untouched and continue to override with their own explicit `<Version>`.

### Phase 4 — Add release-notes plumbing and contributor docs ✅

17. Add `.github/release.yml` configuring the auto-changelog categories.
18. Add `.github/PULL_REQUEST_TEMPLATE.md` with the release-notes checkbox.
19. Add a project-root `CLAUDE.md` (or extend the existing one if introduced elsewhere) instructing agents to maintain `### Unreleased` bullets.
20. In the GitHub UI, create the `breaking` and `release-notes-skip` labels.
21. PR + merge.

**Outcome:** PR #1269 merged. Plumbing in place; new PRs render the template, the auto-changelog config will categorise correctly when invoked, and `CLAUDE.md` documents the release-notes process for AI agents.

### Phase 5 — Backfill `RELEASE_NOTES.md` ✅

22. Walk `git log v2.3.0..naudio3dev` (174 commits).
23. Group commits into themes: WASAPI modernization, NAudio 3 layout, MF reliability, AOT compatibility, ACM hardening, Mp3FileReader lazy-TOC, DirectSound modernization, etc.
24. Draft an `### Unreleased` section at the top of `RELEASE_NOTES.md` with one bullet per user-visible change.
25. PR + merge.

**Outcome:** PR #1270 merged. ~60 bullets under `### Unreleased` organised into six sub-sections (Breaking changes, New features, Performance, Reliability and bug fixes, Modernisation, Packaging). Bullets carry no PR numbers — those can be added selectively before final release.

### Phase 6 — Add the release workflow

26. Add `.github/workflows/release.yml`. Two triggers:
    - `workflow_dispatch` with optional `milestone` input (default empty → uses `preview.${{ github.run_number }}`).
    - `push` of tags matching `v*`.
27. Workflow steps:
    - Determine the version: tag-driven extracts from `${{ github.ref_name }}`; dispatch-driven uses `<VersionPrefix>` + suffix.
    - Validate tag matches `<VersionPrefix>` (final releases only).
    - Validate `RELEASE_NOTES.md` has a matching section (final releases only — pre-releases inherit `Unreleased`).
    - `dotnet pack` all packages with the resolved version.
    - Push to NuGet via trusted publishing.
    - For final releases: create a GitHub Release with body extracted from `RELEASE_NOTES.md` plus auto-generated PR list.
28. PR + merge.

**Exit criterion:** workflow exists, no run yet.

### Phase 7 — Configure NuGet trusted publishing

29. On NuGet.org, configure a trusted publisher policy for the repository (account settings → Trusted Publishers → add GitHub policy).
30. Confirm the workflow has the necessary `permissions: id-token: write` block.
31. Cut a `3.0.0-preview.0` from the workflow_dispatch UI as an end-to-end smoke test.
32. Verify the package appears on NuGet.org with the expected version, release notes, README, icon, and SourceLink debugging info.
33. If the package looks wrong: unlist it (do not delete — NuGet versions cannot be reused), fix, re-cut as `preview.1`.

**Exit criterion:** a valid pre-release exists on NuGet.org and installs cleanly into a test project.

### Phase 8 — Branch flip and protection ⏳

> **Note on order:** in practice this phase ran *before* Phase 7. The Phase 7 smoke test requires `workflow_dispatch` to be visible in the GitHub UI, which only happens for workflows on the default branch. The rename to `main` was therefore prerequisite, not subsequent.

34. In GitHub Settings → Branches, **rename `master` → `release/2.x`**. Confirm any open PRs are re-targeted (open PRs against `master` follow automatically).
35. In GitHub Settings → Branches, **rename `naudio3dev` → `main`**. Confirm open PRs are re-targeted.
36. In GitHub Settings → General, set `main` as the default branch.
37. Add a Ruleset "Protected branches" targeting Default + `release/*`:
    - Require a pull request before merging (approvals: 0 for solo).
    - Require status checks to pass — `build`.
    - Require linear history.
    - Block force pushes; restrict deletions.
    - Disable "Allow merge commits" in repo settings (incompatible with linear history).
38. Cleanup PR follow-up: update `build.yml` push trigger to `[main]`, drop `naudio3dev` from `release.yml` branch allowlist, refresh `CLAUDE.md` and `README.md` links, update `ReleaseStrategy.md` status.
39. Notify any active contributors to update their local clones (`git fetch origin --prune`, `git branch -m naudio3dev main`, `git branch -u origin/main main`, `git remote set-head origin -a`).

**Exit criterion:** `main` is the default; both protected branches block direct pushes; CI is required on every PR; the cleanup PR has merged so workflow files reflect the new branch names.

### Phase 9 — Cut a real preview and retire the old flow ⏳

40. Cut `3.0.0-preview.1` via the release workflow. This is the first preview that ships from `main`. *(Done as a side-effect of Phase 7's smoke test — actually shipped as `preview.2` because `preview.1` was burned on the glob-expansion bug.)*
41. Announce the preview on the project's usual channels. *(Pending manual step; see Communication below.)*
42. Delete `azure-pipelines.yml`.
43. Delete `publish.ps1`.
44. Remove `<GeneratePackageOnBuild>true</GeneratePackageOnBuild>` from all 8 NAudio package csprojs — the release workflow now packs explicitly, and leaving it on accumulates stale `.nupkg` files in `bin/Release/`.
45. Swap the Azure Pipelines build badge in `README.md` for a GitHub Actions badge.
46. Refresh per-package READMEs that still reference `net8.0` (NAudio.Core, Asio, Wasapi, WinMM, WinForms, Midi, Extras) to `net9.0`.

**Exit criterion:** the old flow no longer exists in the repo; the new flow has produced a publicly available pre-release.

## Communication

Branch-rename auto-banner only mentions the `naudio3dev → main` step, which most upstream contributors never saw. The real story for them is `master → release/2.x` plus the appearance of `main`. Notify in three places:

1. Pinned GitHub Discussion or Issue with the full migration guide (case A: PR currently against `master`; case B: local fork no PR; case C: brand new clone).
2. Short notice in `README.md` linking to the discussion.
3. Comment on each open PR auto-retargeted to `release/2.x` clarifying whether the PR should stay on `release/2.x` or rebase against `main`.

This is a manual step that does not block Phase 9's cleanup PR.

### Phase 10 — First final release (separate, not part of this rollout)

When the time comes for `3.0.0` proper:

45. Confirm `RELEASE_NOTES.md` has a curated `### 3.0.0 (date)` section.
46. Update `<VersionPrefix>` to `3.0.0` (no suffix needed — handled by the workflow).
47. Push tag `v3.0.0`. The release workflow does the rest.

## Retiring Azure DevOps

The `azure-pipelines.yml` and `publish.ps1` files are gone in Phase 9, but the Azure DevOps project at `dev.azure.com/naudio` and its associated GitHub access still exist. Recommended order:

### GitHub side

1. Settings → Integrations → Installed GitHub Apps → uninstall the **Azure Pipelines** app if present.
2. Settings → Webhooks → delete any pointing at `dev.azure.com`.
3. Settings → Branches → Ruleset → required status checks → confirm no Azure Pipelines check is required (would block PRs indefinitely).
4. Personal settings → Applications → Authorized OAuth Apps → revoke **Azure DevOps** / **Azure Pipelines** if present.

### Azure DevOps side

1. Pipelines → NAudio pipeline → `...` menu → **Disable** (preserves history) or **Delete**.
2. Project Settings → Service connections → delete any GitHub connection.
3. *(Optional)* Project Settings → Overview → **Delete project** — recoverable for 28 days, then permanently gone. Frees the `naudio` name in the ADO org.

The pragmatic path: disable the pipeline first, wait a couple of weeks, then delete the project.

## Out of scope

- Migrating CHANGELOG generation tooling beyond the GitHub built-in (`.github/release.yml`). If we outgrow it, swap in something heavier later.
- Per-package independent versioning. Lockstep is the chosen model; revisit only if it becomes painful.
- Automatic changelog enforcement via CI block. Explicitly rejected as friction without value.
- Appending the auto-generated PR list to GitHub Release bodies. `gh release create` makes `--notes-file` and `--generate-notes` mutually exclusive; adding both would require fetching auto-notes via `gh api` and concatenating. Possible follow-up if the curated notes alone prove insufficient.
