using UnityEngine;
using UnityEngine.UI;

namespace CarShowroom
{
    public sealed class CarFeaturePanelToggle : MonoBehaviour
    {
        [SerializeField] private GameObject featurePanel;
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;

        public void Configure(GameObject panel, Button open, Button close)
        {
            featurePanel = panel;
            openButton = open;
            closeButton = close;
        }

        private void OnEnable()
        {
            if (openButton) openButton.onClick.AddListener(Open);
            if (closeButton) closeButton.onClick.AddListener(Close);
        }

        private void OnDisable()
        {
            if (openButton) openButton.onClick.RemoveListener(Open);
            if (closeButton) closeButton.onClick.RemoveListener(Close);
        }

        public void Open() { if (featurePanel) featurePanel.SetActive(true); }
        public void Close() { if (featurePanel) featurePanel.SetActive(false); }
    }
}
