using UnityEngine;

namespace SchoolOfFish.Core
{
    public class FishAgent : MonoBehaviour
    {
        [SerializeField] private Renderer bodyRenderer;
        [SerializeField] private TrailRenderer trail;
        [SerializeField] private float trailBaseTime = 0.55f;
        [SerializeField] private float trailPanicBonus = 0.35f;
        [SerializeField, Range(0f, 80f)] private float maxPitchAngleDegrees = 20f;
        [SerializeField] private Color calmEmission = new Color(0.02f, 0.15f, 0.2f);
        [SerializeField] private Color panicEmission = new Color(0.2f, 1f, 0.9f);

        private MaterialPropertyBlock _propertyBlock;
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        private void Awake()
        {
            if (bodyRenderer == null)
            {
                bodyRenderer = GetComponentInChildren<Renderer>();
            }

            if (trail == null)
            {
                trail = GetComponentInChildren<TrailRenderer>();
            }

            _propertyBlock = new MaterialPropertyBlock();
            SetPanicVisual(0f);
        }

        public void ApplySimulation(Vector3 position, Vector3 velocity)
        {
            transform.position = position;

            if (velocity.sqrMagnitude > 0.0001f)
            {
                Vector3 forward = ConstrainPitch(velocity.normalized, transform.forward, maxPitchAngleDegrees);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(forward, Vector3.up),
                    0.25f);
            }
        }

        public void SetMaxPitchAngle(float maxPitch)
        {
            maxPitchAngleDegrees = Mathf.Clamp(maxPitch, 0f, 80f);
        }

        private static Vector3 ConstrainPitch(Vector3 desiredForward, Vector3 fallbackForward, float maxPitchAngleDegrees)
        {
            Vector3 horizontal = new Vector3(desiredForward.x, 0f, desiredForward.z);
            if (horizontal.sqrMagnitude < 0.0001f)
            {
                horizontal = new Vector3(fallbackForward.x, 0f, fallbackForward.z);
                if (horizontal.sqrMagnitude < 0.0001f)
                {
                    horizontal = Vector3.forward;
                }
            }

            horizontal.Normalize();

            float maxY = Mathf.Tan(Mathf.Deg2Rad * Mathf.Clamp(maxPitchAngleDegrees, 0f, 80f));
            float clampedY = Mathf.Clamp(desiredForward.y, -maxY, maxY);
            return new Vector3(horizontal.x, clampedY, horizontal.z).normalized;
        }

        public void SetPanicVisual(float panic01)
        {
            panic01 = Mathf.Clamp01(panic01);

            if (bodyRenderer != null)
            {
                bodyRenderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor(EmissionColor, Color.Lerp(calmEmission, panicEmission, panic01));
                bodyRenderer.SetPropertyBlock(_propertyBlock);
            }

            if (trail != null)
            {
                trail.time = trailBaseTime + trailPanicBonus * panic01;
                trail.startWidth = 0.07f + 0.03f * panic01;
                trail.endWidth = 0f;

                Color c0 = Color.Lerp(new Color(0.3f, 0.7f, 0.9f, 0.9f), new Color(0.5f, 1f, 0.9f, 1f), panic01);
                Color c1 = new Color(c0.r, c0.g, c0.b, 0f);
                trail.colorGradient = BuildGradient(c0, c1);
            }
        }

        private static Gradient BuildGradient(Color start, Color end)
        {
            Gradient g = new Gradient();
            g.SetKeys(
                new[]
                {
                    new GradientColorKey(start, 0f),
                    new GradientColorKey(end, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(start.a, 0f),
                    new GradientAlphaKey(end.a, 1f)
                });
            return g;
        }
    }
}
