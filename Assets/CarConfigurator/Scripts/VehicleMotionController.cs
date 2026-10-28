using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CarShowroom
{
    public enum MotionType { Rotate, Slide }

    [Serializable]
    public class MovingPart
    {
        public string id; // four doors and four separate window panes only
        public Transform pivot;
        public MotionType motionType;
        public Vector3 openEulerOffset;
        public Vector3 openLocalOffset;
        public float duration = 0.8f;
        public bool exterior = true;
        public AudioClip openSound;
        public AudioClip closeSound;
        [NonSerialized] public bool isOpen;
        [NonSerialized] public Vector3 closedPosition;
        [NonSerialized] public Quaternion closedRotation;
        [NonSerialized] public Coroutine running;
    }

    public class VehicleMotionController : MonoBehaviour
    {
        [SerializeField] private ExperienceModeManager mode;
        [SerializeField] private AudioSource effectsSource;
        [SerializeField] private List<MovingPart> movingParts = new List<MovingPart>();
        public void BindMode(ExperienceModeManager value) => mode = value;

        private void Awake()
        {
            foreach (MovingPart part in movingParts)
            {
                if (part == null || !part.pivot) continue;
                part.closedPosition = part.pivot.localPosition;
                part.closedRotation = part.pivot.localRotation;
                part.isOpen = false;
            }
        }

        public bool SetOpen(string id, bool open)
        {
            // Only doors and separate window panes are supported. Bonnet/boot/mirror-fold
            // features were removed from this project scope.
            if (!IsSupportedPart(id)) return false;
            MovingPart part = movingParts.Find(p => p != null && string.Equals(p.id, id, StringComparison.OrdinalIgnoreCase));
            if (part == null || !part.pivot) return false;
            if (part.running != null) StopCoroutine(part.running);
            part.running = StartCoroutine(Animate(part, open));
            part.isOpen = open;
            if (effectsSource && (open ? part.openSound : part.closeSound))
                effectsSource.PlayOneShot(open ? part.openSound : part.closeSound);
            return true;
        }

        public bool IsOpen(string id)
        {
            MovingPart part = movingParts.Find(p => p != null && string.Equals(p.id, id, StringComparison.OrdinalIgnoreCase));
            return part != null && part.isOpen;
        }

        public bool Toggle(string id) => SetOpen(id, !IsOpen(id));
        public void OpenFrontLeftDoor() => SetOpen("front_left_door", true);
        public void CloseFrontLeftDoor() => SetOpen("front_left_door", false);
        public void OpenFrontRightDoor() => SetOpen("front_right_door", true);
        public void CloseFrontRightDoor() => SetOpen("front_right_door", false);
        public void OpenRearLeftDoor() => SetOpen("rear_left_door", true);
        public void CloseRearLeftDoor() => SetOpen("rear_left_door", false);
        public void OpenRearRightDoor() => SetOpen("rear_right_door", true);
        public void CloseRearRightDoor() => SetOpen("rear_right_door", false);
        public void OpenAllDoors() => SetAllDoors(true);
        public void CloseAllDoors() => SetAllDoors(false);

        public void SetAllDoors(bool open)
        {
            SetOpen("front_left_door", open);
            SetOpen("front_right_door", open);
            SetOpen("rear_left_door", open);
            SetOpen("rear_right_door", open);
        }

        private static bool IsSupportedPart(string id)
        {
            switch ((id ?? "").ToLowerInvariant())
            {
                case "front_left_door": case "front_right_door":
                case "rear_left_door": case "rear_right_door":
                case "front_left_window": case "front_right_window":
                case "rear_left_window": case "rear_right_window": return true;
                default: return false;
            }
        }

        private IEnumerator Animate(MovingPart part, bool open)
        {
            Vector3 fromPosition = part.pivot.localPosition;
            Quaternion fromRotation = part.pivot.localRotation;
            Vector3 toPosition = part.closedPosition + (open ? part.openLocalOffset : Vector3.zero);
            Quaternion toRotation = part.closedRotation * Quaternion.Euler(open ? part.openEulerOffset : Vector3.zero);
            float elapsed = 0f;
            float duration = Mathf.Max(0.01f, part.duration);
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                t = t * t * (3f - 2f * t);
                if (part.motionType == MotionType.Slide)
                    part.pivot.localPosition = Vector3.Lerp(fromPosition, toPosition, t);
                else
                    part.pivot.localRotation = Quaternion.Slerp(fromRotation, toRotation, t);
                yield return null;
            }
            if (part.motionType == MotionType.Slide) part.pivot.localPosition = toPosition;
            else part.pivot.localRotation = toRotation;
            part.running = null;
        }
    }
}
