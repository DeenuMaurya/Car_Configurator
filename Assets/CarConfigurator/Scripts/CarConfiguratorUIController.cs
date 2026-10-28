using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CarShowroom
{
    /// <summary>
    /// Binds the existing Assets/Canvas.prefab hierarchy without rebuilding or renaming it.
    /// Also disables its obsolete persistent OnClick calls (many point at missing scripts).
    /// The controller is installed on the EMPTY CarConfiguratorSystem parent, not the model.
    /// </summary>
    [DefaultExecutionOrder(1000)]
    [RequireComponent(typeof(CarInteriorAppearanceController))]
    public sealed class CarConfiguratorUIController : MonoBehaviour
    {
        [Serializable]
        private class MenuSpritePair
        {
            public string path;
            public Sprite offSprite;
            public Sprite onSprite;
        }

        [SerializeField] private CarConfiguratorManager config;
        [SerializeField] private VehicleMotionController motions;
        [SerializeField] private CarCabinControls cabin;
        [SerializeField] private CarInteriorAppearanceController appearance;
        [SerializeField] private AudioSource uiClickSource;
        [SerializeField] private AudioClip uiClick;
        [SerializeField] private List<MenuSpritePair> menuSpritePairs = new List<MenuSpritePair>();

        private Transform menu;
        private Transform canvasRoot;
        private bool isInterior;
        private bool musicEnabled;
        private bool bound;
        private SeatUiContext seatContext;
        private readonly List<Button> boundButtons = new List<Button>();
        private readonly Dictionary<string, string> selectedSwatches = new Dictionary<string, string>();
        private string selectedInteriorCategory = "Interiormenu/DashBoard";
        private string selectedExteriorCategory = "ExteriorMenu/Colour";
        private enum SeatUiContext { Front, Rear, Cover }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallForExistingScene()
        {
            if (SceneManager.GetActiveScene().name != "3D Car Configurator") return;
            GameObject system = GameObject.Find("CarConfiguratorSystem");
            if (!system) return;
            CarConfiguratorUIController ui = system.GetComponent<CarConfiguratorUIController>();
            if (!ui) ui = system.AddComponent<CarConfiguratorUIController>();
            ui.Bind();
        }

        private void Awake()
        {
            if (!config) config = GetComponent<CarConfiguratorManager>();
            if (!motions) motions = GetComponent<VehicleMotionController>();
            if (!cabin) cabin = GetComponent<CarCabinControls>();
            if (!appearance) appearance = GetComponent<CarInteriorAppearanceController>();
        }

        public void Bind()
        {
            if (bound) return;
            GameObject canvas = GameObject.Find("Canvas");
            if (!canvas || !config) { Debug.LogWarning("[Car UI] Canvas or ConfiguratorSystem missing; UI binding skipped."); return; }
            canvasRoot = canvas.transform;
            menu = canvas.transform.Find("MenuBand");
            if (!menu) { Debug.LogWarning("[Car UI] Canvas/MenuBand not found."); return; }
            bound = true;

            // The design uses ONE visible switch icon at a time. Tap it to switch modes.
            BindButton("Interior", () => SetSection(false));
            BindButton("Exterior", () => SetSection(true));
            BindButton("ExteriorMenu/Colour", () => SelectCategory("ExteriorMenu/Colour", "ColourSubMenu"));
            BindButton("ExteriorMenu/Alloy", () => SelectCategory("ExteriorMenu/Alloy", "Alloy"));
            BindButton("ExteriorMenu/Chrome", () => { SelectCategory("ExteriorMenu/Chrome"); TogglePart("chrome"); });
            BindButton("ExteriorMenu/Cladding", () => { SelectCategory("ExteriorMenu/Cladding"); TogglePart("cladding"); });
            BindButton("ExteriorMenu/Door-Visor", () => { SelectCategory("ExteriorMenu/Door-Visor"); TogglePart("door_visor"); });
            BindButton("ExteriorMenu/Roof-Rail", () => { SelectCategory("ExteriorMenu/Roof-Rail"); TogglePart("roof_rail"); });
            BindButton("ExteriorMenu/OpenDoor", CloseAllDoors);
            BindButton("ExteriorMenu/TestDrive", () => Debug.Log("[Car UI] Test Drive awaits approved driving mode."));

            BindButton("ColourSubMenu/Orange", () => SetBodyColor("Orange", new Color(1f, .33f, .06f)));
            BindButton("ColourSubMenu/Black", () => SetBodyColor("Black", new Color(.16f, .17f, .18f)));
            BindButton("ColourSubMenu/yellow", () => SetBodyColor("yellow", new Color(.79f, .68f, .46f)));
            BindButton("ColourSubMenu/Blue", () => SetBodyColor("Blue", new Color(.20f, .68f, .72f)));
            BindButton("ColourSubMenu/White", () => SetBodyColor("White", Color.white));
            BindButton("Alloy/Alloy1", () => ChooseWheel("alloy1", "Alloy1"));
            BindButton("Alloy/Alloy2", () => ChooseWheel("alloy2", "Alloy2"));

            BindButton("Interiormenu/DashBoard", () => SelectCategory("Interiormenu/DashBoard", "DashBoard"));
            BindButton("Interiormenu/FrontSeat", () => ShowSeatChoices(SeatUiContext.Front));
            BindButton("Interiormenu/BackSeat", () => ShowSeatChoices(SeatUiContext.Rear));
            BindButton("Interiormenu/SeatCover", () => ShowSeatChoices(SeatUiContext.Cover));
            BindButton("Interiormenu/Door-Colour", () => SelectCategory("Interiormenu/Door-Colour", "Door"));
            BindButton("Interiormenu/CabinHeight", ToggleCabinLight);
            BindButton("Interiormenu/AmbientLight", () => SelectCategory("Interiormenu/AmbientLight", "AmbienLight"));
            BindButton("Interiormenu/Music", ToggleMusic);

            BindButton("DashBoard/Dashboard-white", () => ApplyDashboard("Dashboard-white", new Color(.91f, .88f, .81f)));
            BindButton("DashBoard/Dashboard-leather", () => ApplyDashboard("Dashboard-leather", new Color(.35f, .18f, .12f)));
            BindButton("DashBoard/Dashboard-premium", () => ApplyDashboard("Dashboard-premium", new Color(.08f, .09f, .10f)));
            BindButton("Seat/Seat-white", () => ApplySeatOption(0));
            BindButton("Seat/Seat-leather", () => ApplySeatOption(1));
            BindButton("Seat/Seat-premium", () => ApplySeatOption(2));
            BindButton("Door/DoorWhite", () => ApplyDoorTrim("DoorWhite", new Color(.86f, .84f, .79f)));
            BindButton("Door/DoorLeather", () => ApplyDoorTrim("DoorLeather", new Color(.32f, .17f, .11f)));

            BindButton("AmbienLight/Pink", () => SetAmbient("Pink", new Color(1f, .03f, .64f)));
            BindButton("AmbienLight/Red", () => SetAmbient("Red", Color.red));
            BindButton("AmbienLight/Green", () => SetAmbient("Green", Color.green));
            BindButton("AmbienLight/Purple", () => SetAmbient("Purple", new Color(.52f, 0f, .94f)));
            BindButton("AmbienLight/Blue", () => SetAmbient("Blue", Color.blue));
            BindButton("AmbienLight/NoColor", DisableAmbient);

            BindCanvasButton(canvas.transform, "SideMenu/Reset", () =>
            {
                config.ResetToDefaults();
                CloseAllDoors();
                musicEnabled = false;
                if (cabin) cabin.SetMusic(false);
                appearance?.RefreshFromConfiguration();
                SetSection(false);
            });
            BindCanvasButton(canvas.transform, "SideMenu/MusicOnOff", ToggleMusic);
            BindCanvasButton(canvas.transform, "SideMenu/Share (1)", ShareConfiguration);
            BindButton("360On", () =>
            {
                CarConfiguratorFeatureActions actions = GetComponent<CarConfiguratorFeatureActions>();
                if (!actions || !actions.ExecuteFeature("orbit_auto", ""))
                    Debug.LogWarning("[Car UI] Assign MobileShowroomOrbit + CarConfiguratorFeatureActions to use 360 inspection.");
            });

            isInterior = menu.Find("Interior") && menu.Find("Interior").gameObject.activeSelf;
            SetSection(isInterior);
            config.ConfigurationChanged += SyncSelectionsFromConfiguration;
            SyncSelectionsFromConfiguration();
            if (appearance) appearance.RefreshFromConfiguration();
            Debug.Log("[Car UI] Bound " + boundButtons.Count + " existing Unity buttons.");
        }

        private void OnDestroy()
        {
            if (config) config.ConfigurationChanged -= SyncSelectionsFromConfiguration;
        }

        private void BindButton(string path, UnityAction action) => BindCanvasButton(menu, path, action);

        private void BindCanvasButton(Transform root, string path, UnityAction action)
        {
            Transform transform = root ? root.Find(path) : null;
            Button button = transform ? transform.GetComponent<Button>() : null;
            if (!button) { Debug.LogWarning("[Car UI] Missing button: " + path); return; }

            // Remove effects of older persistent callbacks referencing unavailable scripts.
            for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
                button.onClick.SetPersistentListenerState(i, UnityEventCallState.Off);
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => { if (uiClickSource && uiClick) uiClickSource.PlayOneShot(uiClick); action(); });
            boundButtons.Add(button);
        }

        private void SetSection(bool interior)
        {
            isInterior = interior;
            SetActive("Interior", interior);
            SetActive("Interiormenu", interior);
            SetActive("Exterior", !interior);
            SetActive("ExteriorMenu", !interior);
            HideSubmenus();
            UpdateMenuSprites();
        }

        private void SelectCategory(string path, string submenu = null)
        {
            if (path.StartsWith("Interiormenu/", StringComparison.Ordinal)) selectedInteriorCategory = path;
            else if (path.StartsWith("ExteriorMenu/", StringComparison.Ordinal)) selectedExteriorCategory = path;
            if (submenu == null) HideSubmenus(); else ShowSubmenu(submenu);
            UpdateMenuSprites();
        }

        private void UpdateMenuSprites()
        {
            if (config && config.Current != null) musicEnabled = config.Current.musicEnabled;
            foreach (MenuSpritePair pair in menuSpritePairs)
            {
                if (pair == null || string.IsNullOrEmpty(pair.path)) continue;
                Transform t = menu ? menu.Find(pair.path) : null;
                if (!t && canvasRoot) t = canvasRoot.Find(pair.path);
                if (!t) continue;
                Button button = t.GetComponent<Button>();
                Image image = button ? button.targetGraphic as Image : t.GetComponent<Image>();
                if (!image) continue;
                bool selected = pair.path == (isInterior ? selectedInteriorCategory : selectedExteriorCategory);
                if (pair.path == "Interiormenu/Music" || pair.path == "SideMenu/MusicOnOff") selected = musicEnabled;
                if (pair.path == "Interiormenu/CabinHeight") selected = config && config.Current != null && config.Current.ambientEnabled;
                string partId = OptionalPartForMenu(pair.path);
                if (partId != null) selected = config && config.Current != null &&
                    config.Current.enabledPartIds != null && config.Current.enabledPartIds.Contains(partId);
                Sprite sprite = selected ? pair.onSprite : pair.offSprite;
                if (sprite) image.sprite = sprite;
            }
        }

        private static string OptionalPartForMenu(string path)
        {
            switch (path)
            {
                case "ExteriorMenu/Chrome": return "chrome";
                case "ExteriorMenu/Cladding": return "cladding";
                case "ExteriorMenu/Door-Visor": return "door_visor";
                case "ExteriorMenu/Roof-Rail": return "roof_rail";
                default: return null;
            }
        }

        private void SelectSwatch(string group, string selectedPath)
        {
            selectedSwatches[group] = selectedPath;
            if (group.StartsWith("Seat:", StringComparison.Ordinal) && group != "Seat:" + seatContext) return;
            string panelName = group.StartsWith("Seat:", StringComparison.Ordinal) ? "Seat" : group;
            Transform panel = menu ? menu.Find(panelName) : null;
            if (!panel) return;
            foreach (Button button in panel.GetComponentsInChildren<Button>(true))
            {
                bool selected = panelName + "/" + button.name == selectedPath;
                Outline outline = button.GetComponent<Outline>();
                if (!outline && selected) outline = button.gameObject.AddComponent<Outline>();
                if (!outline) continue;
                outline.effectColor = Color.white;
                outline.effectDistance = new Vector2(3f, -3f);
                outline.useGraphicAlpha = false;
                outline.enabled = selected;
            }
        }

        // Keeps UI selections accurate after saved configurations load or Reset is pressed.
        private void SyncSelectionsFromConfiguration()
        {
            if (!bound || !config || config.Current == null) return;
            CarConfiguration c = config.Current;

            SelectSwatch("ColourSubMenu", MatchColor(c.bodyColor,
                new[] { "Orange", "Black", "yellow", "Blue", "White" },
                new[] { new Color(1f, .33f, .06f), new Color(.16f, .17f, .18f), new Color(.79f, .68f, .46f), new Color(.20f, .68f, .72f), Color.white }, "ColourSubMenu"));

            SelectSwatch("Alloy", c.wheelStyle == "alloy1" ? "Alloy/Alloy1" : c.wheelStyle == "alloy2" ? "Alloy/Alloy2" : null);
            Color[] seatColors = { new Color(.91f, .88f, .81f), new Color(.37f, .18f, .12f), new Color(.08f, .09f, .10f) };
            string[] seatNames = { "Seat-white", "Seat-leather", "Seat-premium" };
            SelectSwatch("Seat:Front", MatchColor(c.seatColor, seatNames, seatColors, "Seat"));
            SelectSwatch("Seat:Rear", MatchColor(c.rearSeatColor, seatNames, seatColors, "Seat"));
            SelectSwatch("Seat:Cover", string.Equals(c.upholstery, "Fabric", StringComparison.OrdinalIgnoreCase) ? "Seat/Seat-white" :
                string.Equals(c.upholstery, "Leather", StringComparison.OrdinalIgnoreCase) ? "Seat/Seat-leather" :
                string.Equals(c.upholstery, "PremiumLeather", StringComparison.OrdinalIgnoreCase) ? "Seat/Seat-premium" : null);

            SelectSwatch("DashBoard", MatchColor(c.dashboardColor,
                new[] { "Dashboard-white", "Dashboard-leather", "Dashboard-premium" },
                new[] { new Color(.91f, .88f, .81f), new Color(.35f, .18f, .12f), new Color(.08f, .09f, .10f) }, "DashBoard"));
            SelectSwatch("Door", MatchColor(c.doorTrimColor,
                new[] { "DoorWhite", "DoorLeather" },
                new[] { new Color(.86f, .84f, .79f), new Color(.32f, .17f, .11f) }, "Door"));
            SelectSwatch("AmbienLight", !c.ambientEnabled ? "AmbienLight/NoColor" : MatchColor(c.ambientColor,
                new[] { "Pink", "Red", "Green", "Purple", "Blue" },
                new[] { new Color(1f, .03f, .64f), Color.red, Color.green, new Color(.52f, 0f, .94f), Color.blue }, "AmbienLight"));
            UpdateMenuSprites();
        }

        private static string MatchColor(Color current, string[] names, Color[] colors, string panel)
        {
            for (int i = 0; i < names.Length; i++)
                if (Mathf.Abs(current.r - colors[i].r) < .025f &&
                    Mathf.Abs(current.g - colors[i].g) < .025f &&
                    Mathf.Abs(current.b - colors[i].b) < .025f)
                    return panel + "/" + names[i];
            return null;
        }

        private void HideSubmenus()
        {
            foreach (string panel in new[] { "ColourSubMenu", "Alloy", "DashBoard", "Seat", "Door", "AmbienLight" })
                SetActive(panel, false);
        }

        private void ShowSubmenu(string name)
        {
            HideSubmenus();
            SetActive(name, true);
        }

        private void SetActive(string path, bool value)
        {
            Transform obj = menu ? menu.Find(path) : null;
            if (obj) obj.gameObject.SetActive(value);
        }

        private void SetBodyColor(string key, Color color)
        {
            config.SetBodyColor(color);
            config.Save();
            SelectSwatch("ColourSubMenu", "ColourSubMenu/" + key);
        }

        private void ChooseWheel(string id, string key)
        {
            if (config.SetWheelStyle(id)) SelectSwatch("Alloy", "Alloy/" + key);
            else Debug.LogWarning("[Car UI] Configure distinct alloy1/alloy2 wheel objects in CarConfiguratorManager.wheels.");
        }

        private void TogglePart(string id)
        {
            bool enable = !config.Current.enabledPartIds.Contains(id);
            if (config.SetPart(id, enable)) config.Save();
            else Debug.LogWarning("[Car UI] Missing optional part: " + id + ". Add its actual mesh and PartEntry first.");
            UpdateMenuSprites();
        }

        private void CloseAllDoors()
        {
            if (!motions) return;
            motions.SetAllDoors(false);
        }

        private void ShowSeatChoices(SeatUiContext context)
        {
            seatContext = context;
            string category = context == SeatUiContext.Front ? "FrontSeat" : context == SeatUiContext.Rear ? "BackSeat" : "SeatCover";
            SelectCategory("Interiormenu/" + category, "Seat");
            string key = "Seat:" + context;
            string selection;
            selectedSwatches.TryGetValue(key, out selection);
            SelectSwatch(key, selection);
        }

        private void ApplySeatOption(int index)
        {
            Color[] colors = { new Color(.91f, .88f, .81f), new Color(.37f, .18f, .12f), new Color(.08f, .09f, .10f) };
            Color choice = colors[Mathf.Clamp(index, 0, 2)];
            if (seatContext == SeatUiContext.Cover)
                config.SetUpholstery(index == 0 ? "Fabric" : index == 1 ? "Leather" : "PremiumLeather");
            else if (seatContext == SeatUiContext.Rear)
                config.Current.rearSeatColor = choice;
            else config.SetSeatColor(choice);
            config.Apply();
            config.Save();
            if (appearance) appearance.RefreshFromConfiguration();
            SelectSwatch("Seat:" + seatContext, "Seat/" + (index == 0 ? "Seat-white" : index == 1 ? "Seat-leather" : "Seat-premium"));
        }

        private void ApplyDashboard(string key, Color color)
        {
            config.Current.dashboardColor = color;
            config.Apply(); config.Save();
            if (appearance) appearance.RefreshFromConfiguration();
            SelectSwatch("DashBoard", "DashBoard/" + key);
        }

        private void ApplyDoorTrim(string key, Color color)
        {
            config.Current.doorTrimColor = color;
            config.Apply(); config.Save();
            if (appearance) appearance.RefreshFromConfiguration();
            SelectSwatch("Door", "Door/" + key);
        }

        private void SetAmbient(string key, Color color)
        {
            config.Current.ambientColor = color;
            config.Current.ambientEnabled = true;
            config.Apply(); config.Save();
            if (appearance) appearance.RefreshFromConfiguration();
            SelectSwatch("AmbienLight", "AmbienLight/" + key);
        }

        private void DisableAmbient()
        {
            config.Current.ambientEnabled = false;
            config.Apply(); config.Save();
            if (appearance) appearance.RefreshFromConfiguration();
            SelectSwatch("AmbienLight", "AmbienLight/NoColor");
        }

        private void ToggleCabinLight()
        {
            config.Current.ambientEnabled = !config.Current.ambientEnabled;
            if (cabin) cabin.SetAmbientLights(config.Current.ambientEnabled);
            config.Apply();
            config.Save();
            UpdateMenuSprites();
        }

        private void ToggleMusic()
        {
            musicEnabled = config && config.Current != null ? !config.Current.musicEnabled : !musicEnabled;
            if (config && config.Current != null)
            {
                config.Current.musicEnabled = musicEnabled;
                config.Apply();
                config.Save();
            }
            if (cabin) cabin.SetMusic(musicEnabled);
            UpdateMenuSprites();
        }

        private void ShareConfiguration() => GUIUtility.systemCopyBuffer = JsonUtility.ToJson(config.Current, true);
    }
}
