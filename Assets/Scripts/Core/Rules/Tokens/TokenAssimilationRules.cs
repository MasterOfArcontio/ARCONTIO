namespace Arcontio.Core
{
    /// <summary>
    /// AssimilatePredatorAlertRule:
    /// - token PredatorAlert / AlarmDanger
    /// - produce MemoryType.PredatorRumor
    /// 
    /// Scopo:
    /// - non è "ho visto il predatore"
    /// - è "ho sentito dire che c'è un predatore/pericolo"
    /// </summary>
    public sealed class AssimilatePredatorAlertRule : ITokenAssimilationRule
    {
        public bool Matches(in TokenEnvelope env)
        {
            return env.Token.Type == TokenType.PredatorAlert ||
                   env.Token.Type == TokenType.AlarmDanger;
        }

        public bool TryAssimilate(World world, in TokenEnvelope env, out MemoryTrace outTrace)
        {
            outTrace = default;

            // Safety: deve esistere listener
            if (!world.ExistsNpc(env.ListenerId))
                return false;

            // Rumor: reliability degradata rispetto al token
            // (in futuro moltiplicheremo per trustFactor)
            float rumorReliability = env.Token.Reliability01 * 0.75f;

            // ChainDepth aumenta: se lo rispiegherà, sarà ancora meno affidabile
            // (oggi non lo persistiamo nella trace; lo useremo più avanti)
            // int newDepth = env.Token.ChainDepth + 1;

            // Intensità rumor: più bassa dellintensità interna tipica
            float rumorIntensity = env.Token.Intensity01 * 0.60f;

            // Decay rumor più veloce del diretto
            float decay = 0.0060f;

            int cx = 0, cy = 0;
            bool hasCell = env.Token.HasCell;
            if (hasCell)
            {
                cx = env.Token.CellX;
                cy = env.Token.CellY;
            }

            HeardKind heardKind = env.Token.ChainDepth == 0 ? HeardKind.DirectHeard : HeardKind.RumorHeard;

            outTrace = new MemoryTrace
            {
                Type = MemoryType.PredatorRumor,
                SubjectId = env.Token.SubjectId,
                CellX = hasCell ? cx : -1,
                CellY = hasCell ? cy : -1,
                Intensity01 = rumorIntensity,
                Reliability01 = rumorReliability,
                DecayPerTick01 = decay,
                // Gestione dell'origine della notizia
                IsHeard = true,
                HeardKind = heardKind,
                SourceSpeakerId = env.SpeakerId
            };

            return true;
        }
    }

    /// <summary>
    /// AssimilateHelpRequestRule:
    /// - token HelpRequest
    /// - produce MemoryType.AidRequested
    /// 
    /// È una trace "leggera": serve più come trigger decisionale futuro.
    /// </summary>
    public sealed class AssimilateHelpRequestRule : ITokenAssimilationRule
    {
        public bool Matches(in TokenEnvelope env)
        {
            return env.Token.Type == TokenType.HelpRequest;
        }

        public bool TryAssimilate(World world, in TokenEnvelope env, out MemoryTrace outTrace)
        {
            outTrace = default;

            if (!world.ExistsNpc(env.ListenerId))
                return false;

            // Reliability: richiesta aiuto di solito non è "vero/falso" ma "ha chiesto"
            // però teniamo comunque un valore per coerenza.
            float reliability = env.Token.Reliability01;

            // Intensità: teniamo un valore medio, modulato dal token
            float intensity = 0.50f * env.Token.Intensity01;

            // Decay: abbastanza rapido (richiesta aiuto "scade")
            float decay = 0.0100f;

            HeardKind heardKind = env.Token.ChainDepth == 0 ? HeardKind.DirectHeard : HeardKind.RumorHeard;

            outTrace = new MemoryTrace
            {
                Type = MemoryType.AidRequested,
                SubjectId = env.SpeakerId, // "chi ha chiesto aiuto"
                CellX = env.Token.HasCell ? env.Token.CellX : -1,
                CellY = env.Token.HasCell ? env.Token.CellY : -1,
                Intensity01 = intensity,
                Reliability01 = reliability,
                DecayPerTick01 = decay,
                // Gestione dell'origine della notizia
                IsHeard = true,
                HeardKind = heardKind,
                SourceSpeakerId = env.SpeakerId
            };

            return true;
        }
    }

    /// <summary>
    /// AssimilateTheftReportRule (Patch 0.01P3 extension):
    /// - token TheftReportVictim / TheftReportWitness
    /// - produce una MemoryTrace "crime heard" nel listener.
    ///
    /// Design v0:
    /// - Non creiamo un nuovo MemoryType dedicato (tipo TheftRumor) perché la tassonomia
    ///   già contiene TheftWitnessed/TheftSuffered e la trace può marcare IsHeard=true.
    /// - Usiamo SecondarySubjectId per conservare la vittima quando è nota.
    ///
    /// Mapping:
    /// - TheftReportVictim  -> MemoryType.TheftSuffered   (heard)
    /// - TheftReportWitness -> MemoryType.TheftWitnessed  (heard)
    ///
    /// SubjectId / SecondarySubjectId:
    /// - SubjectId = thiefNpcId
    /// - SecondarySubjectId = victimNpcId (se presente)
    ///
    /// Nota:
    /// - Per TheftReportVictim il victimNpcId spesso coincide con SpeakerId, ma non lo assumiamo.
    /// </summary>
    public sealed class AssimilateTheftReportRule : ITokenAssimilationRule
    {
        public bool Matches(in TokenEnvelope env)
        {
            return env.Token.Type == TokenType.TheftReportVictim ||
                   env.Token.Type == TokenType.TheftReportWitness;
        }

        public bool TryAssimilate(World world, in TokenEnvelope env, out MemoryTrace outTrace)
        {
            outTrace = default;

            if (!world.ExistsNpc(env.ListenerId))
                return false;

            // Reliability e intensità: per i report di crimine manteniamo un peso relativamente alto,
            // ma più basso del "diretto" perché comunque è un racconto.
            float reliability = env.Token.Reliability01 * 0.85f;
            float intensity = env.Token.Intensity01 * 0.70f;

            // Decay: crimine rimane più a lungo del rumor di predatore? Qui scegliamo un valore medio.
            float decay = 0.0055f;

            HeardKind heardKind = env.Token.ChainDepth == 0 ? HeardKind.DirectHeard : HeardKind.RumorHeard;

            MemoryType mtype = env.Token.Type == TokenType.TheftReportVictim
                ? MemoryType.TheftSuffered
                : MemoryType.TheftWitnessed;

            outTrace = new MemoryTrace
            {
                Type = mtype,
                SubjectId = env.Token.SubjectId, // thief
                SecondarySubjectId = env.Token.SecondarySubjectId, // victim (se nota)
                CellX = env.Token.HasCell ? env.Token.CellX : -1,
                CellY = env.Token.HasCell ? env.Token.CellY : -1,
                Intensity01 = intensity,
                Reliability01 = reliability,
                DecayPerTick01 = decay,

                // Origine comunicazione
                IsHeard = true,
                HeardKind = heardKind,
                SourceSpeakerId = env.SpeakerId
            };

            return true;
        }
    }
}
