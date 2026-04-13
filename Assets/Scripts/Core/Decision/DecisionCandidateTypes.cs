using UnityEngine;

namespace Arcontio.Core
{
    // =============================================================================
    // DecisionScheduleFrame
    // =============================================================================
    /// <summary>
    /// <para>
    /// Rappresentazione minimale e provvisoria di una finestra di schedule per il
    /// Decision Layer.
    /// </para>
    ///
    /// <para><b>Stub disciplinato per ScheduleFrame</b></para>
    /// <para>
    /// La roadmap colloca lo ScheduleFrame completo in v0.09. Qui serve solo un
    /// contratto piccolo per filtrare i candidati senza introdurre un planner
    /// istituzionale prematuro o una lettura globale della bacheca.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>IsActive</b>: false significa nessun vincolo di schedule.</item>
    ///   <item><b>FocusDomain</b>: dominio favorito dalla finestra corrente.</item>
    ///   <item><b>AllowSurvivalOverrides</b>: permette ai bisogni critici di superare lo schedule.</item>
    /// </list>
    /// </summary>
    public readonly struct DecisionScheduleFrame
    {
        public readonly bool IsActive;
        public readonly DomainKind FocusDomain;
        public readonly bool AllowSurvivalOverrides;

        public DecisionScheduleFrame(bool isActive, DomainKind focusDomain, bool allowSurvivalOverrides)
        {
            IsActive = isActive;
            FocusDomain = focusDomain;
            AllowSurvivalOverrides = allowSurvivalOverrides;
        }

        public bool Allows(DecisionIntentMetadata metadata, bool isCritical)
        {
            // Se non esiste una finestra attiva, la schedule non filtra nulla.
            if (!IsActive)
                return true;

            // Un bisogno critico puo' superare la finestra solo se la policy minima
            // lo consente: e' il ponte verso i futuri floor obbligatori.
            if (isCritical && AllowSurvivalOverrides && metadata.IsEmergencyIntent)
                return true;

            // Le intenzioni senza dominio, come WaitAndObserve, restano sempre
            // ammissibili per evitare set vuoti artificiali.
            if (metadata.Domain == DomainKind.None)
                return true;

            return metadata.Domain == FocusDomain;
        }
    }

    // =============================================================================
    // DecisionNormContext
    // =============================================================================
    /// <summary>
    /// <para>
    /// Contesto minimale per filtrare intenzioni che hanno rischio sociale o normativo.
    /// </para>
    ///
    /// <para><b>Norm System non anticipato</b></para>
    /// <para>
    /// La roadmap introduce norme complete piu' avanti. Questo contesto non legge
    /// leggi globali e non calcola punizioni: applica solo una soglia esplicita che
    /// il chiamante puo' derivare da dati soggettivi, sociali o da policy temporanee.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>NormsActive</b>: abilita o disabilita il filtro normativo.</item>
    ///   <item><b>MaxAcceptedSocialRisk01</b>: rischio massimo accettabile per l'NPC.</item>
    ///   <item><b>EmergencyIgnoresSocialRisk</b>: permette a bisogni critici di superare il filtro.</item>
    /// </list>
    /// </summary>
    public readonly struct DecisionNormContext
    {
        public readonly bool NormsActive;
        public readonly float MaxAcceptedSocialRisk01;
        public readonly bool EmergencyIgnoresSocialRisk;

        public DecisionNormContext(bool normsActive, float maxAcceptedSocialRisk01, bool emergencyIgnoresSocialRisk)
        {
            NormsActive = normsActive;
            MaxAcceptedSocialRisk01 = Clamp01(maxAcceptedSocialRisk01);
            EmergencyIgnoresSocialRisk = emergencyIgnoresSocialRisk;
        }

        public static DecisionNormContext Default()
        {
            return new DecisionNormContext(false, 1f, true);
        }

        public bool Allows(DecisionIntentMetadata metadata, bool isCritical)
        {
            // Se il filtro normativo non e' attivo, questa fase non scarta nulla.
            if (!NormsActive || !metadata.RequiresNormCheck)
                return true;

            // In emergenza critica, la policy MVP puo' permettere azioni ad alto
            // rischio sociale: la penalita' verra' poi gestita dallo scoring.
            if (isCritical && EmergencyIgnoresSocialRisk)
                return true;

            return metadata.SocialRisk01 <= MaxAcceptedSocialRisk01;
        }

        private static float Clamp01(float value)
        {
            if (value < 0f) return 0f;
            if (value > 1f) return 1f;
            return value;
        }
    }

