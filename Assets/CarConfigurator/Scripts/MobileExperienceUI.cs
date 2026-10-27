using UnityEngine;

namespace CarShowroom
{
    // Assign a separate instance in each scene. AR binds when the vehicle is spawned.
    public class MobileExperienceUI : MonoBehaviour
    {
        [SerializeField] private ExperienceModeManager mode;
        [SerializeField] private CarConfiguratorManager configurator;
        [SerializeField] private CarSeatNavigator navigator;
        [SerializeField] private CarCabinControls cabin;
        [SerializeField] private VehicleMotionController motions;
        [SerializeField] private VehicleLightsAndSounds lights;
        [SerializeField] private ShowroomFeatureController cabinFeatures;

        public void BindCar(CarConfiguratorManager car)
        {
            configurator = car;
            if (car)
            {
                if (mode != null && !mode.SeatNavigationAllowed) navigator = null;
                motions = car.GetComponentInParent<VehicleMotionController>();
                lights = car.GetComponentInParent<VehicleLightsAndSounds>();
                cabin = car.GetComponentInParent<CarCabinControls>();
                ShowroomFeatureController carFeatures = car.GetComponentInParent<ShowroomFeatureController>();
                if (carFeatures) cabinFeatures = carFeatures;
                if (motions) motions.BindMode(mode);
                if (cabinFeatures) { cabinFeatures.BindMode(mode); cabinFeatures.BindCabin(cabin); }
            }
        }
        public void SetDoor(string id, bool open) { if (motions) motions.SetOpen(id, open); }
        public void SetAllDoors(bool open) { if (motions) motions.SetAllDoors(open); }
        public void SetHeadlights(bool enabled) { if (lights) lights.SetLowBeams(enabled); }
        public void SetHighBeams(bool enabled) { if (lights) lights.SetHighBeams(enabled); }
        public void SetFogLights(bool enabled) { if (lights) lights.SetFogLights(enabled); }
        public void SetLeftIndicator(bool enabled) { if (lights) lights.SetLeftIndicator(enabled); }
        public void SetRightIndicator(bool enabled) { if (lights) lights.SetRightIndicator(enabled); }
        public void SetBrakeLights(bool enabled) { if (lights) lights.SetBrakeLights(enabled); }
        public void SetHazard(bool enabled) { if (lights) lights.SetHazard(enabled); }
        public void Honk() { if (lights) lights.Honk(); }
        public void SetLocked(bool locked) { if (lights) lights.SetLocked(locked); }
        public void SetPaintFinish(int index)
        {
            if (!configurator || index < 0 || index > 2) return;
            configurator.SetPaintFinish((PaintFinish)index);
            configurator.Save();
        }
        public void SetWheelStyle(string style) { if (configurator) configurator.SetWheelStyle(style); }
        public void SetGrilleStyle(string style) { if (configurator) configurator.SetGrilleStyle(style); }
        public void SetSeatColor(Color color)
        {
            if (!configurator) return;
            configurator.SetSeatColor(color); configurator.Save();
        }
        public void SetUpholstery(string material)
        {
            if (!configurator) return;
            configurator.SetUpholstery(material); configurator.Save();
        }
        public void SetDashboardTrim(string style) { if (configurator) configurator.SetDashboardTrim(style); }
        public void SetSteeringStyle(string style) { if (configurator) configurator.SetSteeringStyle(style); }
        public void SetMusic(bool enabled) { if (cabin) cabin.SetMusic(enabled); }
        public void SetAmbientLighting(bool enabled) { if (cabin) cabin.SetAmbientLights(enabled); }
        public void SetSunroof(bool open) { if (cabin) cabin.SetSunroof(open); }
        public void SetACTemperature(float temperature) { if (cabinFeatures) cabinFeatures.SetACTemperature(temperature); }
        public void SetFanSpeed(int speed) { if (cabinFeatures) cabinFeatures.SetFanSpeed(speed); }
        public void SetInfotainmentScreen(string screen) { if (cabinFeatures) cabinFeatures.SetScreen(screen); }
        public void SetPremiumAudio(bool enabled) { if (cabinFeatures) cabinFeatures.SelectPremiumAudio(enabled); }
        public void SetDigitalCluster(bool enabled) { if (cabinFeatures) cabinFeatures.SetDigitalCluster(enabled); }
        public void SetWirelessCharger(bool enabled) { if (cabinFeatures) cabinFeatures.SetWirelessCharger(enabled); }
        public void SetBodyColor(Color color)
        {
            if (!configurator) return;
            configurator.SetBodyColor(color);
            configurator.Save();
        }
        public void SetVariant(int index)
        {
            if (!configurator || index < 0 || index > 2) return;
            configurator.SetVariant((CarVariant)index);
            configurator.Save();
        }
        public void SetPart(string partId, bool enabled)
        {
            if (!configurator) return;
            if (configurator.SetPart(partId, enabled)) configurator.Save();
        }
        public void SetExteriorPart(string partId, bool enabled) => SetPart(partId, enabled);
        public void GoDriver()
        {
            if (mode == null || !mode.SeatNavigationAllowed) return;
            if (navigator) navigator.GoToSeat(SeatPosition.Driver);
        }
        public void GoRearLeft()
        {
            if (mode == null || !mode.SeatNavigationAllowed) return;
            if (navigator) navigator.GoToSeat(SeatPosition.RearLeft);
        }
        public void SetAC(bool enabled)
        {
            if (cabin) cabin.SetAC(enabled);
        }
    }
}
