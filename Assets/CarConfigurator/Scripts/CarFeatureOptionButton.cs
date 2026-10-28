using UnityEngine;
using UnityEngine.UI;

namespace CarShowroom
{
    [RequireComponent(typeof(Button))]
    public sealed class CarFeatureOptionButton : MonoBehaviour
    {
        [SerializeField] private CarConfiguratorFeatureActions actions;
        [SerializeField] private string command;
        [SerializeField] private string option;
        [SerializeField] private Image selectedImage;
        [SerializeField] private Sprite onSprite;
        [SerializeField] private Sprite offSprite;
        [SerializeField] private Color selectedColor = new Color(.12f, .68f, .9f, 1f);
        [SerializeField] private Color unselectedColor = new Color(.16f, .19f, .24f, .96f);
        private Button button;

        public void Configure(CarConfiguratorFeatureActions owner, string feature, string value, Image background)
        {
            actions = owner;
            command = feature;
            option = value;
            selectedImage = background;
            Refresh();
        }

        private void Awake() => button = GetComponent<Button>();

        private void OnEnable()
        {
            if (!button) button = GetComponent<Button>();
            button.onClick.AddListener(OnClick);
            if (actions) actions.StateChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            if (button) button.onClick.RemoveListener(OnClick);
            if (actions) actions.StateChanged -= Refresh;
        }

        private void OnClick()
        {
            if (!actions) Debug.LogWarning("[Car UI] Assign CarConfiguratorFeatureActions to " + name, this);
            else if (!actions.ExecuteFeature(command, option))
                Debug.LogWarning("[Car UI] " + command + ": " + option + " requires an assigned controller, mesh, or material.", this);
            Refresh();
        }

        public void Refresh()
        {
            if (!selectedImage) selectedImage = GetComponent<Image>();
            if (!selectedImage) return;
            bool selected = actions && actions.IsSelected(command, option);
            selectedImage.color = selected ? selectedColor : unselectedColor;
            Sprite sprite = selected ? onSprite : offSprite;
            if (sprite) selectedImage.sprite = sprite;
        }
    }
}
