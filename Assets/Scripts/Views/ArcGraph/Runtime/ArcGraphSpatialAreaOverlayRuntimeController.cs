using System.Collections.Generic;
using Arcontio.Core;
using UnityEngine;

namespace Arcontio.View.ArcGraph
{
    public sealed class ArcGraphSpatialAreaOverlayRuntimeController : MonoBehaviour
    {
        [SerializeField] private ArcGraphRuntimeContextProvider runtimeContextProvider;
        [SerializeField] private ArcGraphDebugOverlaySceneProbeRenderer overlayConsumer;
        [SerializeField] private bool areaOverlayEnabled;
        [SerializeField] private bool processInUpdate = true;

        private readonly List<WorldSpatialAreaOverlayCell> _areaCells = new(1024);
        private readonly ArcGraphDebugOverlaySnapshot _snapshot = new ArcGraphDebugOverlaySnapshot();
        private readonly ArcGraphDebugOverlayQueue _queue = new ArcGraphDebugOverlayQueue();
        private readonly ArcGraphDebugOverlayQueueBuilder _builder = new ArcGraphDebugOverlayQueueBuilder();

        public bool AreaOverlayEnabled => areaOverlayEnabled;

        public void Configure(
            ArcGraphRuntimeContextProvider provider,
            ArcGraphDebugOverlaySceneProbeRenderer consumer)
        {
            runtimeContextProvider = provider;
            overlayConsumer = consumer;
        }

        public void SetAreaOverlayEnabled(bool enabled)
        {
            areaOverlayEnabled = enabled;
            if (!enabled)
                overlayConsumer?.ClearProbe();
            else
                ProcessFrame();
        }

        private void Update()
        {
            if (processInUpdate && areaOverlayEnabled)
                ProcessFrame();
        }

        public void ProcessFrame()
        {
            if (!areaOverlayEnabled || overlayConsumer == null)
                return;

            ArcGraphRuntimeContext context = runtimeContextProvider != null
                ? runtimeContextProvider.BuildTerrainRuntimeContext()
                : null;
            World world = context?.World;
            if (world == null)
            {
                overlayConsumer.ClearProbe();
                return;
            }

            _areaCells.Clear();
            _snapshot.Clear();
            _queue.Clear();

            world.FillSpatialAreaOverlayData(_areaCells);
            for (int i = 0; i < _areaCells.Count; i++)
            {
                WorldSpatialAreaOverlayCell cell = _areaCells[i];
                _snapshot.AddCell(new ArcGraphDebugCellOverlaySnapshot(
                    new ArcGraphCellCoord(cell.X, cell.Y),
                    ArcGraphDebugOverlayKind.SpatialAreaCell,
                    cell.Intensity01,
                    cell.AreaId,
                    ResolveColorKey(cell.Kind),
                    true));
            }

            _builder.Build(_snapshot, _queue, true, false);
            overlayConsumer.RenderQueue(_queue);
        }

        private static string ResolveColorKey(WorldSpatialAreaKind kind)
        {
            switch (kind)
            {
                case WorldSpatialAreaKind.OpenArea:
                    return "debug/area/open";
                case WorldSpatialAreaKind.ClosedRoom:
                    return "debug/area/room";
                case WorldSpatialAreaKind.Corridor:
                    return "debug/area/corridor";
                default:
                    return "debug/area/cell";
            }
        }
    }
}
