# Release instructions

Quick reminder card for cutting NAudio releases. Full design rationale and history is in [Docs/Architecture/ReleaseStrategy.md](Docs/Architecture/ReleaseStrategy.md).

Prerequisites: `gh` CLI authenticated, working directory inside the repo, on `main` for the local-clone steps.

## 1. Auto-numbered preview

```sh
gh workflow run release.yml
```

Produces `<VersionPrefix>-preview.<run_number>` — e.g. `3.0.0-preview.5`. Watch in the [Actions tab](https://github.com/naudio/NAudio/actions/workflows/release.yml). On success, all 8 packages (`.nupkg` + `.snupkg`) appear on NuGet within a few minutes.

## 2. Named pre-release milestone

Override the suffix via the `milestone` input. Use for alpha / beta / rc progression:

```sh
gh workflow run release.yml -f milestone=rc.1
```

Suffix ordering on NuGet: `alpha.N < beta.N < preview.N < rc.N < (final)`. Always use the **dotted** form (`rc.1`, not `rc1`) so the trailing integer compares numerically — `rc.10 > rc.9` only works with the dot.

## 3. Full release

### a. Pre-flight PR

1. Bump `<VersionPrefix>` in [Directory.Build.props](Directory.Build.props) to the target version (e.g. `3.0.0`).
2. In [RELEASE_NOTES.md](RELEASE_NOTES.md): rename `### Unreleased` → `### 3.0.0 (DD MMM YYYY)`, curate the bullets, add PR numbers where useful.
3. Add a fresh empty `### Unreleased` section above the renamed one so post-release contributors have a place to land bullets.

Merge via the usual protected-branch flow.

### b. Tag and push

From local `main` synced to the merge commit:

```sh
git fetch origin
git checkout main
git pull --ff-only
git tag v3.0.0
git push origin v3.0.0
```

The tag push triggers `release.yml`, which:

- Validates the tag matches `<VersionPrefix>` in `Directory.Build.props`.
- Validates `RELEASE_NOTES.md` has a matching `### 3.0.0` section.
- Packs all 8 NAudio packages (+ matching `.snupkg` symbol packages).
- Pushes everything to NuGet via trusted publishing.
- Creates a GitHub Release titled `NAudio 3.0.0` with body extracted from the `RELEASE_NOTES.md` section.

No further action required for the publish itself. Both validations are fail-fast — if `VersionPrefix` or release notes don't match the tag, the workflow errors before packing.

### c. Post-release version bump PR

Open a small follow-up PR bumping `<VersionPrefix>` in `Directory.Build.props` to the next development version (e.g. `3.0.1` or `3.1.0`). Without this, the next preview dispatch produces a version *lower* than the just-shipped final (since `3.0.0-preview.N < 3.0.0`).

### d. Announce

GitHub Release + NuGet are the canonical channels. Optionally:

- Blog post on markheath.net
- Social media
- Pin a GitHub Discussion for major-version announcements

## Troubleshooting

- **`gh workflow run` returns "workflow not found":** the workflow file must exist on the default branch. After Phase 8 the default is `main`; should always work.
- **NuGet push fails with auth error:** the trusted-publisher policy on NuGet.org has a 7-day temporarily-active window. After a successful publish it becomes permanent, but if no publish happens for 7 days it goes inactive. Re-activate at NuGet.org → username → Trusted Publishing.
- **`Tag v3.0.0 does not match VersionPrefix 3.0.0-alpha`** (or similar): you're tagging a commit whose `Directory.Build.props` doesn't have `<VersionPrefix>3.0.0</VersionPrefix>`. Either retag after the pre-flight PR merges, or push a fixup commit and tag that.
- **`RELEASE_NOTES.md has no '### 3.0.0' section`:** the pre-flight PR didn't rename `### Unreleased`. Push a fix to `main` and delete + re-create the tag.

## See also

- Full release strategy and decision rationale: [Docs/Architecture/ReleaseStrategy.md](Docs/Architecture/ReleaseStrategy.md)
- Release workflow file: [.github/workflows/release.yml](.github/workflows/release.yml)
- Build workflow file: [.github/workflows/build.yml](.github/workflows/build.yml)
