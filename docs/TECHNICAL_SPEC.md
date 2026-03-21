# Healing Fish School — 技術仕様書

> **対応要求仕様書:** [SPECIFICATION.md](../SPECIFICATION.md)
> 最終更新: 2026-03-21

---

## 1. 開発環境

| 項目 | 採用技術・バージョン |
|------|-------------------:|
| エンジン | Unity 2022.3 LTS 以降 |
| レンダリング | Universal Render Pipeline (URP) 17.x |
| 言語 | C# (.NET Standard 2.1) |
| カメラ制御 | Cinemachine 3.1.x (`com.unity.cinemachine`) |
| 入力 | Input System 1.x (`com.unity.inputsystem`) |
| ビルドターゲット | Windows Standalone (64-bit) |

---

## 2. フォルダ構成

```
Assets/
├── Prefabs/
│   └── Fish.prefab          # 小魚プレハブ（Body + TrailRenderer + FishAgent）
├── Scripts/
│   ├── Core/
│   │   ├── BoidsManager.cs      # 群れ全体の更新ループ・空間ハッシュ管理
│   │   ├── FishAgent.cs         # 個体のビジュアル更新（Emission / Trail）
│   │   ├── PredatorController.cs # 大魚の巡回移動
│   │   ├── EnvironmentProvider.cs # 時刻・ライト・Volume 制御
│   │   └── CameraManager.cs     # 固定/追従カメラ切り替え
│   ├── Data/
│   │   ├── BoidsSettings.cs     # 群れパラメータ SO
│   │   ├── FishPersonality.cs   # 個体差パラメータ SO
│   │   └── TimeCycleSettings.cs # 時刻・演出パラメータ SO
│   └── Editor/
│       └── SceneSetupTool.cs    # シーン自動セットアップ（Editor 限定）
├── Settings/
│   ├── BoidsSettings.asset
│   ├── FishPersonality_Default.asset
│   ├── TimeCycleSettings.asset
│   ├── FishMaterial.mat
│   └── TrailMaterial.mat
docs/
├── TECHNICAL_SPEC.md  ← このファイル
SPECIFICATION.md       # 要求仕様書
```

---

## 3. クラス設計

### 3.1 `BoidsManager` （`SchoolOfFish.Core`）

**役割:** 全個体の状態（位置・速度・PanicTimer）を C# コレクションで管理し，毎フレームシミュレーションを更新する中心クラス．

**依存関係:**

```
BoidsManager
  ├── BoidsSettings        (ScriptableObject)
  ├── FishAgent[]          (Prefab instances)
  ├── PredatorController   (シーン参照)
  ├── EnvironmentProvider  (シーン参照)
  └── FishPersonality[]    (ScriptableObject pool)
```

**内部データ構造:**

```csharp
private struct AgentState
{
    Vector3 Position;
    Vector3 Velocity;
    float   PanicTimer;         // > 0 の間パニック状態
    FishPersonality Personality;
    float   WanderSeed;         // Perlin ノイズオフセット
}
```

**更新フロー（`Update` 毎フレーム）:**

1. `BuildSpatialHash()` — 全個体を 3D グリッドにハッシュ登録
2. 各個体ループ：
   - `ComputeSeparation()` → 近傍数と panic 中の個体数を取得
   - `ComputeAlignment()` / `ComputeCohesion()`
   - `ComputeFlee()` — Predator 距離チェック
   - `ComputeWander()` — Perlin ノイズベースの揺らぎ
    - `ComputeBoundary(position, velocity)` — 先読み付き境界回避
    - `ScaleVertical(...)` — ステアリング力の Y 成分を抑制
   - `UpdatePanicState()` — panic 発火・伝播・減衰
   - `maxSpeed` / `maxForce` 計算（panic 中は乗数増加）
    - ステアリング合算 → 速度・位置更新（速度 Y 成分を再抑制）
   - `KeepInsideBounds()` — 境界で速度反転・クランプ
3. `FishAgent.ApplySimulation()` で位置・回転を反映
4. `FishAgent.SetPanicVisual(panic01)` で Emission / Trail を更新
5. `RefreshTankEdgeVisibility()` で水槽の辺表示（任意）を更新

**初期生成定義（向き整合）:**

- `fishPrefab` あり: `Fish.prefab` を生成
- `fishPrefab` なし: ランタイムで `Fish(root) + Body(child)` を生成
- どちらも `Body.localRotation = Quaternion.Euler(90, 180, 0)` を採用し，
  `LookRotation(velocity)` の +Z 前方と見た目の頭方向を一致させる

