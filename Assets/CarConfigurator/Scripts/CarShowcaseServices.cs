using System;
using UnityEngine;

namespace CarShowroom
{
    [Serializable]
    public class PartInformation
    {
        public string partId;
        public string title;
        [TextArea] public string description;
    }

    public class CarShowcaseServices : MonoBehaviour
    {
        [SerializeField] private CarConfiguratorManager configurator;
        [SerializeField] private Light[] showroomLights;
        [SerializeField] private GameObject daylightEnvironment;
        [SerializeField] private GameObject nightEnvironment;
        [SerializeField] private PartInformation[] information;
        [SerializeField] private TMPro.TMP_Text infoTitle;
        [SerializeField] private TMPro.TMP_Text infoBody;
        [SerializeField] private TMPro.TMP_Text priceText;
        [SerializeField] private TMPro.TMP_Text priceBreakdownText;
        [SerializeField] private string currencyPrefix = "₹";

        private void OnEnable()
        {
            if (configurator) configurator.ConfigurationChanged += Refresh;
            Refresh();
        }
        private void OnDisable() { if (configurator) configurator.ConfigurationChanged -= Refresh; }

        public void Refresh()
        {
            if (!configurator) return;
            SetDayNightVisual(configurator.Current.nightMode);
            if (priceText) priceText.text = currencyPrefix + configurator.GetEstimatedPrice().ToString("N0");
            if (priceBreakdownText) priceBreakdownText.text = configurator.GetPriceBreakdown(currencyPrefix);
        }

        public void SetNightMode(bool enabled)
        {
            if (!configurator) return;
            configurator.Current.nightMode = enabled;
            configurator.Save();
            SetDayNightVisual(enabled);
        }

        private void SetDayNightVisual(bool night)
        {
            if (daylightEnvironment) daylightEnvironment.SetActive(!night);
            if (nightEnvironment) nightEnvironment.SetActive(night);
            foreach (Light light in showroomLights) if (light) light.enabled = !night;
        }

        public void ShowPartInfo(string partId)
        {
            PartInformation part = Array.Find(information, p => p != null && p.partId == partId);
            if (infoTitle) infoTitle.text = part != null ? part.title : "Part not found";
            if (infoBody) infoBody.text = part != null ? part.description : "No information available.";
        }

        public string ExportConfigurationJson()
        {
            return configurator ? JsonUtility.ToJson(configurator.Current, true) : "{}";
        }

        // Cross-platform fallback: user can paste the exported JSON into a message.
        public void CopyConfigurationToClipboard()
        {
            GUIUtility.systemCopyBuffer = ExportConfigurationJson();
        }
    }
}
