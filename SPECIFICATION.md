魚群シミュレーション（Healing Fish School）Unity版 仕様書

1．プロジェクト概要

本プロジェクトは，Unityを用いてBoidsアルゴリズムに基づく魚群の動きをシミュレーションし，RTX 3060の描画性能を活かした「高品質な癒やし」を提供することを目的とする．Universal Render Pipeline (URP) を採用し，光の表現やポストプロセスによって，水族館のような没入感のある視覚体験を実現する．

2．システム要件

プラットフォーム: PC (Windows / Standalone)

エンジン: Unity 2022.3 LTS 以降

レンダリングパイプライン: Universal Render Pipeline (URP)

言語: C#

想定ハードウェア: NVIDIA GeForce RTX 3060 以上の環境で高解像度・高フレームレート動作

3．エージェント（小魚）仕様

3.1 挙動ロジック (Boids & More)

基本3規則: Separation（分離），Alignment（整列），Cohesion（結合）を Compute Buffer または C# Job System で計算可能にする．

回避と波及 (Panic Logic):

大魚検知: 大魚との距離が閾値以下の場合，Flee 行動を最優先し，移動速度（maxSpeed）と操舵力（maxForce）を一時的に引き上げる．

情報の伝播: 周囲の個体の IsPanicked フラグを監視し，パニック状態の個体率が高い場合に自身も回避行動へ移行する．

個体差: ScriptableObject により，個体ごとの性格（臆病さ，好奇心，最高速度のゆらぎ）をパラメータ化し，個別に付与する．

3.2 ビジュアル表現

メッシュ: 抽象化された涙型またはポリゴンメッシュ．GPU Instancing を使用し，描画負荷を軽減する．

シェーダー (Shader Graph): - 進行方向への揺らぎ（頂点アニメーション）による生物的な表現．

パニック状態時の自発光（Emission）強度の動的変化．

軌跡 (Trail): Trail Renderer を各個体にアタッチし，時間に応じた減衰と色相変化を付与する．

4．環境・演出仕様

4.1 24時間サイクルとライティング

環境光遷移: URPの Volume プロファイルと Directional Light の強度・色温度を同期させ，現実の24時間（または加速されたサイクル）に合わせたライティング変化を行う．

活性度連動: 時刻プロパティを Boids ロジックの重み係数に反映．夜間は Alignment を弱め，低速で漂う「睡眠モード」へ移行させる．

4.2 ポストプロセス (URP Volume)

Bloom: パニック時の個体発光や環境光を拡散させ，幻想的な雰囲気を演出する．

Depth of Field: 焦点距離を動的に制御し，水中の奥行き感を出す．

Vignette / Color Grading: 時間帯に応じたトーンマッピングを行い，視覚的な落ち着きを与える．

5．システム構成（クラス設計）

| クラス名 | 役割 |
| BoidsManager | 全個体のリスト保持，Job System の発行，近傍探索の最適化（Grid-based）． |
| FishAgent | 各個体のステート（通常/パニック）管理，マテリアルプロパティの更新． |
| PredatorController | 大魚の回遊ロジック．スプライン曲線（Spline Package）に沿った自動巡回． |
| EnvironmentProvider | シミュレーション時刻の管理，URP Volume プロファイルとライトの制御． |
| CameraManager | Cinemachine を使用した追従・固定視点の切り替え，手ブレ演出． |

6．将来の拡張性

Cinemachine: 群れを自動で追い続けるドリーショットの実装．

Visual Effect Graph (VFX Graph): 気泡や浮遊する微粒子（マリンスノー）の追加による密度の向上．

Audio Spatializer: AudioSource を個体に配置し，カメラとの距離に応じた水中環境音の生成．

Input System: クリックによる餌やり，画面を叩くことによる驚かせ（回避誘発）．

