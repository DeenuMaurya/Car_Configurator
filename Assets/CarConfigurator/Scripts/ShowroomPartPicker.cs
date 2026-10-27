using UnityEngine;
using UnityEngine.EventSystems;

namespace CarShowroom
{
    // Physics collider + PartPickTarget required on selectable meshes.
    public class ShowroomPartPicker : MonoBehaviour
    {
        [SerializeField] private Camera sceneCamera;
        [SerializeField] private CarShowcaseServices showcase;
        [SerializeField] private LayerMask selectableLayers = ~0;
        private PartPickTarget selected;

        private void Update()
        {
            if (Input.touchCount == 0 || Input.GetTouch(0).phase != TouchPhase.Began || !sceneCamera) return;
            Touch touch = Input.GetTouch(0);
            if (EventSystem.current && EventSystem.current.IsPointerOverGameObject(touch.fingerId)) return;
            if (!Physics.Raycast(sceneCamera.ScreenPointToRay(touch.position), out RaycastHit hit, 200f, selectableLayers)) return;
            PartPickTarget target = hit.collider.GetComponentInParent<PartPickTarget>();
            if (!target) return;
            if (selected) selected.Highlight(false);
            selected = target;
            selected.Highlight(true);
            if (showcase) showcase.ShowPartInfo(target.partId);
        }
    }
}
