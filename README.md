# Codex レート制限 Tray

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
