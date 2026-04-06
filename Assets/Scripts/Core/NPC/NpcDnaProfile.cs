using System;

namespace Arcontio.Core
{
    // ─────────────────────────────────────────────────────────────────────────
    // NpcDnaProfile — struttura immutabile per-NPC
    //
    // Contiene solo dati strutturali e seed iniziali.
    // NON cambia mai a runtime dopo la creazione dell'NPC.
    // Tutto il comportamento emergente (stress, insoddisfazione, drift)
    // dipende dal confronto tra NpcDnaProfile e NpcProfile (sessione 2).
    //
    // Nota sull'overlap con strutture esistenti:
    // - NpcCore (NPCComponents.cs) è un placeholder che verrà migrato in sessione 5.
    //   I suoi campi (Name, Charisma, Decisiveness, Empathy, Ambition) mappano
    //   rispettivamente su Identity.Name, CognitiveModulators e Dispositions qui sotto.
    // - PersonalityMemoryParams.Resilience01 e CognitiveModulators.Resilience01 sono
    //   lo stesso concetto. In sessione 5, PersonalityMemoryParams.Resilience01 verrà
    //   inizializzato da questo campo.
    // ─────────────────────────────────────────────────────────────────────────


    // ── Identità ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Dati identificativi fissi dell'NPC.
    /// </summary>
    public readonly struct NpcIdentity
    {
        /// <summary>Nome del personaggio.</summary>
        public readonly string Name;

        /// <summary>
        /// Etichetta di origine culturale/geografica (es. "nomade", "contadino_nord").
        /// Usata per determinare ObligationFrame culturale e preferenze iniziali.
        /// </summary>
        public readonly string OriginTag;

        /// <summary>Tick di simulazione in cui l'NPC è stato creato/nato.</summary>
        public readonly int BirthTick;

        /// <summary>Note narrative opzionali di background. Può essere null.</summary>
        public readonly string NarrativeNotes;

        public NpcIdentity(string name, string originTag, int birthTick, string narrativeNotes = null)
        {
            Name           = name;
            OriginTag      = originTag;
            BirthTick      = birthTick;
            NarrativeNotes = narrativeNotes;
        }
    }


    // ── Capacità fisiche e cognitive ─────────────────────────────────────────

    /// <summary>
    /// Capacità fisiche e potenziale cognitivo dell'NPC.
    /// Determinano il cap massimo raggiungibile per ogni dominio in CompetenceProfile.
    /// </summary>
    public readonly struct NpcCapacities
    {
        /// <summary>Forza fisica. Influenza lavori manuali pesanti, combattimento.</summary>
        public readonly float Strength01;

        /// <summary>Resistenza fisica. Influenza la velocità di accumulo fatica.</summary>
        public readonly float Endurance01;

        /// <summary>Agilità/mobilità. Influenza velocità di movimento e reattività.</summary>
        public readonly float Agility01;

        /// <summary>Intelligenza strutturale di base. Influenza la velocità di apprendimento.</summary>
        public readonly float BaseIntelligence01;

        /// <summary>
        /// Cap massimo di competenza per dominio [DomainKind.COUNT].
        /// Un NPC non può superare questo valore in CompetenceProfile per ciascun dominio,
        /// indipendentemente dall'esperienza accumulata.
        /// </summary>
        public readonly float[] CompetenceCap;

        public NpcCapacities(
            float strength01,
            float endurance01,
            float agility01,
            float baseIntelligence01,
            float[] competenceCap)
        {
            Strength01        = strength01;
            Endurance01       = endurance01;
            Agility01         = agility01;
            BaseIntelligence01 = baseIntelligence01;
            CompetenceCap     = competenceCap;
        }
    }


    // ── Preferenze iniziali ───────────────────────────────────────────────────

    /// <summary>
    /// Seed iniziali per PreferenceProfile (sessione 2+).
    /// Rappresentano la predisposizione naturale verso ogni dominio di attività.
    /// Il profilo reale (che può cambiare nel tempo) viene inizializzato da questi valori.
    /// </summary>
    public readonly struct NpcPreferenceSeeds
    {
        /// <summary>
        /// Predisposizione per dominio [DomainKind.COUNT], valori 0-1.
        /// 0 = nessuna affinità, 1 = affinità massima.
        /// </summary>
        public readonly float[] Seeds;

        public NpcPreferenceSeeds(float[] seeds)
        {
            Seeds = seeds;
        }
    }


    // ── Disposizioni caratteriali ─────────────────────────────────────────────

    /// <summary>
    /// Disposizioni caratteriali stabili dell'NPC.
    /// Influenzano le reazioni emotive e le scelte sociali.
    ///
    /// Nota: NpcCore.Empathy mappa su Cooperativeness01;
    ///       NpcCore.Charisma verrà assorbito in CognitiveModulators.Sociability01.
    /// </summary>
    public readonly struct NpcDispositions
    {
        /// <summary>0 = estroverso (cerca interazione), 1 = introverso (preferisce solitudine).</summary>
        public readonly float Introversion01;

        /// <summary>0 = pacifico, 1 = aggressivo/conflittuale.</summary>
        public readonly float Aggressiveness01;

        /// <summary>0 = apatico, 1 = molto curioso verso l'ignoto.</summary>
        public readonly float Curiosity01;

        /// <summary>0 = individualista, 1 = fortemente cooperativo.</summary>
        public readonly float Cooperativeness01;

        public NpcDispositions(
            float introversion01,
            float aggressiveness01,
            float curiosity01,
            float cooperativeness01)
        {
            Introversion01     = introversion01;
            Aggressiveness01   = aggressiveness01;
            Curiosity01        = curiosity01;
            Cooperativeness01  = cooperativeness01;
        }
    }


