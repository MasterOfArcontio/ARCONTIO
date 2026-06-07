using System.Collections.Generic;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphObjectLayer
    // =============================================================================
    /// <summary>
    /// <para>
    /// Layer minimo degli oggetti per il futuro sistema <c>arcgraph</c>.
    /// </para>
    ///
    /// <para><b>Principio architetturale: oggetti visuali da snapshot, non da World</b></para>
    /// <para>
    /// Il layer conserva snapshot oggetto gia' prodotti dall'adapter. Non accede a
    /// <c>World.Objects</c>, non risolve definizioni oggetto, non carica sprite e
    /// non gestisce ownership o stock. Il suo ruolo e' esclusivamente presentativo:
    /// tenere una cache leggibile degli oggetti che un renderer futuro potra'
    /// trasformare in sprite, label o mesh.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>_objects</b>: snapshot oggetto indicizzati per objectId.</item>
    ///   <item><b>ReplaceSnapshots</b>: sostituzione conservativa della cache.</item>
    ///   <item><b>TryGetObject</b>: lettura puntuale per id oggetto.</item>
    ///   <item><b>ClearSnapshots</b>: cleanup della sola cache grafica.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphObjectLayer : ArcGraphLayerBase
    {
        private readonly Dictionary<int, ArcGraphObjectVisualSnapshot> _objects = new();

        public override ArcGraphLayerId LayerId => ArcGraphLayerId.Object;
        public int ObjectCount => _objects.Count;

        // =============================================================================
        // ReplaceSnapshots
        // =============================================================================
        /// <summary>
        /// <para>
        /// Sostituisce la cache oggetti locale con gli snapshot ricevuti.
        /// </para>
        ///
        /// <para><b>Dirty conservativo</b></para>
        /// <para>
        /// Ogni oggetto importato sporca la propria cella e il chunk corrispondente.
        /// Il metodo non tenta ancora di distinguere oggetti invariati, spostati o
        /// rimossi: in questa fase serve a stabilire il layer minimo e il flusso dati
        /// corretto, non l'ottimizzazione finale.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>snapshots</b>: sequenza di oggetti visuali da copiare.</item>
        ///   <item><b>renderState</b>: stato grafico da marcare dirty, opzionale.</item>
        /// </list>
        /// </summary>
        public void ReplaceSnapshots(
            IEnumerable<ArcGraphObjectVisualSnapshot> snapshots,
            ArcGraphRenderState renderState = null)
        {
            _objects.Clear();

            if (snapshots == null)
                return;

            foreach (var snapshot in snapshots)
            {
                if (snapshot.ObjectId <= 0)
                    continue;

                _objects[snapshot.ObjectId] = snapshot;
                MarkCellAndChunkDirty(snapshot.Cell, renderState);
            }
        }

        // =============================================================================
        // TryGetObject
        // =============================================================================
        /// <summary>
        /// <para>
        /// Prova a leggere lo snapshot visuale di un oggetto.
        /// </para>
        ///
        /// <para><b>Lettura senza authority</b></para>
        /// <para>
        /// Se l'oggetto non esiste nella cache, il metodo fallisce senza interrogare
        /// il <c>World</c>. Questo impedisce al layer di diventare una scorciatoia
        /// nascosta verso lo stato simulativo.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>objectId</b>: id runtime richiesto.</item>
        ///   <item><b>snapshot</b>: risultato copiato se presente.</item>
        /// </list>
        /// </summary>
        public bool TryGetObject(int objectId, out ArcGraphObjectVisualSnapshot snapshot)
        {
            return _objects.TryGetValue(objectId, out snapshot);
        }

        // =============================================================================
        // ClearSnapshots
        // =============================================================================
        /// <summary>
        /// <para>
        /// Svuota la cache oggetti locale.
        /// </para>
        ///
        /// <para><b>Cleanup grafico</b></para>
        /// <para>
        /// La pulizia non distrugge oggetti nel mondo, non rilascia reservation e non
        /// modifica stock o ownership. Rimuove solo copie grafiche locali.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>_objects</b>: dizionario svuotato.</item>
        /// </list>
        /// </summary>
        public void ClearSnapshots()
        {
            _objects.Clear();
        }

        public override void Dispose()
        {
            ClearSnapshots();
            base.Dispose();
        }

        private static void MarkCellAndChunkDirty(ArcGraphCellCoord cell, ArcGraphRenderState renderState)
        {
            if (renderState == null)
                return;

            renderState.Dirty.MarkCellDirty(cell);
            renderState.Dirty.MarkChunkDirty(renderState.ResolveChunkCoord(cell));
        }
    }
}
