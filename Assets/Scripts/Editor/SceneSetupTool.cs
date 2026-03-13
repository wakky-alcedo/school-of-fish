using SchoolOfFish.Core;
using SchoolOfFish.Data;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SchoolOfFish.Editor
{
    public static class SceneSetupTool
    {
        private const string MenuRoot = "School Of Fish/";
        private const string SettingsFolder = "Assets/Settings";
        private const string PrefabsFolder = "Assets/Prefabs";

        [MenuItem(MenuRoot + "1. Create Settings Assets")]
        public static void CreateSettingsAssets()
        {
            CreateOrLoad<BoidsSettings>("Assets/Settings/BoidsSettings.asset");
            CreateOrLoad<FishPersonality>("Assets/Settings/FishPersonality_Default.asset");
            CreateOrLoad<TimeCycleSettings>("Assets/Settings/TimeCycleSettings.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[SchoolOfFish] Settings assets created in Assets/Settings/");
        }

        [MenuItem(MenuRoot + "2. Create Fish Prefab")]
        public static void CreateFishPrefab()
        {
            string prefabPath = PrefabsFolder + "/Fish.prefab";

            // ルートオブジェクト
            GameObject root = new GameObject("Fish");

            // ボディ(涙型近似: CapsuledをScaleで代用)
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(root.transform);
            body.transform.localPosition = Vector3.zero;
            body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            body.transform.localScale = new Vector3(0.18f, 0.35f, 0.18f);

            // Colliderは不要なので削除
            Object.DestroyImmediate(body.GetComponent<CapsuleCollider>());

            // Materilaが存在しなければ作る
            string matPath = "Assets/Settings/FishMaterial.mat";
            Material mat;
            if (!System.IO.File.Exists(Application.dataPath + "/Settings/FishMaterial.mat"))
            {
                Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
                mat = new Material(urpLit != null ? urpLit : Shader.Find("Standard"));
                mat.SetColor("_BaseColor", new Color(0.25f, 0.7f, 0.9f));
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", new Color(0.02f, 0.15f, 0.2f));
                mat.enableInstancing = true;
                AssetDatabase.CreateAsset(mat, matPath);
            }
            else
            {
                mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            }

            body.GetComponent<Renderer>().sharedMaterial = mat;

            // TrailRenderer
            TrailRenderer trail = root.AddComponent<TrailRenderer>();
            trail.time = 0.55f;
            trail.startWidth = 0.07f;
            trail.endWidth = 0f;
            trail.minVertexDistance = 0.05f;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.receiveShadows = false;

            // Trail用マテリアル
            string trailMatPath = "Assets/Settings/TrailMaterial.mat";
            if (!System.IO.File.Exists(Application.dataPath + "/Settings/TrailMaterial.mat"))
            {
                Shader particles = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                Material trailMat = new Material(particles != null ? particles : Shader.Find("Sprites/Default"));
                trailMat.SetColor("_BaseColor", new Color(0.4f, 0.85f, 1f, 0.85f));
                AssetDatabase.CreateAsset(trailMat, trailMatPath);
                trail.material = trailMat;
            }
            else
            {
                trail.material = AssetDatabase.LoadAssetAtPath<Material>(trailMatPath);
            }

            // FishAgentコンポーネント
            FishAgent agent = root.AddComponent<FishAgent>();

            // プレハブ化して保存
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[SchoolOfFish] Fish prefab created at " + prefabPath);
        }

        [MenuItem(MenuRoot + "3. Wire Scene References")]
        public static void WireSceneReferences()
        {
            BoidsManager boidsManager = Object.FindFirstObjectByType<BoidsManager>();
            EnvironmentProvider envProvider = Object.FindFirstObjectByType<EnvironmentProvider>();
            PredatorController predator = Object.FindFirstObjectByType<PredatorController>();
            CameraManager camManager = Object.FindFirstObjectByType<CameraManager>();

            if (boidsManager == null)
            {
                Debug.LogError("[SchoolOfFish] BoidsManager not found in scene.");
                return;
            }

            BoidsSettings boidsSO = AssetDatabase.LoadAssetAtPath<BoidsSettings>("Assets/Settings/BoidsSettings.asset");
            TimeCycleSettings timeSO = AssetDatabase.LoadAssetAtPath<TimeCycleSettings>("Assets/Settings/TimeCycleSettings.asset");
            FishPersonality personalitySO = AssetDatabase.LoadAssetAtPath<FishPersonality>("Assets/Settings/FishPersonality_Default.asset");
            GameObject fishPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Fish.prefab");

            // BoidsManager への設定
            SerializedObject soBoidsManager = new SerializedObject(boidsManager);
            SetProperty(soBoidsManager, "settings", boidsSO);
            SetProperty(soBoidsManager, "fishPrefab", fishPrefab != null ? fishPrefab.GetComponent<FishAgent>() : null);
            SetProperty(soBoidsManager, "predator", predator);
            SetProperty(soBoidsManager, "environmentProvider", envProvider);
            SetProperty(soBoidsManager, "personalityPool", new Object[] { personalitySO });
            soBoidsManager.ApplyModifiedProperties();

            // EnvironmentProvider への設定
            if (envProvider != null)
            {
                Light dirLight = Object.FindFirstObjectByType<Light>();
                Volume globalVolume = Object.FindFirstObjectByType<Volume>();

                SerializedObject soEnv = new SerializedObject(envProvider);
                SetProperty(soEnv, "settings", timeSO);
                SetProperty(soEnv, "directionalLight", dirLight);
                SetProperty(soEnv, "globalVolume", globalVolume);
                soEnv.ApplyModifiedProperties();
            }

            // CameraManager への設定 (Main Cameraを固定カメラとして設定)
            if (camManager != null)
            {
                Camera mainCam = Camera.main;
                SerializedObject soCam = new SerializedObject(camManager);
                SetProperty(soCam, "fixedCamera", mainCam);
                soCam.ApplyModifiedProperties();
            }

            // BoidsManager の schoolRoot を自身に設定
            {
                SerializedObject so = new SerializedObject(boidsManager);
                so.FindProperty("schoolRoot").objectReferenceValue = boidsManager.transform;
                so.ApplyModifiedProperties();
            }

            // グローバルVolumeにpost-processコンポーネントを追加（なければ）
            SetupVolumeOverrides();

            EditorUtility.SetDirty(boidsManager);
            if (envProvider != null) EditorUtility.SetDirty(envProvider);
            if (camManager != null) EditorUtility.SetDirty(camManager);

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());

            Debug.Log("[SchoolOfFish] Scene references wired. Press Ctrl+S to save.");
        }

        [MenuItem(MenuRoot + "Run All Setup Steps")]
        public static void RunAll()
        {
            CreateSettingsAssets();
            CreateFishPrefab();
            WireSceneReferences();
            Debug.Log("[SchoolOfFish] All setup steps complete.");
        }

        private static void SetupVolumeOverrides()
        {
            Volume vol = Object.FindFirstObjectByType<Volume>();
            if (vol == null || vol.profile == null)
            {
                return;
            }

            VolumeProfile profile = vol.profile;

            if (!profile.Has<Bloom>())
            {
                Bloom bloom = profile.Add<Bloom>(true);
                bloom.intensity.value = 0.5f;
                bloom.threshold.value = 0.9f;
            }

            if (!profile.Has<DepthOfField>())
            {
                DepthOfField dof = profile.Add<DepthOfField>(true);
                dof.mode.value = DepthOfFieldMode.Bokeh;
                dof.focusDistance.value = 10f;
            }

            if (!profile.Has<Vignette>())
            {
                Vignette vignette = profile.Add<Vignette>(true);
                vignette.intensity.value = 0.2f;
            }

            if (!profile.Has<ColorAdjustments>())
            {
                ColorAdjustments ca = profile.Add<ColorAdjustments>(true);
                ca.postExposure.value = 0f;
            }

            EditorUtility.SetDirty(profile);
        }

        private static T CreateOrLoad<T>(string path) where T : ScriptableObject
        {
            T existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
            {
                return existing;
            }

            T asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void SetProperty(SerializedObject so, string propName, Object value)
        {
            SerializedProperty prop = so.FindProperty(propName);
            if (prop != null)
            {
                prop.objectReferenceValue = value;
            }
        }

        private static void SetProperty(SerializedObject so, string propName, Object[] values)
        {
            SerializedProperty prop = so.FindProperty(propName);
            if (prop == null || !prop.isArray)
            {
                return;
            }

            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }
    }
}
