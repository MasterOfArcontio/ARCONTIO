using System.Collections.Generic;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphWaterLayer
    // =============================================================================
    /// <summary>
    /// <para>
    /// Layer placeholder per la futura visualizzazione dell'acqua.
    /// </para>
    ///
    /// <para><b>Principio architetturale: placeholder passivo</b></para>
    /// <para>
    /// Il layer conserva snapshot acqua gia' prodotti da un eventuale adapter
    /// futuro. Non calcola flusso, non aggiorna livelli, non propaga pressione e
    /// non legge sistemi ambientali. Serve solo a preparare il punto in cui il
    /// renderer potra' disegnare liquidi sopra il terreno.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>_cells</b>: snapshot acqua indicizzati per cella.</item>
    ///   <item><b>ReplaceSnapshots</b>: sostituzione della cache visuale.</item>
    ///   <item><b>TryGetCell</b>: lettura puntuale della cache.</item>
    ///   <item><b>ClearSnapshots</b>: cleanup grafico locale.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphWaterLayer : ArcGraphLayerBase
    {
        private readonly Dictionary<ArcGraphCellCoord, ArcGraphWaterVisualSnapshot> _cells = new();

        public override ArcGraphLayerId LayerId => ArcGraphLayerId.Water;
        public int CellCount => _cells.Count;

        public void ReplaceSnapshots(
            IEnumerable<ArcGraphWaterVisualSnapshot> snapshots,
            ArcGraphRenderState renderState = null)
        {
            _cells.Clear();

            if (snapshots == null)
                return;

            foreach (var snapshot in snapshots)
            {
                _cells[snapshot.Cell] = snapshot;
                renderState?.MarkCellAndChunkDirty(snapshot.Cell);
            }
        }

        public bool TryGetCell(ArcGraphCellCoord cell, out ArcGraphWaterVisualSnapshot snapshot)
        {
            return _cells.TryGetValue(cell, out snapshot);
        }

        public void ClearSnapshots()
        {
            _cells.Clear();
        }

        public override void Dispose()
        {
            ClearSnapshots();
            base.Dispose();
        }
    }

    // =============================================================================
    // ArcGraphVegetationLayer
    // =============================================================================
    /// <summary>
    /// <para>
    /// Layer placeholder per vegetazione, piante ed erba diffusa.
    /// </para>
    ///
    /// <para><b>Principio architetturale: visualizzazione separata dalla biosfera</b></para>
    /// <para>
    /// La biosfera futura decidera' nascita, crescita, morte e seed bank. Questo
    /// layer non possiede nessuna di quelle responsabilita'. Conserva soltanto
    /// snapshot visuali per permettere ad <c>arcgraph</c> di avere gia' uno slot
    /// stabile sopra terreno/acqua e sotto oggetti/actor.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>_cells</b>: vegetazione indicizzata per cella.</item>
    ///   <item><b>ReplaceSnapshots</b>: copia dati esterni nella cache.</item>
    ///   <item><b>TryGetCell</b>: lettura senza interrogare sistemi simulativi.</item>
    ///   <item><b>ClearSnapshots</b>: svuota solo la cache grafica.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphVegetationLayer : ArcGraphLayerBase
    {
        private readonly Dictionary<ArcGraphCellCoord, ArcGraphVegetationVisualSnapshot> _cells = new();

        public override ArcGraphLayerId LayerId => ArcGraphLayerId.Vegetation;
        public int CellCount => _cells.Count;

        public void ReplaceSnapshots(
            IEnumerable<ArcGraphVegetationVisualSnapshot> snapshots,
            ArcGraphRenderState renderState = null)
        {
            _cells.Clear();

            if (snapshots == null)
                return;

            foreach (var snapshot in snapshots)
            {
                _cells[snapshot.Cell] = snapshot;
                renderState?.MarkCellAndChunkDirty(snapshot.Cell);
            }
        }

        public bool TryGetCell(ArcGraphCellCoord cell, out ArcGraphVegetationVisualSnapshot snapshot)
        {
            return _cells.TryGetValue(cell, out snapshot);
        }

        public void ClearSnapshots()
        {
            _cells.Clear();
        }

        public override void Dispose()
        {
            ClearSnapshots();
            base.Dispose();
        }
    }

    // =============================================================================
    // ArcGraphLightLayer
    // =============================================================================
    /// <summary>
    /// <para>
    /// Layer placeholder per luce globale, oscuramento e sorgenti locali.
    /// </para>
    ///
    /// <para><b>Principio architetturale: light buffer visuale</b></para>
    /// <para>
    /// Il layer non propaga luce e non influenza percezione o pathfinding. Funziona
    /// come cache di output: un futuro LightSystem potra' calcolare intensita' per
    /// cella, mentre questo layer conservera' solo il valore gia' risolto.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>_cells</b>: snapshot luce per cella.</item>
    ///   <item><b>ReplaceSnapshots</b>: sostituzione del buffer visuale.</item>
    ///   <item><b>TryGetCell</b>: lettura del dato luce gia' derivato.</item>
    ///   <item><b>ClearSnapshots</b>: reset locale.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphLightLayer : ArcGraphLayerBase
    {
        private readonly Dictionary<ArcGraphCellCoord, ArcGraphLightVisualSnapshot> _cells = new();

        public override ArcGraphLayerId LayerId => ArcGraphLayerId.Light;
        public int CellCount => _cells.Count;

        public void ReplaceSnapshots(
            IEnumerable<ArcGraphLightVisualSnapshot> snapshots,
            ArcGraphRenderState renderState = null)
        {
            _cells.Clear();

            if (snapshots == null)
                return;

            foreach (var snapshot in snapshots)
            {
                _cells[snapshot.Cell] = snapshot;
                renderState?.MarkCellAndChunkDirty(snapshot.Cell);
            }
        }

        public bool TryGetCell(ArcGraphCellCoord cell, out ArcGraphLightVisualSnapshot snapshot)
        {
            return _cells.TryGetValue(cell, out snapshot);
        }

        public void ClearSnapshots()
        {
            _cells.Clear();
        }

        public override void Dispose()
        {
            ClearSnapshots();
            base.Dispose();
        }
    }

    // =============================================================================
    // ArcGraphWeatherLayer
    // =============================================================================
    /// <summary>
    /// <para>
    /// Layer placeholder per overlay meteo globale o per livello <c>Z</c>.
    /// </para>
    ///
    /// <para><b>Principio architetturale: meteo come overlay grafico</b></para>
    /// <para>
    /// Il meteo non viene generato qui. Il layer conserva soltanto lo snapshot
    /// visuale corrente, ad esempio pioggia o neve con una certa intensita'. Non
    /// aggiorna temperatura, umidita', crescita piante o comportamento NPC.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>CurrentWeather</b>: snapshot meteo corrente.</item>
    ///   <item><b>HasWeatherSnapshot</b>: indica se e' stato ricevuto un dato.</item>
    ///   <item><b>ReplaceSnapshot</b>: aggiorna il dato visuale globale.</item>
    ///   <item><b>ClearSnapshot</b>: torna a meteo visuale assente.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphWeatherLayer : ArcGraphLayerBase
    {
        public override ArcGraphLayerId LayerId => ArcGraphLayerId.Weather;

        public ArcGraphWeatherVisualSnapshot CurrentWeather { get; private set; } =
            ArcGraphWeatherVisualSnapshot.None();

        public bool HasWeatherSnapshot { get; private set; }

        public void ReplaceSnapshot(ArcGraphWeatherVisualSnapshot snapshot)
        {
            CurrentWeather = snapshot;
            HasWeatherSnapshot = true;
        }

        public void ClearSnapshot()
        {
            CurrentWeather = ArcGraphWeatherVisualSnapshot.None();
            HasWeatherSnapshot = false;
        }

        public override void Dispose()
        {
            ClearSnapshot();
            base.Dispose();
        }
    }

    // =============================================================================
    // ArcGraphEffectLayer
    // =============================================================================
    /// <summary>
    /// <para>
    /// Layer placeholder per effetti locali, particelle e segnali ambientali.
    /// </para>
    ///
    /// <para><b>Principio architetturale: effetti senza autorita' causale</b></para>
    /// <para>
    /// Fuoco, fumo e segnali potranno avere in futuro sistemi simulativi propri.
    /// Questo layer non decide nulla di tutto cio'. Conserva solo snapshot visuali
    /// indicizzati per id effetto e marca dirty la cella interessata.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>_effects</b>: effetti visuali indicizzati per id.</item>
    ///   <item><b>ReplaceSnapshots</b>: sostituisce la cache effetti.</item>
    ///   <item><b>TryGetEffect</b>: lettura puntuale per id effetto.</item>
    ///   <item><b>ClearSnapshots</b>: cleanup del layer.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphEffectLayer : ArcGraphLayerBase
    {
        private readonly Dictionary<int, ArcGraphEffectVisualSnapshot> _effects = new();

        public override ArcGraphLayerId LayerId => ArcGraphLayerId.Effect;
        public int EffectCount => _effects.Count;

        public void ReplaceSnapshots(
            IEnumerable<ArcGraphEffectVisualSnapshot> snapshots,
            ArcGraphRenderState renderState = null)
        {
            _effects.Clear();

            if (snapshots == null)
                return;

            foreach (var snapshot in snapshots)
            {
                if (snapshot.EffectId <= 0)
                    continue;

                _effects[snapshot.EffectId] = snapshot;
                renderState?.MarkCellAndChunkDirty(snapshot.Cell);
            }
        }

        public bool TryGetEffect(int effectId, out ArcGraphEffectVisualSnapshot snapshot)
        {
            return _effects.TryGetValue(effectId, out snapshot);
        }

        public void ClearSnapshots()
        {
            _effects.Clear();
        }

        public override void Dispose()
        {
            ClearSnapshots();
            base.Dispose();
        }
    }
}
