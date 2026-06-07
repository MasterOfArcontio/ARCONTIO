namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphActorVisualSnapshot
    // =============================================================================
    /// <summary>
    /// <para>
    /// Snapshot read-only minimo per rappresentare un attore nel layer grafico.
    /// </para>
    ///
    /// <para><b>Principio architetturale: actor visuale derivato</b></para>
    /// <para>
    /// La posizione simulativa discreta resta nel <c>World</c>. Questo snapshot e'
    /// una proiezione visuale: dice ad <c>arcgraph</c> quale attore mostrare, in
    /// quale cella base, con quale sprite provvisorio e con quale eventuale moto
    /// visuale. Non conserva riferimenti mutabili a job, NPC o dizionari del mondo.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>ActorId</b>: id dell'entita' visualizzata.</item>
    ///   <item><b>Cell</b>: cella discreta attuale nota alla simulazione.</item>
    ///   <item><b>BaseSpriteKey</b>: chiave Resources o catalogo sprite futuro.</item>
    ///   <item><b>Motion</b>: progresso visuale facoltativo tra due celle.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphActorVisualSnapshot
    {
        public readonly int ActorId;
        public readonly ArcGraphCellCoord Cell;
        public readonly string BaseSpriteKey;
        public readonly ArcGraphActorMotionSnapshot Motion;

        public bool HasMotion => Motion.IsActive;

        // =============================================================================
        // ArcGraphActorVisualSnapshot
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una proiezione visuale completa di un attore.
        /// </para>
        ///
        /// <para><b>Snapshot derivato</b></para>
        /// <para>
        /// Il costruttore copia valori primitivi e value type. Non conserva
        /// riferimenti al <c>World</c>, al Job Layer o a componenti Unity.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>actorId</b>: identita' dell'attore.</item>
        ///   <item><b>cell</b>: cella simulativa discreta.</item>
        ///   <item><b>baseSpriteKey</b>: sprite provvisorio o chiave catalogo futura.</item>
        ///   <item><b>motion</b>: moto visuale opzionale.</item>
        /// </list>
        /// </summary>
        public ArcGraphActorVisualSnapshot(
            int actorId,
            ArcGraphCellCoord cell,
            string baseSpriteKey,
            ArcGraphActorMotionSnapshot motion)
        {
            ActorId = actorId;
            Cell = cell;
            BaseSpriteKey = baseSpriteKey ?? string.Empty;
            Motion = motion;
        }
    }

    // =============================================================================
    // ArcGraphActorMotionSnapshot
    // =============================================================================
    /// <summary>
    /// <para>
    /// Descrive il moto visuale di un attore tra due celle discrete.
    /// </para>
    ///
    /// <para><b>Principio architetturale: interpolazione grafica senza mutazione</b></para>
    /// <para>
    /// Il movimento multi-tick del Job Layer cambia la posizione simulativa solo a
    /// completion. La view, pero', deve poter mostrare un movimento fluido. Questo
    /// snapshot offre ad <c>arcgraph</c> solo i dati per calcolare una progressione
    /// visuale: cella di partenza, cella di arrivo, tick trascorsi e tick richiesti.
    /// Non autorizza il renderer a chiamare <c>SetNpcPos</c> o a completare job.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>FromCell</b>: origine visuale del segmento.</item>
    ///   <item><b>ToCell</b>: destinazione visuale del segmento.</item>
    ///   <item><b>ElapsedTicks</b>: progresso runtime gia' osservato.</item>
    ///   <item><b>RequiredTicks</b>: durata totale richiesta dal segmento.</item>
    ///   <item><b>Progress01</b>: valore normalizzato da 0 a 1.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphActorMotionSnapshot
    {
        public readonly ArcGraphCellCoord FromCell;
        public readonly ArcGraphCellCoord ToCell;
        public readonly int ElapsedTicks;
        public readonly int RequiredTicks;
        public readonly string MotionKind;

        public bool IsActive => RequiredTicks > 0 && !FromCell.Equals(ToCell);

        public float Progress01
        {
            get
            {
                if (RequiredTicks <= 0)
                    return 1f;

                if (ElapsedTicks <= 0)
                    return 0f;

                if (ElapsedTicks >= RequiredTicks)
                    return 1f;

                return ElapsedTicks / (float)RequiredTicks;
            }
        }

        // =============================================================================
        // ArcGraphActorMotionSnapshot
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce un segmento visuale di movimento tra due celle.
        /// </para>
        ///
        /// <para><b>Progress normalizzato</b></para>
        /// <para>
        /// I tick negativi vengono normalizzati a zero. Il calcolo di
        /// <c>Progress01</c> resta derivato, cosi' il chiamante non puo' passare un
        /// progresso incoerente rispetto a durata e tick trascorsi.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>fromCell/toCell</b>: estremi discreti del segmento.</item>
        ///   <item><b>elapsedTicks</b>: progresso osservato.</item>
        ///   <item><b>requiredTicks</b>: durata richiesta.</item>
        ///   <item><b>motionKind</b>: etichetta futura, ad esempio walk/run/slow.</item>
        /// </list>
        /// </summary>
        public ArcGraphActorMotionSnapshot(
            ArcGraphCellCoord fromCell,
            ArcGraphCellCoord toCell,
            int elapsedTicks,
            int requiredTicks,
            string motionKind)
        {
            FromCell = fromCell;
            ToCell = toCell;
            ElapsedTicks = elapsedTicks < 0 ? 0 : elapsedTicks;
            RequiredTicks = requiredTicks < 0 ? 0 : requiredTicks;
            MotionKind = motionKind ?? string.Empty;
        }

        // =============================================================================
        // None
        // =============================================================================
        /// <summary>
        /// <para>
        /// Produce uno snapshot di moto inattivo fermo sulla cella indicata.
        /// </para>
        ///
        /// <para><b>Fallback visuale stabile</b></para>
        /// <para>
        /// Un attore senza running action visiva non deve inventare movimento. Questo
        /// helper crea quindi un segmento nullo che mantiene origine e destinazione
        /// sulla stessa cella.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>cell</b>: cella in cui l'attore resta fermo.</item>
        /// </list>
        /// </summary>
        public static ArcGraphActorMotionSnapshot None(ArcGraphCellCoord cell)
        {
            return new ArcGraphActorMotionSnapshot(cell, cell, 0, 0, string.Empty);
        }
    }
}
