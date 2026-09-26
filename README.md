# BeterEnter

<picture><source media="(prefers-color-scheme: dark)" srcset="assets/logo-dark.svg"><img src="assets/logo.svg" width="320" height="90" alt="Better↩ — BeterEnter"></picture>

ChatGPTで **Enterを改行、Ctrl+Enterを送信**にするツールです。Windowsアプリ版とChrome拡張版があります。OpenAI公式の製品ではありません。

| キー | ChatGPTでの動作 |
| --- | --- |
| Enter | 改行 |
| Shift+Enter | 改行 |
| Ctrl+Enter | Enterとして動作（通常は送信） |

変換確定にもCtrl+Enterを使う想定です。IMEの変換中かどうかは判定せず、キーをChatGPTとIMEに渡します。IMEやアプリの組み合わせによって動作が異なるため、最初に短い文章で確認してください。

## ダウンロード

[GitHub Releases](https://github.com/kinoyu-ai/BetterEnter/releases) から利用する版のZIPを取得します。リリースが表示されない場合、配布ファイルはまだ公開されていません。

### Windowsアプリ

1. Windows x64版ZIPを展開します。
2. 展開先にある `BeterEnter.exe` を起動します。
3. アプリは通知領域に常駐します。アイコンを右クリックすると一時停止や終了ができます。

Windows 10 / 11に対応します。SmartScreenが警告を表示する場合があります。実行ファイルは未署名です。

### Chrome拡張

1. Chrome版ZIPを展開します。
2. `chrome://extensions` を開き、「デベロッパー モード」を有効にします。
3. 「パッケージ化されていない拡張機能を読み込む」を選び、展開したフォルダー内の `chrome-extension` フォルダーを指定します。
4. ChatGPTのページを再読み込みします。

Chrome 111以降に対応します。拡張機能は `chatgpt.com` と `chat.openai.com` の入力欄だけで動作します。

## 対象とプライバシー

Windowsアプリ版はChatGPTのWindowsアプリ、Chrome拡張版はChrome上のChatGPTが対象です。Macには対応していません。検索欄や他のアプリ・サイトには適用されません。

会話やキー入力を収集・送信しません。Chrome版は有効／無効の設定を保存します。Windows版は入力欄の識別に必要な設定を、必要な場合に限りPC内へ保存します。

## 削除

Chrome版は `chrome://extensions` から削除します。Windows版は通知領域のアイコンから終了して、展開したフォルダーを削除します。自動起動を有効にしている場合は、終了前にオフにしてください。

## ライセンスと問い合わせ

ソースコードとロゴは [MIT License](LICENSE) です。ロゴに使用したInterフォントのライセンスは [SIL Open Font License](assets/fonts/OFL.txt) です。質問や不具合は[Issues](https://github.com/kinoyu-ai/BetterEnter/issues)へお寄せください。
