# Phase 1: コアゲームシステム実装手順

## 🎯 目標
声で画面に色を塗れる基本機能を完成させる

## 📋 実装順序

### Step 1: PaintCanvas システムの作成（最優先）

#### 1.1: インターフェース定義
**ファイル**: `Assets/Script/Interfaces/IPaintCanvas.cs`（新規作成）

**実装内容**:
- 塗りシステムのインターフェースを定義
- 既存の`VoiceScatterPlot`の設計パターンを参考にする

**参考**: 既存のイベント駆動パターン（`VolumeAnalyzer.OnVolumeDetected`など）

#### 1.2: PaintSettings（ScriptableObject）
**ファイル**: `Assets/Script/Data/Settings/PaintSettings.cs`（新規作成）

**実装内容**:
- 塗り強度、更新頻度、テクスチャ解像度を管理
- Inspectorで調整可能にする

**参考**: 既存の`ImprovedPitchAnalyzer`の`[Header]`、`[Range]`、`[Tooltip]`の使い方

#### 1.3: PaintCanvas実装
**ファイル**: `Assets/Script/GameLogic/PaintCanvas.cs`（新規作成）

**実装内容**:
- 2D配列で塗り状態を管理（最初はシンプルに）
- イベント発火（既存の`OnVolumeDetected`パターンを参考）
- 上塗り機能は後から追加（最初は単純に塗るだけ）
- **将来の拡張性**: Phase 2（クリエイティブモード）で使用するため、`ResetCanvas()`メソッドを実装することを推奨（Phase 1では必須ではない）

**参考**: 
- `VoiceScatterPlot`のイベント購読パターン
- `GameManager`の`FindObjectOfType`の使い方（ただし、Inspector接続を推奨）

**Phase 2との関係**:
- Phase 2のクリエイティブモードは、Phase 1で実装する`PaintCanvas`をそのまま使用します
- クリエイティブモードでクリアボタンを使うため、`ResetCanvas()`メソッドがあると便利です（Phase 1では必須ではありませんが、実装しておくとPhase 2でスムーズに進められます）

---

### Step 2: 座標変換システム

#### 2.1: VoiceToScreenMapper作成
**ファイル**: `Assets/Script/GameLogic/VoiceToScreenMapper.cs`（新規作成）

**実装内容**:
- `VoiceScatterPlot`の`MapVolumeTo01`と`MapPitchTo01`を参考に実装
- キャリブレーション平均を原点として使用（`VoiceCalibrator.LastAverageVolume`、`VoiceCalibrator.LastAveragePitch`）
- 画面座標（Screen.width, Screen.height）に変換

**既存コードの参考箇所**:
```csharp
// VoiceScatterPlot.cs の MapVolumeTo01, MapPitchTo01 を参考
// キャリブレーション平均を原点として使用
float zeroVolume = VoiceCalibrator.LastAverageVolume;
float zeroPitch = VoiceCalibrator.LastAveragePitch;
```

**実装方針**:
1. `VoiceScatterPlot`の座標変換ロジックをコピー
2. UI座標（RectTransform）ではなく、画面座標（Screen座標系）に変換
3. キャリブレーション平均を原点として使用（既存の実装をそのまま活用）

---

### Step 3: 基本的なゲームマネージャー統合（最小限版）

#### 3.1: PaintBattleGameManager作成
**ファイル**: `Assets/Script/GameLogic/PaintBattleGameManager.cs`（新規作成）

**実装内容**:
- 既存の`VoiceController`や`GameManager`のパターンを参考
- 音声検出 → 座標変換 → 塗り処理の流れを実装
- 最初は攻撃タイプは後回し（常に同じタイプで塗る）

**既存コードの参考箇所**:
```csharp
// VoiceController.cs のパターン
private ImprovedPitchAnalyzer improvedPitchAnalyzer;
private VolumeAnalyzer volumeAnalyzer;

// イベント購読パターン
improvedPitchAnalyzer.OnPitchDetected += OnPitchDetected;
volumeAnalyzer.OnVolumeDetected += OnVolumeDetected;
```

**実装方針**:
1. `ImprovedPitchAnalyzer`と`VolumeAnalyzer`を参照（Inspectorで接続推奨）
2. `ImprovedPitchAnalyzer.lastDetectedPitch`で最新ピッチを取得
3. `VoiceToScreenMapper`で座標変換
4. `PaintCanvas.PaintAt()`で塗り処理
5. 最初は攻撃タイプは固定（後から追加）

