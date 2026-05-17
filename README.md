# Codexレート制限トレイ

<p align="center">
  <img src="https://img.shields.io/badge/.NET-8-512BD4?logo=dotnet&logoColor=white" alt=".NET 8" />
  <img src="https://img.shields.io/badge/C%23-239120?logo=csharp&logoColor=white" alt="C#" />
  <img src="https://img.shields.io/badge/WinGet-0078D4?logo=windows11&logoColor=white" alt="WinGet" />
</p>

## Installation

### Quick Start (Recommended)

```powershell
# Recommended
winget install WalkingWiFi.CodexRateLimitTray
```

Windows タスクトレイに Codex/ChatGPT の `wham/usage` 使用率、残り率、リセット時刻を表示する .NET 8 WinForms アプリです。

## Development

```powershell
dotnet test CodexRateLimitTray.sln
dotnet publish src\CodexRateLimitTray\CodexRateLimitTray.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o artifacts\publish\win-x64
```

このリポジトリでは初回リリースを Windows x64 のみ対象にしています。

## Distribution

配布は GitHub Releases 上の Inno Setup installer を winget manifest から参照します。タグ `vX.Y.Z` を push すると GitHub Actions が test、publish、installer 生成、SHA256 生成、GitHub Release 添付を実行します。

winget manifest の `InstallerSha256` はリリースで生成された `.sha256` の値に置き換えてから `winget-pkgs` に提出します。

### Code signing

このプロジェクトは OSS として SignPath Foundation の無料コード署名を利用する方針です。

Free code signing provided by SignPath.io, certificate by SignPath Foundation.

署名ポリシーは [CODE_SIGNING_POLICY.md](CODE_SIGNING_POLICY.md) に記載しています。SignPath Foundation 申請用の情報は [docs/signpath-application.md](docs/signpath-application.md) にまとめています。

GitHub Actions の release workflow は、未署名インストーラを公開せず、SignPath で署名された installer だけを GitHub Release に添付します。SignPath 承認後、次の repository variables と secret を設定します。

- `SIGNPATH_ORGANIZATION_ID`
- `SIGNPATH_PROJECT_SLUG`
- `SIGNPATH_SIGNING_POLICY_SLUG`
- `SIGNPATH_ARTIFACT_CONFIGURATION_SLUG`
- `SIGNPATH_API_TOKEN` repository secret

これらが未設定の場合、release workflow は失敗します。これは未署名の公式リリースを防ぐためです。

## License

[MIT](LICENSE) © [@walkingwifi28](https://github.com/walkingwifi28)
