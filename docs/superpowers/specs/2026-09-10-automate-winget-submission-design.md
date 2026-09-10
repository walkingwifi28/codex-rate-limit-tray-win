# WinGet manifest submission automation design

## Goal

When a release tag matching `vX.Y.Z` is pushed, create the GitHub Release as today and then automatically generate and submit the corresponding WinGet manifest update as a pull request to `microsoft/winget-pkgs`.

## Design

- Keep the existing `build` job responsible for tests, publishing, installer creation, checksum generation, and GitHub Release assets.
- Validate `GITHUB_REF_NAME` against `^v[0-9]+\.[0-9]+\.[0-9]+$` before any build or submission work. The existing tag glob remains broad enough for GitHub Actions to receive the event, while the explicit check rejects malformed tags.
- Add a `submit-winget` job that depends on `build`, so the installer URL is available before `wingetcreate` downloads it.
- Derive the package version from `GITHUB_REF_NAME` by removing the leading `v`.
- Construct the exact asset URL `https://github.com/${GITHUB_REPOSITORY}/releases/download/${GITHUB_REF_NAME}/CodexRateLimitTray-${version}-win-x64-setup.exe` and verify that the asset is reachable before invoking `wingetcreate`.
- Download and run the known-compatible `wingetcreate` release `v1.12.13.0` from the official GitHub release. Install the .NET 9 runtime required by the executable on the runner.
- Pass a repository Actions secret named `WINGET_CREATE_GITHUB_TOKEN` through the environment. It must be a classic PAT with `public_repo`, owned by the account that owns the `winget-pkgs` fork. `wingetcreate` consumes the environment variable without placing the token in the command line.
- Before submission, query all matching open `microsoft/winget-pkgs` PRs for this package and exact version. If a matching PR exists, report its number and exit successfully without creating another PR. Serialize runs for the same tag with workflow concurrency.
- Run `wingetcreate update WalkingWiFi.CodexRateLimitTray --urls <asset-url> --version <version> --submit --no-open`; the command fetches the current upstream manifest, generates the three manifest files, validates them, recalculates the installer hash, and opens the WinGet PR.

## Error handling and scope

- Fail the job when the tag is malformed, the secret is missing, the expected Release asset is missing/unreachable, manifest generation/validation fails, the WinGet API rejects the request, or `wingetcreate` returns a non-zero exit code. Preserve the command output in the Actions log without printing the token.
- The WinGet fork must be a normal, clean, synchronized fork owned by the token account. Existing fork divergence, merge conflicts, insufficient permissions, API rate limits, or an already-open PR are actionable failures; the workflow must not delete or rewrite a fork automatically.
- A re-run is allowed after a failed submission when the duplicate check finds no matching PR. If a matching PR already exists, the job reports the PR number and exits successfully; the existing PR is updated or reviewed manually rather than being duplicated or rewritten by this workflow. A failed duplicate-check API call fails closed.
- Microsoft validation, review, and merge remain outside this workflow.
- `wingetcreate` reads the existing manifest from `microsoft/winget-pkgs`; this repository's local `manifests` directory is not used as the submission source.
- The `.sha256` Release asset remains a human-facing integrity artifact from the existing build job. It is not required by the submission job because `wingetcreate` downloads the `.exe` and computes the manifest hash itself.

## Verification

- Validate the workflow YAML with an Actions-aware parser when available, run `git diff --check`, and inspect the final diff.
- Verify the tag-to-version and asset URL transformation with a PowerShell command-level check and verify the duplicate-PR query fails closed.
- Run the existing test suite only if workflow changes affect application code; no application code is changed by this design.