**注意**: 
- `FindObjectOfType`は使わず、Inspectorで接続する設計にする
- 既存の`GameManager`は参考にするが、新しい`PaintBattleGameManager`を作成

---

### Step 4: インクエフェクト（簡易版）

#### 4.1: 基本的なパーティクルエフェクト
**ファイル**: `Assets/Script/Graphics/InkEffect.cs`（新規作成）

**実装内容**:
- 最初はシンプルなパーティクルで可視化
- 塗り位置にパーティクルを生成
- 後から`IInkEffect`インターフェース化

**実装方針**:
1. UnityのParticleSystemを使用
2. `PaintCanvas.OnPaintCompleted`イベントを購読
3. 塗り位置にパーティクルを生成

---

### Step 5: デバッグモード（マウス操作で音声入力シミュレート）【オプション・推奨】

> **注意**: このステップは必須ではありませんが、開発・テスト効率を大幅に向上させるため、**Step 3（ゲームマネージャー統合）の後に実装することを推奨**します。

#### 5.1: VoiceDebugSimulator作成
**ファイル**: `Assets/Script/Debug/VoiceDebugSimulator.cs`（新規作成）

**実装内容**:
- デバッグモードがONの時、マウスの左クリックで音声入力をシミュレート
- マウス位置を`VoiceScatterPlot`のマーカー位置に反映
- マウス位置からピッチ・ボリュームに逆変換して、既存のイベントを発火

**実装タイミング**:
- **推奨**: Step 3（ゲームマネージャー統合）の後に実装（Phase1内）
- **理由**: ゲームマネージャー統合後は、声を出さずにテストできるため開発効率が向上
- **代替**: 声を出してテストする場合は、このステップをスキップ可能
- **Phase2での実装**: Phase1で実装しない場合、Phase2（モンスターシステム実装時）で実装することを推奨
  - Phase2ではモンスターの当たり判定や移動パターンのテストにデバッグモードが非常に有用

**実装方針**:
1. デバッグモードのON/OFFをInspectorで設定可能にする
2. マウスの左クリックを検出
3. マウス位置を`VoiceScatterPlot`の`plotArea`内の座標に変換
4. マウス位置（0-1正規化）からピッチ・ボリュームに逆変換
5. `VolumeAnalyzer.OnVolumeDetected`と`ImprovedPitchAnalyzer.OnPitchDetected`と同じイベントを発火

**既存コードの参考箇所**:
```csharp
// VoiceScatterPlot.cs の MapVolumeTo01, MapPitchTo01 の逆変換を実装
// キャリブレーション平均を原点として使用
float zeroVolume = VoiceCalibrator.LastAverageVolume;
float zeroPitch = VoiceCalibrator.LastAveragePitch;
```