**空間ハッシュ近傍探索:**

```
CellKey = FNV-style hash(cellX, cellY, cellZ)
cellSize = BoidsSettings.hashCellSize  (default: 5.0)
探索範囲   = ±maxNeighborCellsPerAxis セル (default: 1 → 3³= 27 セル)
```

**境界回避アルゴリズム（最新版）:**

- 境界回避は `position` と `velocity` を使う予測型
- `boundaryLookAheadTime` 秒先の位置を評価し，現在位置より危険なら先読み結果を採用
- 壁方向へ進んでいる速度に応じて `boundaryVelocityBoost` で斥力増幅
- 境界外に出た場合は `boundaryOutsideBoost` で押し戻しを強化
- 出力ベクトルは正規化せず強度を保持（弱い回避/強い回避を表現）

**デバッグ表示（任意）:**

- `showTankEdges=true` のとき，`schoolRoot` 配下に `TankEdges` を生成
- 12 本の `LineRenderer` で境界ボックスの辺を描画
- `LineAlignment.View` を使い，視点変更時も視認性を維持

**パニックロジック:**

| 条件 | 処理 |
|------|------|
| Predator が `predatorFearRadius` 以内 | `PanicTimer = panicDuration` にリセット |
| 近傍の panic 率 ≥ `panicSpreadThreshold` | `PanicTimer = max(current, panicDuration × 0.65)` |
| その他 | `PanicTimer -= dt` でカウントダウン |

**夜間係数（`EnvironmentProvider.Night01` 参照）:**

```
alignmentWeight = settings.alignmentWeight × Lerp(1, nightAlignmentMultiplier, Night01)
speedFactor     = Lerp(1, nightSpeedMultiplier, Night01)
```

---

### 3.2 `FishAgent` （`SchoolOfFish.Core`）

**役割:** 個体のビジュアル（位置・回転・Emission・Trail）を更新するビュー層．シミュレーション計算は持たない．

**公開 API:**

```csharp
// BoidsManager から毎フレーム呼ばれる
void ApplySimulation(Vector3 position, Vector3 velocity)

// panic01 = 0.0（平常）〜 1.0（フルパニック）
void SetPanicVisual(float panic01)

// BoidsManager からピッチ上限を反映
void SetMaxPitchAngle(float maxPitch)
```

**ビジュアル更新詳細:**

- **回転:** `ConstrainPitch` で前方ベクトルのピッチを制限してから
  `Quaternion.Slerp(current, LookRotation(forward), 0.25f)` を適用
- **Emission:** `MaterialPropertyBlock` で `_EmissionColor` を補間（GPU Instancing と共存可能）
  - 平常: `(0.02, 0.15, 0.20)` / パニック: `(0.20, 1.00, 0.90)`
- **TrailRenderer:**

  | プロパティ | 平常時 | パニック時（panic01=1） |
  |-----------|-------:|---------------------:|
  | `time` | 0.55 s | 0.90 s |
  | `startWidth` | 0.07 | 0.10 |
  | 色 | 青緑（a=0.9） | シアン（a=1.0） |

---

### 3.3 `PredatorController` （`SchoolOfFish.Core`）

**役割:** 大魚の自律巡回移動．2 種類のモードを持つ．

**PatrolMode:**

| モード | 動作 |
|--------|------|
| `Circle` | 中心 Transform を軸に円周上を回遊 + 正弦波の上下移動 |
| `Waypoints` | 指定 Transform のリストを順番に訪問（ループ） |

**主요パラメータ（Inspector から設定）:**

```
moveSpeed              = 4.5   // 移動速度 [m/s]
turnSpeed              = 4.0   // 旋回速度 [rad/s相当]
circleRadius           = 18.0  // 円巡回の半径 [m]
verticalWaveAmplitude  = 2.2   // 上下揺れの振幅 [m]
verticalWaveFrequency  = 0.25  // 上下揺れの周波数 [Hz]
waypointReachDistance  = 0.8   // ウェイポイント到達判定距離 [m]
```

**公開プロパティ:**

```csharp
Vector3 Position { get; }  // BoidsManager.ComputeFlee() から参照
```

---

### 3.4 `EnvironmentProvider` （`SchoolOfFish.Core`）

**役割:** シミュレーション時刻を管理し，Directional Light と URP Volume を毎フレーム同期させる．

**TimeMode:**

| モード | 挙動 |
|--------|------|
| `RealTime` | `DateTime.Now` からシステム時刻を `Day01` (0〜1) に変換 |
| `Accelerated` | `_simulatedSeconds += dt × (86400 / dayLengthSeconds)` で加速 |

