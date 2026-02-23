using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Arcontio.Core;

namespace Arcontio.View.MapGrid
{
    /// <summary>
    /// MapGridNpcHoverTooltipSystem:
    /// Sistema view-only che costruisce un tooltip “hover” basato su:
    /// - cella sotto il puntatore
    /// - NPC eventualmente presente in quella cella
    /// - oggetto eventualmente presente in quella cella
    ///
    /// Nota:
    /// - NON è un System del core (non è nello Scheduler).
    /// - Vive nella View perché è UI/UX.
    ///
    /// Patch (SummaryOverlay, F1):
    /// - Espongo Hide() per consentire a MapGridWorldView di forzare la sparizione
    ///   quando la modalità SummaryOverlay è attiva.
    /// </summary>
    public sealed class MapGridNpcHoverTooltipSystem
    {
        private readonly MapGridNpcTooltipOverlay _overlay;

        private readonly List<MemoryTrace> _topTracesBuffer = new(8);

        private int _lastCellX = int.MinValue;
        private int _lastCellY = int.MinValue;
        private float _refreshCooldown;

        public MapGridNpcHoverTooltipSystem()
        {
            _overlay = new MapGridNpcTooltipOverlay();
        }

        /// <summary>
        /// Forza la sparizione del tooltip.
        /// Usato quando la view attiva un overlay alternativo (es. SummaryOverlay mode).
        /// </summary>
        public void Hide()
        {
            _overlay.Hide();
            _lastCellX = int.MinValue;
            _lastCellY = int.MinValue;
            _refreshCooldown = 0f;
        }

        /// <summary>
        /// NEW SIGNATURE:
        /// - tileSizeWorld serve per convertire world->grid.
        /// </summary>
        public void Tick(World world, Camera cam, Vector2 pointerScreenPos, float tileSizeWorld)
        {
            if (world == null || cam == null || tileSizeWorld <= 0f)
            {
                _overlay.Hide();
                return;
            }

            // Pointer -> world -> grid cell
            Vector3 wp = cam.ScreenToWorldPoint(new Vector3(pointerScreenPos.x, pointerScreenPos.y, 0f));

            int cellX = Mathf.FloorToInt(wp.x / tileSizeWorld);
            int cellY = Mathf.FloorToInt(wp.y / tileSizeWorld);

            // Refresh throttled per cell
            if (cellX != _lastCellX || cellY != _lastCellY)
            {
                _lastCellX = cellX;
                _lastCellY = cellY;
                _refreshCooldown = 0f;
            }
            else
            {
                _refreshCooldown -= Time.unscaledDeltaTime;
            }

            if (_refreshCooldown <= 0f)
            {
                _refreshCooldown = 0.10f;

                int npcId = FindNpcAtCell(world, cellX, cellY);
                int objId = FindObjectAtCell(world, cellX, cellY);

                string txt = BuildTooltip(world, cellX, cellY, npcId, objId);
                _overlay.Show(txt, pointerScreenPos);
            }
            else
            {
                _overlay.MoveTo(pointerScreenPos);
            }
        }

        private static int FindNpcAtCell(World world, int x, int y)
        {
            // World.GridPos è id->pos; per ora scan semplice.
            foreach (var kv in world.GridPos)
            {
                var p = kv.Value;
                if (p.X == x && p.Y == y)
                    return kv.Key;
            }
            return -1;
        }

        private static int FindObjectAtCell(World world, int x, int y)
        {
            // World.Objects: id->instance
            foreach (var kv in world.Objects)
            {
                var o = kv.Value;
                if (o != null && o.CellX == x && o.CellY == y)
                    return kv.Key;
            }
            return -1;
        }

