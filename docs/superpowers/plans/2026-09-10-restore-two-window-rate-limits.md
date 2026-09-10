# 5時間枠・週次枠の2枠対応復元 実装計画

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Codexの`primary_window`（5時間枠）と`secondary_window`（週次枠）をトレイアプリで正しく表示する。

**Architecture:** `UsageState`を5時間枠＋nullable週次枠に戻し、`WhamUsageParser`は必須の5時間枠と任意の週次枠を分離して読む。表示フォーマッター、ポップアップ、ツールチップ、アイコンレンダラーは同じ2枠モデルを受け取り、週次枠がない場合だけ週次表示を空欄・外側リングなしにする。

**Tech Stack:** .NET 8 / C# / WinForms、`System.Text.Json`、GDI+、xUnit。

**Spec:** `docs/superpowers/specs/2026-09-10-restore-two-window-rate-limits-design.md`

### 実行前の保護

実装対象ファイル一覧を次の正規パスとして定義する: `src/CodexRateLimitTray.Core/UsageState.cs`、`src/CodexRateLimitTray.Core/WhamUsageParser.cs`、`src/CodexRateLimitTray.Core/UsageDisplayFormatter.cs`、`src/CodexRateLimitTray.Core/RateLimitIconRenderer.cs`、`src/CodexRateLimitTray/UsagePopupForm.cs`、`src/CodexRateLimitTray/TrayAppContext.cs`、`tests/CodexRateLimitTray.Tests/UsageParsingTests.cs`、`tests/CodexRateLimitTray.Tests/UsageDisplayFormatterTests.cs`、`tests/CodexRateLimitTray.Tests/UsagePopupFormTests.cs`、`tests/CodexRateLimitTray.Tests/IconRendererTests.cs`、`tests/CodexRateLimitTray.Tests/WhamUsageClientTests.cs`。同じPowerShellスコープで`$implementationPaths = @('src/CodexRateLimitTray.Core/UsageState.cs', 'src/CodexRateLimitTray.Core/WhamUsageParser.cs', 'src/CodexRateLimitTray.Core/UsageDisplayFormatter.cs', 'src/CodexRateLimitTray.Core/RateLimitIconRenderer.cs', 'src/CodexRateLimitTray/UsagePopupForm.cs', 'src/CodexRateLimitTray/TrayAppContext.cs', 'tests/CodexRateLimitTray.Tests/UsageParsingTests.cs', 'tests/CodexRateLimitTray.Tests/UsageDisplayFormatterTests.cs', 'tests/CodexRateLimitTray.Tests/UsagePopupFormTests.cs', 'tests/CodexRateLimitTray.Tests/IconRendererTests.cs', 'tests/CodexRateLimitTray.Tests/WhamUsageClientTests.cs')`と`$documentationPaths = @('docs/superpowers/specs/2026-09-10-restore-two-window-rate-limits-design.md', 'docs/superpowers/plans/2026-09-10-restore-two-window-rate-limits.md')`を定義する。`git rev-parse --show-toplevel`を実行して直後に`$LASTEXITCODE`を確認し、失敗時は中断して正規化したrepo rootを保存する。各baseline取得のgitコマンドも直後に終了コードを確認し、失敗時は中断する。実装・テストを開始する前に`$preexistingDotnetCli = Test-Path -LiteralPath (Join-Path (Get-Location) '.dotnet-cli')`、`$initialStaged = @(git diff --cached --name-only)`、`$initialTargetChanges = @(git diff --name-only -- $implementationPaths)`、`$initialUntrackedTargets = @(git ls-files --others --exclude-standard -- $implementationPaths)`、`$initialDocumentationChanges = @(git diff --name-only -- $documentationPaths)`、`$initialDocumentationUntracked = @(git ls-files --others --exclude-standard -- $documentationPaths)`、`$initialStatus = @(git status --porcelain=v1 --untracked-files=all)`を保存する。`$initialStaged`、`$initialTargetChanges`、`$initialUntrackedTargets`のいずれかが空でない場合は、既存変更を保護するため実装を開始せず中断する。今回の計画で作成したspec/planの2正規パスだけを`$taskOwnedDocumentationPaths`として明示し、それらの初期未追跡状態はstage可能なtask-owned baselineとして記録する。それ以外の初期未追跡documentation、trackedな初期documentation差分、または`$initialStatus`から許可したtask-owned documentationPaths以外の未追跡ファイルが見つかった場合は保護対象として中断する。以後の対象テスト・全テスト・ビルド・cleanup・stageはこのスコープ内で順番に実行し、外部コマンドは直後に`$LASTEXITCODE`を確認する。期待されたRED以外でテスト、ビルド、cleanup、stageのいずれかが失敗した場合は、原因記録と安全なcleanup以外の後続処理（stage/commit）へ進まない。