**主要メソッド**:
```csharp
public class VoiceDebugSimulator : MonoBehaviour
{
    [Header("Debug Settings")]
    [Tooltip("デバッグモードを有効にする")]
    public bool enableDebugMode = false;
    
    [Header("References")]
    [Tooltip("VoiceScatterPlotの参照（マーカー位置を更新するため）")]
    public VoiceScatterPlot scatterPlot;
    
    [Header("Volume/Pitch Ranges")]
    [Tooltip("ボリュームの最大値（VoiceDisplayと同期）")]
    public float maxVolume = 1f;
    [Tooltip("ピッチの最小値（ImprovedPitchAnalyzerと同期）")]
    public float minPitch = 80f;
    [Tooltip("ピッチの最大値（ImprovedPitchAnalyzerと同期）")]
    public float maxPitch = 1000f;
    
    [Header("Debug Volume")]
    [Range(0f, 1f)]
    [Tooltip("デバッグモード時の音量（マウス位置から計算されるが、手動調整も可能）")]
    public float debugVolume = 0.5f;
    
    private bool isMouseDown = false;
    private float zeroVolume = 0f;
    private float zeroPitch = 80f;
    
    // 既存のイベントと同じシグネチャで発火
    public System.Action<float> OnVolumeDetected;
    public System.Action<float> OnPitchDetected;
    
    void Start()
    {
        // キャリブレーション平均を取得
        zeroVolume = Mathf.Max(0f, VoiceCalibrator.LastAverageVolume);
        zeroPitch = VoiceCalibrator.LastAveragePitch > 0f ? VoiceCalibrator.LastAveragePitch : minPitch;
        
        // キャリブレーション更新を購読
        VoiceCalibrator.OnCalibrationAveragesUpdated += OnCalibrationAveragesUpdated;
        
        // 範囲を同期
        SyncRanges();
    }
    
    void OnDestroy()
    {
        VoiceCalibrator.OnCalibrationAveragesUpdated -= OnCalibrationAveragesUpdated;
    }
    
    void Update()
    {
        if (!enableDebugMode) return;
        
        // マウスの左クリックを検出
        if (Input.GetMouseButtonDown(0))
        {
            isMouseDown = true;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isMouseDown = false;
            // クリック解除時は無音状態をシミュレート（音量0、ピッチ0）
            OnVolumeDetected?.Invoke(0f);
            OnPitchDetected?.Invoke(0f);
            return;
        }
        
        if (isMouseDown)
        {
            // マウス位置を取得
            Vector2 mouseScreenPos = Input.mousePosition;
            
            // VoiceScatterPlotのplotArea内の座標に変換
            if (scatterPlot != null && scatterPlot.plotArea != null)
            {
                Vector2 plotAreaPos = ConvertMouseToPlotArea(mouseScreenPos);
                
                // plotArea内の位置（0-1正規化）からピッチ・ボリュームに逆変換
                float volume = ConvertPlotPositionToVolume(plotAreaPos);
                float pitch = ConvertPlotPositionToPitch(plotAreaPos);
                
                // イベントを発火（既存のシステムと同じ）
                OnVolumeDetected?.Invoke(volume);
                OnPitchDetected?.Invoke(pitch);
            }
        }
    }
    
    Vector2 ConvertMouseToPlotArea(Vector2 mouseScreenPos)
    {
        if (scatterPlot == null || scatterPlot.plotArea == null)
            return Vector2.zero;
        
        RectTransform plotArea = scatterPlot.plotArea;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            plotArea, mouseScreenPos, null, out Vector2 localPoint);
        
        // plotAreaのサイズで正規化（0-1）
        Vector2 size = plotArea.rect.size;
        Vector2 normalizedPos = new Vector2(
            (localPoint.x + size.x * 0.5f) / size.x,
            (localPoint.y + size.y * 0.5f) / size.y
        );
        
        return normalizedPos;
    }
    
    float ConvertPlotPositionToVolume(Vector2 plotPos)
    {
        // VoiceScatterPlot.MapVolumeTo01の逆変換
        float x01 = scatterPlot.axes == VoiceScatterPlot.AxisMapping.VolumeX_PitchY 
            ? plotPos.x 
            : plotPos.y;
        
        // 0-1から実際のボリューム値に変換
        float leftExtent = Mathf.Max(0.0001f, zeroVolume - 0f);
        float rightExtent = Mathf.Max(0.0001f, maxVolume - zeroVolume);
        
        if (x01 >= 0.5f)
        {
            // 右側（原点より大きい）
            float frac = (x01 - 0.5f) * 2f; // 0.5-1.0 -> 0-1
            return zeroVolume + frac * rightExtent;
        }
        else
        {
            // 左側（原点より小さい）
            float frac = (0.5f - x01) * 2f; // 0-0.5 -> 1-0
            return zeroVolume - frac * leftExtent;
        }
    }
    
    float ConvertPlotPositionToPitch(Vector2 plotPos)
    {
        // VoiceScatterPlot.MapPitchTo01の逆変換
        float y01 = scatterPlot.axes == VoiceScatterPlot.AxisMapping.VolumeX_PitchY 
            ? plotPos.y 
            : plotPos.x;
        
        // matchSliderYAxisの設定を確認
        if (scatterPlot.matchSliderYAxis)
        {
            // スライダーと同じ正規化（原点センタリングなし）
            return Mathf.Lerp(minPitch, maxPitch, y01);
        }
        else
        {
            // 原点センタリングあり
            float downExtent = Mathf.Max(0.0001f, zeroPitch - minPitch);
            float upExtent = Mathf.Max(0.0001f, maxPitch - zeroPitch);
            
            if (y01 >= 0.5f)
            {
                // 上側（原点より大きい）
                float frac = (y01 - 0.5f) * 2f; // 0.5-1.0 -> 0-1
                return zeroPitch + frac * upExtent;
            }
            else
            {
                // 下側（原点より小さい）
                float frac = (0.5f - y01) * 2f; // 0-0.5 -> 1-0
                return zeroPitch - frac * downExtent;
            }
        }
    }
    
    void OnCalibrationAveragesUpdated(float avgVol, float avgPitch)
    {
        zeroVolume = Mathf.Max(0f, avgVol);
        zeroPitch = avgPitch > 0f ? avgPitch : zeroPitch;
    }
    
    void SyncRanges()
    {
        // VoiceDisplayから範囲を取得
        VoiceDisplay voiceDisplay = FindObjectOfType<VoiceDisplay>();
        if (voiceDisplay != null)
        {
            maxVolume = voiceDisplay.maxVolume;
            minPitch = voiceDisplay.minPitch;
            maxPitch = voiceDisplay.maxPitch;
        }
        // ImprovedPitchAnalyzerからも取得可能
        else
        {
            ImprovedPitchAnalyzer pitchAnalyzer = FindObjectOfType<ImprovedPitchAnalyzer>();
            if (pitchAnalyzer != null)
            {
                minPitch = pitchAnalyzer.minFrequency;
                maxPitch = pitchAnalyzer.maxFrequency;
            }
        }
    }
}
```

