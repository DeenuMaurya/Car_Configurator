#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CarShowroom.EditorTools
{
    /// <summary>
    /// Explicit, repeat-safe editor command. Adds a separate mobile scroll panel and
    /// empty reference slots; NEVER alters the existing MenuBand or original car meshes.
    /// </summary>
    public static class CarConfiguratorFeatureScaffolder
    {
        private struct Option
        {
            public string label, action, value;
            public Option(string caption, string command, string parameter = "")
            { label = caption; action = command; value = parameter; }
        }

        private const string GeneratedPath = "Assets/CarConfigurator/Generated";
        private const string MaterialPath = GeneratedPath + "/Materials";

        [MenuItem("Tools/Car Configurator/Create Missing UI + Assignment Slots")]
        public static void CreateAllMenuItem() => CreateAll(false);

        public static void CreateAll(bool silent = false)
        {
            GameObject system = GameObject.Find("CarConfiguratorSystem");
            if (!system)
            {
                system = new GameObject("CarConfiguratorSystem");
                Undo.RegisterCreatedObjectUndo(system, "Create CarConfiguratorSystem");
            }

            CarConfiguratorManager manager = system.GetComponent<CarConfiguratorManager>();
            if (!manager) manager = Undo.AddComponent<CarConfiguratorManager>(system);
            VehicleMotionController motions = GetOrAdd<VehicleMotionController>(system);
            VehicleLightsAndSounds lights = GetOrAdd<VehicleLightsAndSounds>(system);
            CarCabinControls cabin = GetOrAdd<CarCabinControls>(system);
            ShowroomFeatureController cabinFeatures = GetOrAdd<ShowroomFeatureController>(system);
            CarInteriorAppearanceController appearance = GetOrAdd<CarInteriorAppearanceController>(system);
            CarShowcaseServices showcase = GetOrAdd<CarShowcaseServices>(system);
            ShowroomPartPicker partPicker = GetOrAdd<ShowroomPartPicker>(system);
            ExperienceModeManager sceneMode = UnityEngine.Object.FindFirstObjectByType<ExperienceModeManager>();
            bool isAR = SceneManager.GetActiveScene().name.StartsWith("AR", StringComparison.OrdinalIgnoreCase) || SceneManager.GetActiveScene().name.IndexOf("_AR", StringComparison.OrdinalIgnoreCase) >= 0;
            if (!sceneMode)
            {
                sceneMode = GetOrAdd<ExperienceModeManager>(system);
                SerializedObject serialized = new SerializedObject(sceneMode);
                serialized.FindProperty("mode").enumValueIndex = isAR ? 1 : 0;
                serialized.ApplyModifiedProperties();
            }
            isAR |= !sceneMode.SeatNavigationAllowed;
            CarSeatNavigator navigator = isAR ? null : GetOrAdd<CarSeatNavigator>(system);
            CarConfiguratorFeatureActions hub = system.GetComponent<CarConfiguratorFeatureActions>();
            if (!hub) hub = Undo.AddComponent<CarConfiguratorFeatureActions>(system);
            AICommandExecutor ai = GetOrAdd<AICommandExecutor>(system);
            SimpleTextCommandAdapter textCommands = GetOrAdd<SimpleTextCommandAdapter>(system);
            AssignReferenceIfEmpty(hub, "configurator", manager);
            AssignReferenceIfEmpty(hub, "motions", motions);
            AssignReferenceIfEmpty(hub, "lights", lights);
            AssignReferenceIfEmpty(hub, "cabin", cabin);
            AssignReferenceIfEmpty(hub, "features", cabinFeatures);
            AssignReferenceIfEmpty(hub, "appearance", appearance);
            AssignReferenceIfEmpty(cabinFeatures, "cabin", cabin);
            AssignReferenceIfEmpty(appearance, "configurator", manager);
            AssignReferenceIfEmpty(hub, "showcase", showcase);
            AssignReferenceIfEmpty(hub, "mode", sceneMode);
            AssignReferenceIfEmpty(hub, "seats", navigator);
            AssignReferenceIfEmpty(hub, "orbit", UnityEngine.Object.FindFirstObjectByType<MobileShowroomOrbit>());
            AssignReferenceIfEmpty(showcase, "configurator", manager);
            AssignReferenceIfEmpty(partPicker, "showcase", showcase);
            AssignReferenceIfEmpty(navigator, "experienceMode", sceneMode);
            AssignReferenceIfEmpty(cabinFeatures, "mode", sceneMode);
            AssignReferenceIfEmpty(motions, "mode", sceneMode);
            AssignReferenceIfEmpty(ai, "configurator", manager);
            AssignReferenceIfEmpty(ai, "motions", motions);
            AssignReferenceIfEmpty(ai, "lighting", lights);
            AssignReferenceIfEmpty(ai, "cabin", cabin);
            AssignReferenceIfEmpty(ai, "featureActions", hub);
            AssignReferenceIfEmpty(textCommands, "featureActions", hub);
            AssignReferenceIfEmpty(textCommands, "executor", ai);

            CreateReferenceSlots(system.transform, manager, motions);
            CreateUI(hub, textCommands, isAR);
            CreateMaterialPresets();
            Selection.activeGameObject = system;
            EditorSceneManager.MarkSceneDirty(system.scene);
            Debug.Log("[Car Configurator] Created assignment slots + separate FeaturePanel. Assign real model parts, renderers, lights, animators and audio in the Inspector.", system);
            if (!silent) EditorUtility.DisplayDialog("Car Configurator", "Feature UI and empty assignment slots created. Open the FEATURES tab in Play Mode. Assign car meshes/materials and controller references in Inspector. Existing UI was preserved.", "OK");
        }

        [MenuItem("Tools/Car Configurator/Validate Feature Assignments")]
        public static void ValidateAssignmentsMenuItem() => ValidateAssignments(false);

        public static List<string> ValidateAssignments(bool silent = false)
        {
            CarConfiguratorManager manager = UnityEngine.Object.FindFirstObjectByType<CarConfiguratorManager>();
            if (!manager)
            {
                if (!silent) EditorUtility.DisplayDialog("Car Configurator", "No CarConfiguratorManager found. Create feature slots first.", "OK");
                return new List<string> { "No CarConfiguratorManager found" };
            }
            var missing = new List<string>();
            SerializedObject config = new SerializedObject(manager);
            CheckArray(config, "bodyRenderers", missing);
            CheckArray(config, "seatRenderers", missing);
            CheckArray(config, "upholsteryRenderers", missing);
            foreach (string field in new[] { "glossyPaint", "metallicPaint", "mattePaint", "fabricMaterial", "leatherMaterial", "premiumLeatherMaterial" })
                if (!config.FindProperty(field).objectReferenceValue) missing.Add("Material: " + field);
            SerializedProperty parts = config.FindProperty("parts");
            for (int i = 0; i < parts.arraySize; i++)
            {
                SerializedProperty entry = parts.GetArrayElementAtIndex(i);
                if (!entry.FindPropertyRelative("root").objectReferenceValue)
                    missing.Add("Part mesh: " + entry.FindPropertyRelative("id").stringValue);
            }
            foreach (string field in new[] { "wheels", "grilles", "dashboardTrims", "steeringWheels" })
            {
                SerializedProperty styles = config.FindProperty(field);
                for (int i = 0; i < styles.arraySize; i++)
                {
                    SerializedProperty entry = styles.GetArrayElementAtIndex(i);
                    if (!entry.FindPropertyRelative("root").objectReferenceValue)
                        missing.Add(field + " mesh: " + entry.FindPropertyRelative("id").stringValue);
                }
            }
            VehicleMotionController motions = UnityEngine.Object.FindFirstObjectByType<VehicleMotionController>();
            if (motions)
            {
                SerializedObject controller = new SerializedObject(motions);
                SerializedProperty slots = controller.FindProperty("movingParts");
                for (int i = 0; i < slots.arraySize; i++)
                {
                    SerializedProperty entry = slots.GetArrayElementAtIndex(i);
                    if (!entry.FindPropertyRelative("pivot").objectReferenceValue)
                        missing.Add("Moving pivot: " + entry.FindPropertyRelative("id").stringValue);
                }
            }
            string message = missing.Count == 0 ? "All checked references are assigned. Verify visual behavior in Play Mode." :
                missing.Count + " references still need assignment. See Console for full list.";
            if (missing.Count > 0) Debug.LogWarning("[Car Configurator] Missing assignments:\n- " + string.Join("\n- ", missing), manager);
            if (!silent) EditorUtility.DisplayDialog("Car Configurator Validator", message, "OK");
            return missing;
        }

        private static void CheckArray(SerializedObject obj, string field, List<string> missing)
        {
            SerializedProperty array = obj.FindProperty(field);
            if (array == null || array.arraySize == 0) missing.Add("Renderer array: " + field);
        }

        private static T GetOrAdd<T>(GameObject go) where T : Component =>
            go.GetComponent<T>() ? go.GetComponent<T>() : Undo.AddComponent<T>(go);

        private static void CreateReferenceSlots(Transform system, CarConfiguratorManager manager, VehicleMotionController motions)
        {
            Transform root = GetOrCreate(system, "FeatureAssignmentSlots (replace with car meshes)");
            Transform accessory = GetOrCreate(root, "OptionalParts");
            string[] partIds = { "body_kit", "spoiler", "roof_rail", "badge", "chrome", "cladding",
                "door_visor", "headlight_upgrade", "panoramic_roof", "fog_lamp_trim", "side_steps", "sport_diffuser" };
            foreach (string id in partIds) GetOrCreate(accessory, id + "_ASSIGN_MESH_HERE");

            AddMissingParts(manager, partIds);
            AddMissingStyles(manager, "wheels", new[] { "standard", "alloy1", "alloy2" });
            AddMissingStyles(manager, "grilles", new[] { "standard", "sport", "premium" });
            AddMissingStyles(manager, "dashboardTrims", new[] { "standard", "wood", "carbon" });
            AddMissingStyles(manager, "steeringWheels", new[] { "standard", "sport", "premium" });

            Transform pivots = GetOrCreate(root, "DoorAndWindowPivots");
            foreach (string name in new[] { "front_left_door", "front_right_door", "rear_left_door", "rear_right_door",
                         "front_left_window", "front_right_window", "rear_left_window", "rear_right_window" })
                GetOrCreate(pivots, name + "_REFERENCE_ONLY");
            AddMissingMotionSlots(motions);
            Transform anchors = GetOrCreate(root, "ShowroomSeatAnchors (never use in AR)");
            foreach (string name in new[] { "Exterior", "Driver", "FrontPassenger", "RearLeft", "RearRight" })
                GetOrCreate(anchors, name + "Anchor_ASSIGN_TRANSFORM");
            Transform cabin = GetOrCreate(root, "InteriorEffectsAndAudio");
            foreach (string name in new[] { "Sunroof_Animator", "Infotainment_Home", "Infotainment_Audio",
                "Infotainment_Climate", "PremiumSpeakers", "DigitalCluster", "WirelessCharger",
                "AmbientLight", "ACIndicator", "MusicAudio", "EngineAudio", "HornAudio" })
                GetOrCreate(cabin, name + "_ASSIGN_REFERENCE");
            Transform lighting = GetOrCreate(root, "ExteriorLightingReferences");
            foreach (string name in new[] { "Headlights", "HighBeams", "FogLights", "Indicators",
                "BrakeLights", "DayEnvironment", "NightEnvironment" })
                GetOrCreate(lighting, name + "_ASSIGN_REFERENCE");
        }

        private static void AddMissingParts(CarConfiguratorManager manager, IEnumerable<string> ids)
        {
            SerializedObject owner = new SerializedObject(manager);
            SerializedProperty list = owner.FindProperty("parts");
            foreach (string id in ids)
            {
                bool exists = false;
                for (int i = 0; i < list.arraySize; i++)
                    if (list.GetArrayElementAtIndex(i).FindPropertyRelative("id").stringValue == id) { exists = true; break; }
                if (exists) continue;
                int index = list.arraySize;
                list.InsertArrayElementAtIndex(index);
                SerializedProperty part = list.GetArrayElementAtIndex(index);
                part.FindPropertyRelative("id").stringValue = id;
                part.FindPropertyRelative("root").objectReferenceValue = null; // User will assign the correct mesh.
                part.FindPropertyRelative("isOptional").boolValue = true;
                part.FindPropertyRelative("isExterior").boolValue = true;
                part.FindPropertyRelative("minimumVariant").enumValueIndex = 0;
                part.FindPropertyRelative("price").intValue = 0;
            }
            owner.ApplyModifiedProperties();
        }

        private static void AddMissingStyles(CarConfiguratorManager manager, string field, IEnumerable<string> ids)
        {
            SerializedObject owner = new SerializedObject(manager);
            SerializedProperty list = owner.FindProperty(field);
            foreach (string id in ids)
            {
                bool exists = false;
                for (int i = 0; i < list.arraySize; i++)
                    if (list.GetArrayElementAtIndex(i).FindPropertyRelative("id").stringValue == id) { exists = true; break; }
                if (exists) continue;
                int index = list.arraySize;
                list.InsertArrayElementAtIndex(index);
                SerializedProperty entry = list.GetArrayElementAtIndex(index);
                entry.FindPropertyRelative("id").stringValue = id;
                entry.FindPropertyRelative("root").objectReferenceValue = null;
                entry.FindPropertyRelative("minimumVariant").enumValueIndex = 0;
                entry.FindPropertyRelative("price").intValue = 0;
            }
            owner.ApplyModifiedProperties();
        }

        private static void AddMissingMotionSlots(VehicleMotionController controller)
        {
            SerializedObject owner = new SerializedObject(controller);
            SerializedProperty list = owner.FindProperty("movingParts");
            foreach (string id in new[] { "front_left_door", "front_right_door", "rear_left_door", "rear_right_door",
                         "front_left_window", "front_right_window", "rear_left_window", "rear_right_window" })
            {
                bool exists = false;
                for (int i = 0; i < list.arraySize; i++)
                    if (list.GetArrayElementAtIndex(i).FindPropertyRelative("id").stringValue == id) { exists = true; break; }
                if (exists) continue;
                int index = list.arraySize;
                list.InsertArrayElementAtIndex(index);
                SerializedProperty entry = list.GetArrayElementAtIndex(index);
                bool isWindow = id.EndsWith("_window", StringComparison.Ordinal);
                entry.FindPropertyRelative("id").stringValue = id;
                entry.FindPropertyRelative("pivot").objectReferenceValue = null;
                entry.FindPropertyRelative("motionType").enumValueIndex = isWindow ? 1 : 0;
                entry.FindPropertyRelative("openEulerOffset").vector3Value = Vector3.zero;
                entry.FindPropertyRelative("openLocalOffset").vector3Value = isWindow ? new Vector3(0, -.4f, 0) : Vector3.zero;
                entry.FindPropertyRelative("duration").floatValue = .8f;
                entry.FindPropertyRelative("exterior").boolValue = true;
                entry.FindPropertyRelative("openSound").objectReferenceValue = null;
                entry.FindPropertyRelative("closeSound").objectReferenceValue = null;
            }
            owner.ApplyModifiedProperties();
        }

        private static void CreateUI(CarConfiguratorFeatureActions hub, SimpleTextCommandAdapter textCommands, bool isAR)
        {
            GameObject canvasObject = GameObject.Find("Canvas");
            if (!canvasObject)
            {
                canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas),
                    typeof(CanvasScaler), typeof(GraphicRaycaster));
                Undo.RegisterCreatedObjectUndo(canvasObject, "Create Feature UI Canvas");
                canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920);
                scaler.matchWidthOrHeight = .5f;
            }
            if (!GameObject.Find("EventSystem"))
            {
                GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                Undo.RegisterCreatedObjectUndo(eventSystem, "Create UI EventSystem");
            }
            Transform parent = canvasObject.transform;
            if (parent.Find("CarFeaturePanel") || parent.Find("CarFeatureOpenButton"))
            {
                Debug.Log("[Car Configurator] Feature UI already exists. Preserved existing elements.");
                return;
            }

            GameObject panel = CreateUIObject("CarFeaturePanel", parent);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 0);
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(1, .5f);
            rect.sizeDelta = new Vector2(430, -180);
            rect.anchoredPosition = new Vector2(-15, 0);
            Image panelImage = Undo.AddComponent<Image>(panel);
            panelImage.color = new Color(.045f, .06f, .085f, .97f);

            Button open = CreateSimpleButton(parent, "CarFeatureOpenButton", "FEATURES", 110, 60);
            RectTransform openRect = open.GetComponent<RectTransform>();
            openRect.anchorMin = Vector2.one;
            openRect.anchorMax = Vector2.one;
            openRect.pivot = Vector2.one;
            openRect.anchoredPosition = new Vector2(-15, -15);

            Button close = CreateSimpleButton(panel.transform, "Close", "CLOSE   ×", 200, 50);
            RectTransform closeRect = close.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(0, 1);
            closeRect.anchorMax = Vector2.one;
            closeRect.pivot = new Vector2(.5f, 1);
            closeRect.sizeDelta = new Vector2(-20, 50);
            closeRect.anchoredPosition = new Vector2(0, -10);

            GameObject viewport = CreateUIObject("Viewport", panel.transform);
            RectTransform v = viewport.GetComponent<RectTransform>();
            v.anchorMin = Vector2.zero;
            v.anchorMax = Vector2.one;
            v.offsetMin = new Vector2(12, 12);
            v.offsetMax = new Vector2(-12, -70);
            Undo.AddComponent<RectMask2D>(viewport);
            GameObject content = CreateUIObject("Content", viewport.transform);
            RectTransform c = content.GetComponent<RectTransform>();
            c.anchorMin = new Vector2(0, 1);
            c.anchorMax = Vector2.one;
            c.pivot = new Vector2(.5f, 1);
            c.offsetMin = new Vector2(0, 0);
            c.offsetMax = new Vector2(0, 0);
            VerticalLayoutGroup layout = Undo.AddComponent<VerticalLayoutGroup>(content);
            layout.padding = new RectOffset(6, 6, 8, 8);
            layout.spacing = 5;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            ContentSizeFitter fitter = Undo.AddComponent<ContentSizeFitter>(content);
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            ScrollRect scroll = Undo.AddComponent<ScrollRect>(panel);
            scroll.viewport = v;
            scroll.content = c;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            CreateHeader(content.transform, "AI TEXT COMMANDS (speech-to-text input compatible)");
            InputField input = CreateTextInput(content.transform);
            Button send = CreateSimpleButton(content.transform, "SubmitTextCommand", "RUN COMMAND", 380, 46);
            CarFeatureTextCommandUI commandUI = Undo.AddComponent<CarFeatureTextCommandUI>(send.gameObject);
            commandUI.Configure(input, send, textCommands);

            foreach (var group in FeatureCatalog(isAR))
            {
                CreateHeader(content.transform, group.Key);
                foreach (Option item in group.Value)
                {
                    Button button = CreateSimpleButton(content.transform, "Option_" + item.action + "_" + item.value, item.label, 380, 45);
                    CarFeatureOptionButton selector = Undo.AddComponent<CarFeatureOptionButton>(button.gameObject);
                    selector.Configure(hub, item.action, item.value, button.GetComponent<Image>());
                }
            }

            GameObject total = CreateUIObject("PriceBreakdown", content.transform);
            LayoutElement priceLayout = Undo.AddComponent<LayoutElement>(total);
            priceLayout.preferredHeight = 150;
            Text priceText = Undo.AddComponent<Text>(total);
            priceText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            priceText.fontSize = 14;
            priceText.color = new Color(.9f, .93f, 1f);
            priceText.alignment = TextAnchor.UpperLeft;
            priceText.horizontalOverflow = HorizontalWrapMode.Wrap;
            hub.SetGeneratedPriceText(priceText);

            CarFeaturePanelToggle toggle = Undo.AddComponent<CarFeaturePanelToggle>(open.gameObject);
            toggle.Configure(panel, open, close);
            panel.SetActive(false);
        }

        private static Dictionary<string, List<Option>> FeatureCatalog(bool ar)
        {
            var catalog = new Dictionary<string, List<Option>>();
            catalog["EXTERIOR PAINT & FINISH"] = new List<Option> {
                new Option("White", "body_color", "white"), new Option("Black", "body_color", "black"),
                new Option("Red", "body_color", "red"), new Option("Green", "body_color", "green"),
                new Option("Blue", "body_color", "blue"), new Option("Silver", "body_color", "silver"),
                new Option("Orange", "body_color", "orange"), new Option("Beige", "body_color", "beige"),
                new Option("Glossy", "paint_finish", "Glossy"), new Option("Metallic", "paint_finish", "Metallic"),
                new Option("Matte", "paint_finish", "Matte") };
            catalog["WHEELS, GRILLE & EXTERIOR PARTS"] = new List<Option> {
                new Option("Stock Wheels", "wheel", "standard"), new Option("Alloy 1", "wheel", "alloy1"),
                new Option("Alloy 2", "wheel", "alloy2"), new Option("Sport Grille", "grille", "sport"),
                new Option("Premium Grille", "grille", "premium"), new Option("Body Kit", "part_toggle", "body_kit"),
                new Option("Spoiler", "part_toggle", "spoiler"), new Option("Roof Rails", "part_toggle", "roof_rail"),
                new Option("Badges", "part_toggle", "badge"), new Option("Chrome Trim", "part_toggle", "chrome"),
                new Option("Cladding", "part_toggle", "cladding"), new Option("Door Visors", "part_toggle", "door_visor"),
                new Option("Upgraded Headlights", "part_toggle", "headlight_upgrade"),
                new Option("Panoramic Roof Option", "part_toggle", "panoramic_roof"),
                new Option("Side Steps", "part_toggle", "side_steps"),
                new Option("Sport Diffuser", "part_toggle", "sport_diffuser") };
            catalog["DOORS, WINDOWS & LIGHTS"] = new List<Option> {
                new Option("Driver Door", "door_toggle", "front_left_door"), new Option("Front Right Door", "door_toggle", "front_right_door"),
                new Option("Rear Left Door", "door_toggle", "rear_left_door"), new Option("Rear Right Door", "door_toggle", "rear_right_door"),
                new Option("Open All Doors", "all_doors_open"), new Option("Close All Doors", "all_doors_close"),
                new Option("Front Left Window", "window_toggle", "front_left_window"), new Option("Front Right Window", "window_toggle", "front_right_window"),
                new Option("Rear Left Window", "window_toggle", "rear_left_window"), new Option("Rear Right Window", "window_toggle", "rear_right_window"),
                new Option("Headlights", "headlights"), new Option("High Beams", "high_beams"),
                new Option("Fog Lights", "fog_lights"), new Option("Brake Lights", "brake_lights"),
                new Option("Hazards", "hazard"), new Option("Left Signal", "left_indicator"),
                new Option("Right Signal", "right_indicator"), new Option("Horn Sound", "honk"),
                new Option("Lock Sound", "lock"), new Option("Unlock Sound", "unlock") };
            catalog["INTERIOR UPHOLSTERY & TRIM"] = new List<Option> {
                new Option("Fabric", "upholstery", "Fabric"), new Option("Leather", "upholstery", "Leather"),
                new Option("Premium Leather", "upholstery", "PremiumLeather"),
                new Option("Front Seat White", "seat_color", "white"), new Option("Front Seat Beige", "seat_color", "beige"),
                new Option("Front Seat Brown", "seat_color", "brown"), new Option("Front Seat Black", "seat_color", "black"),
                new Option("Rear Seat White", "rear_seat_color", "white"), new Option("Rear Seat Brown", "rear_seat_color", "brown"),
                new Option("Rear Seat Black", "rear_seat_color", "black"),
                new Option("Dashboard Black", "dashboard_color", "black"), new Option("Dashboard Tan", "dashboard_color", "tan"),
                new Option("Door Black", "door_color", "black"), new Option("Door Tan", "door_color", "tan"),
                new Option("Wood Dashboard", "dashboard_trim", "wood"), new Option("Carbon Dashboard", "dashboard_trim", "carbon"),
                new Option("Sport Steering", "steering", "sport"), new Option("Premium Steering", "steering", "premium") };
            catalog["AMBIENT, AC & ELECTRONICS"] = new List<Option> {
                new Option("Pink Ambient", "ambient_color", "pink"), new Option("Red Ambient", "ambient_color", "red"),
                new Option("Green Ambient", "ambient_color", "green"), new Option("Blue Ambient", "ambient_color", "blue"),
                new Option("Purple Ambient", "ambient_color", "purple"), new Option("Ambient Off", "ambient_off"),
                new Option("Sunroof", "sunroof"), new Option("Music On/Off", "music"),
                new Option("Premium Audio", "premium_audio"), new Option("Digital Cluster", "digital_cluster"),
                new Option("Wireless Charger", "wireless_charging"), new Option("AC On/Off", "ac"),
                new Option("AC 18C", "ac_temperature", "18"), new Option("AC 22C", "ac_temperature", "22"),
                new Option("AC 26C", "ac_temperature", "26"), new Option("Fan 1", "fan_speed", "1"),
                new Option("Fan 3", "fan_speed", "3"), new Option("Fan 5", "fan_speed", "5"),
                new Option("Screen Home", "infotainment", "home"), new Option("Screen Music", "infotainment", "audio"),
                new Option("Screen Climate", "infotainment", "climate") };
            catalog["VARIANT, PREVIEW & SHARE"] = new List<Option> {
                new Option("Base Model", "variant", "Base"), new Option("Mid Model", "variant", "Mid"),
                new Option("Top Model", "variant", "Top"), new Option("Day / Night", "day_night"),
                new Option("Show Price", "price"), new Option("Save", "save"), new Option("Load", "load"),
                new Option("Reset", "reset"), new Option("Copy Share JSON", "share") };
            if (!ar)
            {
                catalog["360 INSPECTION & SEATS (SHOWROOM ONLY)"] = new List<Option> {
                    new Option("Auto Rotate 360°", "orbit_auto"),
                    new Option("Rotate Left", "orbit_left"), new Option("Rotate Right", "orbit_right"),
                    new Option("Zoom In", "zoom_in"), new Option("Zoom Out", "zoom_out"),
                    new Option("Driver Seat", "seat", "Driver"), new Option("Front Passenger", "seat", "FrontPassenger"),
                    new Option("Back Left Seat", "seat", "RearLeft"), new Option("Back Right Seat", "seat", "RearRight"),
                    new Option("Exit Car", "exit_car") };
            }
            return catalog;
        }

        private static void CreateMaterialPresets()
        {
            if (!AssetDatabase.IsValidFolder(GeneratedPath)) AssetDatabase.CreateFolder("Assets/CarConfigurator", "Generated");
            if (!AssetDatabase.IsValidFolder(MaterialPath)) AssetDatabase.CreateFolder(GeneratedPath, "Materials");
            MaterialPreset("Paint_Glossy_White", Color.white, 0f, .95f);
            MaterialPreset("Paint_Metallic_Silver", new Color(.7f, .74f, .78f), .88f, .8f);
            MaterialPreset("Paint_Matte_Black", new Color(.08f, .08f, .09f), 0f, .08f);
            MaterialPreset("Interior_Fabric", new Color(.24f, .23f, .22f), 0f, .1f);
            MaterialPreset("Interior_Leather", new Color(.38f, .18f, .11f), 0f, .43f);
            MaterialPreset("Interior_PremiumLeather", new Color(.12f, .11f, .11f), 0f, .63f);
            AssetDatabase.SaveAssets();
        }

        private static void MaterialPreset(string fileName, Color color, float metallic, float smoothness)
        {
            string path = MaterialPath + "/" + fileName + ".mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(path)) return;
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (!shader) shader = Shader.Find("Standard");
            if (!shader) { Debug.LogWarning("[Car Configurator] Assign a URP Lit shader before creating material presets."); return; }
            Material material = new Material(shader) { name = fileName };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            else if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            AssetDatabase.CreateAsset(material, path);
        }

        private static Transform GetOrCreate(Transform root, string name)
        {
            Transform found = root.Find(name);
            if (found) return found;
            GameObject go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            Undo.SetTransformParent(go.transform, root, "Parent " + name);
            return go.transform;
        }

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "Create UI " + name);
            Undo.SetTransformParent(go.transform, parent, "Parent UI " + name);
            go.GetComponent<RectTransform>().localScale = Vector3.one;
            return go;
        }

        private static void CreateHeader(Transform parent, string title)
        {
            GameObject go = CreateUIObject("Header_" + title, parent);
            LayoutElement element = Undo.AddComponent<LayoutElement>(go);
            element.preferredHeight = 45;
            Text text = Undo.AddComponent<Text>(go);
            text.text = title;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 17;
            text.fontStyle = FontStyle.Bold;
            text.color = new Color(.47f, .86f, 1f);
            text.alignment = TextAnchor.MiddleLeft;
        }

        private static Button CreateSimpleButton(Transform parent, string name, string label, float width, float height)
        {
            GameObject go = CreateUIObject(name, parent);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width, height);
            LayoutElement element = Undo.AddComponent<LayoutElement>(go);
            element.preferredHeight = height;
            Image image = Undo.AddComponent<Image>(go);
            image.color = new Color(.16f, .19f, .24f, .96f);
            Button button = Undo.AddComponent<Button>(go);
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None; // selected/unselected color is managed by CarFeatureOptionButton
            GameObject labelGo = CreateUIObject("Label", go.transform);
            RectTransform labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(8, 0);
            labelRect.offsetMax = new Vector2(-8, 0);
            Text text = Undo.AddComponent<Text>(labelGo);
            text.text = label;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 18;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            return button;
        }

        private static InputField CreateTextInput(Transform parent)
        {
            GameObject fieldGo = CreateUIObject("AICommandInput", parent);
            LayoutElement layout = Undo.AddComponent<LayoutElement>(fieldGo);
            layout.preferredHeight = 52;
            Image bg = Undo.AddComponent<Image>(fieldGo);
            bg.color = new Color(.1f, .12f, .16f, 1f);
            InputField input = Undo.AddComponent<InputField>(fieldGo);
            input.targetGraphic = bg;
            GameObject textGo = CreateUIObject("CommandText", fieldGo.transform);
            RectTransform t = textGo.GetComponent<RectTransform>();
            t.anchorMin = Vector2.zero;
            t.anchorMax = Vector2.one;
            t.offsetMin = new Vector2(12, 0);
            t.offsetMax = new Vector2(-12, 0);
            Text text = Undo.AddComponent<Text>(textGo);
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 18;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleLeft;
            text.supportRichText = false;
            text.raycastTarget = false;
            GameObject placeholderGo = CreateUIObject("Placeholder", fieldGo.transform);
            RectTransform p = placeholderGo.GetComponent<RectTransform>();
            p.anchorMin = Vector2.zero;
            p.anchorMax = Vector2.one;
            p.offsetMin = new Vector2(12, 0);
            p.offsetMax = new Vector2(-12, 0);
            Text hint = Undo.AddComponent<Text>(placeholderGo);
            hint.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            hint.fontSize = 16;
            hint.fontStyle = FontStyle.Italic;
            hint.color = new Color(.56f, .6f, .67f);
            hint.text = "change car color blue";
            hint.alignment = TextAnchor.MiddleLeft;
            hint.raycastTarget = false;
            input.textComponent = text;
            input.placeholder = hint;
            input.lineType = InputField.LineType.SingleLine;
            return input;
        }

        private static void AssignReferenceIfEmpty(Component component, string name, UnityEngine.Object reference)
        {
            if (!component || !reference) return;
            SerializedObject obj = new SerializedObject(component);
            SerializedProperty field = obj.FindProperty(name);
            if (field == null || field.objectReferenceValue) return;
            field.objectReferenceValue = reference;
            obj.ApplyModifiedProperties();
        }
    }
}
#endif
