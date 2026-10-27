using System.Collections;
using UnityEngine;

namespace CarShowroom
{
    public class CarSeatNavigator : MonoBehaviour
    {
        [SerializeField] private Transform cameraRig;
        [SerializeField] private ExperienceModeManager experienceMode;
        [SerializeField] private Transform exteriorAnchor;
        [SerializeField] private Transform driverAnchor;
        [SerializeField] private Transform frontPassengerAnchor;
        [SerializeField] private Transform rearLeftAnchor;
        [SerializeField] private Transform rearRightAnchor;
        [SerializeField] private float transitionSeconds = 1.2f;
        [SerializeField] private Behaviour[] disableDuringSeatedMode;
        private Coroutine moving;

        public void GoToSeat(SeatPosition seat)
        {
            if (experienceMode != null && !experienceMode.SeatNavigationAllowed) return;
            Transform destination = seat == SeatPosition.Driver ? driverAnchor :
                seat == SeatPosition.FrontPassenger ? frontPassengerAnchor :
                seat == SeatPosition.RearLeft ? rearLeftAnchor : rearRightAnchor;
            Move(destination, true);
        }

        public void ExitCar()
        {
            if (experienceMode != null && !experienceMode.SeatNavigationAllowed) return;
            Move(exteriorAnchor, false);
        }

        private void Move(Transform destination, bool seated)
        {
            if (cameraRig == null || destination == null) { Debug.LogWarning("Seat navigation anchors are missing."); return; }
            if (moving != null) StopCoroutine(moving);
            moving = StartCoroutine(Transition(destination, seated));
        }

        private IEnumerator Transition(Transform destination, bool seated)
        {
            foreach (Behaviour behaviour in disableDuringSeatedMode)
                if (behaviour != null) behaviour.enabled = false;
            Vector3 fromPosition = cameraRig.position;
            Quaternion fromRotation = cameraRig.rotation;
            float t = 0;
            float duration = Mathf.Max(0.01f, transitionSeconds);
            while (t < 1)
            {
                t = Mathf.Min(1, t + Time.deltaTime / duration);
                float smooth = t * t * (3 - 2 * t);
                cameraRig.SetPositionAndRotation(Vector3.Lerp(fromPosition, destination.position, smooth),
                    Quaternion.Slerp(fromRotation, destination.rotation, smooth));
                yield return null;
            }
            foreach (Behaviour behaviour in disableDuringSeatedMode)
                if (behaviour != null) behaviour.enabled = !seated;
            moving = null;
        }
    }
}