        private string BuildTooltip(World world, int cellX, int cellY, int npcId, int objId)
        {
            var sb = new StringBuilder(1400);

            // ============================================================
            // ALWAYS TOP: COORDINATE GRIGLIA
            // ============================================================
            sb.Append("<b>Cell:</b> (")
              .Append(cellX).Append(", ").Append(cellY).Append(")\n");

            // ============================================================
            // OBJECT (if any)
            // ============================================================
            if (objId >= 0 && world.Objects.TryGetValue(objId, out var inst) && inst != null)
            {
                sb.Append("\n<b>Object</b>\n");

                // Nome + def
                string displayName = inst.DefId;
                if (world.TryGetObjectDef(inst.DefId, out var def) && def != null && !string.IsNullOrWhiteSpace(def.DisplayName))
                    displayName = def.DisplayName;

                sb.Append("Name: <b>").Append(displayName).Append("</b>")
                  .Append("  <color=#aaaaaa>#").Append(objId).Append("</color>\n");

                sb.Append("DefId: ").Append(inst.DefId).Append("\n");

                // Ownership + occupancy (da WorldObjectInstance)
                sb.Append("Owner: ").Append(inst.OwnerKind).Append(":").Append(inst.OwnerId).Append("\n");
                sb.Append("OccupantNpcId: ").Append(inst.OccupantNpcId).Append("\n");

                // Use state (se presente)
                if (world.ObjectUse.TryGetValue(objId, out var use))
                {
                    sb.Append("Use: ").Append(use.IsInUse ? "<color=#ffcc66>IN USE</color>" : "free")
                      .Append("  byNpc=").Append(use.UsingNpcId)
                      .Append("\n");
                }

                // Food stock (se presente)
                if (world.FoodStocks.TryGetValue(objId, out var stock))
                {
                    sb.Append("FoodStock: units=").Append(stock.Units)
                      .Append(" owner=").Append(stock.OwnerKind).Append(":").Append(stock.OwnerId)
                      .Append(stock.IsEmpty ? " <color=#ff6666>(empty)</color>" : "")
                      .Append("\n");
                }

                // Proprietà data-driven
                if (world.TryGetObjectDef(inst.DefId, out var d2) && d2 != null && d2.Properties != null && d2.Properties.Count > 0)
                {
                    sb.Append("Properties:\n");
                    int limit = Mathf.Min(8, d2.Properties.Count);
                    for (int i = 0; i < limit; i++)
                    {
                        var kv = d2.Properties[i];
                        sb.Append("• ").Append(kv.Key).Append(" = ").Append(kv.Value.ToString("0.###")).Append("\n");
                    }

                    if (d2.Properties.Count > limit)
                        sb.Append("<color=#aaaaaa>… +").Append(d2.Properties.Count - limit).Append(" more</color>\n");
                }
            }

            // ============================================================
            // NPC (if any)
            // ============================================================
            if (npcId >= 0)
            {
                sb.Append("\n<b>NPC</b>\n");

                string name = world.NpcCore.TryGetValue(npcId, out var core) ? core.Name : $"NPC_{npcId}";
                sb.Append("Name: <b>").Append(name).Append("</b>")
                  .Append("  <color=#aaaaaa>#").Append(npcId).Append("</color>\n");

                if (world.Needs.TryGetValue(npcId, out var needs))
                {
                    sb.Append("Hunger: ").Append(needs.Hunger01.ToString("0.00"))
                      .Append(needs.IsHungry ? " <color=#ffcc66>(hungry)</color>" : "")
                      .Append("   |   Fatigue: ").Append(needs.Fatigue01.ToString("0.00"))
                      .Append(needs.IsTired ? " <color=#ffcc66>(tired)</color>" : "")
                      .Append("\n");
                }

                // Intent: placeholder (qui poi agganci lo stato decisionale reale)
                if (world.NpcMoveIntents.TryGetValue(npcId, out var mi) && mi.Active)
                {
                    sb.Append("MoveIntent: <b>active</b>")
                      .Append(" reason=").Append(mi.Reason)
                      .Append(" target=(").Append(mi.TargetX).Append(",").Append(mi.TargetY).Append(")\n");
                }
                else
                {
                    sb.Append("MoveIntent: idle\n");
                }

                // Memory traces (top 5)
                sb.Append("\n<b>MemoryTrace (top 5)</b>\n");
                if (world.Memory.TryGetValue(npcId, out var store) && store != null)
                {
                    store.GetTopTraces(5, _topTracesBuffer);

                    if (_topTracesBuffer.Count == 0)
                    {
                        sb.Append("<color=#aaaaaa>(none)</color>\n");
                    }
                    else
                    {
                        for (int i = 0; i < _topTracesBuffer.Count; i++)
                        {
                            var t = _topTracesBuffer[i];
                            sb.Append("• ").Append(t.Type)
                              .Append("  I=").Append(t.Intensity01.ToString("0.00"))
                              .Append("  R=").Append(t.Reliability01.ToString("0.00"))
                              .Append("\n");
                        }
                    }
                }
                else
                {
                    sb.Append("<color=#aaaaaa>(no store)</color>\n");
                }
            }

            // Caso "solo coordinate": non aggiungo altro.
            return sb.ToString();
        }
    }
}