**公開プロパティ（BoidsManager から参照）:**

```csharp
float Day01   { get; }  // 1日の進捗 0.0(0時) 〜 1.0(24時)
float Night01 { get; }  // 夜の強度 0.0(昼) 〜 1.0(夜) ※正弦波から計算
```

**Night01 の計算式:**

```
sunHeight = sin((Day01 - 0.25) × 2π)
Night01   = InverseLerp(0.1, -0.35, sunHeight)  // 正午=0, 深夜=1
```

**Directional Light 制御:**

- `intensity` — `TimeCycleSettings.directionalLightIntensity` AnimationCurve で評価
- `color` — `TimeCycleSettings.directionalLightColor` Gradient で評価
- `rotation` — `Day01 × 360 − 90` 度（太陽の仰角）

**URP Volume 制御（毎フレーム `profile.TryGet<T>()` で取得）:**

| Volume Override | 制御プロパティ | AnimationCurve/Gradient |
|-----------------|--------------|------------------------|
| `Bloom` | `intensity` | `bloomByDay` |
| `DepthOfField` | `focusDistance` | `depthOfFieldFocusDistanceByDay` |
| `Vignette` | `intensity` | `vignetteByDay` |
| `ColorAdjustments` | `postExposure` | `postExposureByDay` |
| `ColorAdjustments` | `colorFilter` | `colorFilterByDay` |

---

### 3.5 `CameraManager` （`SchoolOfFish.Core`）

**役割:** 固定カメラと追従カメラの ON/OFF を切り替える．

**操作:**
- `C` キーでモードをトグル（`switchKey` で変更可）

**追従カメラの補間:**
- 位置: `Lerp(current, desired, 1 − exp(−followSmooth × dt))` — 指数平滑化
- フォローオフセット: `(0, 6, −14)` （`followOffset` で変更可）

---

## 4. ScriptableObject 設計

### 4.1 `BoidsSettings`

パス: `Assets/Settings/BoidsSettings.asset`

| フィールド | 型 | デフォルト | 説明 |
|-----------|-----|----------:|------|
| `fishCount` | int | 500 | 生成する個体数 |
| `spawnExtents` | Vector3 | (30, 12, 30) | 生成・境界範囲（半径） |
| `neighborRadius` | float | 4.5 | 近傍探索の最大距離 |
| `separationRadius` | float | 1.6 | 分離力が働く距離 |
| `baseMaxSpeed` | float | 6.0 | 基本最大速度 [m/s] |
| `baseMaxForce` | float | 10.0 | 基本最大操舵力 |
| `separationWeight` | float | 1.7 | 分離の重み |
| `alignmentWeight` | float | 1.0 | 整列の重み |
| `cohesionWeight` | float | 1.1 | 結合の重み |
| `wanderWeight` | float | 0.35 | 揺らぎの重み |
| `boundaryWeight` | float | 0.8 | 境界反発の重み |
| `boundaryRepulsionDistance` | float | 3.0 | 境界回避の開始距離 |
| `boundaryLookAheadTime` | float | 0.45 | 境界回避の先読み時間 [s] |
| `boundaryVelocityBoost` | float | 1.2 | 壁方向速度による斥力の増幅係数 |
| `boundaryOutsideBoost` | float | 2.0 | 境界外での押し戻し強化係数 |
| `verticalSwimMultiplier` | float [0〜1] | 0.4 | 全ステアリング/速度の Y 成分係数 |
| `maxPitchAngleDegrees` | float [0〜80] | 20.0 | 個体のピッチ角上限 [deg] |
| `predatorFearRadius` | float | 8.0 | Flee 発動距離 |
| `panicDuration` | float | 2.5 | パニック継続時間 [s] |
| `panicSpreadRadius` | float | 5.5 | パニック伝播の影響範囲 |
| `panicSpreadThreshold` | float | 0.35 | 伝播が起きる panic 率の閾値 |
| `panicSpeedMultiplier` | float | 1.8 | パニック中の速度倍率 |
| `panicForceMultiplier` | float | 1.5 | パニック中の操舵力倍率 |
| `nightAlignmentMultiplier` | float | 0.5 | 夜間の整列重み係数 |
| `nightSpeedMultiplier` | float | 0.65 | 夜間の速度係数 |
| `hashCellSize` | float | 5.0 | 空間ハッシュのセルサイズ |
| `maxNeighborCellsPerAxis` | int | 1 | 近傍探索のセル探索範囲 |

### 4.2 `FishPersonality`

パス: `Assets/Settings/FishPersonality_*.asset`（複数作成してプールに登録）

