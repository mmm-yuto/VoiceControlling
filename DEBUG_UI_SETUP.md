# デバッグUI設定手順

## 概要
クライアントが描こうとしたときに、その情報（プレイヤーIDと色）を全プレイヤーの画面に表示するデバッグ機能です。

## セットアップ手順

### 1. UI Canvasの準備
1. Hierarchyで `Canvas` を選択（既に存在する場合はスキップ）
2. 存在しない場合は、`UI` → `Canvas` で作成

### 2. デバッグテキストの作成
1. Canvasの子として空のGameObjectを作成（例: `DebugPaintText`）
2. `TextMeshPro - Text (UI)` コンポーネントを追加
   - `GameObject` → `UI` → `TextMeshPro - Text (UI)`
3. `NetworkPaintDebugUI` コンポーネントを追加
   - `Add Component` → `Network Paint Debug UI`

### 3. コンポーネントの設定
1. `NetworkPaintDebugUI` コンポーネントを選択
2. Inspectorで以下を設定:
   - **Debug Text**: 同じGameObjectの `TextMeshProUGUI` コンポーネントをドラッグ&ドロップ
   - **Max Messages**: 表示するメッセージの最大数（デフォルト: 5）
   - **Message Display Time**: メッセージの表示時間（秒）（デフォルト: 3）

### 4. テキストの位置とスタイル設定
1. `TextMeshProUGUI` コンポーネントを選択
2. 位置を調整（例: 画面左上）
   - `RectTransform` で位置を調整
3. フォントサイズや色を調整（必要に応じて）

## 動作確認

### テスト手順
1. ホストとクライアントでゲームを開始
2. クライアント側で描画を試みる
3. 全プレイヤーの画面に以下のようなメッセージが表示される:
   ```
   Client Player 2 tried to paint with color RGB(1.00, 0.00, 1.00)
   ```

### 表示される情報
- **Player ID**: 描画を試みたクライアントのプレイヤーID
- **Color**: RGB値（例: RGB(1.00, 0.00, 1.00)）

## カスタマイズ

### メッセージの表示時間を変更
- `NetworkPaintDebugUI` コンポーネントの `Message Display Time` を変更

### 表示するメッセージ数を変更
- `NetworkPaintDebugUI` コンポーネントの `Max Messages` を変更

### メッセージの形式を変更
- `NetworkPaintDebugUI.cs` の `AddDebugMessage()` メソッドを編集

## 注意事項

- このコンポーネントはデバッグ用です。本番環境では無効化することを推奨します
- メッセージは自動的に削除されます（設定した表示時間後に）
- 複数のメッセージが同時に表示される場合、古いメッセージから順に削除されます
