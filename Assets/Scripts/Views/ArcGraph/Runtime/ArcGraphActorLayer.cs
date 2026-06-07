using System.Collections.Generic;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphActorLayer
    // =============================================================================
    /// <summary>
    /// <para>
    /// Layer minimo degli attori per il futuro sistema <c>arcgraph</c>.
    /// </para>
    ///
    /// <para><b>Principio architetturale: actor visuale senza controllo NPC</b></para>
    /// <para>
    /// Il layer conserva snapshot actor gia' derivati dal runtime. Non decide
    /// movimento, non completa running action, non cambia posizione e non interroga
    /// bisogni, job o decisioni. Anche quando uno snapshot contiene moto visuale,
    /// quel moto resta solo un'informazione di presentazione per interpolare sprite.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>_actors</b>: snapshot actor indicizzati per actorId.</item>
    ///   <item><b>ReplaceSnapshots</b>: sostituzione conservativa della cache.</item>
    ///   <item><b>TryGetActor</b>: lettura puntuale per id attore.</item>
    ///   <item><b>TryGetActorPose</b>: lettura della posa grafica interpolata.</item>
    ///   <item><b>ClearSnapshots</b>: cleanup della sola cache grafica.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphActorLayer : ArcGraphLayerBase
    {
        private readonly Dictionary<int, ArcGraphActorVisualSnapshot> _actors = new();

        public override ArcGraphLayerId LayerId => ArcGraphLayerId.Actor;
        public int ActorCount => _actors.Count;

        // =============================================================================
        // ReplaceSnapshots
        // =============================================================================
        /// <summary>
        /// <para>
        /// Sostituisce la cache actor locale con gli snapshot ricevuti.
        /// </para>
        ///
        /// <para><b>Dirty conservativo per movimento visuale</b></para>
        /// <para>
        /// Se un actor ha moto attivo, il layer marca dirty sia la cella di origine
        /// sia la cella di destinazione del segmento. Questo prepara l'interpolazione
        /// futura senza spostare l'NPC nel <c>World</c>. Se non c'e' moto, viene
        /// marcata solo la cella discreta dello snapshot.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>snapshots</b>: sequenza di actor visuali da copiare.</item>
        ///   <item><b>renderState</b>: stato grafico da marcare dirty, opzionale.</item>
        /// </list>
        /// </summary>
        public void ReplaceSnapshots(
            IEnumerable<ArcGraphActorVisualSnapshot> snapshots,
            ArcGraphRenderState renderState = null)
        {
            _actors.Clear();

            if (snapshots == null)
                return;

            foreach (var snapshot in snapshots)
            {
                if (snapshot.ActorId <= 0)
                    continue;

                _actors[snapshot.ActorId] = snapshot;
                MarkActorDirty(snapshot, renderState);
            }
        }

        // =============================================================================
        // TryGetActor
        // =============================================================================
        /// <summary>
        /// <para>
        /// Prova a leggere lo snapshot visuale di un attore.
        /// </para>
        ///
        /// <para><b>Lettura senza scorciatoie cognitive</b></para>
        /// <para>
        /// Il metodo non cerca NPC nel <c>World</c> se lo snapshot manca. Il layer
        /// deve restare un consumer di dati grafici, non un modo alternativo per
        /// interrogare lo stato oggettivo della simulazione.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>actorId</b>: id attore richiesto.</item>
        ///   <item><b>snapshot</b>: risultato copiato se presente.</item>
        /// </list>
        /// </summary>
        public bool TryGetActor(int actorId, out ArcGraphActorVisualSnapshot snapshot)
        {
            return _actors.TryGetValue(actorId, out snapshot);
        }

        // =============================================================================
        // TryGetActorPose
        // =============================================================================
        /// <summary>
        /// <para>
        /// Prova a leggere la posa visuale risolta di un attore.
        /// </para>
        ///
        /// <para><b>Separazione tra lettura actor e posa render</b></para>
        /// <para>
        /// Il renderer futuro potra' usare questo metodo per ottenere coordinate
        /// grafiche frazionarie gia' interpolate. Se lo snapshot contiene un moto
        /// multi-tick, la posa si trovera' tra origine e destinazione; se non lo
        /// contiene, coincidera' con la cella discreta. In entrambi i casi il layer
        /// non modifica la simulazione e non forza completion del movimento.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>actorId</b>: id attore richiesto.</item>
        ///   <item><b>pose</b>: posa visuale risolta se l'attore e' presente.</item>
        /// </list>
        /// </summary>
        public bool TryGetActorPose(int actorId, out ArcGraphActorVisualPoseSnapshot pose)
        {
            if (!_actors.TryGetValue(actorId, out var snapshot))
            {
                pose = default;
                return false;
            }

            pose = snapshot.ResolvePose();
            return true;
        }

        // =============================================================================
        // ClearSnapshots
        // =============================================================================
        /// <summary>
        /// <para>
        /// Svuota la cache actor locale.
        /// </para>
        ///
        /// <para><b>Cleanup grafico</b></para>
        /// <para>
        /// La pulizia non rimuove NPC, non annulla job e non altera running action.
        /// Elimina solo le copie visuali detenute dal layer.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>_actors</b>: dizionario svuotato.</item>
        /// </list>
        /// </summary>
        public void ClearSnapshots()
        {
            _actors.Clear();
        }

        public override void Dispose()
        {
            ClearSnapshots();
            base.Dispose();
        }

        private static void MarkActorDirty(ArcGraphActorVisualSnapshot snapshot, ArcGraphRenderState renderState)
        {
            if (renderState == null)
                return;

            renderState.MarkCellAndChunkDirty(snapshot.Cell);

            if (!snapshot.HasMotion)
                return;

            renderState.MarkCellAndChunkDirty(snapshot.Motion.FromCell);
            renderState.MarkCellAndChunkDirty(snapshot.Motion.ToCell);
        }
    }
}
