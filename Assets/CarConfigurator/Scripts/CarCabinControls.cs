using UnityEngine;

namespace CarShowroom
{
    public class CarCabinControls : MonoBehaviour
    {
        [SerializeField] private Animator sunroofAnimator;
        [SerializeField] private Animator driverDoorAnimator;
        [SerializeField] private GameObject[] ambientLights;
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource engineSource;
        [SerializeField] private GameObject acIndicator;
        public bool IsACOn { get; private set; }
        public bool IsMusicOn { get; private set; }
        public bool IsEngineOn { get; private set; }
        public bool IsSunroofOpen { get; private set; }
        public bool AreAmbientLightsOn { get; private set; }

        public void SetAC(bool on) { IsACOn = on; if (acIndicator) acIndicator.SetActive(on); }
        public void SetMusic(bool on)
        {
            IsMusicOn = on;
            if (!musicSource) return;
            if (on && musicSource.clip && !musicSource.isPlaying) musicSource.Play();
            else if (!on && musicSource.isPlaying) musicSource.Stop();
        }
        public void SetEngine(bool on)
        {
            IsEngineOn = on;
            if (!engineSource) return;
            if (on && engineSource.clip) engineSource.Play(); else engineSource.Stop();
        }
        public void SetAmbientLights(bool on)
        {
            AreAmbientLightsOn = on;
            foreach (var lightRoot in ambientLights) if (lightRoot) lightRoot.SetActive(on);
        }
        public void SetSunroof(bool open) { IsSunroofOpen = open; if (sunroofAnimator) sunroofAnimator.SetBool("Open", open); }
        public void SetDriverDoor(bool open) { if (driverDoorAnimator) driverDoorAnimator.SetBool("Open", open); }
    }
}
