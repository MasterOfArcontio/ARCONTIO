using System.Collections.Generic;

namespace Arcontio.Core
{
    /// <summary>
    /// DebugFovTelemetry:
    ///
    /// Un "ponte" puramente diagnostico tra Core (tick) e View (frame).
    ///
    /// Problema risolto:
    /// - Il tick-rate può essere più alto del frame-rate.
    /// - Se la View legge solo "l'ultimo cono" (snapshot), perde i coni intermedi.
    ///
    /// Soluzione:
    /// - Accumulo per finestra di N tick.
    /// - Per ogni NPC mantengo una heatmap per-cell che conta quante volte la cella
    ///   è stata inclusa nei coni calcolati durante la finestra.
    /// - Ogni N tick: swap buffer (write -> read) e reset del write.
    /// - La view legge SOLO il buffer read (stabile per tutta la finestra successiva).
    ///
    /// Note architetturali:
    /// - Questa classe NON modifica la simulazione.
    /// - È disattivabile da game_params.json.
    /// - Non è ottimizzata per scalare a mappe enormi: è tooling debug.
    /// </summary>
    public sealed class DebugFovTelemetry
    {
        private readonly int _width;
        private readonly int _height;
        private readonly int _size;
        private readonly int _windowTicks;

        private int _ticksIntoWindow;

        // ============================================================
        // Double buffer per heatmap
        // - _writeHeat: viene aggiornato durante i tick
        // - _readHeat : viene letto dalla view (stabile)
        // ============================================================
        private readonly Dictionary<int, int[]> _writeHeatByNpc = new(256);
        private readonly Dictionary<int, int[]> _readHeatByNpc = new(256);

        public int WindowTicks => _windowTicks;

        public DebugFovTelemetry(int width, int height, int windowTicks)
        {
            _width = width <= 0 ? 1 : width;
            _height = height <= 0 ? 1 : height;
            _size = _width * _height;

            _windowTicks = windowTicks <= 0 ? 1 : windowTicks;
            _ticksIntoWindow = 0;
        }

        /// <summary>
        /// Registra una cella come "vista" in questo tick per un NPC.
        /// Incrementa il contatore della cella nel write buffer.
        /// </summary>
        public void RecordCell(int npcId, int x, int y)
        {
            if (npcId <= 0) return;
            if (x < 0 || y < 0 || x >= _width || y >= _height) return;

            if (!_writeHeatByNpc.TryGetValue(npcId, out var heat) || heat == null)
            {
                // Array 1D per ridurre overhead e permettere swap/clear efficienti.
                heat = new int[_size];
                _writeHeatByNpc[npcId] = heat;
            }

            heat[(y * _width) + x]++;
        }

        /// <summary>
        /// Da chiamare ESATTAMENTE una volta per tick, dopo che la percezione
        /// ha avuto modo di registrare celle.
        ///
        /// Ogni _windowTicks tick: swap e reset.
        /// </summary>
        public void AdvanceTickWindow()
        {
            _ticksIntoWindow++;

            if (_ticksIntoWindow >= _windowTicks)
            {
                SwapBuffers();
                _ticksIntoWindow = 0;
            }
        }

        /// <summary>
        /// Ottiene la heatmap READ per un NPC.
        /// La view deve leggere SOLO questa.
        /// </summary>
        public bool TryGetReadHeat(int npcId, out int[] heat)
            => _readHeatByNpc.TryGetValue(npcId, out heat) && heat != null;

        public int Width => _width;
        public int Height => _height;
        public int Size => _size;

        // ============================================================
        // Internals
        // ============================================================
        private void SwapBuffers()
        {
            // Strategia:
            // - Non vogliamo allocare nuovi array ogni finestra.
            // - Quindi per ogni NPC:
            //   - il write diventa read
            //   - il vecchio read (se esiste) diventa write e viene azzerato
            //
            // Questo mantiene stabili le allocazioni una volta "riscaldato".

            // 1) Mettiamo da parte i vecchi read, così possiamo riciclarli.
            var oldRead = new Dictionary<int, int[]>(_readHeatByNpc);
            _readHeatByNpc.Clear();

            // 2) Snapshot delle chiavi del write.
            //    IMPORTANTISSIMO: non possiamo modificare un Dictionary mentre lo enumeriamo.
            var writeKeys = new List<int>(_writeHeatByNpc.Keys);

            // 3) Per ogni npc presente nel write, promuoviamo a read.
            for (int i = 0; i < writeKeys.Count; i++)
            {
                int npcId = writeKeys[i];
                if (!_writeHeatByNpc.TryGetValue(npcId, out var write) || write == null)
                    continue;

                _readHeatByNpc[npcId] = write;

                // Riciclo: se avevo un vecchio read, lo riuso come nuovo write (clear).
                if (oldRead.TryGetValue(npcId, out var recycled) && recycled != null)
                {
                    ClearArray(recycled);
                    _writeHeatByNpc[npcId] = recycled;
                }
                else
                {
                    // Nessun array da riciclare: mantengo un array nuovo per il write.
                    // NB: qui non devo usare 'write' perché ora è il read.
                    _writeHeatByNpc[npcId] = new int[_size];
                }
            }

            // 4) NPC che erano nel vecchio read ma non nel write attuale:
            //    significa che in questa finestra non hanno registrato nulla.
            //    In debug va bene lasciarli sparire dal read.
            //    (Se vuoi stabilità visiva assoluta, puoi invece mantenerli a zero.)

            // 5) Importantissimo: il write era stato sovrascritto con array riciclati,
            //    ma potrebbero esserci ancora chiavi "extra" rimaste nel dict.
            //    Le rimuoviamo.
            var keys = new List<int>(_writeHeatByNpc.Keys);
            for (int i = 0; i < keys.Count; i++)
            {
                int npcId = keys[i];
                if (!_readHeatByNpc.ContainsKey(npcId))
                    _writeHeatByNpc.Remove(npcId);
            }
        }

        private static void ClearArray(int[] arr)
        {
            // Clear manuale: in Unity/IL2CPP Array.Clear è ok, ma qui manteniamo
            // una versione esplicita per ridurre sorprese su piattaforme.
            for (int i = 0; i < arr.Length; i++)
                arr[i] = 0;
        }
    }
}
