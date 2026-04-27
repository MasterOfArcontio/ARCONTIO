using UnityEngine;
using UnityEngine.InputSystem;

namespace Arcontio.View.MapGrid
{
    public sealed class MapGridPointerInputActionsProvider : MonoBehaviour
    {
        [Header("Input Actions")]
        [SerializeField] private InputActionReference pointAction;

        [SerializeField] private bool autoEnable = true;

        public InputActionReference PointActionRef => pointAction;

        public void SetPointAction(InputActionReference actionRef)
        {
            // IMPORTANT: se stiamo cambiando action a runtime,
            // disabilitiamo quella vecchia (se autoEnable) prima di sostituire.
            if (autoEnable && pointAction?.action != null && pointAction.action.enabled)
                pointAction.action.Disable();

            pointAction = actionRef;

            if (autoEnable && pointAction?.action != null)
                pointAction.action.Enable();
        }

        private void OnEnable()
        {
            if (!autoEnable) return;
            if (pointAction?.action != null) pointAction.action.Enable();
        }

        private void OnDisable()
        {
            if (!autoEnable) return;
            if (pointAction?.action != null) pointAction.action.Disable();
        }

        public bool TryGetPointerScreenPosition(out Vector2 screenPos)
        {
            screenPos = default;
            if (pointAction?.action == null) return false;

            screenPos = pointAction.action.ReadValue<Vector2>();
            return true;
        }
    }
}
