# BeterEnter

<picture><source media="(prefers-color-scheme: dark)" srcset="assets/logo-dark.svg"><img src="assets/logo.svg" width="320" height="90" alt="Better↩ — BeterEnter"></picture>

ChatGPTで **Enterを改行、Ctrl+Enterを送信**にするツールです。Windowsアプリ版とChrome拡張版があります。OpenAI公式の製品ではありません。

| キー | ChatGPTでの動作 |
| --- | --- |
| Enter | 改行 |
| Shift+Enter | 改行 |
| Ctrl+Enter | 送信 |

IME変換中のEnterは変更しないため、通常どおり変換確定に使えます。変換確定後、Ctrl+Enterで送信します。

## ダウンロードと導入

**現在、GitHub Releasesに実行用ZIPは公開されていません。** このページからダウンロードできるのはソースコードです。

[ソースコードをZIPでダウンロード](https://github.com/kinoyu-ai/BetterEnter/archive/refs/heads/main.zip)

### Chrome拡張

1. ソースZIPを展開します。
2. Chromeで `chrome://extensions` を開き、「デベロッパー モード」を有効にします。
3. 「パッケージ化されていない拡張機能を読み込む」を選び、展開先にある `chrome-extension` フォルダーを指定します。
4. ChatGPTのページを再読み込みします。

Chrome 111以降が必要です。拡張機能は `chatgpt.com` と `chat.openai.com` の入力欄だけで動作します。Chromeが動作するWindows・macOS・Linuxで利用できます。

### Windowsアプリ

リポジトリには完成済みのEXEは含まれていません。Windows 10 / 11（64 bit）でソースから作成できます。

1. 上記のソースZIPを展開します。
2. 展開したフォルダーでPowerShellを開き、次を実行します。

   ```powershell
   powershell -NoProfile -ExecutionPolicy Bypass -File .\build-package.ps1
   ```

3. `dist` に作成される `BeterEnter-v*-windows-x64.zip` を展開し、中の `BeterEnter.exe` を起動します。

ビルドにはWindows付属の.NET Framework 4.xが必要です。起動後は通知領域に常駐し、アイコンを右クリックすると一時停止や終了ができます。EXEは未署名のため、Windows SmartScreenが警告を表示する場合があります。

## 対象とプライバシー

Windowsアプリ版はChatGPTのWindowsアプリが前面にある間だけ動作します。Chrome拡張版はChrome上のChatGPTの入力欄だけが対象です。他のアプリやサイトには適用しません。

会話やキー入力を収集・送信しません。Chrome版は有効／無効の設定を保存します。Windows版は入力欄の識別に必要な設定を、必要な場合に限りPC内へ保存します。

## 削除

Chrome版は `chrome://extensions` から削除します。Windows版は通知領域のアイコンから終了して、展開したフォルダーを削除します。自動起動を有効にした場合は、終了前にオフにしてください。

## ライセンスと問い合わせ

ソースコードとロゴは [MIT License](LICENSE) です。ロゴに使用したInterフォントのライセンスは [SIL Open Font License](assets/fonts/OFL.txt) です。質問や不具合は[Issues](https://github.com/kinoyu-ai/BetterEnter/issues)へお寄せください。