| フィールド | 型 | デフォルト | 説明 |
|-----------|-----|----------:|------|
| `timidness` | float [0.5〜2.0] | 1.0 | 臆病さ（Flee 強度に乗算） |
| `curiosity` | float [0.5〜2.0] | 1.0 | 好奇心（将来の餌やり機能用） |
| `maxSpeedVariance` | float [0.7〜1.3] | 1.0 | 最高速度のゆらぎ倍率 |

### 4.3 `TimeCycleSettings`

パス: `Assets/Settings/TimeCycleSettings.asset`

| フィールド | 型 | デフォルト | 説明 |
|-----------|-----|----------:|------|
| `dayLengthSeconds` | float | 300 | 加速モード時の 1 日の長さ [s] |
| `directionalLightColor` | Gradient | — | 時刻ごとの光の色 |
| `directionalLightIntensity` | AnimationCurve | 0.15→1.0 | 時刻ごとの光の強度 |
| `bloomByDay` | AnimationCurve | 0.2→0.6 | Bloom 強度 |
| `depthOfFieldFocusDistanceByDay` | AnimationCurve | 8→14 | DoF 焦点距離 [m] |
| `vignetteByDay` | AnimationCurve | 0.35→0.15 | Vignette 強度 |
| `postExposureByDay` | AnimationCurve | −0.25→0.1 | 露出補正 [EV] |
| `colorFilterByDay` | Gradient | — | カラーフィルター色 |

---

## 5. シーン構成

```
SampleScene
├── Main Camera              [Camera, AudioListener, UniversalAdditionalCameraData]
├── Directional Light        [Light, UniversalAdditionalLightData]
│     ← EnvironmentProvider が毎フレーム intensity / color / rotation を更新
├── Global Volume            [Volume]
│     ← EnvironmentProvider が Bloom / DoF / Vignette / ColorAdjustments を更新
├── SchoolRoot               [BoidsManager]
│     ← 小魚 500 体の親 Transform を兼ねる
│     ├── TankEdges (optional) [LineRenderer x 12]
│     └── Fish(0〜499)       [FishAgent, TrailRenderer]
│          └── Body          [Renderer]
├── EnvironmentController    [EnvironmentProvider]
├── CameraManager            [CameraManager]
└── Predator                 [PredatorController]
```

---

## 6. Editor ツール

**メニュー:** `School Of Fish` → 以下の 3 ステップ（または `Run All Setup Steps` で一括実行）

| ステップ | メニュー項目 | 処理内容 |
|---------|------------|---------|
| 1 | `1. Create Settings Assets` | `BoidsSettings` / `FishPersonality` / `TimeCycleSettings` アセットを `Assets/Settings/` に作成 |
| 2 | `2. Create Fish Prefab` | `Fish(root)` 配下に `Body(child)` を持つ Fish.prefab を `Assets/Prefabs/` に作成（`Body.localRotation = Quaternion.Euler(90,180,0)`） |
| 3 | `3. Wire Scene References` | シーン内の全コンポーネント間の参照を設定，Volume Override を追加，シーンをダーティマーク |

---

## 7. パフォーマンス設計方針

| 項目 | 対策 |
|------|------|
| 近傍探索 | 空間ハッシュ（Dictionary + 固定セルサイズ）で O(n) 近似 |
| 描画 | `enableInstancing = true` のマテリアル + `MaterialPropertyBlock` で GPU Instancing 有効 |
| GC | `List` / `Dictionary` は `Start` 時に上限容量で初期確保し，毎フレームの Alloc を抑制 |
| 重力省略 | 境界反発のみで垂直方向を制御，Rigidbody は使用しない |
| dt キャップ | `dt = Mathf.Min(Time.deltaTime, 0.05f)` でフレーム落ち時の発散を防止 |

---

## 8. 既知の制限と今後の課題

| 課題 | 詳細 | 対応方針 |
|------|------|---------|
| Wander がやや直線的 | Perlin ノイズが低周波のため方向変化が遅い | 速度に応じてノイズ周波数を可変にする |
| CameraManager が `Camera.main` 依存 | 複数カメラ構成への拡張が難しい | Cinemachine `CinemachineBrain` への移行を検討 |
| Panic 伝播が空間ハッシュ未使用 | `panicSpreadRadius` はハッシュ解除後の全探索 | `EnumerateNeighbors` に半径を渡して統一する |
| 個体の回転が Y 軸固定 | 急速方向転換時に Roll が発生しない | `up` ベクトルを速度の外積で求めるか許容範囲を広げる |
