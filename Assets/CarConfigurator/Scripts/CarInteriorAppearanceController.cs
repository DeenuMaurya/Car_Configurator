using System;
using System.Collections.Generic;
using UnityEngine;

namespace CarShowroom
{
    /// <summary>
    /// Uses MATERIAL SLOTS, not whole-mesh recoloring, so a combined FBX interior can
    /// expose dashboard/door/seat choices without changing every material on the mesh.
    /// Material names are from this repository. Missing separate seat geometry is reported.
    /// </summary>
    [DefaultExecutionOrder(1100)]
    public class CarInteriorAppearanceController : MonoBehaviour
    {
        [SerializeField] private CarConfiguratorManager configurator;
        [SerializeField] private Light[] cabinAmbientLights;
        [SerializeField] private Renderer[] separatelyModeledRearSeats;
        private MaterialPropertyBlock block;
        private readonly HashSet<string> warned = new HashSet<string>();

        private void Awake()
        {
            if (block == null) block = new MaterialPropertyBlock();
            if (!configurator) configurator = GetComponent<CarConfiguratorManager>();
        }

        private void OnEnable()
        {
            if (!configurator) configurator = GetComponent<CarConfiguratorManager>();
            if (configurator) configurator.ConfigurationChanged += RefreshFromConfiguration;
        }

        private void OnDisable()
        {
            if (configurator) configurator.ConfigurationChanged -= RefreshFromConfiguration;
        }

        private void Start() => RefreshFromConfiguration();

        public void RefreshFromConfiguration()
        {
            if (!configurator || configurator.Current == null) return;
            CarConfiguration state = configurator.Current;
            ApplyToNamedSlots("dashboard", state.dashboardColor, "dashboard", "deshboard");
            ApplyToNamedSlots("door-trim", state.doorTrimColor, "interior_door", "door-02");
            ApplyToNamedSlots("seats", state.seatColor, "seat_color", "seat_color01", "seat", "fabric", "leather", "upholstery");
            if (separatelyModeledRearSeats != null && separatelyModeledRearSeats.Length > 0)
            {
                foreach (Renderer renderer in separatelyModeledRearSeats)
                {
                    if (!renderer) continue;
                    var materials = renderer.sharedMaterials;
                    for (int i = 0; i < materials.Length; i++) SetSlotColor(renderer, i, materials[i], state.rearSeatColor);
                }
            }
            else WarnOnce("rear-seats", "Back Seat selection needs separate rear-seat renderer/material mapping; it is saved but cannot be visually separated yet.");
            if (cabinAmbientLights != null && cabinAmbientLights.Length > 0)
            {
                foreach (Light light in cabinAmbientLights)
                {
                    if (!light) continue;
                    light.enabled = state.ambientEnabled;
                    light.color = state.ambientColor;
                }
            }
            else if (state.ambientEnabled) WarnOnce("ambient", "Assign cabinAmbientLights for colored ambient lighting; no interior Light references exist yet.");
        }

        private void ApplyToNamedSlots(string feature, Color color, params string[] tokens)
        {
            int matched = 0;
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
            {
                if (!renderer) continue;
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    Material material = materials[i];
                    if (!material) continue;
                    string name = material.name.ToLowerInvariant();
                    if (!Array.Exists(tokens, token => name.Contains(token))) continue;
                    SetSlotColor(renderer, i, material, color);
                    matched++;
                }
            }
            if (matched == 0) WarnOnce(feature, "No matching " + feature + " material slots found. Assign verified renderers/materials in Inspector.");
        }

        private void SetSlotColor(Renderer renderer, int index, Material material, Color color)
        {
            if (!material) return;
            string property = material.HasProperty("_BaseColor") ? "_BaseColor" :
                material.HasProperty("_Color") ? "_Color" : null;
            if (property == null) return;
            renderer.GetPropertyBlock(block, index);
            block.SetColor(property, color);
            renderer.SetPropertyBlock(block, index);
        }

        private void WarnOnce(string feature, string message)
        {
            if (warned.Add(feature)) Debug.LogWarning("[Car Appearance] " + message, this);
        }
    }
}
