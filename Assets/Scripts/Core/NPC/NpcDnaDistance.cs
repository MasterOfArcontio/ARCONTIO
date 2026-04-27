namespace Arcontio.Core
{
    // ─────────────────────────────────────────────────────────────────────────
    // NpcDnaDistance.cs — calcolo distanza DNA↔NpcProfile
    //
    // Misura quanto l'NPC si è allontanato dalla sua natura originale (DNA).
    // Risultato in [0, 1]: 0 = perfettamente allineato, 1 = massima divergenza.
    //
    // Tre assi di confronto, tutti per dominio (DomainKind):
    //   Preference  — preferenze correnti vs seed originali
    //   Competence  — competenza corrente vs cap massimo (quanto è "sottoutilizzato")
    //   Obligation  — senso di obbligo corrente vs frame originale
    //
    // I pesi dei tre assi sono configurabili tramite DnaDistanceWeights.
    // I valori di default sono stati calibrati per dare priorità alle preferenze
    // (che impattano insoddisfazione narrativa) rispetto a competenza e obbligo.
    //
    // Usi:
    //   - Trigger RoleDissatisfaction: distanza > NpcThresholds.RoleDissatisfaction01
    //   - Input scoring Decision Layer (v0.05): termine PreferenceAffinity
    //   - Debug overlay (sessione 6): colore NPC in base alla distanza
    // ─────────────────────────────────────────────────────────────────────────


    // ── Pesi ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Pesi dei tre assi nel calcolo della distanza DNA↔NpcProfile.
    /// La somma dei pesi NON deve essere necessariamente 1 — vengono normalizzati
    /// internamente da NpcDnaDistance.Compute.
    /// </summary>
    public readonly struct DnaDistanceWeights
    {
        /// <summary>Peso dell'asse Preferenza. Default: 0.5</summary>
        public readonly float Preference;

        /// <summary>Peso dell'asse Competenza. Default: 0.3</summary>
        public readonly float Competence;

        /// <summary>Peso dell'asse Obbligo. Default: 0.2</summary>
        public readonly float Obligation;

        public DnaDistanceWeights(float preference, float competence, float obligation)
        {
            Preference = preference;
            Competence = competence;
            Obligation = obligation;
        }

        /// <summary>
        /// Pesi di default calibrati per dare priorità narrativa alla preferenza.
        /// Preference=0.5, Competence=0.3, Obligation=0.2
        /// </summary>
        public static readonly DnaDistanceWeights Default =
            new DnaDistanceWeights(0.5f, 0.3f, 0.2f);
    }


    // ── Risultato ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Risultato del calcolo distanza DNA↔NpcProfile.
    ///
    /// Total: distanza complessiva normalizzata [0, 1].
    /// I tre campi per-asse (PreferenceDistance, CompetenceDistance, ObligationDistance)
    /// sono distanze parziali già pesate e normalizzate [0, 1].
    /// Usati dal debug overlay (sessione 6) per mostrare quale asse contribuisce di più.
    /// </summary>
    public readonly struct DnaDistanceResult
    {
        /// <summary>Distanza complessiva [0, 1]. Confronta con NpcThresholds.RoleDissatisfaction01.</summary>
        public readonly float Total;

        /// <summary>Contributo dell'asse preferenza alla distanza totale [0, 1].</summary>
        public readonly float PreferenceDistance;

        /// <summary>Contributo dell'asse competenza alla distanza totale [0, 1].</summary>
        public readonly float CompetenceDistance;

        /// <summary>Contributo dell'asse obbligo alla distanza totale [0, 1].</summary>
        public readonly float ObligationDistance;

        public DnaDistanceResult(
            float total,
            float preferenceDistance,
            float competenceDistance,
            float obligationDistance)
        {
            Total               = total;
            PreferenceDistance  = preferenceDistance;
            CompetenceDistance  = competenceDistance;
            ObligationDistance  = obligationDistance;
        }

        /// <summary>Risultato zero: NPC perfettamente allineato al DNA.</summary>
        public static readonly DnaDistanceResult Zero =
            new DnaDistanceResult(0f, 0f, 0f, 0f);

        public override string ToString() =>
            $"Dist={Total:0.000} (pref={PreferenceDistance:0.000} comp={CompetenceDistance:0.000} obl={ObligationDistance:0.000})";
    }


    // ── Calcolatore ───────────────────────────────────────────────────────────

    /// <summary>
    /// NpcDnaDistance — calcola la distanza tra il DNA immutabile e il profilo corrente.
    ///
    /// Tutti i metodi sono statici e senza stato — possono essere chiamati da qualsiasi
    /// System o Rule senza istanziazione.
    /// </summary>
    public static class NpcDnaDistance
    {
        private const int DomainCount = (int)DomainKind.COUNT;

        // ── API principale ─────────────────────────────────────────────────────

        /// <summary>
        /// Calcola la distanza DNA↔NpcProfile con i pesi di default.
        /// </summary>
        public static DnaDistanceResult Compute(NpcDnaProfile dna, NpcProfile profile)
            => Compute(dna, profile, DnaDistanceWeights.Default);

        /// <summary>
        /// Calcola la distanza DNA↔NpcProfile con pesi personalizzati.
        ///
        /// Formula per asse (esempio Preferenza):
        ///   prefDist = Σ(d=1..COUNT-1) |DNA.Preferences.Seeds[d] - Profile.Preference.Values[d]|
        ///              / (COUNT - 1)   ← media su domini non-None
        ///
        /// Distanza totale:
        ///   total = (w_pref * prefDist + w_comp * compDist + w_obl * oblDist)
        ///           / (w_pref + w_comp + w_obl)
        /// </summary>
        public static DnaDistanceResult Compute(
            NpcDnaProfile    dna,
            NpcProfile       profile,
            DnaDistanceWeights weights)
        {
            // Domini validi: escludiamo DomainKind.None (indice 0) e COUNT (sentinella).
            // Iteriamo da 1 a COUNT-1 incluso.
            const int firstDomain = 1; // DomainKind.Agriculture = 1
            const int lastDomain  = DomainCount - 1;
            int validDomains = lastDomain; // COUNT - 1 = 8

            float prefSum  = 0f;
            float compSum  = 0f;
            float oblSum   = 0f;

            float[] dnaPrefSeeds  = dna.Preferences.Seeds;
            float[] dnaCompCaps   = dna.Capacities.CompetenceCap;
            float[] dnaOblSeeds   = dna.ObligationFrame.Seeds;

            float[] currPref = profile.Preference.Values;
            float[] currComp = profile.Competence.Values;
            float[] currObl  = profile.Obligation.Values;

            for (int d = firstDomain; d < DomainCount; d++)
            {
                // Asse Preferenza: distanza tra seed e valore corrente
                float dnaPref = (dnaPrefSeeds != null && d < dnaPrefSeeds.Length)
                    ? dnaPrefSeeds[d] : 0f;
                prefSum += Abs(dnaPref - currPref[d]);

                // Asse Competenza: quanto l'NPC è lontano dal suo cap massimo
                // (sottoutilizzo del potenziale → fonte di insoddisfazione)
                float dnaCap = (dnaCompCaps != null && d < dnaCompCaps.Length)
                    ? dnaCompCaps[d] : 1f;
                compSum += Abs(dnaCap - currComp[d]);

                // Asse Obbligo: distanza tra frame culturale originale e stato corrente
                float dnaObl = (dnaOblSeeds != null && d < dnaOblSeeds.Length)
                    ? dnaOblSeeds[d] : 0f;
                oblSum += Abs(dnaObl - currObl[d]);
            }

            // Normalizza per numero di domini
            float inv = validDomains > 0 ? 1f / validDomains : 0f;
            float prefDist = prefSum * inv;
            float compDist = compSum * inv;
            float oblDist  = oblSum  * inv;

            // Somma pesata normalizzata
            float totalWeight = weights.Preference + weights.Competence + weights.Obligation;
            float total = totalWeight > 0f
                ? (weights.Preference * prefDist +
                   weights.Competence * compDist +
                   weights.Obligation * oblDist)
                  / totalWeight
                : 0f;

            // Clamp difensivo: i valori per-asse già in [0,1] ma total può avere
            // imprecisioni floating point ai bordi
            total = total < 0f ? 0f : (total > 1f ? 1f : total);

            return new DnaDistanceResult(total, prefDist, compDist, oblDist);
        }

        // ── Query rapide ───────────────────────────────────────────────────────

        /// <summary>
        /// Restituisce solo la distanza totale senza breakdown per asse.
        /// Più leggero di Compute — usalo dove non serve il debug breakdown.
        /// </summary>
        public static float ComputeTotal(NpcDnaProfile dna, NpcProfile profile)
            => Compute(dna, profile, DnaDistanceWeights.Default).Total;

        /// <summary>
        /// Verifica se la distanza supera la soglia di insoddisfazione del DNA.
        /// Equivalente a: ComputeTotal(dna, profile) > dna.Thresholds.RoleDissatisfaction01
        /// </summary>
        public static bool IsRoleDissatisfied(NpcDnaProfile dna, NpcProfile profile)
            => ComputeTotal(dna, profile) > dna.Thresholds.RoleDissatisfaction01;

        // ── Utilità interna ────────────────────────────────────────────────────

        private static float Abs(float v) => v < 0f ? -v : v;
    }
}