現在の作業を再開する場合も、`$preexistingDotnetCli`が未定義または`[bool]`以外ならcleanupを実行しない。`git rev-parse --show-toplevel`の正規化結果と現在の作業ツリーの正規化絶対パスが一致することを確認し、作業対象パスはその作業ツリー直下の`.dotnet-cli`に解決できることを確認する。ファイル、別ディレクトリ、junction、symlinkなどのReparsePointは対象にしない。

---

## Chunk 1: データモデルとレスポンスパーサー

### Task 1: 2枠レスポンスの回帰テストを先に追加する

**Files:**
- Modify: `tests/CodexRateLimitTray.Tests/UsageParsingTests.cs`

- [ ] **Step 1: 現行APIに対する失敗テストを追加する**

  `primary_window` を使用率25.5%、`secondary_window` を使用率80%として、週次枠が`secondary_window`から取得されることを検証する。現在のパーサーは`primary_window`を週次枠に入れるため、このテストは既存実装でアサーション失敗になる。

- [ ] **Step 2: 2枠モデルを前提に既存テストを更新する**

  `UsageState.Success`の生成を`Success(fiveHour, week)`へ更新し、5時間枠の`ResetText`、週次枠の`WeekResetText`、残り率クランプをそれぞれ検証する。`used_percent: -10`は残り`100%`、`used_percent: 125`は残り`0%`になることを明示的に検証する。テストデータの`reset_at`はUTC固定値を使い、東京タイムゾーン変換も1ケース残す。

- [ ] **Step 3: 欠落・不正レスポンスのテストを追加する**

  次のケースを`UsageErrorKind.InvalidResponse`または部分成功として明示する。

  - `secondary_window: null`
  - `secondary_window`キー欠落
  - `primary_window`の欠落、null、型違い
  - `secondary_window`の型違い、必須フィールド欠落
  - malformed JSON、トップレベル配列・文字列・null
  - `used_percent`の文字列・真偽値・非有限値
  - `reset_at`の整数範囲外、浮動小数値

  `secondary_window`のnull/欠落だけは`FiveHour`を保持し、`Week == null`かつエラーなしとする。それ以外のmalformed JSON、型違い、必須キー欠落、非有限値、時刻範囲外はすべて`InvalidResponse`とする。

- [ ] **Step 4: 失敗テストを実行する**

  Chunk 1の最初のdotnet実行より前に、以後の対象テスト・全テスト・ビルド・cleanupまで同じPowerShellスコープで`$preexistingDotnetCli = Test-Path .dotnet-cli`を記録する。Chunk 1/2/3のRun項目は別々のシェルに分けず、このスコープ内で順番に実行する。

  Run: `$preexistingDotnetCli = Test-Path .dotnet-cli; $env:DOTNET_CLI_HOME = (Join-Path (Get-Location) '.dotnet-cli'); dotnet test tests/CodexRateLimitTray.Tests/CodexRateLimitTray.Tests.csproj --configuration Release --filter FullyQualifiedName~UsageParsingTests`

  Expected: 新しい`FiveHour`/`Week`契約と`Success(fiveHour, week)`シグネチャがまだ存在しないため、初回は意図した契約差分によるコンパイル失敗、または既存APIだけを使うREDケースのアサーション失敗になる。これは実装前のTDD RED確認であり、最終GREEN判定とは別扱いにする。NuGet復元が必要な場合は、同じコマンドをネットワーク許可付きで再実行する。型名解決不能以外の無関係なコンパイルエラーは修正してから続行する。

