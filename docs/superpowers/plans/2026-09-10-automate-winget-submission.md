# Automate WinGet Submission Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Submit a validated WinGet manifest pull request automatically after each valid release tag.

**Architecture:** Extend the existing tag-triggered release workflow. The current `build` job remains responsible for tests and publishing the GitHub Release; a dependent `submit-winget` job validates the tag and asset, prevents duplicate package/version PRs, then runs a pinned `wingetcreate` with the Actions secret token.

**Tech Stack:** GitHub Actions, Windows PowerShell, `actions/setup-dotnet@v4`, WinGet-Create 1.12.13.0, GitHub CLI, GitHub Actions secrets.

---

## Chunk 1: Release workflow automation

### Task 1: Add release validation and safe serialization

**Files:**
- Modify: `C:\Users\atush\source\repos\codex-rate-limit-tray-win\.github\workflows\release.yml`

- [ ] Add workflow concurrency keyed by the release ref with `cancel-in-progress: false`, so a retry cannot overlap another run for the same tag.
- [ ] Add a first step in the existing `build` job that rejects tags not matching `^v[0-9]+\.[0-9]+\.[0-9]+$`.

### Task 2: Add the dependent WinGet submission job

**Files:**
- Modify: `C:\Users\atush\source\repos\codex-rate-limit-tray-win\.github\workflows\release.yml`

- [ ] Add `submit-winget` with `needs: build`, `runs-on: windows-latest`, and read-only repository permissions.
- [ ] Install .NET 9.x and download the pinned official `v1.12.13.0/wingetcreate.exe` release.
- [ ] Derive `version` from `GITHUB_REF_NAME`, construct the complete URL `https://github.com/${GITHUB_REPOSITORY}/releases/download/${GITHUB_REF_NAME}/CodexRateLimitTray-${version}-win-x64-setup.exe`, and fail if that Release asset cannot be reached.
- [ ] Use paginated GitHub Search API results for open `microsoft/winget-pkgs` PRs matching `WalkingWiFi.CodexRateLimitTray`; inspect every returned title and body and treat it as a duplicate only when the package ID matches exactly (case-sensitive) and the version appears as an exact token in the title or body. On a match, report the PR number and exit successfully; fail closed if the query itself fails.
- [ ] Require `WINGET_CREATE_GITHUB_TOKEN`, pass it only through the environment, and run `wingetcreate update` with `--urls`, `--version`, `--submit`, and `--no-open`.
- [ ] Propagate a non-zero `wingetcreate` exit code so manifest, checksum, fork-sync, permission, API, and rate-limit failures are visible as failed workflow runs.

### Task 3: Document the required setup

**Files:**
- Modify: `C:\Users\atush\source\repos\codex-rate-limit-tray-win\README.md`

- [ ] Explain that valid tag pushes now submit the WinGet PR automatically.
- [ ] Document the one-time `WINGET_CREATE_GITHUB_TOKEN` repository secret, classic PAT with `public_repo`, and clean `winget-pkgs` fork prerequisite.
- [ ] Keep the manual `wingetcreate` command as a recovery/fallback path and explain that Microsoft review/merge remains separate.

## Chunk 2: Verification

### Task 4: Validate the configuration

**Files:**
- Verify: `C:\Users\atush\source\repos\codex-rate-limit-tray-win\.github\workflows\release.yml`
- Verify: `C:\Users\atush\source\repos\codex-rate-limit-tray-win\README.md`

- [ ] Run `git diff --check`.
- [ ] If available, run `actionlint C:\Users\atush\source\repos\codex-rate-limit-tray-win\.github\workflows\release.yml` and expect exit code 0; otherwise use a PowerShell structural check to confirm one `submit-winget` job, `needs: build`, and balanced workflow blocks are present.
- [ ] Run this PowerShell transformation check and expect `0.2.0` plus `https://github.com/walkingwifi28/codex-rate-limit-tray-win/releases/download/v0.2.0/CodexRateLimitTray-0.2.0-win-x64-setup.exe`:

```powershell
$tag = "v0.2.0"
$version = $tag -replace '^v', ''
$url = "https://github.com/walkingwifi28/codex-rate-limit-tray-win/releases/download/$tag/CodexRateLimitTray-$version-win-x64-setup.exe"
if ($version -ne "0.2.0" -or $url -ne "https://github.com/walkingwifi28/codex-rate-limit-tray-win/releases/download/v0.2.0/CodexRateLimitTray-0.2.0-win-x64-setup.exe") { throw "Unexpected release URL transformation" }
Write-Output "$version $url"
```

- [ ] Perform a structural fail-closed check that the duplicate-PR PowerShell block checks `$LASTEXITCODE` immediately after `gh pr list` and throws before parsing JSON when it is non-zero.
- [ ] Inspect the full final diff unconditionally.
- [ ] Run the existing test suite only if the workflow change affects application code; this change does not.
- [ ] Report that an actual PR submission still requires the repository secret and a synchronized fork, so it cannot be fully exercised locally without external credentials.