    // ── Posizione sociale iniziale ────────────────────────────────────────────

    /// <summary>
    /// Posizione sociale di partenza dell'NPC nel mondo.
    /// Può evolvere a runtime, ma qui rappresenta lo stato al momento della creazione.
    /// </summary>
    public readonly struct NpcSocialPosition
    {
        /// <summary>Classe sociale di appartenenza iniziale: "lower" | "middle" | "upper".</summary>
        public readonly string SocialClass;

        /// <summary>Id del gruppo iniziale. -1 = nessun gruppo.</summary>
        public readonly int InitialGroupId;

        /// <summary>Reputazione iniziale nella comunità, 0-1.</summary>
        public readonly float InitialReputation01;

        public NpcSocialPosition(string socialClass, int initialGroupId, float initialReputation01)
        {
            SocialClass          = socialClass;
            InitialGroupId       = initialGroupId;
            InitialReputation01  = initialReputation01;
        }
    }


    // ── Quadro degli obblighi ─────────────────────────────────────────────────

    /// <summary>
    /// Seed per ObligationProfile (sessione 2+).
    /// Rappresenta quanto l'NPC sente il dovere verso ogni dominio,
    /// modellato dalla cultura di origine.
    /// </summary>
    public readonly struct NpcObligationFrame
    {
        /// <summary>
        /// Senso di obbligo per dominio [DomainKind.COUNT], valori 0-1.
        /// 0 = nessun obbligo percepito, 1 = obbligo massimo.
        /// </summary>
        public readonly float[] Seeds;

        /// <summary>
        /// Etichetta culturale di provenienza (es. "tribale", "feudale", "mercantile").
        /// Usata per selezionare regole di ObligationFrame culturale appropriate.
        /// </summary>
        public readonly string CulturalOrigin;

        public NpcObligationFrame(float[] seeds, string culturalOrigin)
        {
            Seeds          = seeds;
            CulturalOrigin = culturalOrigin;
        }
    }


    // ── Soglie comportamentali ────────────────────────────────────────────────

    /// <summary>
    /// Soglie che determinano quando l'NPC cambia modalità comportamentale.
    /// Variano per individuo: alcuni NPC entrano in panico prima, altri resistono.
    /// </summary>
    public readonly struct NpcThresholds
    {
        /// <summary>
        /// Soglia di allerta bisogno (0-1).
        /// Quando un bisogno supera questa soglia, l'NPC inizia ad anticipare
        /// l'approvvigionamento anche se non è ancora critico.
        /// </summary>
        public readonly float NeedAlert01;

        /// <summary>
        /// Soglia critica bisogno (0-1).
        /// Quando un bisogno supera questa soglia, la sopravvivenza diventa
        /// il floor assoluto e sovrascrive ogni altra priorità.
        /// </summary>
        public readonly float NeedCritical01;

        /// <summary>
        /// Soglia di insoddisfazione ruolo (0-1).
        /// Quando il gap tra PreferenceProfile e ruolo assegnato supera questa soglia,
        /// viene emesso un trigger narrativo.
        /// </summary>
        public readonly float RoleDissatisfaction01;

        /// <summary>
        /// Soglia di stress critico (0-1).
        /// Al superamento, l'NPC può intraprendere azioni disperate (furto, fuga, collasso).
        /// </summary>
        public readonly float StressCritical01;

        public NpcThresholds(
            float needAlert01,
            float needCritical01,
            float roleDissatisfaction01,
            float stressCritical01)
        {
            NeedAlert01           = needAlert01;
            NeedCritical01        = needCritical01;
            RoleDissatisfaction01 = roleDissatisfaction01;
            StressCritical01      = stressCritical01;
        }
    }


    // ── Modulatori cognitivi ──────────────────────────────────────────────────

