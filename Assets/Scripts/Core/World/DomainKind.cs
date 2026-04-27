namespace Arcontio.Core
{
    /// <summary>
    /// Dominio di attività dell'NPC.
    /// Usato come indice nei vettori per-dominio (preferenze, competenze, obblighi).
    /// COUNT è una sentinella: rappresenta la dimensione degli array indicizzati per dominio.
    /// </summary>
    public enum DomainKind
    {
        None         = 0,
        Agriculture  = 1,  // produzione cibo, coltivazione
        Construction = 2,  // costruzione, riparazione strutture
        Security     = 3,  // guardia, difesa, pattuglia
        Crafting     = 4,  // produzione manufatti
        Storage      = 5,  // gestione magazzino e risorse
        Governance   = 6,  // ruoli istituzionali, norme
        Social       = 7,  // interazione, diplomazia, mediazione
        Exploration  = 8,  // scouting, esplorazione territorio

        COUNT = 9  // sentinella — dimensione degli array per-dominio
    }
}
