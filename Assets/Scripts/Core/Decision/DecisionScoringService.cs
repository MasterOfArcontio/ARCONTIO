using System.Collections.Generic;

namespace Arcontio.Core
{
    // =============================================================================
    // DecisionScoringService
    // =============================================================================
    /// <summary>
    /// <para>
    /// Servizio della Fase 2 che calcola lo score composito dei candidati prodotti
    /// dalla Fase 1.
    /// </para>
    ///
    /// <para><b>Scoring incrementale e spiegabile</b></para>
    /// <para>
    /// La sessione 5 introduce solo il termine lineare di urgenza del bisogno. Le
    /// sessioni successive aggiungeranno competenza, preferenza, obbligo, memoria e
    /// modulatori cognitivi mantenendo lo stesso punto di composizione.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>ScoreCandidates</b>: valuta in-place una lista di candidati.</item>
    ///   <item><b>NeedUrgency</b>: primo contributo lineare e data-driven.</item>
    ///   <item><b>Breakdown</b>: array di contributi copiato su ogni candidato.</item>
    /// </list>
    /// </summary>
    public sealed class DecisionScoringService
    {
        private readonly List<DecisionScoreContribution> _contributions = new(8);

        // =============================================================================
        // ScoreCandidates
        // =============================================================================
        /// <summary>
        /// <para>
        /// Calcola lo score dei candidati disponibili e aggiorna ciascun elemento
        /// della lista con punteggio finale e breakdown.
        /// </para>
        ///
        /// <para><b>Mutazione locale della lista candidati</b></para>
        /// <para>
        /// La funzione non modifica World, BeliefStore o profili NPC. Lavora solo sui
        /// candidati gia' costruiti dalla Fase 1, rendendo chiaro il confine tra
        /// generazione e scoring.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Loop</b>: processa candidati disponibili uno per volta.</item>
        ///   <item><b>NeedUrgency</b>: somma il contributo lineare del bisogno.</item>
        ///   <item><b>Write back</b>: riassegna la struct nella lista dopo la mutazione.</item>
        /// </list>
        /// </summary>
        public void ScoreCandidates(List<DecisionCandidate> candidates, DecisionScoringConfig config)
        {
            ScoreCandidates(default, candidates, config);
        }

        // =============================================================================
        // ScoreCandidates
        // =============================================================================
        /// <summary>
        /// <para>
        /// Calcola lo score dei candidati usando anche il contesto NPC disponibile.
        /// </para>
        ///
        /// <para><b>Scoring con NpcProfile</b></para>
        /// <para>
        /// Questa variante consente alla Fase 2 di leggere competenza e preferenza
        /// dal profilo runtime gia' risolto dal chiamante, senza cercare l'NPC nel
        /// World e senza introdurre accessi globali.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>NeedUrgency</b>: contributo lineare della sessione 5.</item>
        ///   <item><b>Competence</b>: affinita' pratica del dominio candidato.</item>
        ///   <item><b>Preference</b>: desiderabilita' soggettiva del dominio candidato.</item>
        /// </list>
        /// </summary>
        public void ScoreCandidates(
            in DecisionEvaluationContext context,
            List<DecisionCandidate> candidates,
            DecisionScoringConfig config)
        {
            if (candidates == null)
                return;

            for (int i = 0; i < candidates.Count; i++)
            {
                var candidate = candidates[i];
                if (!candidate.IsAvailable)
                    continue;

                _contributions.Clear();

                float score = 0f;
                score += AddNeedUrgencyContribution(candidate, config);
                score += AddCompetenceContribution(context.Profile, candidate, config);
                score += AddPreferenceContribution(context.Profile, candidate, config);
                score += AddObligationContribution(context.Profile, candidate, config);
                score = ApplyMandatoryFloors(context.Profile, candidate, config, score);

                candidate.AttachScore(score, _contributions.ToArray());
                candidates[i] = candidate;
            }
        }

        // =============================================================================
        // AddNeedUrgencyContribution
        // =============================================================================
        /// <summary>
        /// <para>
        /// Calcola il contributo lineare della pressione del bisogno primario.
        /// </para>
        ///
        /// <para><b>NeedUrgency continua</b></para>
        /// <para>
        /// Il valore del bisogno e' gia' normalizzato 0-1 in <c>DecisionCandidate</c>.
        /// La sessione 5 lo usa direttamente come funzione lineare, moltiplicata per
        /// un peso nominato in <c>DecisionScoringConfig</c>.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Input</b>: <c>NeedUrgency01</c> del candidato.</item>
        ///   <item><b>Peso</b>: <c>needUrgencyWeight</c>.</item>
        ///   <item><b>Output</b>: contributo positivo allo score finale.</item>
        /// </list>
        /// </summary>
        private float AddNeedUrgencyContribution(DecisionCandidate candidate, DecisionScoringConfig config)
        {
            float contribution = candidate.NeedUrgency01 * config.needUrgencyWeight;
            _contributions.Add(new DecisionScoreContribution("NeedUrgency", contribution));
            return contribution;
        }