### Task 2: 2枠の状態モデルとパーサーを実装する

**Files:**
- Modify: `src/CodexRateLimitTray.Core/UsageState.cs`
- Modify: `src/CodexRateLimitTray.Core/WhamUsageParser.cs`
- Test: `tests/CodexRateLimitTray.Tests/UsageParsingTests.cs`

- [ ] **Step 1: `UsageState`を2枠契約へ変更する**

  `FiveHour`を必須の`UsageWindow`、`Week`を`UsageWindow?`にする。`Success`は両方を受け取り、`Error`は5時間枠のダミー値と`Week = null`を持つ。`UsageWindow`の残り率・文字列フォーマットは既存のInvariantCulture実装を維持する。

  モデル変更後、`rg -n "UsageState\.Success|\.Week|FiveHour" src tests`（`rg`が利用できない場合は`git grep -n`またはPowerShellの`Select-String`）で全呼び出しを検索する。表示系実装と既存テストを、少なくとも新しい2引数`Success(fiveHour, week)`でコンパイルでき、`Week=null`を安全に扱う状態へ移行してから、`dotnet build tests/CodexRateLimitTray.Tests/CodexRateLimitTray.Tests.csproj --configuration Release`を実行する。直後の`$LASTEXITCODE`が0でなければTask 2 Step 4へ進まず停止する。これは表示仕様の本実装前に必要なコンパイル移行ゲートであり、Task 3/4の2行・2重リング実装とは分離する。

- [ ] **Step 2: `WhamUsageParser`でprimary/secondaryを分離する**

  `primary_window`を`FiveHour`として必須読込する。`secondary_window`はキーがない、またはJSON nullならnull、それ以外はウィンドウとして読込する。JSONルート・`rate_limit`・必須フィールドの型不正は`InvalidResponse`へ分類する。週次枠が存在して不正なら、5時間枠が読めていても全体を`InvalidResponse`とする。

- [ ] **Step 3: 数値と時刻の境界を守る**

  `used_percent`は有限値を受け取り、既存の残り率計算およびリング描画で0〜100にクランプする。`1e999`のように`double.IsFinite`がfalseになるJSON数値は`InvalidResponse`とする。文字列・真偽値は`GetDouble`前の型検証で不正とする。`reset_at`はUnix秒整数として読み、`GetInt64`の形式・オーバーフロー例外と`DateTimeOffset.FromUnixTimeSeconds`の`ArgumentOutOfRangeException`を`InvalidResponse`へ変換する。実装するcatchフィルターは `ex is JsonException or KeyNotFoundException or InvalidOperationException or FormatException or OverflowException or ArgumentOutOfRangeException` とし、この集合だけを`InvalidResponse`へマッピングし、それ以外の予期しない例外は握りつぶさない。補助フィールドは読み飛ばす。

- [ ] **Step 4: パーサーのテストを再実行する**

  Run: `$env:DOTNET_CLI_HOME = (Join-Path (Get-Location) '.dotnet-cli'); dotnet test tests/CodexRateLimitTray.Tests/CodexRateLimitTray.Tests.csproj --configuration Release --filter FullyQualifiedName~UsageParsingTests`

  Expected: `UsageParsingTests`が全件PASS。

- [ ] **Step 5: 変更を確認する**

  Run: `git diff --check`

  Expected: 出力なし。

---

## Chunk 2: 表示・ポップアップ・アイコン

### Task 3: 表示契約の回帰テストを追加する

**Files:**
- Modify: `tests/CodexRateLimitTray.Tests/UsageDisplayFormatterTests.cs`
- Modify: `tests/CodexRateLimitTray.Tests/UsagePopupFormTests.cs`
- Modify: `tests/CodexRateLimitTray.Tests/IconRendererTests.cs`

