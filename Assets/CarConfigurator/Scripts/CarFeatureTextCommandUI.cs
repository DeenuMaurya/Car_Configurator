using UnityEngine;
using UnityEngine.UI;

namespace CarShowroom
{
    public sealed class CarFeatureTextCommandUI : MonoBehaviour
    {
        [SerializeField] private InputField commandInput;
        [SerializeField] private Button submitButton;
        [SerializeField] private SimpleTextCommandAdapter adapter;

        public void Configure(InputField input, Button button, SimpleTextCommandAdapter controller)
        {
            commandInput = input;
            submitButton = button;
            adapter = controller;
        }

        private void OnEnable() { if (submitButton) submitButton.onClick.AddListener(Submit); }
        private void OnDisable() { if (submitButton) submitButton.onClick.RemoveListener(Submit); }

        public void Submit()
        {
            if (!commandInput || !adapter || string.IsNullOrWhiteSpace(commandInput.text)) return;
            adapter.Submit(commandInput.text);
        }
    }
}
