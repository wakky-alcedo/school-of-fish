using UnityEngine;

namespace SchoolOfFish.Data
{
    [CreateAssetMenu(menuName = "School Of Fish/Boids Settings", fileName = "BoidsSettings")]
    public class BoidsSettings : ScriptableObject
    {
        [Header("Population")]
        [Tooltip("概要: 生成する小魚の総数。\n目安: 300〜700 (RTX 3060で500が標準)。\n大きくすると: 群れ密度と計算負荷が増える。\n小さくすると: 軽くなるが群れの迫力が減る。\n計算式: for (i=0; i<fishCount; i++) で個体を生成・更新。")]
        [Min(1)] public int fishCount = 500;

        [Tooltip("概要: 水槽の半径サイズ (x,y,z)。\n目安: (30,12,30)。\n大きくすると: 群れが拡散し境界に当たりにくい。\n小さくすると: 密集しやすく境界回避が頻発。\n計算式: localPosを[-spawnExtents,+spawnExtents]で生成/境界判定。")]
        public Vector3 spawnExtents = new Vector3(30f, 12f, 30f);

        [Header("Core Boids")]
        [Tooltip("概要: 近傍として参照する半径。\n目安: 3.5〜6.0。\n大きくすると: 集団同期が強く滑らか、計算量増。\n小さくすると: 個体行動が増えて粗くなる。\n計算式: distance <= neighborRadius の個体を列挙。")]
        [Min(0f)] public float neighborRadius = 4.5f;

        [Tooltip("概要: 分離力を発生させる近接半径。\n目安: 1.0〜2.2。\n大きくすると: 互いに離れやすく疎な群れになる。\n小さくすると: 密着しやすく交差が増える。\n計算式: if (d^2 <= separationRadius^2) separation += toMe/(d^2+0.01)。")]
        [Min(0f)] public float separationRadius = 1.6f;

        [Tooltip("概要: 通常時の最高速度上限。\n目安: 4.5〜7.5。\n大きくすると: 全体が俊敏になる。\n小さくすると: ゆったり泳ぐ。\n計算式: velocity = ClampMagnitude(velocity + steer*dt, baseMaxSpeed*係数)。")]
        [Min(0f)] public float baseMaxSpeed = 6f;

        [Tooltip("概要: 1フレームで加えられる操舵力上限。\n目安: 8〜14。\n大きくすると: 急旋回・反応が速い。\n小さくすると: 曲がりが緩やか。\n計算式: steer = ClampMagnitude(desired, baseMaxForce*係数)。")]
        [Min(0f)] public float baseMaxForce = 10f;

        [Tooltip("概要: 分離(ぶつかり回避)の重み。\n目安: 1.2〜2.2。\n大きくすると: 互いに離れやすい。\n小さくすると: 密度高めで群れが詰まる。\n計算式: desired += separation * separationWeight。")]
        [Min(0f)] public float separationWeight = 1.7f;

        [Tooltip("概要: 整列(近傍と進行方向を揃える)重み。\n目安: 0.7〜1.4。\n大きくすると: 同じ向きに揃いやすい。\n小さくすると: バラつきが増える。\n計算式: desired += alignment * alignmentWeight。")]
        [Min(0f)] public float alignmentWeight = 1f;

        [Tooltip("概要: 結合(群れ中心へ寄る)重み。\n目安: 0.8〜1.5。\n大きくすると: 群れがまとまりやすい。\n小さくすると: 拡散しやすい。\n計算式: desired += cohesion * cohesionWeight。")]
        [Min(0f)] public float cohesionWeight = 1.1f;

        [Tooltip("概要: ランダム揺らぎの重み。\n目安: 0.2〜0.6。\n大きくすると: 自然なゆらぎ増、やや散漫。\n小さくすると: 機械的だが安定。\n計算式: desired += wander * wanderWeight。")]
        [Min(0f)] public float wanderWeight = 0.35f;

        [Tooltip("概要: 境界回避ベクトルの重み。\n目安: 0.8〜1.8。\n大きくすると: 壁際で強く押し返す。\n小さくすると: 壁際ギリギリまで寄る。\n計算式: desired += boundary * boundaryWeight。")]
        [Min(0f)] public float boundaryWeight = 0.8f;

        [Tooltip("概要: 壁斥力を開始する距離。\n目安: 2.0〜6.0。\n大きくすると: 早めに壁回避。\n小さくすると: 壁近くまで寄る。\n計算式: start = extent - boundaryRepulsionDistance。")]
        [Min(0.1f)] public float boundaryRepulsionDistance = 3f;

        [Tooltip("概要: 境界回避の先読み時間。\n目安: 0.3〜0.8 秒。\n大きくすると: 早めに曲がる。\n小さくすると: 直前で回避。\n計算式: futurePos = localPos + localVel * boundaryLookAheadTime。")]
        [Min(0f)] public float boundaryLookAheadTime = 0.45f;

