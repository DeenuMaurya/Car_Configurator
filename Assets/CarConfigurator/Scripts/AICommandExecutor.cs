using System;
using UnityEngine;

namespace CarShowroom
{
    [Serializable]
    public class AIAction
    {
        public string action;
        public string target;
        public string value;
    }

    public class AICommandExecutor : MonoBehaviour
    {
        [SerializeField] private CarConfiguratorManager configurator;
        [SerializeField] private CarSeatNavigator navigator;
        [SerializeField] private CarCabinControls cabin;
        [SerializeField] private ExperienceModeManager modeManager;
        [SerializeField] private VehicleMotionController motions;
        [SerializeField] private VehicleLightsAndSounds lighting;
        [SerializeField] private CarShowcaseServices showcase;

        public void BindForAR(CarConfiguratorManager car, VehicleMotionController vehicleMotions,
            VehicleLightsAndSounds vehicleLighting, ExperienceModeManager currentMode)
        {
            configurator = car;
            motions = vehicleMotions;
            lighting = vehicleLighting;
            modeManager = currentMode;
            cabin = car ? car.GetComponentInParent<CarCabinControls>() : null;
            navigator = null;
        }

        // Accept only an allowlisted action; never execute model-supplied code.
        public bool ExecuteJson(string json)
        {
            try { return Execute(JsonUtility.FromJson<AIAction>(json)); }
            catch (Exception e) { Debug.LogWarning("Invalid AI action: " + e.Message); return false; }
        }

        public bool Execute(AIAction command)
        {
            if (command == null || string.IsNullOrWhiteSpace(command.action)) return false;
            string action = command.action.Trim().ToLowerInvariant();
            string value = (command.value ?? "").Trim().ToLowerInvariant();
            // AR allows configuration/cabin interactions, but absolutely no seat/camera navigation.
            if (modeManager != null && !modeManager.SeatNavigationAllowed &&
                (action == "go_to_seat" || action == "exit_car"))
            {
                Debug.LogWarning("Seat navigation is disabled in AR: " + action);
                return false;
            }
            switch (action)
            {
                case "change_car_color":
                    if (!configurator || !TryColor(value, out Color body)) return false;
                    configurator.SetBodyColor(body); configurator.Save(); return true;
                case "change_seat_color":
                    if (!configurator || !TryColor(value, out Color seat)) return false;
                    configurator.SetSeatColor(seat); configurator.Save(); return true;
                case "set_paint_finish":
                    if (!configurator || !Enum.TryParse(value, true, out PaintFinish finish) ||
                        !Enum.IsDefined(typeof(PaintFinish), finish)) return false;
                    configurator.SetPaintFinish(finish); configurator.Save(); return true;
                case "set_variant":
                    if (!configurator || !Enum.TryParse(value, true, out CarVariant variant) ||
                        !Enum.IsDefined(typeof(CarVariant), variant)) return false;
                    configurator.SetVariant(variant); configurator.Save(); return true;
                case "set_wheel_style":
                    return configurator && configurator.SetWheelStyle(value);
                case "set_grille_style":
                    return configurator && configurator.SetGrilleStyle(value);
                case "set_dashboard_trim":
                    return configurator && configurator.SetDashboardTrim(value);
                case "set_steering_style":
                    return configurator && configurator.SetSteeringStyle(value);
                case "set_upholstery":
                    if (!configurator || (value != "fabric" && value != "leather" && value != "premiumleather")) return false;
                    configurator.SetUpholstery(value); configurator.Save(); return true;
                case "set_part":
                    if (!configurator || string.IsNullOrWhiteSpace(command.target)) return false;
                    if (value != "on" && value != "off") return false;
                    bool changed = configurator.SetPart(command.target, value == "on");
                    if (changed) configurator.Save();
                    return changed;
                case "go_to_seat":
                    if (!navigator || !Enum.TryParse(value, true, out SeatPosition position)) return false;
                    navigator.GoToSeat(position); return true;
                case "exit_car":
                    if (!navigator) return false; navigator.ExitCar(); return true;
                case "set_ac":
                    if (!cabin || !TryOnOff(value, out bool ac)) return false;
                    cabin.SetAC(ac); return true;
                case "set_music":
                    if (!cabin || !TryOnOff(value, out bool music)) return false;
                    cabin.SetMusic(music); return true;
                case "set_sunroof":
                    if (!cabin || !TryOnOff(value, out bool roof)) return false;
                    cabin.SetSunroof(roof); return true;
                case "set_ambient_lights":
                    if (!cabin || !TryOnOff(value, out bool lights)) return false;
                    cabin.SetAmbientLights(lights); return true;
                case "set_engine":
                    if (!cabin || !TryOnOff(value, out bool engine)) return false;
                    cabin.SetEngine(engine); return true;
                case "set_motion":
                    if (!motions || string.IsNullOrWhiteSpace(command.target) || !TryOnOff(value, out bool open)) return false;
                    return motions.SetOpen(command.target, open);
                case "set_all_doors":
                    if (!motions || !TryOnOff(value, out bool allDoors)) return false;
                    motions.SetAllDoors(allDoors); return true;
                case "fold_mirrors":
                    if (!motions || !TryOnOff(value, out bool folded)) return false;
                    motions.FoldBothMirrors(folded); return true;
                case "set_lights":
                    if (!lighting || !TryOnOff(value, out bool lit)) return false;
                    switch ((command.target ?? "").ToLowerInvariant())
                    {
                        case "headlights": lighting.SetLowBeams(lit); return true;
                        case "high_beams": lighting.SetHighBeams(lit); return true;
                        case "fog_lights": lighting.SetFogLights(lit); return true;
                        case "brake_lights": lighting.SetBrakeLights(lit); return true;
                        case "hazard": lighting.SetHazard(lit); return true;
                        case "left_indicator": lighting.SetLeftIndicator(lit); return true;
                        case "right_indicator": lighting.SetRightIndicator(lit); return true;
                        default: return false;
                    }
                case "honk": if (!lighting) return false; lighting.Honk(); return true;
                case "set_locked":
                    if (!lighting || !TryOnOff(value, out bool locked)) return false;
                    lighting.SetLocked(locked); return true;
                case "set_night_mode":
                    if (!showcase || !TryOnOff(value, out bool night)) return false;
                    showcase.SetNightMode(night); return true;
                default: return false;
            }
        }

        private static bool TryOnOff(string value, out bool on)
        {
            on = value == "on" || value == "open" || value == "fold";
            return on || value == "off" || value == "close" || value == "unfold";
        }

        private static bool TryColor(string name, out Color color)
        {
            switch (name)
            {
                case "red": color = Color.red; return true;
                case "green": color = Color.green; return true;
                case "white": color = Color.white; return true;
                case "black": color = Color.black; return true;
                case "blue": color = Color.blue; return true;
                case "silver": color = new Color(.65f, .68f, .70f); return true;
                case "beige": color = new Color(.78f, .69f, .55f); return true;
                case "brown": color = new Color(.28f, .14f, .07f); return true;
                default: color = Color.white; return false;
            }
        }
    }
}