- [ ] **Step 1: 表示テストを2行仕様へ更新する**

  `UsageDisplayLines`の`FiveHour`と`Week`を検証し、5時間は`HH:mm`、週次は`MM/dd HH:mm`、InvariantCultureの完全一致文字列を固定する。`reset_at = 1715781600`（UTC 2024-05-15 14:00）を`TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time")`へ変換した5時間表示が`23:00`、`reset_at = 1716094800`（UTC 2024-05-19 05:00）の週次表示が`05/19 14:00`になることを検証する。週次枠なしでは`Week`行が空文字になることを検証する。

  期待する完全一致文字列は、使用率25%/80%の場合の`5時間 : 残り 75%       23:00`と`週   : 残り 20% 05/19 14:00`である。

- [ ] **Step 2: ツールチップの完全一致テストを追加する**

  両枠ありで`Codexレート制限 : 75% / 20%`、週次枠なしで`Codexレート制限 : 75% / -`になることを検証する。現在の単一枠期待値は置き換える。

- [ ] **Step 3: ポップアップの2行テストを追加する**

  入力を`UsageState.Success(new UsageWindow(6, new DateTimeOffset(2026, 5, 17, 18, 48, 0, TimeSpan.Zero)), new UsageWindow(1, new DateTimeOffset(2026, 5, 24, 13, 48, 0, TimeSpan.Zero))`に固定する。上段`Top=192`の6ラベルを左端`12, 58, 70, 104, 156, 214`に配置し、文字列を順に`5時間`, `:`, `残り`, `94%`, `""`, `18:48`とする。下段`Top=220`は`週`, `:`, `残り`, `99%`, `05/24`, `13:48`とする。各ラベルの幅・右端が既存のポップアップ幅281px内に収まることも検証する。週次枠なしでは下段6ラベルが空文字で、エラー表示にならないことを検証する。最大時刻`23:59`、100%表示、InvariantCulture以外のCurrentCultureも確認する。CurrentCultureを変更するテストは`try/finally`で元のカルチャーを必ず復元する。

  右端の期待値は各ラベルの`Right <= form.ClientSize.Width - 10`とし、週次欠落時は下段6ラベルすべての`Text == ""`を確認する。

  ClientSizeは`281x260`、上段`Top=192`・下段`Top=220`の22px行とし、両行の各ラベルが`Bottom <= form.ClientSize.Height`となることも確認する。

- [ ] **Step 4: アイコンの2重リングテストを追加する**

  外側リングが週次使用率、内側リングが5時間使用率を使うこと、週次枠なしでは内側だけになることを検証する。画像サイズは314px、座標は0始まり、既存の`AssertColorNear`許容値を使う。50%の外側リングについて、内径半径100pxの外側にある12時寄り点`(170,32)`と3時点`(282,157)`が使用色、9時点`(32,157)`が透明になることを検証する。内側リングは25% fixtureとして半径100px未満の12時寄り点`(165,82)`、時計回り扇形内の`(220,130)`、9時点`(82,157)`で別の5時間使用率を検証する。0%、100%、範囲外クランプ、既存のテーマ色・週次進捗針のテストも維持する。

  外側は週次50%で`OuterRingColor`、内側は5時間25%でテーマ別`InnerRingColor`を使う。外側サンプルは`(170,32)`・`(282,157)`が使用色、`(32,157)`が透明、内側サンプルは`(165,82)`・`(220,130)`が使用色、`(82,157)`が透明であることを、314px画像の0始まり座標で確認する。

  通常の既存アイコンfixtureはすべて両枠ありの`Success(fiveHour, week)`へ更新し、週次進捗針のfixtureも週次リセット時刻を持つ状態にする。週次枠なしの外側リングなしは専用の`Week = null` fixtureで検証する。

