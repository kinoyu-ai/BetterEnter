# 更新履歴

## 1.4.1

- IME変換中も例外にせず、Enterを改行、Ctrl+Enterを送信として処理するように変更しました。

## 1.4.0

- Chrome拡張はChatGPTの送信処理より先にEnterを止め、入力欄へ直接改行を挿入する方式に変更しました。
- Ctrl+Enterは入力欄の送信ボタンを実行するため、Ctrl+Enterのときだけ送信されます。
- WindowsアプリはChatGPTの入力欄をUI Automationで識別できない場合にもキー変更が働くようにしました。
- IME変換中のEnterは変更せず、変換確定に使えるようにしました。

## 1.3.0

- 製品名を BeterEnter に統一しました。
- 「Better↩」ロゴを追加しました。Windowsの通知領域では一時停止状態もアイコンで表示します。
- Chrome拡張とWindowsアプリで、Enterは改行、Ctrl+Enterは通常のEnterとして動作します。
- Windowsアプリは通知領域に常駐し、右クリックメニューから一時停止・再開・終了できます。

## 1.2.0

- WindowsアプリとChrome拡張に専用アイコンを追加しました。
- 一時停止中も通知領域にアイコンを表示するようにしました。

## 1.1.0

- ChatGPTの入力欄でEnterを改行、Ctrl+Enterを通常のEnterとして扱うようにしました。
- WindowsアプリとChrome拡張を追加しました。
