using System;
using SchoolOfFish.Data;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SchoolOfFish.Core
{
    public class EnvironmentProvider : MonoBehaviour
    {
        public enum TimeMode
        {
            RealTime,
            Accelerated
        }

        [SerializeField] private TimeMode timeMode = TimeMode.Accelerated;
        [SerializeField] private TimeCycleSettings settings;
        [SerializeField] private Light directionalLight;
        [SerializeField] private Volume globalVolume;

        private float _simulatedSeconds;

        public float Day01 { get; private set; }
        public float Night01 { get; private set; }

        private void Update()
        {
            UpdateClock();
            ApplyLighting();
            ApplyVolume();
        }

        private void UpdateClock()
        {
            if (timeMode == TimeMode.RealTime)
            {
                DateTime now = DateTime.Now;
                Day01 = (now.Hour + now.Minute / 60f + now.Second / 3600f) / 24f;
            }
            else
            {
                float dayLength = settings != null ? Mathf.Max(30f, settings.dayLengthSeconds) : 300f;
                _simulatedSeconds += Time.deltaTime * (86400f / dayLength);
                Day01 = Mathf.Repeat(_simulatedSeconds / 86400f, 1f);
            }

            float sunHeight = Mathf.Sin((Day01 - 0.25f) * Mathf.PI * 2f);
            Night01 = Mathf.InverseLerp(0.1f, -0.35f, sunHeight);
        }

        private void ApplyLighting()
        {
            if (directionalLight == null || settings == null)
            {
                return;
            }

            directionalLight.intensity = settings.directionalLightIntensity.Evaluate(Day01);
            directionalLight.color = settings.directionalLightColor.Evaluate(Day01);

            float sunAngle = Day01 * 360f - 90f;
            directionalLight.transform.rotation = Quaternion.Euler(sunAngle, 25f, 0f);
        }

        private void ApplyVolume()
        {
            if (globalVolume == null || settings == null || globalVolume.profile == null)
            {
                return;
            }

            VolumeProfile profile = globalVolume.profile;

            if (profile.TryGet(out Bloom bloom))
            {
                bloom.intensity.value = settings.bloomByDay.Evaluate(Day01);
            }

            if (profile.TryGet(out DepthOfField dof))
            {
                dof.focusDistance.value = settings.depthOfFieldFocusDistanceByDay.Evaluate(Day01);
            }

            if (profile.TryGet(out Vignette vignette))
            {
                vignette.intensity.value = settings.vignetteByDay.Evaluate(Day01);
            }

            if (profile.TryGet(out ColorAdjustments colorAdjustments))
            {
                colorAdjustments.postExposure.value = settings.postExposureByDay.Evaluate(Day01);
                colorAdjustments.colorFilter.value = settings.colorFilterByDay.Evaluate(Day01);
            }
        }
    }
}
