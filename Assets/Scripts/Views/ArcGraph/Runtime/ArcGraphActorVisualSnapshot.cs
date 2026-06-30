using Arcontio.Core;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphActorRunningActionOverlaySnapshot
    // =============================================================================
    /// <summary>
    /// <para>
    /// Snapshot visuale read-only dell'attivita' temporizzata mostrabile sopra un
    /// NPC ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: progresso job copiato, non comandabile</b></para>
    /// <para>
    /// Il dato nasce dal <c>RunningActionStore</c>, ma qui diventa una proiezione
    /// visuale fatta di soli valori primitivi: label, kind e percentuali. Il
    /// renderer puo' disegnare una barra senza conoscere job, command buffer,
    /// sistemi o store mutabili.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>IsActive</b>: indica se l'overlay va mostrato.</item>
    ///   <item><b>ActionKind/JobActionId</b>: identita' minima della running action.</item>
    ///   <item><b>Elapsed/Required</b>: contatori tick copiati e normalizzati.</item>
    ///   <item><b>Progress/Remaining</b>: valori 0-1 gia' pronti per la barra.</item>
    ///   <item><b>Label</b>: testo compatto gia' normalizzato per la UI ArcGraph.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphActorRunningActionOverlaySnapshot
    {
        public readonly bool IsActive;
        public readonly RunningActionKind ActionKind;
        public readonly string JobActionId;
        public readonly int ElapsedTicks;
        public readonly int RequiredTicks;
        public readonly float Progress01;
        public readonly float Remaining01;
        public readonly string Label;

        public ArcGraphActorRunningActionOverlaySnapshot(
            bool isActive,
            RunningActionKind actionKind,
            string jobActionId,
            int elapsedTicks,
            int requiredTicks,
            string label)
        {
            IsActive = isActive;
            ActionKind = actionKind;
            JobActionId = jobActionId ?? string.Empty;
            ElapsedTicks = elapsedTicks < 0 ? 0 : elapsedTicks;
            RequiredTicks = requiredTicks < 0 ? 0 : requiredTicks;
            Progress01 = ResolveProgress01(ElapsedTicks, RequiredTicks);
            Remaining01 = Clamp01(1f - Progress01);
            Label = string.IsNullOrWhiteSpace(label)
                ? ResolveFallbackLabel(actionKind)
                : label.Trim();
        }

        public static ArcGraphActorRunningActionOverlaySnapshot None()
        {
            return new ArcGraphActorRunningActionOverlaySnapshot(
                false,
                RunningActionKind.None,
                string.Empty,
                0,
                0,
                string.Empty);
        }

        public static ArcGraphActorRunningActionOverlaySnapshot FromRunningAction(
            RunningActionProgressSnapshot snapshot)
        {
            if (snapshot.IsTerminal)
                return None();

            return new ArcGraphActorRunningActionOverlaySnapshot(
                true,
                snapshot.Kind,
                snapshot.JobActionId,
                snapshot.ElapsedTicks,
                snapshot.RequiredTicks,
                ResolveLabel(snapshot.Kind, snapshot.JobActionId));
        }

        private static string ResolveLabel(
            RunningActionKind kind,
            string jobActionId)
        {
            string action = string.IsNullOrWhiteSpace(jobActionId)
                ? string.Empty
                : jobActionId.Trim().ToLowerInvariant();

            if (action.Contains("pickup"))
                return "Raccoglie";

            if (action.Contains("consume") || action.Contains("eat"))
                return "Mangia";

            if (action.Contains("ready"))
                return "Prepara";

            if (action.Contains("look"))
                return "Osserva";

            if (action.Contains("wait"))
                return "Attende";

            return ResolveFallbackLabel(kind);
        }

        private static string ResolveFallbackLabel(RunningActionKind kind)
        {
            switch (kind)
            {
                case RunningActionKind.Movement:
                    return "Si muove";
                case RunningActionKind.UseObject:
                    return "Usa";
                case RunningActionKind.Wait:
                    return "Attende";
                default:
                    return "Azione";
            }
        }

        private static float ResolveProgress01(int elapsedTicks, int requiredTicks)
        {
            if (requiredTicks <= 0)
                return 1f;

            return Clamp01((float)elapsedTicks / requiredTicks);
        }

        private static float Clamp01(float value)
        {
            if (value <= 0f)
                return 0f;

            if (value >= 1f)
                return 1f;

            return value;
        }
    }

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
    ///   <item><b>FacingDirectionKey</b>: facing canonico copiato dal World per idle/look direction.</item>
    ///   <item><b>Hunger01</b>: valore fame copiato dal componente Needs.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphActorVisualSnapshot
    {
        public readonly int ActorId;
        public readonly ArcGraphCellCoord Cell;
        public readonly string BaseSpriteKey;
        public readonly ArcGraphActorMotionSnapshot Motion;
        public readonly string FacingDirectionKey;
        public readonly bool HasHungerValue;
        public readonly float Hunger01;
        public readonly ArcGraphActorRunningActionOverlaySnapshot RunningActionOverlay;

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
            ArcGraphActorMotionSnapshot motion,
            bool hasHungerValue = false,
            float hunger01 = 0f,
            string facingDirectionKey = "",
            ArcGraphActorRunningActionOverlaySnapshot runningActionOverlay = default)
        {
            ActorId = actorId;
            Cell = cell;
            BaseSpriteKey = baseSpriteKey ?? string.Empty;
            Motion = motion;
            FacingDirectionKey = NormalizeDirectionKey(facingDirectionKey);
            HasHungerValue = hasHungerValue;
            Hunger01 = Clamp01(hunger01);
            RunningActionOverlay = runningActionOverlay;
        }

        // =============================================================================
        // ResolvePose
        // =============================================================================
        /// <summary>
        /// <para>
        /// Calcola la posa visuale effettiva dello snapshot actor.
        /// </para>
        ///
        /// <para><b>Principio architetturale: posa grafica derivata</b></para>
        /// <para>
        /// Il renderer futuro non dovrebbe duplicare la formula di interpolazione
        /// tra celle. Questo metodo restituisce una posa gia' pronta: se il moto e'
        /// attivo usa il progresso normalizzato del segmento, altrimenti mantiene
        /// l'attore esattamente sulla cella discreta nota al <c>World</c>. Il metodo
        /// non modifica runtime, job, actor o dirty state.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Cell</b>: fallback quando non esiste moto attivo.</item>
        ///   <item><b>Motion</b>: sorgente di origine, destinazione e progresso.</item>
        ///   <item><b>ArcGraphActorVisualPoseSnapshot</b>: output grafico frazionario.</item>
        /// </list>
        /// </summary>
        public ArcGraphActorVisualPoseSnapshot ResolvePose()
        {
            return ArcGraphActorVisualPoseSnapshot.FromActorSnapshot(this);
        }

        private static float Clamp01(float value)
        {
            if (value <= 0f)
                return 0f;

            if (value >= 1f)
                return 1f;

            return value;
        }

        private static string NormalizeDirectionKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            string normalized = value.Trim().ToLowerInvariant();
            switch (normalized)
            {
                case "north":
                case "south":
                case "east":
                case "west":
                    return normalized;
                default:
                    return string.Empty;
            }
        }
    }

    // =============================================================================
    // ArcGraphActorVisualPoseSnapshot
    // =============================================================================
    /// <summary>
    /// <para>
    /// Posa visuale frazionaria di un attore nel sistema <c>arcgraph</c>.
    /// </para>
    ///
    /// <para><b>Principio architetturale: separazione tra posizione simulativa e posizione disegnata</b></para>
    /// <para>
    /// La simulazione conserva posizioni discrete a cella. La grafica, invece, deve
    /// poter disegnare un attore a meta' tra due celle quando un movimento multi-tick
    /// e' ancora in corso. Questa struttura rappresenta solo quella posizione
    /// disegnata: non e' una posizione valida per pathfinding, collisioni, job o
    /// decision layer.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>ActorId</b>: id dell'attore visualizzato.</item>
    ///   <item><b>DiscreteCell</b>: cella simulativa ancora vera per il runtime.</item>
    ///   <item><b>VisualX/VisualY/VisualZ</b>: coordinate grafiche frazionarie.</item>
    ///   <item><b>BaseSpriteKey</b>: sprite provvisorio da usare.</item>
    ///   <item><b>HasMotion/Progress01</b>: stato del segmento visuale.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphActorVisualPoseSnapshot
    {
        public readonly int ActorId;
        public readonly ArcGraphCellCoord DiscreteCell;
        public readonly float VisualX;
        public readonly float VisualY;
        public readonly float VisualZ;
        public readonly string BaseSpriteKey;
        public readonly bool HasMotion;
        public readonly float Progress01;

        // =============================================================================
        // ArcGraphActorVisualPoseSnapshot
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce una posa visuale actor gia' risolta.
        /// </para>
        ///
        /// <para><b>Snapshot value-only</b></para>
        /// <para>
        /// Tutti i valori sono primitivi o value type. Questo evita che la posa
        /// trattenga riferimenti a oggetti Unity, job runtime o stato mondo mutabile.
        /// Il progresso viene normalizzato per sicurezza tra <c>0</c> e <c>1</c>.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>actorId</b>: identita' dell'attore.</item>
        ///   <item><b>discreteCell</b>: posizione simulativa discreta.</item>
        ///   <item><b>visualX/Y/Z</b>: posizione di disegno.</item>
        ///   <item><b>baseSpriteKey</b>: chiave sprite.</item>
        ///   <item><b>hasMotion/progress01</b>: stato visuale del movimento.</item>
        /// </list>
        /// </summary>
        public ArcGraphActorVisualPoseSnapshot(
            int actorId,
            ArcGraphCellCoord discreteCell,
            float visualX,
            float visualY,
            float visualZ,
            string baseSpriteKey,
            bool hasMotion,
            float progress01)
        {
            ActorId = actorId;
            DiscreteCell = discreteCell;
            VisualX = visualX;
            VisualY = visualY;
            VisualZ = visualZ;
            BaseSpriteKey = baseSpriteKey ?? string.Empty;
            HasMotion = hasMotion;
            Progress01 = Clamp01(progress01);
        }

        // =============================================================================
        // FromActorSnapshot
        // =============================================================================
        /// <summary>
        /// <para>
        /// Deriva una posa visuale da uno snapshot actor.
        /// </para>
        ///
        /// <para><b>Interpolazione lineare preparatoria</b></para>
        /// <para>
        /// Se lo snapshot contiene un movimento attivo, la posa viene calcolata con
        /// interpolazione lineare tra <c>FromCell</c> e <c>ToCell</c>. In assenza di
        /// moto, la posa coincide con la cella discreta. La formula e' volutamente
        /// semplice: easing, animazioni sprite e curve diverse restano demandate a
        /// moduli futuri, ma avranno gia' un contratto stabile su cui innestarsi.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>snapshot</b>: sorgente actor read-only.</item>
        ///   <item><b>progress</b>: normalizzato dal motion snapshot.</item>
        ///   <item><b>visual coordinates</b>: coordinate frazionarie calcolate.</item>
        /// </list>
        /// </summary>
        public static ArcGraphActorVisualPoseSnapshot FromActorSnapshot(ArcGraphActorVisualSnapshot snapshot)
        {
            if (!snapshot.HasMotion)
            {
                return new ArcGraphActorVisualPoseSnapshot(
                    snapshot.ActorId,
                    snapshot.Cell,
                    snapshot.Cell.X,
                    snapshot.Cell.Y,
                    snapshot.Cell.Z,
                    snapshot.BaseSpriteKey,
                    hasMotion: false,
                    progress01: 1f);
            }

            float progress = snapshot.Motion.Progress01;
            var from = snapshot.Motion.FromCell;
            var to = snapshot.Motion.ToCell;

            return new ArcGraphActorVisualPoseSnapshot(
                snapshot.ActorId,
                snapshot.Cell,
                Lerp(from.X, to.X, progress),
                Lerp(from.Y, to.Y, progress),
                Lerp(from.Z, to.Z, progress),
                snapshot.BaseSpriteKey,
                hasMotion: true,
                progress);
        }

        private static float Lerp(float from, float to, float progress01)
        {
            return from + ((to - from) * Clamp01(progress01));
        }

        private static float Clamp01(float value)
        {
            if (value <= 0f)
                return 0f;

            if (value >= 1f)
                return 1f;

            return value;
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
        // CreateMovement
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea un segmento visuale di movimento normalizzato.
        /// </para>
        ///
        /// <para><b>Contratto futuro per running action multi-tick</b></para>
        /// <para>
        /// Quando il Job Layer esporra' in modo read-only origine e destinazione del
        /// movimento in corso, l'adapter potra' usare questo helper per costruire il
        /// motion snapshot senza duplicare normalizzazioni. Per ora il metodo resta
        /// solo preparatorio e non legge direttamente alcun runtime.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>fromCell</b>: origine del segmento visuale.</item>
        ///   <item><b>toCell</b>: destinazione del segmento visuale.</item>
        ///   <item><b>elapsedTicks</b>: tick gia' avanzati.</item>
        ///   <item><b>requiredTicks</b>: durata totale del movimento.</item>
        /// </list>
        /// </summary>
        public static ArcGraphActorMotionSnapshot CreateMovement(
            ArcGraphCellCoord fromCell,
            ArcGraphCellCoord toCell,
            int elapsedTicks,
            int requiredTicks)
        {
            return new ArcGraphActorMotionSnapshot(
                fromCell,
                toCell,
                elapsedTicks,
                requiredTicks,
                "Movement");
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
