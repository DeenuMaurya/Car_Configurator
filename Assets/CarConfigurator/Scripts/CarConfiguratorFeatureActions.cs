using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CarShowroom
{
    /// <summary>
    /// Inspector-friendly command entry point for showroom and mobile AR.
    /// Add this to an EMPTY manager object. Assign actual meshes/materials to the
    /// specialized controllers; this script never substitutes fake car geometry.
    /// </summary>
    public sealed class CarConfiguratorFeatureActions : MonoBehaviour
    {
        [Header("Assign references from your scene or spawned AR car")]
        [SerializeField] private CarConfiguratorManager configurator;
        [SerializeField] private VehicleMotionController motions;
        [SerializeField] private VehicleLightsAndSounds lights;
        [SerializeField] private CarCabinControls cabin;
        [SerializeField] private ShowroomFeatureController features;
        [SerializeField] private CarInteriorAppearanceController appearance;
        [SerializeField] private CarShowcaseServices showcase;
        [SerializeField] private CarSeatNavigator seats;
        [SerializeField] private ExperienceModeManager mode;
        [SerializeField] private MobileShowroomOrbit orbit;
        [SerializeField] private Text generatedPriceBreakdown;

        public event Action StateChanged;
        public CarConfiguratorManager Configurator => configurator;

        private void Awake()
        {
            if (!configurator) configurator = GetComponent<CarConfiguratorManager>();
            if (configurator) BindCar(configurator);
        }

        private void OnDestroy()
        {
            if (configurator) configurator.ConfigurationChanged -= OnConfigurationChanged;
        }

        public void BindCar(CarConfiguratorManager car)
        {
            if (configurator) configurator.ConfigurationChanged -= OnConfigurationChanged;
            configurator = car;
            if (!car) return;
            motions = car.GetComponentInParent<VehicleMotionController>();
            lights = car.GetComponentInParent<VehicleLightsAndSounds>();
            cabin = car.GetComponentInParent<CarCabinControls>();
            features = car.GetComponentInParent<ShowroomFeatureController>();
            appearance = car.GetComponentInParent<CarInteriorAppearanceController>();
            if (!mode) mode = FindFirstObjectByType<ExperienceModeManager>();
            if (!showcase) showcase = FindFirstObjectByType<CarShowcaseServices>();
            if (!seats && (mode == null || mode.SeatNavigationAllowed)) seats = FindFirstObjectByType<CarSeatNavigator>();
            if (!orbit) orbit = FindFirstObjectByType<MobileShowroomOrbit>();
            if (motions) motions.BindMode(mode);
            if (features) { features.BindMode(mode); features.BindCabin(cabin); }
            configurator.ConfigurationChanged += OnConfigurationChanged;
            RestoreCabinState();
            NotifyStateChanged();
        }

        private void OnConfigurationChanged()
        {
            RestoreCabinState();
            RefreshPriceText();
            NotifyStateChanged();
        }

        private void NotifyStateChanged() => StateChanged?.Invoke();

        public void SetGeneratedPriceText(Text label)
        {
            generatedPriceBreakdown = label;
            RefreshPriceText();
        }

        private void RefreshPriceText()
        {
            if (generatedPriceBreakdown && configurator && configurator.Current != null)
                generatedPriceBreakdown.text = configurator.GetPriceBreakdown();
        }

        private void RestoreCabinState()
        {
            if (!configurator || configurator.Current == null) return;
            CarConfiguration c = configurator.Current;
            if (cabin)
            {
                cabin.SetAC(c.acEnabled);
                cabin.SetMusic(c.musicEnabled);
                cabin.SetSunroof(c.sunroofOpen);
                cabin.SetAmbientLights(c.ambientEnabled);
            }
            if (features)
            {
                features.SetAC(c.acEnabled);
                features.SetACTemperature(c.acTemperature <= 0 ? 22f : c.acTemperature);
                features.SetFanSpeed(c.acFanSpeed);
                features.SelectPremiumAudio(c.premiumAudio);
                features.SetDigitalCluster(c.digitalInstrumentCluster);
                features.SetWirelessCharger(c.wirelessCharging);
                features.SetScreen(string.IsNullOrEmpty(c.infotainmentPage) ? "home" : c.infotainmentPage);
            }
            if (showcase) showcase.Refresh();
            RefreshPriceText();
        }

        // Can also be invoked manually through a Unity UI Button via CarFeatureOptionButton.
        public bool ExecuteFeature(string action, string value)
        {
            if (string.IsNullOrWhiteSpace(action)) return false;
            string command = action.Trim().ToLowerInvariant();
            string option = (value ?? "").Trim();
            CarConfiguration state = configurator ? configurator.Current : null;
            bool handled = true;
            bool changedConfiguration = false;

            switch (command)
            {
                case "body_color":
                case "seat_color":
                case "rear_seat_color":
                case "dashboard_color":
                case "door_color":
                case "ambient_color":
                    if (state == null || !TryColor(option, out Color color)) return false;
                    if (command == "body_color") state.bodyColor = color;
                    if (command == "seat_color") state.seatColor = color;
                    if (command == "rear_seat_color") state.rearSeatColor = color;
                    if (command == "dashboard_color") state.dashboardColor = color;
                    if (command == "door_color") state.doorTrimColor = color;
                    if (command == "ambient_color") { state.ambientColor = color; state.ambientEnabled = true; }
                    changedConfiguration = true;
                    break;
                case "ambient_off":
                    if (state == null) return false;
                    state.ambientEnabled = false;
                    changedConfiguration = true;
                    break;
                case "ambient_on":
                    if (state == null) return false;
                    state.ambientEnabled = true;
                    changedConfiguration = true;
                    break;
                case "paint_finish":
                    if (state == null || !Enum.TryParse(option, true, out PaintFinish paint) ||
                        !Enum.IsDefined(typeof(PaintFinish), paint)) return false;
                    state.paintFinish = paint;
                    changedConfiguration = true;
                    break;
                case "variant":
                    if (state == null || !Enum.TryParse(option, true, out CarVariant variant) ||
                        !Enum.IsDefined(typeof(CarVariant), variant)) return false;
                    state.variant = variant;
                    changedConfiguration = true;
                    break;
                case "upholstery":
                    if (state == null || (option != "Fabric" && option != "Leather" && option != "PremiumLeather")) return false;
                    state.upholstery = option;
                    changedConfiguration = true;
                    break;
                case "wheel": return Finish(configurator && configurator.SetWheelStyle(option));
                case "grille": return Finish(configurator && configurator.SetGrilleStyle(option));
                case "dashboard_trim": return Finish(configurator && configurator.SetDashboardTrim(option));
                case "steering": return Finish(configurator && configurator.SetSteeringStyle(option));
                case "part_toggle":
                    if (state == null || state.enabledPartIds == null) return false;
                    if (!configurator.SetPart(option, !state.enabledPartIds.Contains(option))) return false;
                    changedConfiguration = true;
                    break;
                case "door_toggle":
                case "window_toggle": return Finish(motions && motions.Toggle(option));
                case "door_open":
                case "window_open": return Finish(motions && motions.SetOpen(option, true));
                case "door_close":
                case "window_close": return Finish(motions && motions.SetOpen(option, false));
                case "all_doors_open":
                    if (!motions) return false;
                    motions.SetAllDoors(true);
                    break;
                case "all_doors_close":
                    if (!motions) return false;
                    motions.SetAllDoors(false);
                    break;
                case "headlights":
                    if (!lights) return false;
                    lights.SetLowBeams(!lights.AreLowBeamsOn);
                    break;
                case "set_headlights":
                    if (!lights || !TryOnOff(option, out bool beamState)) return false;
                    lights.SetLowBeams(beamState);
                    break;
                case "high_beams":
                    if (!lights) return false;
                    lights.SetHighBeams(!lights.AreHighBeamsOn);
                    break;
                case "fog_lights":
                    if (!lights) return false;
                    lights.SetFogLights(!lights.AreFogLightsOn);
                    break;
                case "brake_lights":
                    if (!lights) return false;
                    lights.SetBrakeLights(!lights.AreBrakeLightsOn);
                    break;
                case "hazard":
                    if (!lights) return false;
                    lights.SetHazard(!lights.IsHazardOn);
                    break;
                case "left_indicator":
                    if (!lights) return false;
                    lights.SetLeftIndicator(!lights.IsLeftIndicatorOn);
                    break;
                case "right_indicator":
                    if (!lights) return false;
                    lights.SetRightIndicator(!lights.IsRightIndicatorOn);
                    break;
                case "honk": if (!lights) return false; lights.Honk(); break;
                case "lock": if (!lights) return false; lights.SetLocked(true); break;
                case "unlock": if (!lights) return false; lights.SetLocked(false); break;
                case "ac":
                    if (state == null) return false;
                    state.acEnabled = !state.acEnabled;
                    changedConfiguration = true;
                    break;
                case "set_ac":
                    if (state == null || !TryOnOff(option, out bool acState)) return false;
                    state.acEnabled = acState;
                    changedConfiguration = true;
                    break;
                case "ac_temperature":
                    if (state == null || !float.TryParse(option, System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out float temperature)) return false;
                    state.acTemperature = Mathf.Clamp(temperature, 16f, 30f);
                    changedConfiguration = true;
                    break;
                case "fan_speed":
                    if (state == null || !int.TryParse(option, out int speed)) return false;
                    state.acFanSpeed = Mathf.Clamp(speed, 0, 5);
                    changedConfiguration = true;
                    break;
                case "music":
                    if (state == null) return false;
                    state.musicEnabled = !state.musicEnabled;
                    changedConfiguration = true;
                    break;
                case "set_music":
                    if (state == null || !TryOnOff(option, out bool musicState)) return false;
                    state.musicEnabled = musicState;
                    changedConfiguration = true;
                    break;
                case "sunroof":
                    if (state == null) return false;
                    state.sunroofOpen = !state.sunroofOpen;
                    changedConfiguration = true;
                    break;
                case "set_sunroof":
                    if (state == null || !TryOnOff(option, out bool sunroofState)) return false;
                    state.sunroofOpen = sunroofState;
                    changedConfiguration = true;
                    break;
                case "premium_audio":
                    if (state == null) return false;
                    state.premiumAudio = !state.premiumAudio;
                    changedConfiguration = true;
                    break;
                case "digital_cluster":
                    if (state == null) return false;
                    state.digitalInstrumentCluster = !state.digitalInstrumentCluster;
                    changedConfiguration = true;
                    break;
                case "wireless_charging":
                    if (state == null) return false;
                    state.wirelessCharging = !state.wirelessCharging;
                    changedConfiguration = true;
                    break;
                case "infotainment":
                    if (state == null || (option != "home" && option != "audio" && option != "climate")) return false;
                    state.infotainmentPage = option;
                    changedConfiguration = true;
                    break;
                case "day_night":
                    if (state == null) return false;
                    state.nightMode = !state.nightMode;
                    changedConfiguration = true;
                    break;
                case "set_night_mode":
                    if (state == null || !TryOnOff(option, out bool nightState)) return false;
                    state.nightMode = nightState;
                    changedConfiguration = true;
                    break;
                case "show_part_info":
                    if (!showcase) return false;
                    showcase.ShowPartInfo(option);
                    break;
                case "orbit_left": if (!orbit || IsAR()) return false; orbit.RotateBy(-30f); break;
                case "orbit_right": if (!orbit || IsAR()) return false; orbit.RotateBy(30f); break;
                case "orbit_auto": if (!orbit || IsAR()) return false; orbit.ToggleAutoRotate(); break;
                case "zoom_in": if (!orbit || IsAR()) return false; orbit.ZoomBy(-.5f); break;
                case "zoom_out": if (!orbit || IsAR()) return false; orbit.ZoomBy(.5f); break;
                case "seat":
                    if (IsAR() || !seats || !Enum.TryParse(option, true, out SeatPosition position)) return false;
                    seats.GoToSeat(position);
                    break;
                case "exit_car":
                    if (IsAR() || !seats) return false;
                    seats.ExitCar();
                    break;
                case "save": if (state == null) return false; configurator.Save(); break;
                case "load": if (state == null) return false; configurator.Load(); RestoreCabinState(); break;
                case "reset": if (state == null) return false; configurator.ResetToDefaults(); RestoreCabinState(); break;
                case "share":
                    if (showcase) showcase.CopyConfigurationToClipboard();
                    else if (state != null) GUIUtility.systemCopyBuffer = JsonUtility.ToJson(state, true);
                    else return false;
                    break;
                case "price":
                    if (state == null) return false;
                    if (showcase) showcase.Refresh();
                    RefreshPriceText();
                    break;
                default: handled = false; break;
            }
            if (!handled) { Debug.LogWarning("[Car Features] Unknown action: " + action); return false; }
            if (changedConfiguration)
            {
                configurator.Apply();
                configurator.Save();
                if (appearance) appearance.RefreshFromConfiguration();
                RestoreCabinState();
            }
            NotifyStateChanged();
            return true;
        }

        private bool IsAR() => mode != null ? !mode.SeatNavigationAllowed :
            (SceneManager.GetActiveScene().name.StartsWith("AR", StringComparison.OrdinalIgnoreCase) ||
             SceneManager.GetActiveScene().name.IndexOf("_AR", StringComparison.OrdinalIgnoreCase) >= 0 ||
             SceneManager.GetActiveScene().name.IndexOf("AR_", StringComparison.OrdinalIgnoreCase) >= 0);

        private bool Finish(bool success)
        {
            if (success) NotifyStateChanged();
            else Debug.LogWarning("[Car Features] Missing assigned style or moving part reference.", this);
            return success;
        }

        public bool IsSelected(string action, string value)
        {
            string command = (action ?? "").ToLowerInvariant();
            CarConfiguration c = configurator ? configurator.Current : null;
            switch (command)
            {
                case "body_color": return c != null && TryColor(value, out Color body) && SameColor(c.bodyColor, body);
                case "seat_color": return c != null && TryColor(value, out Color seat) && SameColor(c.seatColor, seat);
                case "rear_seat_color": return c != null && TryColor(value, out Color rear) && SameColor(c.rearSeatColor, rear);
                case "dashboard_color": return c != null && TryColor(value, out Color dash) && SameColor(c.dashboardColor, dash);
                case "door_color": return c != null && TryColor(value, out Color door) && SameColor(c.doorTrimColor, door);
                case "ambient_color": return c != null && c.ambientEnabled && TryColor(value, out Color ambient) && SameColor(c.ambientColor, ambient);
                case "ambient_off": return c != null && !c.ambientEnabled;
                case "variant": return c != null && string.Equals(c.variant.ToString(), value, StringComparison.OrdinalIgnoreCase);
                case "paint_finish": return c != null && string.Equals(c.paintFinish.ToString(), value, StringComparison.OrdinalIgnoreCase);
                case "upholstery": return c != null && string.Equals(c.upholstery, value, StringComparison.OrdinalIgnoreCase);
                case "wheel": return c != null && c.wheelStyle == value;
                case "grille": return c != null && c.grilleStyle == value;
                case "dashboard_trim": return c != null && c.dashboardTrim == value;
                case "steering": return c != null && c.steeringStyle == value;
                case "part_toggle": return c != null && c.enabledPartIds != null && c.enabledPartIds.Contains(value);
                case "door_toggle": case "window_toggle": return motions && motions.IsOpen(value);
                case "headlights": return lights && lights.AreLowBeamsOn;
                case "high_beams": return lights && lights.AreHighBeamsOn;
                case "fog_lights": return lights && lights.AreFogLightsOn;
                case "brake_lights": return lights && lights.AreBrakeLightsOn;
                case "hazard": return lights && lights.IsHazardOn;
                case "left_indicator": return lights && lights.IsLeftIndicatorOn;
                case "right_indicator": return lights && lights.IsRightIndicatorOn;
                case "ac": return c != null && c.acEnabled;
                case "music": return c != null && c.musicEnabled;
                case "sunroof": return c != null && c.sunroofOpen;
                case "premium_audio": return c != null && c.premiumAudio;
                case "digital_cluster": return c != null && c.digitalInstrumentCluster;
                case "wireless_charging": return c != null && c.wirelessCharging;
                case "infotainment": return c != null && c.infotainmentPage == value;
                case "day_night": return c != null && c.nightMode;
                case "orbit_auto": return orbit && orbit.IsAutoRotating;
                default: return false;
            }
        }

        public void ExecuteFromUI(string action) => ExecuteFeature(action, "");
        public void ChangeBodyToRed() => ExecuteFeature("body_color", "red");
        public void ChangeBodyToBlue() => ExecuteFeature("body_color", "blue");
        public void ChangeBodyToWhite() => ExecuteFeature("body_color", "white");
        public void ToggleHeadlights() => ExecuteFeature("headlights", "");
        public void ToggleMusic() => ExecuteFeature("music", "");
        public void ToggleAC() => ExecuteFeature("ac", "");
        public void ToggleSunroof() => ExecuteFeature("sunroof", "");
        public void ToggleDayNight() => ExecuteFeature("day_night", "");
        public void SaveConfiguration() => ExecuteFeature("save", "");
        public void LoadConfiguration() => ExecuteFeature("load", "");
        public void ResetConfiguration() => ExecuteFeature("reset", "");
        public void ShareConfiguration() => ExecuteFeature("share", "");

        private static bool SameColor(Color a, Color b) =>
            Mathf.Abs(a.r - b.r) < .025f && Mathf.Abs(a.g - b.g) < .025f && Mathf.Abs(a.b - b.b) < .025f;

        private static bool TryOnOff(string value, out bool enabled)
        {
            enabled = string.Equals(value, "on", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "open", StringComparison.OrdinalIgnoreCase);
            return enabled || string.Equals(value, "off", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "close", StringComparison.OrdinalIgnoreCase);
        }

        public static bool TryColor(string text, out Color color)
        {
            switch ((text ?? "").Trim().ToLowerInvariant())
            {
                case "white": color = Color.white; return true;
                case "black": color = new Color(.16f, .17f, .18f); return true;
                case "red": color = Color.red; return true;
                case "green": color = Color.green; return true;
                case "blue": color = new Color(.20f, .68f, .72f); return true;
                case "silver": color = new Color(.72f, .75f, .77f); return true;
                case "orange": color = new Color(1f, .33f, .06f); return true;
                case "yellow": color = new Color(.79f, .68f, .46f); return true;
                case "beige": color = new Color(.78f, .69f, .55f); return true;
                case "brown": color = new Color(.30f, .16f, .10f); return true;
                case "tan": color = new Color(.57f, .39f, .26f); return true;
                case "pink": color = new Color(1f, .03f, .64f); return true;
                case "purple": color = new Color(.52f, 0f, .94f); return true;
                default: return ColorUtility.TryParseHtmlString(text, out color);
            }
        }
    }
}
