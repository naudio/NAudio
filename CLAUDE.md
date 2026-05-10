# Working in NAudio

This file gives AI agents (Claude, etc.) the conventions for contributing to NAudio. Humans should read [README.md](README.md) and [Docs/Architecture/](Docs/Architecture/) instead.

## Orientation

- **NAudio 3 is in pre-release on the `naudio3dev` branch** (renamed to `main` in a future phase). NAudio 2 maintenance happens on `master` (renamed to `release/2.x` in the same future phase). Default to targeting `naudio3dev` unless explicitly told otherwise.
- **Architecture docs** in [Docs/Architecture/](Docs/Architecture/) are the source of truth for cross-cutting decisions:
  - [ReleaseStrategy.md](Docs/Architecture/ReleaseStrategy.md) — release/branch/version flow
  - [NAudio3AssemblyLayoutPlan.md](Docs/Architecture/NAudio3AssemblyLayoutPlan.md) — package structure
  - [MODERNIZATION.md](Docs/Architecture/MODERNIZATION.md) — modernisation phases

## Release notes

When you make a **user-visible change** — new public API, behaviour change, bug fix, deprecation, packaging change — add a one-line bullet to the `### Unreleased` section at the top of [RELEASE_NOTES.md](RELEASE_NOTES.md). Match the style of existing entries:

- Past tense (`Fixed X`, `Added Y`, `Updated Z`)
- Mention the GitHub PR or issue number if known: `(#1234)`
- One line, no prose paragraphs — the maintainer will edit at release time

**Skip the release-notes entry only for:** purely internal refactors, test-only changes, dependency bumps with no observable effect, docs/comment fixes. If unsure whether a change is user-visible, **add the entry** and let the maintainer remove it if not needed.

## PR labelling

When opening a PR (where you have permission), apply one of: `breaking`, `enhancement`, `bug`, `documentation`. These feed the auto-generated changelog at release time. Use `release-notes-skip` for PRs that should not appear in the changelog at all.

## Versioning

Package versions are centralised in [Directory.Build.props](Directory.Build.props) as `<VersionPrefix>`. Do **not** add a per-csproj `<Version>` to NAudio packages — they're meant to stay in lockstep. The tool/sample apps (MixDiff, AudioFileInspector, MidiFileConverter) keep their own explicit `<Version>` and are exempt.

## CI

Every PR runs build + test on `windows-latest` via [.github/workflows/build.yml](.github/workflows/build.yml). Tests requiring real audio hardware should be marked with `TestCategory=IntegrationTest` so they're filtered out of the headless run.
