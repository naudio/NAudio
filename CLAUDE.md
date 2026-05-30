# Working in NAudio

This file gives AI agents (Claude, etc.) the conventions for contributing to NAudio. Humans should read [README.md](README.md) and [Docs/Architecture/](Docs/Architecture/) instead.

## Orientation

- **NAudio 3 development happens on `main`.** NAudio 2 maintenance happens on `release/2.x`. Default to targeting `main` unless explicitly told otherwise. `main` and `release/*` are protected — all changes go through PRs.
- **Use descriptive branch names.** Cloud agents are often started on an auto-generated branch (e.g. `claude/loving-galileo-Wpz4o`). Do not push that name to the NAudio repo. Before the first push, rename the branch to something that describes the work, referencing an issue or PR number where relevant — e.g. `feature/channel-mixer-sample-provider`, `fix/1234-wasapi-leak`, `docs/release-strategy`. Keep your existing commits; just move them with `git branch -m <new-name>` before `git push -u origin <new-name>`.
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

## Documentation site

Tutorials live in [Docs/](Docs/) as Markdown and are published to a DocFX site on GitHub Pages by [.github/workflows/docs.yml](.github/workflows/docs.yml); the API reference is generated automatically from the source XML doc comments. When you **add a new tutorial**, also add an entry for it in [Docs/toc.yml](Docs/toc.yml) so it appears in the sidebar — a CI check fails the build if any `Docs/*.md` is missing from the TOC (an unlisted page still builds but is orphaned from the navigation). Internal `Docs/Architecture/` docs are excluded from the published site.

## PR labelling

When opening a PR (where you have permission), apply one of: `breaking`, `enhancement`, `bug`, `documentation`. These feed the auto-generated changelog at release time. Use `release-notes-skip` for PRs that should not appear in the changelog at all.

## Versioning

Package versions are centralised in [Directory.Build.props](Directory.Build.props) as `<VersionPrefix>`. Do **not** add a per-csproj `<Version>` to NAudio packages — they're meant to stay in lockstep. The tool/sample apps (MixDiff, AudioFileInspector, MidiFileConverter) keep their own explicit `<Version>` and are exempt.

## Building & testing on Linux

Some cloud and CI environments start without a .NET SDK on the PATH — this isn't a property of the repo but of the host. The default Anthropic cloud-agent sandbox is one example, and other infrastructure (GitHub Copilot agents, fresh CI runners, etc.) may differ now or in the future. So **first check whether `dotnet` is available**; only install it if it isn't. On Debian/Ubuntu, install .NET 10 via apt: add Microsoft's feed with `wget -q https://packages.microsoft.com/config/ubuntu/24.04/packages-microsoft-prod.deb -O /tmp/ms.deb && sudo dpkg -i /tmp/ms.deb && sudo apt-get update`, then `sudo apt-get install -y dotnet-sdk-10.0` (the SDK builds the `net9.0` libraries fine).

Because `NAudio.Core.Tests` references the `NAudio` meta-package, which pulls in Windows-only projects (`NAudio.WinForms`, `NAudio.Wasapi`), you must pass `-p:EnableWindowsTargeting=true` on Linux or the build fails with `NETSDK1100`. Build and run the cross-platform tests with:

```
dotnet build NAudio.Core.Tests/NAudio.Core.Tests.csproj -p:EnableWindowsTargeting=true
dotnet test --project NAudio.Core.Tests/NAudio.Core.Tests.csproj -p:EnableWindowsTargeting=true --filter "TestCategory!=IntegrationTest"
```

The `TestCategory!=IntegrationTest` filter skips tests needing real audio hardware; note that .NET 10's `dotnet test` wants `--project` rather than a positional path.

## CI

Every PR runs build + test on `windows-latest` via [.github/workflows/build.yml](.github/workflows/build.yml). Tests requiring real audio hardware should be marked with `TestCategory=IntegrationTest` so they're filtered out of the headless run.
