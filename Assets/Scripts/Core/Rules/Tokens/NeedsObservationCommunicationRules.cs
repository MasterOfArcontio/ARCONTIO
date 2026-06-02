namespace Arcontio.Core
{
    // =============================================================================
    // NeedsObservationEmissionRule
    // =============================================================================
    /// <summary>
    /// <para>
    /// Converte memorie dirette di bisogni osservati in messaggi simbolici
    /// comunicabili a un NPC vicino. In questa prima versione tratta solo fatti gia'
    /// avvenuti e gia' ricordati: consumo di cibo e riposo nel letto.
    /// </para>
    ///
    /// <para><b>Principio architetturale: comunicazione di memoria, non telepatia</b></para>
    /// <para>
    /// La rule non legge il <c>World</c> per scoprire fatti nuovi. Parte da una
    /// <c>MemoryTrace</c> gia' presente nello speaker e produce un token degradabile.
    /// Le memorie gia' sentite non vengono riemesse, cosi' evitiamo catene rumorali
    /// premature prima di una policy sociale dedicata.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>FoodConsumed</b>: diventa <c>FoodConsumedReport</c>.</item>
    ///   <item><b>BedRested</b>: diventa <c>BedRestedReport</c>.</item>
    ///   <item><b>Gating</b>: solo trace dirette con intensita' minima.</item>
    /// </list>
    /// </summary>
    public sealed class NeedsObservationEmissionRule : ITokenEmissionRule
    {
        public bool Matches(in MemoryTrace trace)
        {
            return trace.Type == MemoryType.FoodConsumed || trace.Type == MemoryType.BedRested;
        }

        public bool TryCreateToken(
            World world,
            long tickIndex,
            int speakerNpcId,
            int listenerNpcId,
            in MemoryTrace trace,
            out TokenEnvelope token)
        {
            token = default;

            if (trace.IsHeard)
                return false;

            if (trace.Intensity01 < 0.20f)
                return false;

            TokenType tokenType = trace.Type == MemoryType.FoodConsumed
                ? TokenType.FoodConsumedReport
                : TokenType.BedRestedReport;

            var symbolic = new SymbolicToken(
                type: tokenType,
                subjectId: trace.SubjectId,
                intensity01: trace.Intensity01,
                reliability01: trace.Reliability01,
                chainDepth: 0,
                hasCell: trace.CellX >= 0 && trace.CellY >= 0,
                cellX: trace.CellX,
                cellY: trace.CellY,
                secondarySubjectId: trace.SecondarySubjectId);

            token = new TokenEnvelope(
                speakerId: speakerNpcId,
                listenerId: listenerNpcId,
                channel: TokenChannel.ProximityTalk,
                tickIndex: tickIndex,
                token: symbolic);

            return true;
        }
    }

    // =============================================================================
    // AssimilateNeedsObservationRule
    // =============================================================================
    /// <summary>
    /// <para>
    /// Trasforma un racconto su un fatto needs osservato in una memoria sentita del
    /// listener. La traccia risultante resta distinta dalla memoria diretta tramite
    /// <c>IsHeard</c>, <c>HeardKind</c> e <c>SourceSpeakerId</c>.
    /// </para>
    ///
    /// <para><b>Principio architetturale: memoria comunicata a bassa autorita'</b></para>
    /// <para>
    /// Il listener non tratta il racconto come percezione diretta: affidabilita' e
    /// intensita' vengono degradate, e nessuna mutazione di world/belief speciale
    /// viene introdotta qui.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>FoodConsumedReport</b>: produce <c>MemoryType.FoodConsumed</c> sentita.</item>
    ///   <item><b>BedRestedReport</b>: produce <c>MemoryType.BedRested</c> sentita.</item>
    ///   <item><b>Degrado</b>: riduce intensita' e affidabilita' rispetto al token.</item>
    /// </list>
    /// </summary>
    public sealed class AssimilateNeedsObservationRule : ITokenAssimilationRule
    {
        public bool Matches(in TokenEnvelope env)
        {
            return env.Token.Type == TokenType.FoodConsumedReport
                || env.Token.Type == TokenType.BedRestedReport;
        }

        public bool TryAssimilate(World world, in TokenEnvelope env, out MemoryTrace outTrace)
        {
            outTrace = default;

            if (world == null || !world.ExistsNpc(env.ListenerId))
                return false;

            MemoryType memoryType = env.Token.Type == TokenType.FoodConsumedReport
                ? MemoryType.FoodConsumed
                : MemoryType.BedRested;

            HeardKind heardKind = env.Token.ChainDepth == 0 ? HeardKind.DirectHeard : HeardKind.RumorHeard;

            outTrace = new MemoryTrace
            {
                Type = memoryType,
                SubjectId = env.Token.SubjectId,
                SecondarySubjectId = env.Token.SecondarySubjectId,
                SubjectDefId = env.Token.Type.ToString(),
                CellX = env.Token.HasCell ? env.Token.CellX : -1,
                CellY = env.Token.HasCell ? env.Token.CellY : -1,
                Intensity01 = env.Token.Intensity01 * 0.65f,
                Reliability01 = env.Token.Reliability01 * 0.80f,
                DecayPerTick01 = memoryType == MemoryType.FoodConsumed ? 0.0040f : 0.0035f,
                IsHeard = true,
                HeardKind = heardKind,
                SourceSpeakerId = env.SpeakerId
            };

            return true;
        }
    }
}
