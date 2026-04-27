using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Arcontio.View.MapGrid
{
    /// <summary>
    /// MapGridDraggableCard:
    /// Componente UI per rendere una card trascinabile con mouse (Left Button).
    ///
    /// IMPORTANT:
    /// - Questo componente non salva offset da solo: emette un callback con la nuova anchoredPosition.
    /// - Il caller (MapGridEntitySummaryOverlay) decide come mappare quella posizione in offset rispetto all'anchor entità.
    /// - Usiamo le interfacce EventSystems standard (BeginDrag/Drag/EndDrag).
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class MapGridDraggableCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler
    {
        private RectTransform _rt;
        private RectTransform _canvasRt;

        private bool _dragging;
        private Vector2 _pointerOffset;

        private Func<Vector2> _getAnchorLocal;         // anchor attuale in canvas-local
        private Action<Vector2, Vector2> _onDragged;   // (newCardPosLocal, anchorLocal)

        public bool IsDragging => _dragging;

        public void Init(RectTransform canvasRt, Func<Vector2> getAnchorLocal, Action<Vector2, Vector2> onDragged)
        {
            _rt = GetComponent<RectTransform>();
            _canvasRt = canvasRt;
            _getAnchorLocal = getAnchorLocal;
            _onDragged = onDragged;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            // Portiamo la card in primo piano quando cliccata (UX: evita che sia "sotto" altre card).
            transform.SetAsLastSibling();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_rt == null || _canvasRt == null)
                return;

            _dragging = true;

            // Calcola offset tra punto click e pivot della card, in local canvas.
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRt, eventData.position, eventData.pressEventCamera, out var localPointer))
            {
                _pointerOffset = _rt.anchoredPosition - localPointer;
            }
            else
            {
                _pointerOffset = Vector2.zero;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_dragging || _rt == null || _canvasRt == null)
                return;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRt, eventData.position, eventData.pressEventCamera, out var localPointer))
                return;

            Vector2 newPos = localPointer + _pointerOffset;
            _rt.anchoredPosition = newPos;

            var anchor = _getAnchorLocal != null ? _getAnchorLocal() : Vector2.zero;
            _onDragged?.Invoke(newPos, anchor);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _dragging = false;

            // Un ultimo commit per sicurezza.
            var anchor = _getAnchorLocal != null ? _getAnchorLocal() : Vector2.zero;
            _onDragged?.Invoke(_rt != null ? _rt.anchoredPosition : Vector2.zero, anchor);
        }
    }
}