- [ ] **Step 5: 表示テストを実行してREDを確認する**

  Run: `$env:DOTNET_CLI_HOME = (Join-Path (Get-Location) '.dotnet-cli'); dotnet test tests/CodexRateLimitTray.Tests/CodexRateLimitTray.Tests.csproj --configuration Release --filter "FullyQualifiedName~UsageDisplayFormatterTests|FullyQualifiedName~UsagePopupFormTests|FullyQualifiedName~IconRendererTests"`

  Expected: 2枠の表示・リング期待値が現行の単一枠実装に対して失敗する。これは実装前のTDD RED確認であり、Task 4後のGREEN判定とは別扱いにする。

### Task 4: 表示・アイコンを2枠モデルへ戻す

**Files:**
- Modify: `src/CodexRateLimitTray.Core/UsageDisplayFormatter.cs`
- Modify: `src/CodexRateLimitTray/UsagePopupForm.cs`
- Modify: `src/CodexRateLimitTray.Core/RateLimitIconRenderer.cs`
- Test: `tests/CodexRateLimitTray.Tests/UsageDisplayFormatterTests.cs`
- Test: `tests/CodexRateLimitTray.Tests/UsagePopupFormTests.cs`
- Test: `tests/CodexRateLimitTray.Tests/IconRendererTests.cs`

- [ ] **Step 1: フォーマッターを2枠に戻す**

  `UsageDisplayLines(string FiveHour, string Week)`を復元する。5時間行はリセット時刻のみ、週次行は日付＋時刻を使い、`Week == null`なら空文字を返す。ツールチップは指定された2枠テンプレートを使う。

- [ ] **Step 2: ポップアップを固定2行へ戻す**

  2行分の高さ、2つのラベル行、5時間／週の列配置を復元する。週次枠がnullの場合は週次行の6ラベルを空文字にし、エラー表示は出さない。通常のエラー状態では既存の2行エラー表示を維持する。

  ClientSizeは`281x260`、上段`Top=192`、下段`Top=220`とし、2行の下端がClientSize内に収まるようにする。

- [ ] **Step 3: 2重リングと針を復元する**

  `RingGeometry`とテーマ別内側色を復元し、キャンバス314pxでは外径314px、内径200px（`outerDiameter * 200 / 314`）の中央配置にする。外側色は`#339CFF`、ライト内側色は`#1A1C1F`、ダーク内側色は`#FFFFFF`、針は`#FF0000`とする。外側=週次、内側=5時間の順で描画し、扇形は使用率を12時開始・時計回りで塗り、0〜100にクランプする。週次枠がない場合は外側リングと針を描画しない。

- [ ] **Step 4: 表示テストをGREENにする**

  Run: `$env:DOTNET_CLI_HOME = (Join-Path (Get-Location) '.dotnet-cli'); dotnet test tests/CodexRateLimitTray.Tests/CodexRateLimitTray.Tests.csproj --configuration Release --filter "FullyQualifiedName~UsageDisplayFormatterTests|FullyQualifiedName~UsagePopupFormTests|FullyQualifiedName~IconRendererTests"`

  Expected: 対象テスト全件PASS。

- [ ] **Step 5: 変更を確認する**

  Run: `git diff --check`

  Expected: 出力なし。

---

## Chunk 3: 統合検証と引き渡し

### Task 5: クライアント統合と全テストを確認する

**Files:**
- Modify if required: `tests/CodexRateLimitTray.Tests/WhamUsageClientTests.cs`
- Modify if required: `tests/CodexRateLimitTray.Tests/UsageParsingTests.cs`
- Modify if required: `tests/CodexRateLimitTray.Tests/UsageDisplayFormatterTests.cs`
- Modify if required: `tests/CodexRateLimitTray.Tests/UsagePopupFormTests.cs`
- Modify if required: `tests/CodexRateLimitTray.Tests/IconRendererTests.cs`
- Verify: `src/CodexRateLimitTray/TrayAppContext.cs`

