using UnityEngine;

namespace CarShowroom
{
    public class VehicleLightsAndSounds : MonoBehaviour
    {
        [SerializeField] private Light[] lowBeams;
        [SerializeField] private Light[] highBeams;
        [SerializeField] private Light[] fogLights;
        [SerializeField] private GameObject[] leftIndicators;
        [SerializeField] private GameObject[] rightIndicators;
        [SerializeField] private GameObject[] brakeEmissiveMeshes;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioClip horn;
        [SerializeField] private AudioClip lockSound;
        [SerializeField] private AudioClip unlockSound;
        [SerializeField] private AudioClip indicatorTick;
        [SerializeField] private float blinkInterval = 0.5f;
        private bool left, right, hazard;
        private float blinkTimer;
        private bool blinkLit;

        public void SetLowBeams(bool on) => SetLights(lowBeams, on);
        public void SetHighBeams(bool on) => SetLights(highBeams, on);
        public void SetFogLights(bool on) => SetLights(fogLights, on);
        public void SetBrakeLights(bool on) => SetObjects(brakeEmissiveMeshes, on);
        public void SetLeftIndicator(bool on) { left = on; if (on) { right = false; hazard = false; } RefreshIndicators(); }
        public void SetRightIndicator(bool on) { right = on; if (on) { left = false; hazard = false; } RefreshIndicators(); }
        public void SetHazard(bool on) { hazard = on; if (on) { left = false; right = false; } RefreshIndicators(); }
        public void Honk() => Play(horn);
        public void SetLocked(bool locked) => Play(locked ? lockSound : unlockSound);

        private void Update()
        {
            if (!left && !right && !hazard) return;
            blinkTimer += Time.deltaTime;
            if (blinkTimer < Mathf.Max(.1f, blinkInterval)) return;
            blinkTimer = 0;
            blinkLit = !blinkLit;
            RefreshIndicators();
            if (blinkLit) Play(indicatorTick);
        }

        private void RefreshIndicators()
        {
            SetObjects(leftIndicators, (hazard || left) && blinkLit);
            SetObjects(rightIndicators, (hazard || right) && blinkLit);
        }

        private static void SetLights(Light[] lights, bool on)
        {
            foreach (Light light in lights) if (light) light.enabled = on;
        }

        private static void SetObjects(GameObject[] objects, bool on)
        {
            foreach (GameObject obj in objects) if (obj) obj.SetActive(on);
        }

        private void Play(AudioClip clip) { if (sfxSource && clip) sfxSource.PlayOneShot(clip); }
    }
}
