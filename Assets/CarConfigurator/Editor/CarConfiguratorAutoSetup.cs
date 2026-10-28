#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CarShowroom.EditorTools
{
    /// <summary>
    /// Model-aware, conservative setup for DeenuMaurya/Car_Configurator.
    /// Operates on a scene instance only. All controller scripts go on an EMPTY parent
    /// CarConfiguratorSystem, while the actual model stays a child without new scripts.
    /// Source FBX/prefab assets are not overwritten.
    /// </summary>
    public static class CarConfiguratorAutoSetup
    {
        private const string SourcePrefabPath =
            "Assets/RealisticCars_HD/RealisticCar01_HD/Prefabs/Car01_Driving/RealisticCar01_Driving_Prefab08_RightHanded.prefab";

        [MenuItem("Tools/Car Configurator/Create Car Instance + Auto Setup")]
        public static void CreateInstanceAndSetup()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePrefabPath);
            if (!prefab)
            {
                EditorUtility.DisplayDialog("Car Configurator", "Car prefab not found at expected path. Select an existing car root and use Auto Setup Selected Car.", "OK");
                return;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (!instance) return;
            Undo.RegisterCreatedObjectUndo(instance, "Create Car Configurator Instance");
            Selection.activeGameObject = instance;
            Setup(instance);
        }

        [MenuItem("Tools/Car Configurator/Auto Setup Selected Car")]
        public static void SetupSelected()
        {
            GameObject selected = Selection.activeGameObject;
            if (!selected || EditorUtility.IsPersistent(selected))
            {
                EditorUtility.DisplayDialog("Car Configurator", "Select a car model or CarConfiguratorSystem in Hierarchy (not an asset in Project view).", "OK");
                return;
            }
            Setup(selected);
        }

        [MenuItem("Tools/Car Configurator/Save Selected As NEW Configured Prefab")]
        public static void SaveNewPrefab()
        {
            GameObject selected = Selection.activeGameObject;
            if (!selected || EditorUtility.IsPersistent(selected))
            {
                EditorUtility.DisplayDialog("Car Configurator", "Select the CarConfiguratorSystem in Hierarchy first.", "OK");
                return;
            }
            if (selected.GetComponent<CarConfiguratorManager>() == null &&
                selected.transform.parent && selected.transform.parent.GetComponent<CarConfiguratorManager>())
                selected = selected.transform.parent.gameObject;
            if (selected.GetComponent<CarConfiguratorManager>() == null)
            {
                EditorUtility.DisplayDialog("Car Configurator", "Select the empty CarConfiguratorSystem parent (with CarConfiguratorManager).", "OK");
                return;
            }
            string folder = "Assets/CarConfigurator/Generated";
            if (!AssetDatabase.IsValidFolder("Assets/CarConfigurator")) AssetDatabase.CreateFolder("Assets", "CarConfigurator");
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/CarConfigurator", "Generated");
            string uniquePath = AssetDatabase.GenerateUniqueAssetPath(folder + "/CarConfigurator_Configured.prefab");
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(selected, uniquePath);
            if (saved) Debug.Log("[Car Configurator] New prefab saved: " + uniquePath);
        }

        private static void Setup(GameObject selectedModel)
        {
            List<string> warnings = new List<string>();
            GameObject system = GetOrCreateSystemParent(selectedModel);
            if (!system) return;
            Dictionary<string, Transform> indexed = new Dictionary<string, Transform>(StringComparer.OrdinalIgnoreCase);
            foreach (Transform t in system.GetComponentsInChildren<Transform>(true))
                if (!indexed.ContainsKey(t.name)) indexed.Add(t.name, t);

            CarConfiguratorManager configurator = GetOrAdd<CarConfiguratorManager>(system);
            VehicleMotionController motions = GetOrAdd<VehicleMotionController>(system);
            VehicleLightsAndSounds lights = GetOrAdd<VehicleLightsAndSounds>(system);
            CarCabinControls cabin = GetOrAdd<CarCabinControls>(system);
            AICommandExecutor ai = GetOrAdd<AICommandExecutor>(system);
            ShowroomFeatureController features = GetOrAdd<ShowroomFeatureController>(system);
            CarInteriorAppearanceController appearance = GetOrAdd<CarInteriorAppearanceController>(system);
            CarConfiguratorUIController ui = GetOrAdd<CarConfiguratorUIController>(system);
            CarConfiguratorFeatureActions actions = GetOrAdd<CarConfiguratorFeatureActions>(system);
            SetReference(ai, "configurator", configurator);
            SetReference(ai, "motions", motions);
            SetReference(ai, "lighting", lights);
            SetReference(ai, "cabin", cabin);
            SetReference(features, "cabin", cabin);
            SetReference(appearance, "configurator", configurator);
            SetReference(ui, "config", configurator);
            SetReference(ui, "motions", motions);
            SetReference(ui, "cabin", cabin);
            SetReference(ui, "appearance", appearance);
            SetReference(actions, "configurator", configurator);
            SetReference(actions, "motions", motions);
            SetReference(actions, "lights", lights);
            SetReference(actions, "cabin", cabin);
            SetReference(actions, "features", features);
            SetReference(actions, "appearance", appearance);
            SetReference(ai, "featureActions", actions);

            // The prefab has specific Car01_Body_LOD0..3 objects. These renderers are suggestions:
            // if a renderer combines paint and non-paint materials, manually narrow its material slots.
            List<Renderer> paintCandidates = new List<Renderer>();
            for (int i = 0; i <= 3; i++)
            {
                Transform body = Find(indexed, "Car01_Body_LOD" + i);
                if (body) paintCandidates.AddRange(body.GetComponentsInChildren<Renderer>(true));
            }
            SetReferenceArray(configurator, "bodyRenderers", paintCandidates.Distinct().ToArray());
            if (paintCandidates.Count == 0) warnings.Add("No body LOD renderers found. Paint references need manual assignment.");
            else warnings.Add("Review bodyRenderers: Car01_Body LOD renderers may contain glass/chrome submaterials. Color only the verified paint material slots.");

            // Never assign Car01_Interior_LOD0 as seatRenderers: it can recolor the whole dashboard.
            warnings.Add("Seat material/color references NOT assigned: no unambiguous seat-only renderer found.");

            AddMotionIfFound(motions, indexed, "front_left_door", "Car01_Door_Front_Left", warnings);
            AddMotionIfFound(motions, indexed, "front_right_door", "Car01_Door_Front_Right", warnings);
            AddMotionIfFound(motions, indexed, "rear_left_door", "Car01_Door_Back_Left", warnings);
            AddMotionIfFound(motions, indexed, "rear_right_door", "Car01_Door_Back_Right", warnings);
            foreach (string missing in new[] { "front_left_window", "front_right_window",
                         "rear_left_window", "rear_right_window" })
                warnings.Add("Needs separate pivot/mesh mapping: " + missing);

            SetReferenceArray(lights, "leftIndicators", FindGO(indexed, "Car01_TurnLights_Left"));
            SetReferenceArray(lights, "rightIndicators", FindGO(indexed, "Car01_TurnLights_Right"));
            SetReferenceArray(lights, "brakeEmissiveMeshes", FindGO(indexed, "Car01_BrakeLights"));
            warnings.Add("Headlights, fog lights and sounds need verified Light, emissive-material and AudioClip references.");
            warnings.Add("Door opening Euler angles default to ZERO: verify hinge pivots and set appropriate angles before testing.");
            warnings.Add("Configure URP paint/leather presets, wheel/grille variants, prices, seat anchors and UI manually.");
            warnings.Add("AR scene uses existing template; still needs ARCarPlacementManager and MobileExperienceUI wiring.");
            warnings.Add("All new scripts are on the empty CarConfiguratorSystem parent; model meshes remain in its child hierarchy.");
            warnings.Add("If older controller scripts were already added directly on the model, inspect and remove those duplicates manually after checking serialized references.");

            EditorUtility.SetDirty(configurator);
            EditorUtility.SetDirty(motions);
            EditorUtility.SetDirty(lights);
            Selection.activeGameObject = system;
            string report = "[Car Configurator] Setup on " + system.name + ".\n" + string.Join("\n", warnings.Select(w => "- " + w));
            Debug.LogWarning(report, system);
            EditorUtility.DisplayDialog("Car Configurator Setup Complete", "Known 4-door/lighting references mapped. " + warnings.Count + " review items logged in Console. Source model was NOT overwritten.", "OK");
        }

        private static GameObject GetOrCreateSystemParent(GameObject selected)
        {
            if (selected.name == "CarConfiguratorSystem" || selected.GetComponent<CarConfiguratorManager>())
                return selected;
            Transform existingParent = selected.transform.parent;
            if (existingParent && existingParent.name == "CarConfiguratorSystem")
                return existingParent.gameObject;

            Transform oldParent = selected.transform.parent;
            int siblingIndex = selected.transform.GetSiblingIndex();
            GameObject system = new GameObject("CarConfiguratorSystem");
            Undo.RegisterCreatedObjectUndo(system, "Create Car Configurator System");
            if (oldParent) Undo.SetTransformParent(system.transform, oldParent, "Parent Car System");
            system.transform.SetPositionAndRotation(selected.transform.position, selected.transform.rotation);
            system.transform.localScale = Vector3.one;
            system.transform.SetSiblingIndex(siblingIndex);
            Undo.SetTransformParent(selected.transform, system.transform, "Move Car Under Car System");
            return system;
        }

        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            T existing = go.GetComponent<T>();
            return existing ? existing : Undo.AddComponent<T>(go);
        }

        private static void SetReference(Component owner, string field, UnityEngine.Object target)
        {
            SerializedObject serialized = new SerializedObject(owner);
            SerializedProperty property = serialized.FindProperty(field);
            if (property == null || property.objectReferenceValue != null) return;
            property.objectReferenceValue = target;
            serialized.ApplyModifiedProperties();
        }

        private static Transform Find(Dictionary<string, Transform> indexed, string name)
        {
            return indexed.TryGetValue(name, out Transform found) ? found : null;
        }

        private static GameObject[] FindGO(Dictionary<string, Transform> indexed, string name)
        {
            Transform found = Find(indexed, name);
            return found ? new[] { found.gameObject } : Array.Empty<GameObject>();
        }

        private static void SetReferenceArray<T>(Component owner, string field, T[] items) where T : UnityEngine.Object
        {
            if (items == null || items.Length == 0) return;
            SerializedObject serialized = new SerializedObject(owner);
            SerializedProperty property = serialized.FindProperty(field);
            if (property == null || !property.isArray || property.arraySize > 0) return; // preserve user choices
            property.arraySize = items.Length;
            for (int i = 0; i < items.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
            serialized.ApplyModifiedProperties();
        }

        private static void AddMotionIfFound(VehicleMotionController owner,
            Dictionary<string, Transform> indexed, string id, string meshName, List<string> warnings)
        {
            Transform pivot = Find(indexed, meshName);
            if (!pivot) { warnings.Add("Missing door object: " + meshName); return; }
            SerializedObject serialized = new SerializedObject(owner);
            SerializedProperty list = serialized.FindProperty("movingParts");
            if (list == null) return;
            for (int i = 0; i < list.arraySize; i++)
                if (list.GetArrayElementAtIndex(i).FindPropertyRelative("id").stringValue == id) return;
            int index = list.arraySize;
            list.InsertArrayElementAtIndex(index);
            SerializedProperty entry = list.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative("id").stringValue = id;
            entry.FindPropertyRelative("pivot").objectReferenceValue = pivot;
            entry.FindPropertyRelative("motionType").enumValueIndex = (int)MotionType.Rotate;
            entry.FindPropertyRelative("openEulerOffset").vector3Value = Vector3.zero;
            entry.FindPropertyRelative("openLocalOffset").vector3Value = Vector3.zero;
            entry.FindPropertyRelative("duration").floatValue = 0.8f;
            entry.FindPropertyRelative("exterior").boolValue = true;
            entry.FindPropertyRelative("openSound").objectReferenceValue = null;
            entry.FindPropertyRelative("closeSound").objectReferenceValue = null;
            serialized.ApplyModifiedProperties();
        }
    }
}
#endif
