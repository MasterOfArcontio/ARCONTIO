namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphLayerId
    // =============================================================================
    /// <summary>
    /// <para>
    /// Identifica i layer grafici logici che il futuro sistema <c>arcgraph</c>
    /// dovra' coordinare.
    /// </para>
    ///
    /// <para><b>Principio architetturale: presentazione stratificata</b></para>
    /// <para>
    /// La mappa di ARCONTIO non deve piu' essere trattata come una cella con un
    /// solo contenuto visuale. Ogni cella puo' avere terreno, acqua, vegetazione,
    /// oggetti, attori, effetti e overlay diagnostici. Questa enum non renderizza
    /// nulla: offre soltanto un vocabolario stabile per ordinare i futuri moduli
    /// grafici senza legarli al sistema provvisorio <c>MapGrid</c>.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Terrain</b>: terreno, pavimenti e transizioni base.</item>
    ///   <item><b>Water</b>: liquidi e profondita' visuale futura.</item>
    ///   <item><b>Vegetation</b>: erba diffusa, piante e crescita visuale.</item>
    ///   <item><b>Object</b>: strutture, porte, letti, banchi e ostacoli.</item>
    ///   <item><b>Item</b>: risorse depositate e oggetti trasportabili.</item>
    ///   <item><b>Actor</b>: NPC, animali e creature.</item>
    ///   <item><b>Effect</b>: fuoco, fumo, particelle e segnali locali.</item>
    ///   <item><b>Light</b>: luce globale, luce locale e oscuramento.</item>
    ///   <item><b>Weather</b>: pioggia, neve, vento visuale e overlay ambientali.</item>
    ///   <item><b>Debug</b>: diagnostica, FOV, landmark, selection e strumenti operatore.</item>
    /// </list>
    /// </summary>
    public enum ArcGraphLayerId
    {
        None = 0,
        Terrain = 10,
        Water = 20,
        Vegetation = 30,
        Object = 40,
        Item = 50,
        Actor = 60,
        Effect = 70,
        Light = 80,
        Weather = 90,
        Debug = 100
    }
}