- [ ] **Step 1: HTTPクライアントの2枠fixtureを確認する**

  `WhamUsageClientTests`の成功レスポンスfixtureに5時間・週次の両ウィンドウを入れ、Bearer認証とエンドポイント検証を維持する。取得結果が成功し、`FiveHour`と`Week`の使用率・リセット時刻をfixtureどおり保持していることもassertする。クライアント自身に枠の意味付けロジックを追加しない。

- [ ] **Step 2: 呼び出し元のNullable契約を確認する**

  全消費箇所について、`UsageDisplayFormatter`は週次行を空文字・ツールチップを`-`、`UsagePopupForm`は週次6ラベルを空文字、`RateLimitIconRenderer`は内側リングのみ・週次針なしとなることを確認する。

  `TrayAppContext`が取得成功状態をそのまま表示へ渡し、週次nullableを直接参照していないことを確認する。必要な場合だけコンパイルエラーを解消する。

  あわせて`rg -n "UsageState|\.Week|FiveHour" src tests`で全消費箇所を検索し、nullableの未処理アクセスがないことを確認する。`rg`が利用できない場合は`git grep -n`またはPowerShellの`Select-String -Path src\*,tests\* -Pattern 'UsageState|\.Week|FiveHour'`へフォールバックする。

- [ ] **Step 3: 全テストを実行する**

  Run: `$env:DOTNET_CLI_HOME = (Join-Path (Get-Location) '.dotnet-cli'); dotnet test CodexRateLimitTray.sln --configuration Release`

  Expected: exit code 0、全テストPASS。NuGet復元でネットワークエラーになる場合は同じコマンドをネットワーク許可付きで再実行し、なお失敗する場合は原因を最終報告に記載する。

- [ ] **Step 4: リリースビルドを確認する**

  Run: `$env:DOTNET_CLI_HOME = (Join-Path (Get-Location) '.dotnet-cli'); dotnet build CodexRateLimitTray.sln --configuration Release --no-restore`

  Expected: exit code 0、ビルドエラーなし。

- [ ] **Step 5: 作業ツリーと差分を確認する**

  検証用に作成した作業ツリー直下の`.dotnet-cli`が存在する場合は、内容を確認したうえでその一時ディレクトリだけを削除し、他のユーザーファイルや既存変更は削除しない。

  cleanupを差分確認より先に実行する。`$preexistingDotnetCli`が未定義または`[bool]`でない場合はthrowしてcleanup・差分確認・stageへ進まない。`git rev-parse --show-toplevel`の正規化結果と現在の作業ツリーの正規化絶対パスが一致しない場合もthrowする。`$dotnetCliPath = [System.IO.Path]::GetFullPath((Join-Path (Get-Location) '.dotnet-cli'))`が作業ツリー直下の期待パスと完全一致し、`Test-Path -LiteralPath $dotnetCliPath -PathType Leaf -ErrorAction Stop`がfalseで、存在時の`Get-Item -LiteralPath $dotnetCliPath -Force -ErrorAction Stop`のAttributesに`ReparsePoint`が含まれないことを確認する。そのうえで`if ($preexistingDotnetCli -eq $false -and (Test-Path -LiteralPath $dotnetCliPath -PathType Container -ErrorAction Stop)) { Get-ChildItem -Force -LiteralPath $dotnetCliPath -ErrorAction Stop | Select-Object -First 20; Remove-Item -LiteralPath $dotnetCliPath -Recurse -Force -ErrorAction Stop; if (Test-Path -LiteralPath $dotnetCliPath -ErrorAction Stop) { throw '.dotnet-cli cleanup failed' } }`を使い、今回作成したディレクトリだけを削除する。既存ディレクトリなら削除しない。cleanupで例外が出た場合は差分確認・stage・commitへ進まない。

  Run: `git status --short; git diff --stat; git diff --check; git diff --cached --check`

  Expected: 意図したソース・テスト・設計/計画ファイルだけが変更され、作業ツリーとindexの空白エラーがない。未追跡の設計/計画ファイルはstage後のcachedチェックで検証する。テストまたはビルドが失敗している場合はこの差分確認後に停止し、stage/commitしない。

