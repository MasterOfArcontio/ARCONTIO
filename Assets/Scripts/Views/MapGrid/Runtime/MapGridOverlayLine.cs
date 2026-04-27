using UnityEngine;
using UnityEngine.UI;

namespace Arcontio.View.MapGrid
{
    /// <summary>
    /// MapGridOverlayLine:
    /// UI line renderer estremamente leggero, pensato per Canvas ScreenSpaceOverlay.
    ///
    /// NOTE:
    /// - Usiamo un Graphic custom per evitare LineRenderer (che è world-space e spesso crea problemi di sorting).
    /// - La linea è disegnata come un singolo quad (2 triangoli) con spessore costante in pixel.
    /// - Il componente è "view-only": non conosce nulla del core, riceve solo due punti in coordinate local del canvas.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class MapGridOverlayLine : MaskableGraphic
    {
        [SerializeField] private float thickness = 2f;

        private Vector2 _a;
        private Vector2 _b;
        private bool _hasPoints;

        /// <summary>Imposta spessore (pixel) della linea.</summary>
        public void SetThickness(float px)
        {
            thickness = Mathf.Max(0.5f, px);
            SetVerticesDirty();
        }

        /// <summary>
        /// Imposta gli estremi della linea in coordinate local del canvas (anchored space).
        /// </summary>
        public void SetEndpoints(Vector2 a, Vector2 b)
        {
            // Hard guard: se arriva un endpoint invalido, NON disegnare.
            if (!IsFinite(a) || !IsFinite(b))
            {
                _hasPoints = false;
                SetVerticesDirty();
                return;
            }

            _a = a;
            _b = b;
            _hasPoints = true;
            SetVerticesDirty();
        }

        private static bool IsFinite(Vector2 v)
        {
            return IsFinite(v.x) && IsFinite(v.y);
        }

        private static bool IsFinite(float f)
        {
            return !float.IsNaN(f) && !float.IsInfinity(f);
        }

        /// <summary>Nasconde la linea senza distruggerla.</summary>
        public void SetVisible(bool visible)
        {
            // Non usare canvasRenderer qui: può non essere ancora presente e genera MissingComponentException.
            // Per un Graphic UI, è sufficiente disabilitare/abilitare il componente.
            enabled = visible;

            // Forza rebuild mesh quando torna visibile.
            if (visible) SetVerticesDirty();
        }

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;

            // Safety: garantisce che ci sia un CanvasRenderer.
            if (GetComponent<CanvasRenderer>() == null)
                gameObject.AddComponent<CanvasRenderer>();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            if (!_hasPoints)
                return;

            if (!IsFinite(_a) || !IsFinite(_b))
                return;

            // Segmento degenerato -> niente.
            Vector2 dir = _b - _a;
            float len = dir.magnitude;
            if (len < 0.001f)
                return;

            dir /= len;

            // Perpendicolare.
            Vector2 n = new Vector2(-dir.y, dir.x);
            float half = thickness * 0.5f;

            Vector2 v0 = _a + n * half;
            Vector2 v1 = _a - n * half;
            Vector2 v2 = _b - n * half;
            Vector2 v3 = _b + n * half;

            // Colore: usiamo Graphic.color (impostabile da caller).
            UIVertex vert = UIVertex.simpleVert;
            vert.color = color;

            vert.position = v0; vh.AddVert(vert);
            vert.position = v1; vh.AddVert(vert);
            vert.position = v2; vh.AddVert(vert);
            vert.position = v3; vh.AddVert(vert);

            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(2, 3, 0);
        }
    }
}
