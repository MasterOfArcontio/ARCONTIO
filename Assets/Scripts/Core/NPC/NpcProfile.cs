using System;

namespace Arcontio.Core
{
    // ─────────────────────────────────────────────────────────────────────────
    // NpcProfile — stato runtime mutabile per-NPC
    //
    // Contiene i tre assi evolutivi dell'NPC: competenza, preferenza, obbligo.
    // Tutti indicizzati per DomainKind.
    //
    // A differenza di NpcDnaProfile (immutabile, natura originale),
    // NpcProfile cambia nel tempo per effetto di:
    //   - esperienza accumulata (Competence)
    //   - abitudine e ruolo (Preference)
    //   - pressione culturale e istituzionale (Obligation)
    //
    // Il confronto NpcDnaProfile ↔ NpcProfile misura quanto l'NPC si è
    // allontanato dalla sua natura originale → insoddisfazione, drift, stress.
    // (implementato in sessione 4)
    //
    // AssignedRole è incluso qui come campo opzionale (null = nessun ruolo).
    // La serializzazione JSON è gestita in sessione 3.
    // ─────────────────────────────────────────────────────────────────────────


    // ── Profilo competenze ────────────────────────────────────────────────────

    /// <summary>
    /// Competenza effettiva dell'NPC per ogni dominio di attività.
    ///
    /// Inizializzata a zero (o da seed) al momento della creazione.
    /// Cresce con l'esperienza pratica nel dominio (implementato in sessioni future).
    /// Non può superare NpcCapacities.CompetenceCap[dominio].
    /// </summary>
    public sealed class CompetenceProfile
    {
        /// <summary>
        /// Livello di competenza per dominio [DomainKind.COUNT], valori 0-1.
        /// 0 = nessuna competenza, 1 = competenza massima raggiungibile.
        /// </summary>
        public readonly float[] Values;

        public CompetenceProfile()
        {
            Values = new float[(int)DomainKind.COUNT];
        }

        /// <summary>
        /// Legge la competenza per un dominio specifico.
        /// Ritorna 0 se il dominio è None o out of range.
        /// </summary>
        public float Get(DomainKind domain)
        {
            int idx = (int)domain;
            if (idx <= 0 || idx >= (int)DomainKind.COUNT) return 0f;
            return Values[idx];
        }

        /// <summary>
        /// Imposta la competenza per un dominio, rispettando il cap fornito.
        /// </summary>
        public void Set(DomainKind domain, float value, float cap = 1f)
        {
            int idx = (int)domain;
            if (idx <= 0 || idx >= (int)DomainKind.COUNT) return;
            Values[idx] = Math.Min(Math.Max(value, 0f), cap);
        }

        /// <summary>
        /// Inizializza tutti i valori a zero (stato di partenza senza esperienza).
        /// </summary>
        public static CompetenceProfile Zero() => new CompetenceProfile();

        /// <summary>
        /// Crea un CompetenceProfile con valori iniziali da un array di seed.
        /// Usato per NPC che iniziano con competenze preesistenti (es. veterani).
        /// </summary>
        public static CompetenceProfile FromSeeds(float[] seeds, float[] caps = null)
        {
            var p = new CompetenceProfile();
            int len = Math.Min(seeds.Length, (int)DomainKind.COUNT);
            for (int i = 0; i < len; i++)
            {
                float cap = (caps != null && i < caps.Length) ? caps[i] : 1f;
                p.Values[i] = Math.Min(Math.Max(seeds[i], 0f), cap);
            }
            return p;
        }
    }


    // ── Profilo preferenze ────────────────────────────────────────────────────

    /// <summary>
    /// Preferenza effettiva dell'NPC per ogni dominio di attività.
    ///
    /// Inizializzata dai seed in NpcPreferenceSeeds.
    /// Può derivare nel tempo per effetto di abitudine, ruolo e pressione sociale,
    /// modulata da NpcCognitiveModulators.DriftResistance01.
    /// </summary>
    public sealed class PreferenceProfile
    {
        /// <summary>
        /// Preferenza per dominio [DomainKind.COUNT], valori 0-1.
        /// 0 = nessuna affinità, 1 = affinità massima.
        /// </summary>
        public readonly float[] Values;

        public PreferenceProfile()
        {
            Values = new float[(int)DomainKind.COUNT];
        }

        /// <summary>
        /// Legge la preferenza per un dominio specifico.
        /// </summary>
        public float Get(DomainKind domain)
        {
            int idx = (int)domain;
            if (idx <= 0 || idx >= (int)DomainKind.COUNT) return 0f;
            return Values[idx];
        }