#### 5.2: 既存システムとの統合

**実装方針**:
1. `VoiceScatterPlot`を修正して、デバッグモード時は`VoiceDebugSimulator`のイベントを優先購読
2. または、`VoiceDebugSimulator`を`VolumeAnalyzer`と`ImprovedPitchAnalyzer`の代わりに使用する設計

**推奨実装**:
- `VoiceScatterPlot`にデバッグモード対応を追加
- デバッグモード時は`VoiceDebugSimulator`のイベントを購読
- 通常モード時は既存の`VolumeAnalyzer`と`ImprovedPitchAnalyzer`のイベントを購読

**VoiceScatterPlotへの追加**:
```csharp
// VoiceScatterPlot.cs に追加
[Header("Debug")]
[Tooltip("デバッグモード（マウス操作で音声入力をシミュレート）")]
public bool enableDebugMode = false;
public VoiceDebugSimulator debugSimulator;

void Start()
{
    // 既存のコード...
    
    // デバッグモード時はVoiceDebugSimulatorのイベントを購読
    if (enableDebugMode && debugSimulator != null)
    {
        debugSimulator.OnVolumeDetected += OnVolume;
        debugSimulator.OnPitchDetected += OnPitch;
    }
    else
    {
        // 通常モード：既存のイベント購読
        if (volumeAnalyzer != null)
            volumeAnalyzer.OnVolumeDetected += OnVolume;
        // ...既存のコード
    }
}
```

---

## 🔧 既存コードとの統合ポイント

### 音声検出システムとの接続
- **ImprovedPitchAnalyzer**: `lastDetectedPitch`プロパティで最新ピッチを取得
- **VolumeAnalyzer**: `OnVolumeDetected`イベントで音量を取得
- **VoiceCalibrator**: `LastAverageVolume`、`LastAveragePitch`でキャリブレーション平均を取得

### 座標変換の実装
- **VoiceScatterPlot**: `MapVolumeTo01`、`MapPitchTo01`のロジックを再利用
- キャリブレーション平均を原点として使用（既存の実装をそのまま活用）
- UI座標ではなく、画面座標（Screen座標系）に変換

### イベント駆動パターン
- 既存の`OnVolumeDetected`、`OnPitchDetected`パターンを参考
- `PaintCanvas.OnPaintCompleted`イベントを発火
- 他のシステムがイベントを購読できるようにする

---

## 📝 実装チェックリスト

### Step 1: PaintCanvas システム
- [ ] `IPaintCanvas.cs`を作成
- [ ] `PaintSettings.cs`を作成（ScriptableObject）
- [ ] `PaintCanvas.cs`を作成（基本実装）
- [ ] `PaintSettings.asset`を作成（Inspectorで設定）
- [ ] （推奨）`PaintCanvas.ResetCanvas()`メソッドを実装（Phase 2のクリエイティブモードで使用）
- [ ] テスト：`PaintCanvas.PaintAt()`で指定位置に塗れることを確認
- [ ] テスト：`PaintCanvas.ResetCanvas()`でキャンバスがクリアされることを確認（Phase 2用）

