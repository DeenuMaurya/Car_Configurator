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
        [SerializeField] private CarConfiguratorManager config;
        [SerializeField] private VehicleMotionController motions;
        [SerializeField] private CarCabinControls cabin;
        [SerializeField] private CarInteriorAppearanceController appearance;
        [SerializeField] private AudioSource uiClickSource;
        [SerializeField] private AudioClip uiClick;

        private Transform menu;
        private bool isInterior;
        private bool musicEnabled;
        private bool bound;
        private SeatUiContext seatContext;
        private readonly List<Button> boundButtons = new List<Button>();
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
            menu = canvas.transform.Find("MenuBand");
            if (!menu) { Debug.LogWarning("[Car UI] Canvas/MenuBand not found."); return; }
            bound = true;

            // The design uses ONE visible switch icon at a time. Tap it to switch modes.
            BindButton("Interior", () => SetSection(false));
            BindButton("Exterior", () => SetSection(true));
            BindButton("ExteriorMenu/Colour", () => ShowSubmenu("ColourSubMenu"));
            BindButton("ExteriorMenu/Alloy", () => ShowSubmenu("Alloy"));
            BindButton("ExteriorMenu/Chrome", () => TogglePart("chrome"));
            BindButton("ExteriorMenu/Cladding", () => TogglePart("cladding"));
            BindButton("ExteriorMenu/Door-Visor", () => TogglePart("door_visor"));
            BindButton("ExteriorMenu/Roof-Rail", () => TogglePart("roof_rail"));
            BindButton("ExteriorMenu/OpenDoor", CloseAllDoors);
            BindButton("ExteriorMenu/TestDrive", () => Debug.Log("[Car UI] Test Drive awaits approved driving mode."));

            BindButton("ColourSubMenu/Orange", () => SetBodyColor(new Color(1f, .33f, .06f)));
            BindButton("ColourSubMenu/Black", () => SetBodyColor(new Color(.16f, .17f, .18f)));
            BindButton("ColourSubMenu/yellow", () => SetBodyColor(new Color(.79f, .68f, .46f)));
            BindButton("ColourSubMenu/Blue", () => SetBodyColor(new Color(.20f, .68f, .72f)));
            BindButton("ColourSubMenu/White", () => SetBodyColor(Color.white));
            BindButton("Alloy/Alloy1", () => ChooseWheel("alloy1"));
            BindButton("Alloy/Alloy2", () => ChooseWheel("alloy2"));

            BindButton("Interiormenu/DashBoard", () => ShowSubmenu("DashBoard"));
            BindButton("Interiormenu/FrontSeat", () => ShowSeatChoices(SeatUiContext.Front));
            BindButton("Interiormenu/BackSeat", () => ShowSeatChoices(SeatUiContext.Rear));
            BindButton("Interiormenu/SeatCover", () => ShowSeatChoices(SeatUiContext.Cover));
            BindButton("Interiormenu/Door-Colour", () => ShowSubmenu("Door"));
            BindButton("Interiormenu/CabinHeight", ToggleCabinLight);
            BindButton("Interiormenu/AmbientLight", () => ShowSubmenu("AmbienLight"));
            BindButton("Interiormenu/Music", ToggleMusic);

            BindButton("DashBoard/Dashboard-white", () => ApplyDashboard(new Color(.91f, .88f, .81f)));
            BindButton("DashBoard/Dashboard-leather", () => ApplyDashboard(new Color(.35f, .18f, .12f)));
            BindButton("DashBoard/Dashboard-premium", () => ApplyDashboard(new Color(.08f, .09f, .10f)));
            BindButton("Seat/Seat-white", () => ApplySeatOption(0));
            BindButton("Seat/Seat-leather", () => ApplySeatOption(1));
            BindButton("Seat/Seat-premium", () => ApplySeatOption(2));
            BindButton("Door/DoorWhite", () => ApplyDoorTrim(new Color(.86f, .84f, .79f)));
            BindButton("Door/DoorLeather", () => ApplyDoorTrim(new Color(.32f, .17f, .11f)));

            BindButton("AmbienLight/Pink", () => SetAmbient(new Color(1f, .03f, .64f)));
            BindButton("AmbienLight/Red", () => SetAmbient(Color.red));
            BindButton("AmbienLight/Green", () => SetAmbient(Color.green));
            BindButton("AmbienLight/Purple", () => SetAmbient(new Color(.52f, 0f, .94f)));
            BindButton("AmbienLight/Blue", () => SetAmbient(Color.blue));
            BindButton("AmbienLight/NoColor", DisableAmbient);

            BindCanvasButton(canvas.transform, "SideMenu/Reset", () => { config.ResetToDefaults(); CloseAllDoors(); appearance?.RefreshFromConfiguration(); SetSection(false); });
            BindCanvasButton(canvas.transform, "SideMenu/MusicOnOff", ToggleMusic);
            BindCanvasButton(canvas.transform, "SideMenu/Share (1)", ShareConfiguration);
            BindButton("360On", () => Debug.Log("[Car UI] 360 camera mode requires a configured camera orbit."));

            isInterior = menu.Find("Interior") && menu.Find("Interior").gameObject.activeSelf;
            SetSection(isInterior);
            if (appearance) appearance.RefreshFromConfiguration();
            Debug.Log("[Car UI] Bound " + boundButtons.Count + " existing Unity buttons.");
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

        private void SetBodyColor(Color color) { config.SetBodyColor(color); config.Save(); }

        private void ChooseWheel(string id)
        {
            if (!config.SetWheelStyle(id)) Debug.LogWarning("[Car UI] Configure distinct alloy1/alloy2 wheel objects in CarConfiguratorManager.wheels.");
        }

        private void TogglePart(string id)
        {
            bool enable = !config.Current.enabledPartIds.Contains(id);
            if (config.SetPart(id, enable)) config.Save();
            else Debug.LogWarning("[Car UI] Missing optional part: " + id + ". Add its actual mesh and PartEntry first.");
        }

        private void CloseAllDoors()
        {
            if (!motions) return;
            motions.SetAllDoors(false);
        }

        private void ShowSeatChoices(SeatUiContext context) { seatContext = context; ShowSubmenu("Seat"); }

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
        }

        private void ApplyDashboard(Color color)
        {
            config.Current.dashboardColor = color;
            config.Apply(); config.Save();
            if (appearance) appearance.RefreshFromConfiguration();
        }

        private void ApplyDoorTrim(Color color)
        {
            config.Current.doorTrimColor = color;
            config.Apply(); config.Save();
            if (appearance) appearance.RefreshFromConfiguration();
        }

        private void SetAmbient(Color color)
        {
            config.Current.ambientColor = color;
            config.Current.ambientEnabled = true;
            config.Apply(); config.Save();
            if (appearance) appearance.RefreshFromConfiguration();
        }

        private void DisableAmbient()
        {
            config.Current.ambientEnabled = false;
            config.Apply(); config.Save();
            if (appearance) appearance.RefreshFromConfiguration();
        }

        private void ToggleCabinLight()
        {
            config.Current.ambientEnabled = !config.Current.ambientEnabled;
            if (cabin) cabin.SetAmbientLights(config.Current.ambientEnabled);
            config.Apply();
            config.Save();
        }

        private void ToggleMusic()
        {
            musicEnabled = !musicEnabled;
            if (cabin) cabin.SetMusic(musicEnabled);
        }

        private void ShareConfiguration() => GUIUtility.systemCopyBuffer = JsonUtility.ToJson(config.Current, true);
    }
}
