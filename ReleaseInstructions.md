# Release instructions (NAudio 2.x)

Reminder card for shipping NAudio 2.x maintenance releases from the `release/2.x` branch. NAudio 3 is released from `main` using the same flow; its [ReleaseInstructions.md](https://github.com/naudio/NAudio/blob/main/ReleaseInstructions.md) and [ReleaseStrategy.md](https://github.com/naudio/NAudio/blob/main/Docs/Architecture/ReleaseStrategy.md) have the full rationale.

Releases are built and pushed by [.github/workflows/release.yml](.github/workflows/release.yml) on GitHub Actions, using NuGet trusted publishing. No API key is needed.

## Getting fixes onto the branch

Open PRs against `release/2.x` (backports of `main` fixes, or 2.x-only fixes). Add a bullet under `### Unreleased` in [RELEASE_NOTES.md](RELEASE_NOTES.md) for anything user-visible. CI ([build.yml](.github/workflows/build.yml)) builds, tests and runs package validation on every PR.

Package validation compares each package against the last stable 2.x release (`PackageValidationBaselineVersion` in [Directory.Build.targets](Directory.Build.targets)) and fails on any removed public API or target framework. A maintenance release must not break consumers, so treat a `CP`/`PKV` error as a bug in the PR rather than something to suppress.

## Pre-release (optional)

```sh
gh workflow run release.yml --ref release/2.x
```

Or in the Actions tab, pick the **release** workflow, choose `release/2.x` in the branch dropdown, and run it. This produces `<VersionPrefix>-preview.<run_number>` (e.g. `2.4.1-preview.57`). Add `-f milestone=rc.1` for a named suffix instead. Users on stable versions don't see pre-releases.

The workflow has the same file name as `main`'s on purpose. GitHub only lists dispatchable workflows from the default branch, but runs the copy from the branch you choose.

## Full release

1. **Pre-flight PR to `release/2.x`:**
   - Make sure `<VersionPrefix>` in [Directory.Build.props](Directory.Build.props) is the version you're shipping (normally it already is, from step 3 of the last release).
   - In `RELEASE_NOTES.md`, rename `### Unreleased` to `### 2.4.1 (DD MMM YYYY)` and add a fresh empty `### Unreleased` above it.
2. **Tag the merge commit:**

   ```sh
   git fetch origin
   git checkout release/2.x
   git pull --ff-only
   git tag v2.4.1
   git push origin v2.4.1
   ```

   The tag push runs `release.yml` from the tagged commit. It checks that the tag matches `VersionPrefix` and that `RELEASE_NOTES.md` has a matching section, then builds, tests, packs the nine packages and pushes them to NuGet. Last, it creates a GitHub Release with the notes and packages attached, marked as **not** the latest release, so NAudio 3 stays the repository's "Latest".
3. **Post-release PR to `release/2.x`** (once the packages are live on NuGet): bump `<VersionPrefix>` to the next patch version (e.g. `2.4.2`), so later previews sort above the release just shipped. In the same PR, set `<PackageValidationBaselineVersion>` in `Directory.Build.targets` to the version just shipped.

## Packages

`NAudio.Core`, `NAudio.Asio`, `NAudio.WinForms`, `NAudio.Midi`, `NAudio.WinMM`, `NAudio.Wasapi`, `NAudio.Uap`, `NAudio` and `NAudio.Extras`, all at the lockstep `VersionPrefix`. The list is in the `Pack` step of `release.yml`, which also checks the count.

## Troubleshooting

- **NuGet push fails with an auth error:** check the trusted-publishing policy at NuGet.org → your account → Trusted Publishing. It is shared with `main` (keyed on the `release.yml` file name), and goes inactive if a temporarily-active policy isn't used within 7 days. The publishing account must own every package, including the 2.x-only `NAudio.Uap`.
- **`Tag v2.4.1 does not match VersionPrefix ...`:** the tagged commit's `Directory.Build.props` has a different version. Fix it in a PR, then delete and re-create the tag on the new merge commit.
- **`RELEASE_NOTES.md has no '### 2.4.1' section`:** the pre-flight PR didn't rename `### Unreleased`. Same fix.