### Step 2: 座標変換システム
- [ ] `VoiceToScreenMapper.cs`を作成
- [ ] `VoiceScatterPlot`の座標変換ロジックを参考に実装
- [ ] キャリブレーション平均を原点として使用
- [ ] テスト：音声値 → 画面座標の変換が正しいことを確認

### Step 3: ゲームマネージャー統合
- [ ] `PaintBattleGameManager.cs`を作成
- [ ] 既存の`ImprovedPitchAnalyzer`と`VolumeAnalyzer`を参照
- [ ] `VoiceToScreenMapper`で座標変換
- [ ] `PaintCanvas.PaintAt()`で塗り処理
- [ ] テスト：声を出すと画面に色が塗られることを確認

### Step 4: インクエフェクト（簡易版）
- [ ] `InkEffect.cs`を作成（基本実装）
- [ ] `PaintCanvas.OnPaintCompleted`イベントを購読
- [ ] 塗り位置にパーティクルを生成
- [ ] テスト：塗り時にエフェクトが表示されることを確認

### Step 5: デバッグモード（マウス操作で音声入力シミュレート）【オプション】
- [ ] `VoiceDebugSimulator.cs`を作成
- [ ] マウス位置を`VoiceScatterPlot`のplotArea内の座標に変換
- [ ] マウス位置からピッチ・ボリュームに逆変換
- [ ] 既存のイベント（`OnVolumeDetected`、`OnPitchDetected`）を発火
- [ ] `VoiceScatterPlot`にデバッグモード対応を追加
- [ ] テスト：マウス左クリックでマーカーが動き、声を出した時と同じ処理になることを確認

> **注意**: このステップは必須ではありませんが、開発効率向上のため推奨されます。

---

## 🎯 最初の動作確認目標

**最小限のプロトタイプ**:
1. 声を出すと画面の指定位置に色が塗られる
2. キャリブレーション平均を原点として使用（既存の実装を活用）
3. 塗り位置にパーティクルエフェクトが表示される
4. （オプション）デバッグモードでマウス操作で音声入力をシミュレートできる
5. （推奨）`PaintCanvas.ResetCanvas()`でキャンバスをクリアできる（Phase 2のクリエイティブモードで使用）

**この状態が完成したら**:
- **Phase 2**: クリエイティブモード（声で絵を描くモード）に進むことができます
- 攻撃タイプシステム（Step 1.2）を追加
- 上塗り機能を追加
- より高度なエフェクトを追加

---

## 📚 参考ファイル

### 既存の実装パターン
- **座標変換**: `Assets/Script/UI/VoiceScatterPlot.cs`（`MapVolumeTo01`、`MapPitchTo01`）
- **イベント駆動**: `Assets/Script/VoiceDetection/VolumeAnalyzer.cs`（`OnVolumeDetected`）
- **コンポーネント参照**: `Assets/Script/GameLogic/GameManager.cs`（`FindObjectOfType`の使い方、ただしInspector接続を推奨）
- **キャリブレーション**: `Assets/Script/UI/VoiceCalibrator.cs`（`LastAverageVolume`、`LastAveragePitch`）

### 音声検出システム
- **ピッチ検出**: `Assets/Script/VoiceDetection/ImprovedPitchAnalyzer.cs`（`lastDetectedPitch`プロパティ）
- **音量検出**: `Assets/Script/VoiceDetection/VolumeAnalyzer.cs`（`OnVolumeDetected`イベント）

### デバッグシステム
- **デバッグシミュレーター**: `Assets/Script/Debug/VoiceDebugSimulator.cs`（マウス操作で音声入力をシミュレート）

---

## ⚠️ 注意事項

1. **Inspector接続を優先**: `FindObjectOfType`は使わず、Inspectorで依存関係を接続する設計にする
2. **既存コードを活用**: `VoiceScatterPlot`の座標変換ロジックを再利用
3. **段階的実装**: 最初はシンプルに、後から機能を追加
4. **イベント駆動**: 既存のイベントパターンを参考にする
5. **キャリブレーション対応**: 既存の`VoiceCalibrator`のデータを活用

