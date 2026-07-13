# Codex レート制限

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

### WinGet manifest の更新

`wingetcreate` を使うと、manifest の生成、SHA256 の計算、`winget-pkgs` への Pull Request 作成まで自動化できます。初回だけインストールと GitHub 認証を行います。

```powershell
winget install wingetcreate
wingetcreate token -s
```

GitHub Release の作成後、リリースしたバージョンと installer URL を指定して実行します。

```powershell
wingetcreate update WalkingWiFi.CodexRateLimitTray `
  -u "https://github.com/walkingwifi28/codex-rate-limit-tray-win/releases/download/v0.1.4/CodexRateLimitTray-0.1.4-win-x64-setup.exe" `
  -v 0.1.4 `
  --submit
```

次回以降は URL と `-v` のバージョン番号を対象のリリースに合わせて変更します。

### Code signing

このプロジェクトの GitHub Releases で公開する installer は、現時点では未署名です。

未署名 installer のため、Windows Defender SmartScreen で警告が表示される場合があります。配布物の改ざん確認には、GitHub Release に添付される `.sha256` と winget manifest の `InstallerSha256` を利用します。

配布方針は [CODE_SIGNING_POLICY.md](CODE_SIGNING_POLICY.md) に記載しています。将来コード署名証明書を利用できる状態になった場合は、release workflow を署名済み installer の公開に戻す予定です。

## License

[MIT](LICENSE) © [@walkingwifi28](https://github.com/walkingwifi28)
