using System.Collections.Generic;
using UnityEngine;
using Arcontio.Core;

namespace Arcontio.View.MapGrid
{
    /// <summary>
    /// MapGridNpcBalloonView:
    /// Componente view-only che mostra un "balloon" (fumetto) sopra un NPC quando il core
    /// emette un NpcBalloonSignal.
    ///
    /// Importante:
    /// - Questo script NON decide quando mostrare il balloon.
    /// - Legge world.TryGetNpcBalloonSignal(npcId) e reagisce solo se il tick è nuovo.
    ///
    /// Asset policy:
    /// - Gli sprite sono caricati da Resources (prefabless).
    /// - I path vengono passati via Init(...) da MapGridWorldView.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MapGridNpcBalloonView : MonoBehaviour
    {
        [Header("Runtime bind")]
        public int NpcId;

        private readonly Dictionary<NpcBalloonKind, Sprite> _sprites = new();

        private GameObject _balloonGo;
        private SpriteRenderer _balloonSr;

        private float _yOffsetWorld = 0.55f;
        private float _visibleSeconds = 1.25f;
        private int _lastConsumedTick = int.MinValue;
        private float _hideAtTime;

        /// <summary>
        /// Init: chiamato da MapGridWorldView appena crea il GameObject dell'NPC.
        /// </summary>
        public void Init(
            int npcId,
            float yOffsetWorld,
            float visibleSeconds,
            Dictionary<NpcBalloonKind, string> spriteResourcePaths
        )
        {
            NpcId = npcId;
            _yOffsetWorld = yOffsetWorld;
            _visibleSeconds = visibleSeconds;

            EnsureBalloonRenderer();
            _sprites.Clear();

            if (spriteResourcePaths != null)
            {
                foreach (var kv in spriteResourcePaths)
                {
                    var sprite = string.IsNullOrWhiteSpace(kv.Value)
                        ? null
                        : Resources.Load<Sprite>(kv.Value);

                    _sprites[kv.Key] = sprite;
                }
            }

            HideImmediate();
        }

        private void Update()
        {
            // Fail-safe: se manca NPC id, non fare nulla.
            if (NpcId <= 0) return;

            // Auto-hide a tempo.
            if (_balloonGo != null && _balloonGo.activeSelf && Time.time >= _hideAtTime)
                HideImmediate();

            // World binding (view-only): prendo il world corrente.
            var world = MapGridWorldProvider.TryGetWorld();
            if (world == null) return;

            if (!world.TryGetNpcBalloonSignal(NpcId, out var sig))
                return;

            // Consumo solo se il tick è nuovo.
            if (sig.Tick <= _lastConsumedTick)
                return;

            _lastConsumedTick = sig.Tick;

            // Kind None => non mostrare.
            if (sig.Kind == NpcBalloonKind.None)
                return;

            Show(sig.Kind);
        }

        private void EnsureBalloonRenderer()
        {
            if (_balloonGo != null) return;

            _balloonGo = new GameObject("NpcBalloon");
            _balloonGo.transform.SetParent(transform, false);

            // Offset in world-space, relativo all'NPC.
            _balloonGo.transform.localPosition = new Vector3(0f, _yOffsetWorld, 0f);

            _balloonSr = _balloonGo.AddComponent<SpriteRenderer>();
            _balloonSr.sortingOrder = 5000; // sopra NPC e overlay principali; è debug/UX.
        }

        private void Show(NpcBalloonKind kind)
        {
            EnsureBalloonRenderer();

            // In caso di sprite mancante, evitiamo errori: nascondiamo e basta.
            if (!_sprites.TryGetValue(kind, out var sprite) || sprite == null)
            {
                HideImmediate();
                return;
            }

            _balloonSr.sprite = sprite;
            _balloonGo.SetActive(true);
            _hideAtTime = Time.time + _visibleSeconds;
        }

        private void HideImmediate()
        {
            if (_balloonGo != null)
                _balloonGo.SetActive(false);
        }
    }
}
