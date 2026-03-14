using SchoolOfFish.Data;
using UnityEngine;

namespace SchoolOfFish.Core
{
    public class BoidsRuntimeTuningPanel : MonoBehaviour
    {
        [SerializeField] private BoidsSettings settings;
        [SerializeField] private BoidsManager boidsManager;
        [SerializeField] private KeyCode toggleKey = KeyCode.F2;
        [SerializeField] private bool visibleOnStart;

        private Rect _windowRect = new Rect(20f, 20f, 380f, 520f);
        private bool _visible;

        private void Awake()
        {
            _visible = visibleOnStart;

            if (boidsManager == null)
            {
                boidsManager = FindFirstObjectByType<BoidsManager>();
            }

            if (settings == null && boidsManager != null)
            {
                settings = boidsManager.Settings;
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                _visible = !_visible;
            }
        }

        private void OnGUI()
        {
            if (!_visible)
            {
                return;
            }

            _windowRect = GUI.Window(GetInstanceID(), _windowRect, DrawWindow, "Boids Runtime Tuning");
        }

        private void DrawWindow(int id)
        {
            if (settings == null)
            {
                GUILayout.Label("BoidsSettings reference is missing.");
                if (GUILayout.Button("Retry Find"))
                {
                    boidsManager = boidsManager != null ? boidsManager : FindFirstObjectByType<BoidsManager>();
                    settings = boidsManager != null ? boidsManager.Settings : null;
                }

                GUI.DragWindow(new Rect(0f, 0f, 9999f, 20f));
                return;
            }

            GUILayout.Label("[F2] Toggle this panel");
            GUILayout.Space(4f);

            settings.baseMaxSpeed = Slider("Base Max Speed", settings.baseMaxSpeed, 0.5f, 20f);
            settings.baseMaxForce = Slider("Base Max Force", settings.baseMaxForce, 0.5f, 30f);
            settings.neighborRadius = Slider("Neighbor Radius", settings.neighborRadius, 0.5f, 10f);
            settings.separationRadius = Slider("Separation Radius", settings.separationRadius, 0.1f, 5f);
            settings.separationWeight = Slider("Separation Weight", settings.separationWeight, 0f, 4f);
            settings.alignmentWeight = Slider("Alignment Weight", settings.alignmentWeight, 0f, 4f);
            settings.cohesionWeight = Slider("Cohesion Weight", settings.cohesionWeight, 0f, 4f);
            settings.wanderWeight = Slider("Wander Weight", settings.wanderWeight, 0f, 2f);
            settings.boundaryWeight = Slider("Boundary Weight", settings.boundaryWeight, 0f, 2f);
            settings.predatorFearRadius = Slider("Predator Fear Radius", settings.predatorFearRadius, 0f, 20f);
            settings.panicDuration = Slider("Panic Duration", settings.panicDuration, 0f, 10f);
            settings.panicSpreadThreshold = Slider("Panic Spread Threshold", settings.panicSpreadThreshold, 0f, 1f);
            settings.panicSpeedMultiplier = Slider("Panic Speed Mult", settings.panicSpeedMultiplier, 1f, 3f);
            settings.panicForceMultiplier = Slider("Panic Force Mult", settings.panicForceMultiplier, 1f, 3f);
            settings.nightAlignmentMultiplier = Slider("Night Align Mult", settings.nightAlignmentMultiplier, 0f, 1f);
            settings.nightSpeedMultiplier = Slider("Night Speed Mult", settings.nightSpeedMultiplier, 0f, 1f);
            settings.hashCellSize = Slider("Hash Cell Size", settings.hashCellSize, 0.5f, 10f);
            settings.maxNeighborCellsPerAxis = Mathf.RoundToInt(Slider("Neighbor Cell Range", settings.maxNeighborCellsPerAxis, 1f, 4f));

            GUILayout.Space(6f);
            GUILayout.Label("Note: fishCount is spawn-time only.");
            GUILayout.Label("To change count, stop and respawn fish.");

            GUI.DragWindow(new Rect(0f, 0f, 9999f, 20f));
        }

        private static float Slider(string label, float value, float min, float max)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(170f));
            float newValue = GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(130f));
            GUILayout.Label(newValue.ToString("0.00"), GUILayout.Width(60f));
            GUILayout.EndHorizontal();
            return newValue;
        }
    }
}
