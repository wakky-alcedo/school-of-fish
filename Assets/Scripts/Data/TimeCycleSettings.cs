using UnityEngine;

namespace SchoolOfFish.Data
{
    [CreateAssetMenu(menuName = "School Of Fish/Time Cycle Settings", fileName = "TimeCycleSettings")]
    public class TimeCycleSettings : ScriptableObject
    {
        [Header("Accelerated Time")]
        [Min(30f)] public float dayLengthSeconds = 300f;

        [Header("Sun")]
        public Gradient directionalLightColor;
        public AnimationCurve directionalLightIntensity = AnimationCurve.Linear(0f, 0.15f, 1f, 1f);

        [Header("Volume")]
        public AnimationCurve bloomByDay = AnimationCurve.Linear(0f, 0.2f, 1f, 0.6f);
        public AnimationCurve depthOfFieldFocusDistanceByDay = AnimationCurve.Linear(0f, 8f, 1f, 14f);
        public AnimationCurve vignetteByDay = AnimationCurve.Linear(0f, 0.35f, 1f, 0.15f);
        public AnimationCurve postExposureByDay = AnimationCurve.Linear(0f, -0.25f, 1f, 0.1f);
        public Gradient colorFilterByDay;
    }
}