        [Tooltip("概要: 壁方向へ進む速度に応じた斥力増幅。\n目安: 1.0〜2.5。\n大きくすると: 正面衝突に強く反応。\n小さくすると: 速度依存が弱い。\n計算式: boundary *= (1 + outward01 * boundaryVelocityBoost)。")]
        [Min(0f)] public float boundaryVelocityBoost = 1.2f;

        [Tooltip("概要: 境界外に出た時の押し戻し追加倍率。\n目安: 1.5〜4.0。\n大きくすると: はみ出し復帰が速い。\n小さくすると: 復帰が緩い。\n計算式: over01に応じて (1 + over01 * boundaryOutsideBoost) を加算。")]
        [Min(0f)] public float boundaryOutsideBoost = 2f;

        [Tooltip("概要: 上下方向(Y)の泳ぎ量係数。\n目安: 0.25〜0.55。\n大きくすると: 立体的に上下へ泳ぐ。\n小さくすると: 水平寄りになる。\n計算式: v.y *= verticalSwimMultiplier (steer/velocity両方)。")]
        [Range(0f, 1f)] public float verticalSwimMultiplier = 0.4f;

        [Tooltip("概要: 魚の見た目ピッチ角の上限。\n目安: 10〜30 度。\n大きくすると: 上下を向きやすい。\n小さくすると: 水平姿勢を保つ。\n計算式: forward.y を ±tan(maxPitchAngleDegrees) 相当でClamp。")]
        [Range(0f, 80f)] public float maxPitchAngleDegrees = 20f;

        [Header("Panic")]
        [Tooltip("概要: 大魚を脅威として認識する距離。\n目安: 6〜12。\n大きくすると: 早めに逃走開始。\n小さくすると: 近づくまで反応しない。\n計算式: if (distance <= predatorFearRadius) flee有効。")]
        [Min(0f)] public float predatorFearRadius = 8f;

        [Tooltip("概要: パニック継続時間。\n目安: 1.5〜3.5 秒。\n大きくすると: パニック余韻が長い。\n小さくすると: すぐ落ち着く。\n計算式: PanicTimer = panicDuration; 毎フレーム PanicTimer -= dt。")]
        [Min(0f)] public float panicDuration = 2.5f;

        [Tooltip("概要: (将来拡張用) パニック伝播半径。\n目安: 4〜7。\n大きくすると: 広域伝播しやすい設計値。\n小さくすると: 局所伝播向け設計値。\n計算式: 現状コードでは未使用（将来 neighbor 半径条件に統合予定）。")]
        [Min(0f)] public float panicSpreadRadius = 5.5f;

        [Tooltip("概要: 近傍パニック率の伝播閾値。\n目安: 0.25〜0.5。\n大きくすると: 伝播しにくい。\n小さくすると: 伝播しやすい。\n計算式: ratio = panickedNeighbors/neighborCount; ratio>=thresholdで発火。")]
        [Range(0f, 1f)] public float panicSpreadThreshold = 0.35f;

        [Tooltip("概要: パニック中の速度倍率。\n目安: 1.4〜2.2。\n大きくすると: 逃走速度が上がる。\n小さくすると: 通常時との差が小さい。\n計算式: maxSpeed *= panicSpeedMultiplier。")]
        [Min(1f)] public float panicSpeedMultiplier = 1.8f;

        [Tooltip("概要: パニック中の操舵力倍率。\n目安: 1.2〜2.0。\n大きくすると: 急激に方向転換する。\n小さくすると: 旋回差が小さい。\n計算式: maxForce *= panicForceMultiplier。")]
        [Min(1f)] public float panicForceMultiplier = 1.5f;

        [Header("Night Behavior")]
        [Tooltip("概要: 夜間時の整列係数。\n目安: 0.4〜0.8。\n大きくすると: 夜でも方向が揃いやすい。\n小さくすると: 夜の隊列感が弱まる。\n計算式: alignmentWeight *= Lerp(1, nightAlignmentMultiplier, Night01)。")]
        [Range(0f, 1f)] public float nightAlignmentMultiplier = 0.5f;

        [Tooltip("概要: 夜間時の速度係数。\n目安: 0.5〜0.8。\n大きくすると: 夜も速い。\n小さくすると: 夜にゆっくり泳ぐ。\n計算式: speedFactor = Lerp(1, nightSpeedMultiplier, Night01)。")]
        [Range(0f, 1f)] public float nightSpeedMultiplier = 0.65f;

        [Header("Optimization")]
        [Tooltip("概要: 空間ハッシュのセルサイズ。\n目安: neighborRadius付近(4〜6)。\n大きくすると: バケット粗くなり候補増。\n小さくすると: セル増で管理コスト増。\n計算式: cell = Floor(localPos / hashCellSize)。")]
        [Min(0.1f)] public float hashCellSize = 5f;

        [Tooltip("概要: 近傍探索時のセル検索半径。\n目安: 1 (3x3x3セル)。\n大きくすると: 正確だが重い。\n小さくすると: 取りこぼしが増える。\n計算式: for x,y,z in [-r,+r] を走査。")]
        [Range(1, 8)] public int maxNeighborCellsPerAxis = 1;
    }
}
