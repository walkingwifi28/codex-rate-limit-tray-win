# Code Signing Policy

Codex Rate Limit Tray is an open-source Windows tray application distributed from
the GitHub repository at:

https://github.com/walkingwifi28/codex-rate-limit-tray-win

## Current Signing Status

Release installers are currently unsigned.

The project previously planned to use SignPath Foundation for free open-source
code signing, but the application was not approved. Until another code signing
option is available, official releases publish unsigned Inno Setup installers
with SHA256 checksums.

Unsigned installers may show Windows Defender SmartScreen warnings. Users should
verify that the installer URL points to this repository's GitHub Releases page
and that the SHA256 hash matches the release checksum or the Windows Package
Manager manifest.

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
build configuration, release tags, and release artifacts.

Only maintainers with write access to the repository may create release tags or
publish release artifacts.

## Release Artifacts

The project publishes release artifacts produced from this repository:

- `CodexRateLimitTray-<version>-win-x64-setup.exe`
- `CodexRateLimitTray-<version>-win-x64-setup.exe.sha256`

The installer is created by the release workflow from a tag named `vX.Y.Z`. The
installer is built with Inno Setup from the published .NET application output.

Debug builds, local builds, pull request builds, test binaries, and modified
third-party binaries are not official release artifacts.

## Build Provenance

Release artifacts must be produced by GitHub Actions from this repository.

The release workflow:

1. Checks out the tagged source revision.
2. Runs the test suite.
3. Publishes the Windows x64 .NET application.
4. Builds the Inno Setup installer.
5. Computes the installer SHA256 checksum.
6. Publishes the unsigned installer and its SHA256 checksum to GitHub Releases.

## Release Rules

- Releases are created from version tags matching `v*.*.*`.
- Version numbers in release tags, installers, and winget manifests must match.
- Release artifacts must not be modified after publication.
- If an installer is rebuilt, it must receive a new version tag and checksum.
- The `InstallerSha256` value in the winget manifest must match the installer
  attached to the GitHub Release.

## Future Signing

If a code signing certificate or signing service becomes available, the release
workflow should be updated to publish signed installers. From that point forward,
new releases should document the signing identity and verification steps here.
