using UnityEngine;

namespace CarShowroom
{
    public class PartPickTarget : MonoBehaviour
    {
        public string partId;
        public GameObject highlightOutline;
        public void Highlight(bool enabled) { if (highlightOutline) highlightOutline.SetActive(enabled); }
    }
}
