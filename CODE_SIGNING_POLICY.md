# Code Signing Policy

Codex Rate Limit Tray is an open-source Windows tray application distributed from
the GitHub repository at:

https://github.com/walkingwifi28/codex-rate-limit-tray-win

Free code signing provided by SignPath.io, certificate by SignPath Foundation.

## Project

- Name: Codex Rate Limit Tray
- Package identifier: `WalkingWiFi.CodexRateLimitTray`
- License: MIT
- Source repository: `walkingwifi28/codex-rate-limit-tray-win`
- Primary release channel: GitHub Releases
- Package manager channel: Windows Package Manager manifests in this repository

## Maintainers

The repository owner and maintainers of
`walkingwifi28/codex-rate-limit-tray-win` are responsible for source changes,
build configuration, release tags, and signing requests.

Only maintainers with write access to the repository may create release tags or
request signed release artifacts.

## Signed Artifacts

The project signs only release artifacts produced from this repository:

- `CodexRateLimitTray-<version>-win-x64-setup.exe`

The signed installer is created by the release workflow from a tag named
`vX.Y.Z`. The installer is built with Inno Setup from the published .NET
application output.

Debug builds, local builds, pull request builds, test binaries, and modified
third-party binaries are not signed.

## Build Provenance

Release artifacts must be produced by GitHub Actions from this repository.

The release workflow:

1. Checks out the tagged source revision.
2. Runs the test suite.
3. Publishes the Windows x64 .NET application.
4. Builds the Inno Setup installer.
5. Uploads the unsigned installer as a GitHub Actions artifact.
6. Submits the artifact to SignPath for signing.
7. Publishes only the signed installer and its SHA256 checksum to GitHub
   Releases.

Unsigned installers must not be attached to public releases.

## Release Rules

- Releases are created from version tags matching `v*.*.*`.
- Version numbers in release tags, installers, and winget manifests must match.
- Signed artifacts must not be modified after signing.
- If a signed installer is rebuilt, it must be signed again and receive a new
  checksum.
- The `InstallerSha256` value in the winget manifest must match the signed
  installer attached to the GitHub Release.

## Security Expectations

The project does not use a local private key, PFX certificate, or maintainer
hardware token for public releases. Signing is delegated to SignPath so that the
private key remains protected by SignPath infrastructure.

Maintainers must not attempt to bypass the signing policy by publishing unsigned
installers as official releases.
