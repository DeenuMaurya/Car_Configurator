using UnityEngine;
using UnityEngine.EventSystems;

namespace CarShowroom
{
    // Attach to showroom camera; assign vehicle center as target.
    public class MobileShowroomOrbit : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float distance = 5f;
        [SerializeField] private float minDistance = 2f;
        [SerializeField] private float maxDistance = 9f;
        [SerializeField] private float orbitSpeed = .15f;
        [SerializeField] private float pinchSpeed = .01f;
        [SerializeField] private float minPitch = -15f;
        [SerializeField] private float maxPitch = 75f;
        [SerializeField] private bool allowInput = true;
        [SerializeField] private float automaticOrbitDegreesPerSecond = 12f;
        private float yaw;
        private float pitch = 15f;
        public bool IsAutoRotating { get; private set; }

        public void SetOrbitInput(bool allowed) => allowInput = allowed;
        public void RotateBy(float degrees) => yaw += degrees;
        public void ZoomBy(float delta) => distance = Mathf.Clamp(distance + delta, minDistance, maxDistance);
        public void SetAutoRotate(bool enabled) => IsAutoRotating = enabled;
        public void ToggleAutoRotate() => SetAutoRotate(!IsAutoRotating);

        private void LateUpdate()
        {
            if (!target || !allowInput) return;
            if (IsAutoRotating) yaw += automaticOrbitDegreesPerSecond * Time.deltaTime;
            if (Input.touchCount == 1)
            {
                Touch touch = Input.GetTouch(0);
                if (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                {
                    yaw += touch.deltaPosition.x * orbitSpeed;
                    pitch = Mathf.Clamp(pitch - touch.deltaPosition.y * orbitSpeed, minPitch, maxPitch);
                }
            }
            else if (Input.touchCount == 2)
            {
                Touch a = Input.GetTouch(0), b = Input.GetTouch(1);
                if (EventSystem.current == null || (!EventSystem.current.IsPointerOverGameObject(a.fingerId) && !EventSystem.current.IsPointerOverGameObject(b.fingerId)))
                {
                    float current = Vector2.Distance(a.position, b.position);
                    float previous = Vector2.Distance(a.position - a.deltaPosition, b.position - b.deltaPosition);
                    distance = Mathf.Clamp(distance - (current - previous) * pinchSpeed, minDistance, maxDistance);
                }
            }
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
            transform.SetPositionAndRotation(target.position + rotation * (Vector3.back * distance), rotation);
        }
    }
}