    // =============================================================================
    // DecisionEvaluationContext
    // =============================================================================
    /// <summary>
    /// <para>
    /// Snapshot degli input permessi al Decision Layer per valutare i candidati di
    /// un singolo NPC.
    /// </para>
    ///
    /// <para><b>Input espliciti, niente accesso globale implicito</b></para>
    /// <para>
    /// Il contesto riceve solo componenti gia' risolti dal chiamante: bisogni, DNA,
    /// profilo runtime, posizione e belief store soggettivo. Questo rende visibile
    /// cosa entra nella decisione e prepara l'audit omniscience.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>NpcId/Tick</b>: identita' e tempo della valutazione.</item>
    ///   <item><b>Needs/Dna/Profile</b>: stato individuale ammesso.</item>
    ///   <item><b>NpcPosition</b>: posizione necessaria alle query belief.</item>
    ///   <item><b>Beliefs</b>: store soggettivo per-NPC, letto solo via QuerySystem nelle sessioni successive.</item>
    ///   <item><b>NormContext</b>: filtro esplicito per rischio sociale e norme MVP.</item>
    /// </list>
    /// </summary>
    public readonly struct DecisionEvaluationContext
    {
        public readonly int NpcId;
        public readonly int Tick;
        public readonly NpcNeeds Needs;
        public readonly NpcDnaProfile Dna;
        public readonly NpcProfile Profile;
        public readonly Vector2Int NpcPosition;
        public readonly BeliefStore Beliefs;
        public readonly BeliefQueryConfig BeliefQueryConfig;
        public readonly DecisionScheduleFrame ScheduleFrame;
        public readonly DecisionNormContext NormContext;

        public DecisionEvaluationContext(
            int npcId,
            int tick,
            NpcNeeds needs,
            NpcDnaProfile dna,
            NpcProfile profile,
            Vector2Int npcPosition,
            BeliefStore beliefs,
            BeliefQueryConfig beliefQueryConfig,
            DecisionScheduleFrame scheduleFrame)
            : this(
                npcId,
                tick,
                needs,
                dna,
                profile,
                npcPosition,
                beliefs,
                beliefQueryConfig,
                scheduleFrame,
                DecisionNormContext.Default())
        {
        }

        public DecisionEvaluationContext(
            int npcId,
            int tick,
            NpcNeeds needs,
            NpcDnaProfile dna,
            NpcProfile profile,
            Vector2Int npcPosition,
            BeliefStore beliefs,
            BeliefQueryConfig beliefQueryConfig,
            DecisionScheduleFrame scheduleFrame,
            DecisionNormContext normContext)
        {
            NpcId = npcId;
            Tick = tick;
            Needs = needs;
            Dna = dna;
            Profile = profile;
            NpcPosition = npcPosition;
            Beliefs = beliefs;
            BeliefQueryConfig = beliefQueryConfig;
            ScheduleFrame = scheduleFrame;
            NormContext = normContext;
        }
    }

    // =============================================================================
    // DecisionCandidate
    // =============================================================================
    /// <summary>
    /// <para>
    /// Candidato decisionale prodotto dalla Fase 1 e poi arricchito dalla Fase 2.
    /// </para>
    ///
    /// <para><b>Candidato, non comando</b></para>
    /// <para>
    /// Il candidato non modifica il mondo e non contiene ancora un Job. Descrive
    /// soltanto una possibile intenzione, la sua urgenza e, nelle sessioni successive,
    /// il belief target e lo score spiegabile.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Kind/Metadata</b>: identita' semantica del candidato.</item>
    ///   <item><b>NeedUrgency01</b>: pressione fisiologica o psicologica normalizzata.</item>
    ///   <item><b>IsCritical</b>: flag derivato da soglia DNA/bisogni.</item>
    ///   <item><b>FilteredReason</b>: stringa diagnostica quando il candidato viene scartato.</item>
    /// </list>
    /// </summary>
    public struct DecisionCandidate
    {
        public DecisionIntentKind Kind;
        public DecisionIntentMetadata Metadata;
        public float NeedUrgency01;
        public bool IsCritical;
        public bool IsAvailable;
        public string FilteredReason;
        public BeliefQueryResult BeliefResult;
        public float FinalScore;

        public static DecisionCandidate Available(DecisionIntentMetadata metadata, float urgency01, bool isCritical)
        {
            return new DecisionCandidate
            {
                Kind = metadata.Kind,
                Metadata = metadata,
                NeedUrgency01 = Clamp01(urgency01),
                IsCritical = isCritical,
                IsAvailable = true,
                FilteredReason = string.Empty,
                BeliefResult = BeliefQueryResult.Empty(),
                FinalScore = 0f
            };
        }

        // =============================================================================
        // AttachBeliefResult
        // =============================================================================
        /// <summary>
        /// <para>
        /// Aggancia al candidato il risultato prodotto dal QuerySystem per il target
        /// belief richiesto dall'intenzione.
        /// </para>
        ///
        /// <para><b>Risultato spiegabile come dato, non come decisione</b></para>
        /// <para>
        /// Il candidato conserva il risultato per la Fase 2, ma non ricalcola score e
        /// non interroga direttamente il BeliefStore. La scelta del belief resta del
        /// <c>BeliefQueryService</c>.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>BeliefResult</b>: copia del risultato strutturato da usare negli step successivi.</item>
        /// </list>
        /// </summary>
        public void AttachBeliefResult(BeliefQueryResult beliefResult)
        {
            // La Fase 1 non interpreta lo score del belief: conserva solo il risultato
            // prodotto dal QuerySystem per renderlo disponibile alla Fase 2 e al debug.
            BeliefResult = beliefResult;
        }

        public static DecisionCandidate Filtered(DecisionIntentMetadata metadata, string reason)
        {
            return new DecisionCandidate
            {
                Kind = metadata.Kind,
                Metadata = metadata,
                NeedUrgency01 = 0f,
                IsCritical = false,
                IsAvailable = false,
                FilteredReason = reason ?? string.Empty,
                BeliefResult = BeliefQueryResult.Empty(),
                FinalScore = 0f
            };
        }

        private static float Clamp01(float value)
        {
            if (value < 0f) return 0f;
            if (value > 1f) return 1f;
            return value;
        }
    }
}