        /// <summary>
        /// Imposta la preferenza per un dominio (clampata in [0,1]).
        /// </summary>
        public void Set(DomainKind domain, float value)
        {
            int idx = (int)domain;
            if (idx <= 0 || idx >= (int)DomainKind.COUNT) return;
            Values[idx] = Math.Min(Math.Max(value, 0f), 1f);
        }

        /// <summary>
        /// Crea un PreferenceProfile inizializzato dai seed del DNA.
        /// </summary>
        public static PreferenceProfile FromSeeds(float[] seeds)
        {
            var p = new PreferenceProfile();
            int len = Math.Min(seeds.Length, (int)DomainKind.COUNT);
            for (int i = 0; i < len; i++)
                p.Values[i] = Math.Min(Math.Max(seeds[i], 0f), 1f);
            return p;
        }
    }


    // ── Profilo obblighi ──────────────────────────────────────────────────────

    /// <summary>
    /// Senso di obbligo effettivo dell'NPC per ogni dominio di attività.
    ///
    /// Inizializzato dai seed in NpcObligationFrame.
    /// Viene rinforzato dal ruolo assegnato (AssignedRole) e dalla pressione
    /// istituzionale (implementato dal Role System in v0.07).
    /// Influenza direttamente lo scoring nel Decision Layer (v0.05).
    /// </summary>
    public sealed class ObligationProfile
    {
        /// <summary>
        /// Senso di obbligo per dominio [DomainKind.COUNT], valori 0-1.
        /// 0 = nessun obbligo percepito, 1 = obbligo massimo.
        /// </summary>
        public readonly float[] Values;

        public ObligationProfile()
        {
            Values = new float[(int)DomainKind.COUNT];
        }

        /// <summary>
        /// Legge l'obbligo per un dominio specifico.
        /// </summary>
        public float Get(DomainKind domain)
        {
            int idx = (int)domain;
            if (idx <= 0 || idx >= (int)DomainKind.COUNT) return 0f;
            return Values[idx];
        }

        /// <summary>
        /// Imposta l'obbligo per un dominio (clampato in [0,1]).
        /// </summary>
        public void Set(DomainKind domain, float value)
        {
            int idx = (int)domain;
            if (idx <= 0 || idx >= (int)DomainKind.COUNT) return;
            Values[idx] = Math.Min(Math.Max(value, 0f), 1f);
        }

        /// <summary>
        /// Crea un ObligationProfile inizializzato dai seed del DNA.
        /// </summary>
        public static ObligationProfile FromSeeds(float[] seeds)
        {
            var p = new ObligationProfile();
            int len = Math.Min(seeds.Length, (int)DomainKind.COUNT);
            for (int i = 0; i < len; i++)
                p.Values[i] = Math.Min(Math.Max(seeds[i], 0f), 1f);
            return p;
        }
    }


    // ── Profilo runtime NPC ───────────────────────────────────────────────────

    /// <summary>
    /// NpcProfile — stato runtime mutabile dell'NPC.
    ///
    /// Aggregato dei tre assi evolutivi: Competence, Preference, Obligation.
    /// Tutti derivano dai seed in NpcDnaProfile ma cambiano con l'esperienza.
    ///
    /// AssignedRole: etichetta del ruolo istituzionale corrente.
    /// null = nessun ruolo assegnato (NPC libero o in cerca di ruolo).
    /// Sessione 3 aggiunge serializzazione JSON di questa struttura.
    /// Sessione 4 aggiunge il calcolo della distanza DNA↔NpcProfile.
    /// </summary>
    public sealed class NpcProfile
    {
        public readonly CompetenceProfile Competence;
        public readonly PreferenceProfile Preference;
        public readonly ObligationProfile Obligation;

        /// <summary>
        /// Ruolo istituzionale corrente dell'NPC.
        /// null = nessun ruolo. Assegnato da AssignRoleCommand (v0.07+).
        /// </summary>
        public string AssignedRole;

        public NpcProfile(
            CompetenceProfile competence,
            PreferenceProfile preference,
            ObligationProfile obligation,
            string assignedRole = null)
        {
            Competence   = competence;
            Preference   = preference;
            Obligation   = obligation;
            AssignedRole = assignedRole;
        }

        /// <summary>
        /// Crea un NpcProfile inizializzato dai valori seed del DNA.
        /// CompetenceProfile parte da zero (nessuna esperienza alla creazione).
        /// PreferenceProfile e ObligationProfile partono dai rispettivi seed.
        /// </summary>
        public static NpcProfile InitFromDna(NpcDnaProfile dna)
        {
            return new NpcProfile(
                competence: CompetenceProfile.Zero(),
                preference: PreferenceProfile.FromSeeds(dna.Preferences.Seeds),
                obligation: ObligationProfile.FromSeeds(dna.ObligationFrame.Seeds)
            );
        }
    }
}
