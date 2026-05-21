# SignPath Foundation Application Notes

This document is kept as historical reference for a previous attempt to obtain
free open-source code signing through SignPath Foundation.

The application was not approved, so the current release workflow does not use
SignPath and does not require any SignPath repository variables or secrets. See
`CODE_SIGNING_POLICY.md` for the current release artifact policy.

## Project Summary

Codex Rate Limit Tray is a Windows task tray application for displaying
Codex/ChatGPT `wham/usage` rate-limit usage, remaining percentage, and reset
time. It is implemented as a .NET 8 WinForms application.

- Repository: https://github.com/walkingwifi28/codex-rate-limit-tray-win
- License: MIT
- Release artifacts: Windows x64 Inno Setup installer
- Package identifier: `WalkingWiFi.CodexRateLimitTray`
- Maintainer: repository owner `walkingwifi28`

## Why Signing Was Requested

The project distributes a Windows installer through GitHub Releases and
references that installer from Windows Package Manager manifests. Windows
Defender SmartScreen may show an unrecognized application warning for unsigned
installers. Signing would allow users to verify that the installer was produced
by the open-source project and was not modified after release.

## Requested Signing Scope

The requested signing scope was release installers only:

```text
CodexRateLimitTray-<version>-win-x64-setup.exe
```

The project did not request signing for:

- Local developer builds
- Pull request artifacts
- Debug builds
- Third-party binaries
- Modified upstream software

## Future Use

If another free or paid signing option becomes available, this document can be
used as source material for a new application. The release workflow and
`CODE_SIGNING_POLICY.md` should be updated at the same time to document the new
signing process.
