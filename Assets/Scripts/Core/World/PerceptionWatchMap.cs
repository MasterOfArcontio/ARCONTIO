using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arcontio.Core
{
    // =============================================================================
    // PerceptionWatchMap
    // =============================================================================
    /// <summary>
    /// <para>
    /// Registro runtime leggero delle zone osservate da ciascun NPC.
    /// </para>
    ///
    /// <para><b>Principio architetturale: copertura percettiva soggettiva</b></para>
    /// <para>
    /// La struttura non legge oggetti, non sceglie target e non modifica decisioni.
    /// Riceve solo il risultato geometrico dei sistemi di percezione e conserva il
    /// tick in cui una zona e' stata osservata da un NPC. Questo rende possibile,
    /// nelle patch successive, evitare ricerche ripetitive in zone gia' controllate
    /// senza introdurre conoscenza onnisciente.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Zone</b>: la mappa lavora per zona, non per cella singola, per contenere memoria e costo.</item>
    ///   <item><b>Per NPC</b>: ogni NPC mantiene una vista soggettiva separata.</item>
    ///   <item><b>Limiti</b>: massimo zone per NPC e raccolta obsolete controllata da configurazione.</item>
    ///   <item><b>Garbage bounded</b>: la pulizia rimuove al massimo N entry per giro, evitando picchi.</item>
    /// </list>
    /// </summary>
    public sealed class PerceptionWatchMap
    {
        private readonly Dictionary<int, Dictionary<long, int>> _lastSeenTickByNpcAndZone = new(256);
        private readonly List<int> _npcGarbage = new(128);
        private readonly List<long> _zoneGarbage = new(256);
        private readonly int _mapWidth;
        private readonly int _mapHeight;
        private readonly int _zoneSizeCells;
        private readonly int _maxZonesPerNpc;
        private readonly int _staleAfterTicks;
        private readonly int _garbageCollectEveryTicks;
        private readonly int _garbageCollectMaxEntriesPerRun;
        private int _lastGarbageCollectTick = -1;

        public int ZoneSizeCells => _zoneSizeCells;
        public int MaxZonesPerNpc => _maxZonesPerNpc;
        public int StaleAfterTicks => _staleAfterTicks;

        public PerceptionWatchMap(
            int mapWidth,
            int mapHeight,
            int zoneSizeCells,
            int maxZonesPerNpc,
            int staleAfterTicks,
            int garbageCollectEveryTicks,
            int garbageCollectMaxEntriesPerRun)
        {
            _mapWidth = Math.Max(1, mapWidth);
            _mapHeight = Math.Max(1, mapHeight);
            _zoneSizeCells = Math.Max(1, zoneSizeCells);
            _maxZonesPerNpc = Math.Max(1, maxZonesPerNpc);
            _staleAfterTicks = Math.Max(1, staleAfterTicks);
            _garbageCollectEveryTicks = Math.Max(1, garbageCollectEveryTicks);
            _garbageCollectMaxEntriesPerRun = Math.Max(1, garbageCollectMaxEntriesPerRun);
        }

        // =============================================================================
        // RecordObservedZoneAtCell
        // =============================================================================
        /// <summary>
        /// <para>
        /// Registra come osservata la zona che contiene una cella vista.
        /// </para>
        /// </summary>
        public void RecordObservedZoneAtCell(int npcId, int cellX, int cellY, int tick)
        {
            if (npcId <= 0)
                return;

            if (cellX < 0 || cellX >= _mapWidth || cellY < 0 || cellY >= _mapHeight)
                return;

            int zoneX = cellX / _zoneSizeCells;
            int zoneY = cellY / _zoneSizeCells;
            RecordObservedZone(npcId, zoneX, zoneY, tick);
        }

        // =============================================================================
        // RecordObservedZone
        // =============================================================================
        /// <summary>
        /// <para>
        /// Registra una zona gia' risolta come osservata da un NPC.
        /// </para>
        /// </summary>
        public void RecordObservedZone(int npcId, int zoneX, int zoneY, int tick)
        {
            if (npcId <= 0)
                return;

            if (zoneX < 0 || zoneY < 0)
                return;

            if (zoneX * _zoneSizeCells >= _mapWidth || zoneY * _zoneSizeCells >= _mapHeight)
                return;

            if (!_lastSeenTickByNpcAndZone.TryGetValue(npcId, out var zones))
            {
                zones = new Dictionary<long, int>(Math.Min(_maxZonesPerNpc, 64));
                _lastSeenTickByNpcAndZone[npcId] = zones;
            }

            zones[MakeZoneKey(zoneX, zoneY)] = tick;
            TrimOldestZonesIfNeeded(zones);
        }

        public bool TryGetLastSeenTick(int npcId, int cellX, int cellY, out int lastSeenTick)
        {
            lastSeenTick = -1;
            if (cellX < 0 || cellX >= _mapWidth || cellY < 0 || cellY >= _mapHeight)
                return false;

            if (!_lastSeenTickByNpcAndZone.TryGetValue(npcId, out var zones))
                return false;

            long key = MakeZoneKey(cellX / _zoneSizeCells, cellY / _zoneSizeCells);
            return zones.TryGetValue(key, out lastSeenTick);
        }

        public int GetTrackedZoneCount(int npcId)
        {
            return _lastSeenTickByNpcAndZone.TryGetValue(npcId, out var zones)
                ? zones.Count
                : 0;
        }

        public void ClearNpc(int npcId)
        {
            _lastSeenTickByNpcAndZone.Remove(npcId);
        }

        // =============================================================================
        // GarbageCollectIfDue
        // =============================================================================
        /// <summary>
        /// <para>
        /// Rimuove zone obsolete con un limite massimo per giro.
        /// </para>
        /// </summary>
        public void GarbageCollectIfDue(int tick)
        {
            if (_lastGarbageCollectTick >= 0
                && tick - _lastGarbageCollectTick < _garbageCollectEveryTicks)
            {
                return;
            }

            _lastGarbageCollectTick = tick;
            int removed = 0;
            int staleBeforeTick = tick - _staleAfterTicks;
            _npcGarbage.Clear();

            foreach (var npcPair in _lastSeenTickByNpcAndZone)
            {
                _zoneGarbage.Clear();
                foreach (var zonePair in npcPair.Value)
                {
                    if (zonePair.Value <= staleBeforeTick)
                    {
                        _zoneGarbage.Add(zonePair.Key);
                        removed++;
                        if (removed >= _garbageCollectMaxEntriesPerRun)
                            break;
                    }
                }

                for (int i = 0; i < _zoneGarbage.Count; i++)
                    npcPair.Value.Remove(_zoneGarbage[i]);

                if (npcPair.Value.Count == 0)
                    _npcGarbage.Add(npcPair.Key);

                if (removed >= _garbageCollectMaxEntriesPerRun)
                    break;
            }

            for (int i = 0; i < _npcGarbage.Count; i++)
                _lastSeenTickByNpcAndZone.Remove(_npcGarbage[i]);
        }

        private void TrimOldestZonesIfNeeded(Dictionary<long, int> zones)
        {
            while (zones.Count > _maxZonesPerNpc)
            {
                long oldestKey = 0;
                int oldestTick = int.MaxValue;
                bool found = false;

                foreach (var pair in zones)
                {
                    if (!found || pair.Value < oldestTick)
                    {
                        oldestKey = pair.Key;
                        oldestTick = pair.Value;
                        found = true;
                    }
                }

                if (!found)
                    return;

                zones.Remove(oldestKey);
            }
        }

        private static long MakeZoneKey(int zoneX, int zoneY)
        {
            return ((long)zoneX << 32) ^ (uint)zoneY;
        }
    }
}
