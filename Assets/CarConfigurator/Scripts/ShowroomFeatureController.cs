using UnityEngine;

namespace CarShowroom
{
    // Both modes use cabin feature controls; AR never initiates seat navigation.
    public class ShowroomFeatureController : MonoBehaviour
    {
        [SerializeField] private ExperienceModeManager mode;
        [SerializeField] private CarCabinControls cabin;
        [SerializeField] private GameObject infotainmentHome;
        [SerializeField] private GameObject infotainmentAudio;
        [SerializeField] private GameObject infotainmentClimate;
        [SerializeField] private GameObject basicAudioSystem;
        [SerializeField] private GameObject premiumAudioSystem;
        [SerializeField] private GameObject analogCluster;
        [SerializeField] private GameObject digitalCluster;
        [SerializeField] private GameObject wirelessCharger;
        [SerializeField] private TMPro.TMP_Text acTemperatureText;
        [SerializeField] private TMPro.TMP_Text acFanText;
        private bool acOn;
        private float temperature = 22f;
        private int fanSpeed = 1;
        public bool IsACOn => acOn;
        public bool IsPremiumAudioSelected { get; private set; }
        public bool IsDigitalClusterSelected { get; private set; }
        public bool IsWirelessChargerSelected { get; private set; }
        public string CurrentScreen { get; private set; } = "home";

        public void BindMode(ExperienceModeManager value) => mode = value;
        public void BindCabin(CarCabinControls value) => cabin = value;
        private bool Allowed => mode == null || mode.CabinInteractionAllowed;

        public void SetScreen(string screen)
        {
            if (!Allowed) return;
            CurrentScreen = screen;
            if (infotainmentHome) infotainmentHome.SetActive(screen == "home");
            if (infotainmentAudio) infotainmentAudio.SetActive(screen == "audio");
            if (infotainmentClimate) infotainmentClimate.SetActive(screen == "climate");
        }

        public void SelectPremiumAudio(bool premium)
        {
            if (!Allowed) return;
            IsPremiumAudioSelected = premium;
            if (basicAudioSystem) basicAudioSystem.SetActive(!premium);
            if (premiumAudioSystem) premiumAudioSystem.SetActive(premium);
        }

        public void SetDigitalCluster(bool digital)
        {
            if (!Allowed) return;
            IsDigitalClusterSelected = digital;
            if (analogCluster) analogCluster.SetActive(!digital);
            if (digitalCluster) digitalCluster.SetActive(digital);
        }

        public void SetWirelessCharger(bool installed)
        {
            if (Allowed) IsWirelessChargerSelected = installed;
            if (Allowed && wirelessCharger) wirelessCharger.SetActive(installed);
        }

        public void SetAC(bool on)
        {
            if (!Allowed) return;
            acOn = on;
            if (cabin) cabin.SetAC(on);
            RefreshAC();
        }

        public void SetACTemperature(float celsius)
        {
            if (!Allowed) return;
            temperature = Mathf.Clamp(celsius, 16f, 30f);
            RefreshAC();
        }

        public void SetFanSpeed(int speed)
        {
            if (!Allowed) return;
            fanSpeed = Mathf.Clamp(speed, 0, 5);
            RefreshAC();
        }

        private void RefreshAC()
        {
            if (acTemperatureText) acTemperatureText.text = acOn ? temperature.ToString("0") + "°C" : "OFF";
            if (acFanText) acFanText.text = acOn ? "Fan " + fanSpeed : "Fan OFF";
        }
    }
}