        // =============================================================================
        // AddCompetenceContribution
        // =============================================================================
        /// <summary>
        /// <para>
        /// Calcola il contributo della competenza runtime dell'NPC nel dominio
        /// dell'intenzione candidata.
        /// </para>
        ///
        /// <para><b>CompetenceAffinity</b></para>
        /// <para>
        /// La competenza e' letta dal <c>NpcProfile</c>, non dal DNA: rappresenta
        /// esperienza maturata o stato runtime. Se il profilo manca, il contributo
        /// resta zero per non inventare capacita' implicite.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Domain</b>: dominio dichiarato dal catalogo intenzioni.</item>
        ///   <item><b>Profile</b>: sorgente dei valori runtime.</item>
        ///   <item><b>Output</b>: valore 0-1 moltiplicato per peso configurato.</item>
        /// </list>
        /// </summary>
        private float AddCompetenceContribution(NpcProfile profile, DecisionCandidate candidate, DecisionScoringConfig config)
        {
            if (profile == null || profile.Competence == null || candidate.Metadata.Domain == DomainKind.None)
            {
                _contributions.Add(new DecisionScoreContribution("CompetenceAffinity", 0f));
                return 0f;
            }

            float contribution = profile.Competence.Get(candidate.Metadata.Domain) * config.competenceWeight;
            _contributions.Add(new DecisionScoreContribution("CompetenceAffinity", contribution));
            return contribution;
        }

        // =============================================================================
        // AddPreferenceContribution
        // =============================================================================
        /// <summary>
        /// <para>
        /// Calcola il contributo della preferenza runtime dell'NPC nel dominio
        /// dell'intenzione candidata.
        /// </para>
        ///
        /// <para><b>PreferenceAffinity</b></para>
        /// <para>
        /// La preferenza rappresenta quanto l'NPC tende a volere quel dominio di
        /// attivita'. A differenza dell'urgenza fisiologica, non forza da sola la
        /// scelta, ma differenzia NPC con profili diversi.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Domain</b>: dominio dichiarato dal catalogo intenzioni.</item>
        ///   <item><b>Profile</b>: sorgente dei valori runtime.</item>
        ///   <item><b>Output</b>: valore 0-1 moltiplicato per peso configurato.</item>
        /// </list>
        /// </summary>
        private float AddPreferenceContribution(NpcProfile profile, DecisionCandidate candidate, DecisionScoringConfig config)
        {
            if (profile == null || profile.Preference == null || candidate.Metadata.Domain == DomainKind.None)
            {
                _contributions.Add(new DecisionScoreContribution("PreferenceAffinity", 0f));
                return 0f;
            }

            float contribution = profile.Preference.Get(candidate.Metadata.Domain) * config.preferenceWeight;
            _contributions.Add(new DecisionScoreContribution("PreferenceAffinity", contribution));
            return contribution;
        }

        // =============================================================================
        // AddObligationContribution
        // =============================================================================
        /// <summary>
        /// <para>
        /// Calcola il contributo della pressione di obbligo per il dominio
        /// dell'intenzione candidata.
        /// </para>
        ///
        /// <para><b>ObligationPressure</b></para>
        /// <para>
        /// L'obbligo appartiene a <c>NpcProfile</c> perche' e' runtime e puo' essere
        /// modificato da ruolo, istituzione o pressione culturale. In questa fase
        /// diventa un termine positivo dello score, separato dal gate leggero della
        /// Fase 1.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Domain</b>: dominio candidato usato come indice dell'obbligo.</item>
        ///   <item><b>Weight</b>: peso <c>obligationWeight</c> centralizzato in config.</item>
        ///   <item><b>Output</b>: contributo positivo allo score finale.</item>
        /// </list>
        /// </summary>
        private float AddObligationContribution(NpcProfile profile, DecisionCandidate candidate, DecisionScoringConfig config)
        {
            if (profile == null || profile.Obligation == null || candidate.Metadata.Domain == DomainKind.None)
            {
                _contributions.Add(new DecisionScoreContribution("ObligationPressure", 0f));
                return 0f;
            }

            float contribution = profile.Obligation.Get(candidate.Metadata.Domain) * config.obligationWeight;
            _contributions.Add(new DecisionScoreContribution("ObligationPressure", contribution));
            return contribution;
        }

        // =============================================================================
        // ApplyMandatoryFloors
        // =============================================================================
        /// <summary>
        /// <para>
        /// Applica i floor obbligatori previsti dalla roadmap per bisogno critico e
        /// obbligo alto.
        /// </para>
        ///
        /// <para><b>Floor come vincolo spiegabile</b></para>
        /// <para>
        /// Il floor non seleziona direttamente il candidato: alza solo lo score minimo
        /// quando una pressione e' abbastanza forte. Il contributo aggiunto viene
        /// registrato nel breakdown per rendere visibile il motivo del salto.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>CriticalNeedFloor</b>: soglia minima per bisogni critici.</item>
        ///   <item><b>HighObligationFloor</b>: soglia minima per obblighi sopra threshold.</item>
        ///   <item><b>Delta</b>: solo la differenza necessaria viene aggiunta al breakdown.</item>
        /// </list>
        /// </summary>
        private float ApplyMandatoryFloors(
            NpcProfile profile,
            DecisionCandidate candidate,
            DecisionScoringConfig config,
            float currentScore)
        {
            float flooredScore = currentScore;

            if (candidate.IsCritical && flooredScore < config.criticalNeedFloor)
                flooredScore = config.criticalNeedFloor;

            if (profile != null
                && profile.Obligation != null
                && candidate.Metadata.Domain != DomainKind.None
                && profile.Obligation.Get(candidate.Metadata.Domain) >= config.highObligationThreshold
                && flooredScore < config.highObligationFloor)
            {
                flooredScore = config.highObligationFloor;
            }

            float delta = flooredScore - currentScore;
            _contributions.Add(new DecisionScoreContribution("MandatoryFloor", delta));
            return flooredScore;
        }
    }
}