- [ ] **Step 6: コミットを試みる**

  コミット前に、実装前に保存した`$initialStaged`、`$initialTargetChanges`、`$initialUntrackedTargets`、`$initialDocumentationChanges`、`$initialDocumentationUntracked`を再表示してbaselineを確認し、`git status --porcelain=v1 --untracked-files=all`の現在値から既存baselineを除いた変更集合が許可パス集合（`$implementationPaths + $documentationPaths`）の部分集合であることをブロッキング条件として検証する。`$currentChanges`から、実装対象で実際に変更されたパスと、task-ownedで`$initialDocumentationChanges`に含まれないdocumentationPathsだけを`$stagePaths`として計算する。初期未追跡のtask-owned documentationPathsはこの計画で作成したものとしてstage対象に含め、task-ownedでない初期未追跡またはtracked初期差分のdocumentationPathsはstage対象から除外する。stage対象はこの実変更集合だけで、未変更ファイルを固定的に要求しない。テスト・ビルド・cleanupが成功していない場合はここへ進まない。`$stagePaths`をワイルドカードなしの明示的な引数として`git add -- $stagePaths`し、直後の`$LASTEXITCODE`が0でなければcommitせず停止する。`git diff --cached --name-only`が計算済みの`$stagePaths`と完全一致し、baselineとしてstage対象外にした既存変更を含まないこと、`git diff --cached --check`が空であることをブロッキング条件として確認する。各gitコマンドの終了コードが0でない場合や検証条件に違反する場合はcommitへ進まない。.git書き込み権限、フック、identity、merge stateなどの理由でコミットできない場合は、staged変更を勝手に破棄せず、失敗理由とstaged状態を報告する。

  `$stagePaths`に含められる正規パスは、src/CodexRateLimitTray.Core/UsageState.cs、src/CodexRateLimitTray.Core/WhamUsageParser.cs、src/CodexRateLimitTray.Core/UsageDisplayFormatter.cs、src/CodexRateLimitTray.Core/RateLimitIconRenderer.cs、src/CodexRateLimitTray/UsagePopupForm.cs、src/CodexRateLimitTray/TrayAppContext.cs、tests/CodexRateLimitTray.Tests/UsageParsingTests.cs、tests/CodexRateLimitTray.Tests/UsageDisplayFormatterTests.cs、tests/CodexRateLimitTray.Tests/UsagePopupFormTests.cs、tests/CodexRateLimitTray.Tests/IconRendererTests.cs、tests/CodexRateLimitTray.Tests/WhamUsageClientTests.cs、docs/superpowers/specs/2026-09-10-restore-two-window-rate-limits-design.md、docs/superpowers/plans/2026-09-10-restore-two-window-rate-limits.mdだけとする。`TrayAppContext.cs`は実際の変更集合に含まれる場合だけ、既存baselineのdocumentationPathsはstage対象から除外する。コミット失敗時はindexを保持し、git status --shortとgit diff --cached --name-onlyの結果を報告する。

  `TrayAppContext.cs`を実際に変更した場合は、上記の対象一覧と`git add`コマンドへ src/CodexRateLimitTray/TrayAppContext.cs を追加する。その他の既存変更はstageしない。

  Run: まず`git diff --cached --name-only`が空であることを確認し、現在の実変更集合からbaselineを除いて計算した`$stagePaths`だけに対して、ワイルドカードなしの`git add -- $stagePaths`を実行する。次に`git diff --cached --name-only`が計算済み`$stagePaths`と完全一致すること、`git diff --cached --check`が空であることを検証する。許可パス外、baseline既存差分、または未変更パスが含まれる場合はコミットしない。検証後に`git commit -m "5時間枠と週次枠の表示を復元"`を実行する。

  Expected: 日本語コミットが作成される。`.git`の書き込み権限が引き続き拒否される場合はコミットせず、変更内容と権限エラーを報告する。
