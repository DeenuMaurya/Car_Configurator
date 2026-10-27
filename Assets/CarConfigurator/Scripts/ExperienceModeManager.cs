using UnityEngine;
using UnityEngine.SceneManagement;

namespace CarShowroom
{
    public enum ExperienceMode { Showroom, ARExterior }

    // Keep one manager in each scene, configured with the appropriate mode.
    public class ExperienceModeManager : MonoBehaviour
    {
        [SerializeField] private ExperienceMode mode = ExperienceMode.Showroom;
        [SerializeField] private string showroomSceneName = "Mobile_Showroom";
        [SerializeField] private string arSceneName = "Mobile_AR";

        public ExperienceMode Mode => mode;
        // AR supports remote cabin controls but never teleports/moves the viewer into a seat.
        public bool CabinInteractionAllowed => true;
        public bool SeatNavigationAllowed => mode == ExperienceMode.Showroom;
        [System.Obsolete("Use SeatNavigationAllowed or CabinInteractionAllowed explicitly.")]
        public bool InteriorAllowed => SeatNavigationAllowed;

        public void OpenShowroom() => SceneManager.LoadScene(showroomSceneName);
        public void OpenAR() => SceneManager.LoadScene(arSceneName);
    }
}
