#if UNITY_AR_FOUNDATION
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace CarShowroom
{
    public class ARCarPlacementManager : MonoBehaviour
    {
        [SerializeField] private ARRaycastManager raycastManager;
        [SerializeField] private GameObject carPrefab;
        [SerializeField] private Camera arCamera;
        [SerializeField] private MobileExperienceUI mobileUI;
        [SerializeField] private ExperienceModeManager arMode;
        [SerializeField] private AICommandExecutor arExecutor;
        private GameObject spawnedCar;
        public event Action<GameObject> CarPlaced;
        private static readonly List<ARRaycastHit> Hits = new List<ARRaycastHit>();

        private void Update()
        {
            if (Input.touchCount != 1 || Input.GetTouch(0).phase != TouchPhase.Began) return;
            Touch touch = Input.GetTouch(0);
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId)) return;
            if (!raycastManager || !carPrefab || !arCamera) return;
            if (!raycastManager.Raycast(touch.position, Hits, TrackableType.PlaneWithinPolygon)) return;
            Pose pose = Hits[0].pose;
            if (spawnedCar == null)
            {
                spawnedCar = Instantiate(carPrefab, pose.position, pose.rotation);
                VehicleMotionController vehicleMotion = spawnedCar.GetComponentInChildren<VehicleMotionController>();
                if (vehicleMotion && arMode) vehicleMotion.BindMode(arMode);
                CarConfiguratorManager configurator = spawnedCar.GetComponentInChildren<CarConfiguratorManager>();
                if (configurator != null)
                {
                    configurator.Load();
                    if (mobileUI != null) mobileUI.BindCar(configurator);
                    if (arExecutor != null)
                        arExecutor.BindForAR(configurator,
                            vehicleMotion,
                            spawnedCar.GetComponentInChildren<VehicleLightsAndSounds>(), arMode);
                }
                CarPlaced?.Invoke(spawnedCar);
            }
            else spawnedCar.transform.SetPositionAndRotation(pose.position, pose.rotation);
        }
    }
}
#endif