---

## ❓ 実装前の確認事項

以下の点について、実装前に決定が必要です：

### 1. PaintCanvasの表示方法
**質問**: 塗りデータを画面に表示する方法は？
- **Option A**: Texture2Dを作成して、UI ImageやRawImageに表示
- **Option B**: RenderTextureを使用して、カメラに表示
- **Option C**: 最初はパーティクルエフェクトのみで可視化（データ構造は後から追加）

**推奨**: Phase1では**Option C**（パーティクルエフェクトのみ）で開始し、後からTexture2Dを追加

### 2. 塗りの範囲
**質問**: 1回の塗りで、どの範囲を塗るか？
- **Option A**: 1ピクセルだけ（最初はシンプルに）
- **Option B**: 半径を持つ円状に塗る（インパクトショット用）
- **Option C**: 前フレームからの軌跡に沿って塗る（ストリームペイント用）

**推奨**: Phase1では**Option A**（1ピクセル）で開始し、後から半径や軌跡を追加

### 3. 座標系の詳細
**質問**: Screen座標系の原点はどこか？
- UnityのScreen座標系は**左下が(0,0)**、右上が(Screen.width, Screen.height)
- `VoiceScatterPlot`はUI座標系（RectTransform）を使用しているが、`VoiceToScreenMapper`はScreen座標系に変換

**確認**: 画面座標への変換時に、Y軸の反転が必要か？（UI座標系は左上が原点、Screen座標系は左下が原点）

### 4. 攻撃タイプのenum定義
**質問**: 最初は固定だが、enumは定義するか？
- **Option A**: 最初はenumを定義せず、後から追加
- **Option B**: 最初からenumを定義（`AttackType.Normal`など）

**推奨**: **Option B**（最初からenumを定義）。`PaintAt()`のシグネチャに`PaintType`を含める

### 5. プレイヤーIDの初期値
**質問**: Phase1は1プレイヤーのみだが、playerIdはどうするか？
- **Option A**: `playerId = 1`で固定
- **Option B**: `PaintSettings`で設定可能にする

**推奨**: **Option A**（`playerId = 1`で固定）。後からマルチプレイヤー対応を追加

### 6. 無音時の処理
**質問**: 無音時（`VoiceScatterPlot`のように中心に戻る時）は、塗りをどうするか？
- **Option A**: 塗りを停止（無音時は塗らない）
- **Option B**: 最後の位置を保持して塗り続ける
- **Option C**: 中心位置に塗る

**推奨**: **Option A**（無音時は塗らない）。`VoiceScatterPlot`の無音判定ロジックを参考にする

### 7. 塗りの解像度
**質問**: `PaintSettings.textureWidth/Height`と実際の画面解像度の関係は？
- **Option A**: 画面解像度と同じ（1920x1080など）
- **Option B**: 固定解像度（例: 960x540）で、画面解像度に関係なく動作
- **Option C**: 画面解像度に応じて自動調整

**推奨**: Phase1では**Option B**（固定解像度、960x540など）。後から自動調整を追加

### 8. 塗りの可視化（Phase1後）
**質問**: 塗りデータを視覚化する方法は？（Phase1後）
- **Option A**: Texture2Dを作成して、UI Imageに表示
- **Option B**: RenderTextureを使用
- **Option C**: 3Dオブジェクト（Planeなど）にテクスチャを適用

**推奨**: **Option A**（Texture2D + UI Image）。シンプルで変更しやすい

---

## 📋 実装前の決定事項まとめ

以下の設定でPhase1を開始することを推奨：

1. **表示方法**: 最初はパーティクルエフェクトのみ（データ構造は後から追加）
2. **塗りの範囲**: 1ピクセル（後から半径や軌跡を追加）
3. **座標系**: Screen座標系（左下が原点）、Y軸の反転は不要（Unityの標準）
4. **攻撃タイプ**: enumを定義（`PaintType.Normal`など）
5. **プレイヤーID**: `playerId = 1`で固定
6. **無音時**: 塗りを停止（`VoiceScatterPlot`の無音判定を参考）
7. **解像度**: 固定解像度（960x540など）で開始
8. **可視化**: Phase1後、Texture2D + UI Imageで実装

これらの決定事項に基づいて実装を進めますか？それとも、別の設定を希望しますか？