    /// <summary>
    /// Modulatori cognitivi stabili: influenzano la valutazione delle opzioni
    /// e il comportamento decisionale nelle formule di scoring (v0.05+).
    ///
    /// Nota: Resilience01 è lo stesso concetto di PersonalityMemoryParams.Resilience01.
    /// In sessione 5, PersonalityMemoryParams.Resilience01 verrà inizializzato
    /// direttamente da questo campo invece di usare il default statico.
    ///
    /// Mappa con NpcCore (placeholder che verrà rimosso in sessione 5):
    ///   NpcCore.Charisma    → Sociability01
    ///   NpcCore.Decisiveness → inverso di Impulsivity01
    /// </summary>
    public readonly struct NpcCognitiveModulators
    {
        /// <summary>0 = molto riflessivo, 1 = molto impulsivo.</summary>
        public readonly float Impulsivity01;

        /// <summary>0 = temerario (cerca il rischio), 1 = molto prudente.</summary>
        public readonly float RiskAversion01;

        /// <summary>0 = indipendente (ignora la norma), 1 = gregario (segue il gruppo).</summary>
        public readonly float Conformism01;

        /// <summary>0 = pessimista, 1 = ottimista.</summary>
        public readonly float Optimism01;

        /// <summary>
        /// Resistenza psicologica allo stress e ai cambiamenti.
        /// 0 = fragile, 1 = molto resiliente.
        /// Vedi nota su PersonalityMemoryParams.Resilience01.
        /// </summary>
        public readonly float Resilience01;

        /// <summary>0 = solitario, 1 = fortemente socievole.</summary>
        public readonly float Sociability01;

        /// <summary>
        /// Resistenza al drift dei valori.
        /// 0 = la natura originale tende a riemergere rapidamente,
        /// 1 = i cambiamenti acquisiti a runtime si mantengono a lungo.
        /// </summary>
        public readonly float DriftResistance01;

        public NpcCognitiveModulators(
            float impulsivity01,
            float riskAversion01,
            float conformism01,
            float optimism01,
            float resilience01,
            float sociability01,
            float driftResistance01)
        {
            Impulsivity01    = impulsivity01;
            RiskAversion01   = riskAversion01;
            Conformism01     = conformism01;
            Optimism01       = optimism01;
            Resilience01     = resilience01;
            Sociability01    = sociability01;
            DriftResistance01 = driftResistance01;
        }
    }


    // ── Tratti narrativi ──────────────────────────────────────────────────────

    /// <summary>
    /// Tratti narrativi discreti dell'NPC.
    /// Usati per tag story, dialoghi, reazioni speciali.
    /// Possono coesistere più tratti (flags).
    /// </summary>
    [Flags]
    public enum NpcTraitKind : uint
    {
        None        = 0,
        Courageous  = 1 << 0,   // coraggioso
        Cowardly    = 1 << 1,   // vigliacco
        Loyal       = 1 << 2,   // leale
        Treacherous = 1 << 3,   // traditore
        Generous    = 1 << 4,   // generoso
        Greedy      = 1 << 5,   // avido
        Curious     = 1 << 6,   // curioso
        Incurious   = 1 << 7,   // incurioso
        Honest      = 1 << 8,   // onesto
        Deceptive   = 1 << 9,   // ingannatore
        Empathetic  = 1 << 10,  // empatico
        Callous     = 1 << 11,  // insensibile
        Ambitious   = 1 << 12,  // ambizioso
        Complacent  = 1 << 13,  // compiacente/inerte
        Disciplined = 1 << 14,  // disciplinato
        Impulsive   = 1 << 15   // impulsivo (tratto narrativo, distinto da Impulsivity01)
    }


    // ── Profilo DNA principale ────────────────────────────────────────────────

    /// <summary>
    /// NpcDnaProfile — struttura immutabile per-NPC.
    ///
    /// Aggregato di tutti i sotto-profili statici che descrivono la "natura" dell'NPC.
    /// NON contiene stato runtime: nessun valore cambia dopo la creazione.
    ///
    /// Il comportamento emergente (stress, insoddisfazione, drift) emerge dal confronto
    /// tra NpcDnaProfile (natura) e NpcProfile (stato corrente, sessione 2+).
    ///
    /// ExtensionData non è implementato: nessun caso d'uso concreto in questa versione.
    /// </summary>
    public sealed class NpcDnaProfile
    {
        public readonly NpcIdentity            Identity;
        public readonly NpcCapacities          Capacities;
        public readonly NpcPreferenceSeeds     Preferences;
        public readonly NpcDispositions        Dispositions;
        public readonly NpcSocialPosition      SocialPosition;
        public readonly NpcObligationFrame     ObligationFrame;
        public readonly NpcThresholds          Thresholds;
        public readonly NpcCognitiveModulators CognitiveModulators;
        public readonly NpcTraitKind           Traits;

        /// <summary>
        /// Tag stringa liberi per categorizzazione e filtraggio (es. "esiliato", "veterano").
        /// Può essere null o vuoto.
        /// </summary>
        public readonly string[] Tags;

        public NpcDnaProfile(
            NpcIdentity            identity,
            NpcCapacities          capacities,
            NpcPreferenceSeeds     preferences,
            NpcDispositions        dispositions,
            NpcSocialPosition      socialPosition,
            NpcObligationFrame     obligationFrame,
            NpcThresholds          thresholds,
            NpcCognitiveModulators cognitiveModulators,
            NpcTraitKind           traits,
            string[]               tags = null)
        {
            Identity            = identity;
            Capacities          = capacities;
            Preferences         = preferences;
            Dispositions        = dispositions;
            SocialPosition      = socialPosition;
            ObligationFrame     = obligationFrame;
            Thresholds          = thresholds;
            CognitiveModulators = cognitiveModulators;
            Traits              = traits;
            Tags                = tags;
        }
    }
}
