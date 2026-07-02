using System;

namespace Arcontio.Core
{
    // =============================================================================
    // BiologicalResourceSearchFromLandmarkEvent
    // =============================================================================
    /// <summary>
    /// <para>
    /// Evento soggettivo prodotto quando un NPC esegue una ricerca operativa di una
    /// risorsa biologica partendo da un landmark biologico noto.
    /// </para>
    ///
    /// <para><b>Principio architetturale: memoria di azione, non query onnisciente</b></para>
    /// <para>
    /// L'evento non nasce dalle query read-only F/G e non interroga la Biosfera.
    /// Rappresenta invece il fatto cognitivo-operativo "questo NPC ha cercato il
    /// prodotto X usando questo landmark come ancora". Per questo motivo e' gia'
    /// legato a un singolo attore e non deve essere ridistribuito ai testimoni.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>ActorNpcId</b>: NPC che ha compiuto la ricerca.</item>
    ///   <item><b>LandmarkNodeId</b>: landmark biologico usato come ancora.</item>
    ///   <item><b>LandmarkKind</b>: tipo compatto del landmark, filtrato dalla rule.</item>
    ///   <item><b>ProductKey</b>: risorsa biologica cercata, normalizzata in minuscolo.</item>
    ///   <item><b>CellX/CellY</b>: cella soggettiva associata al landmark o alla ricerca.</item>
    ///   <item><b>SearchQuality01</b>: qualita' della ricerca/azione nel range 0..1.</item>
    /// </list>
    /// </summary>
    public sealed class BiologicalResourceSearchFromLandmarkEvent : IWorldEvent
    {
        public readonly int ActorNpcId;
        public readonly int LandmarkNodeId;
        public readonly LandmarkRegistry.LandmarkKind LandmarkKind;
        public readonly string ProductKey;
        public readonly int CellX;
        public readonly int CellY;
        public readonly float SearchQuality01;

        // =============================================================================
        // BiologicalResourceSearchFromLandmarkEvent
        // =============================================================================
        /// <summary>
        /// <para>
        /// Costruisce l'evento normalizzando solo i dati soggettivi che servono alla
        /// memoria. Non valida l'esistenza del landmark o del prodotto: quella resta
        /// responsabilita' del producer operativo futuro e della memory rule.
        /// </para>
        /// </summary>
        public BiologicalResourceSearchFromLandmarkEvent(
            int actorNpcId,
            int landmarkNodeId,
            LandmarkRegistry.LandmarkKind landmarkKind,
            string productKey,
            int cellX,
            int cellY,
            float searchQuality01)
        {
            ActorNpcId = actorNpcId;
            LandmarkNodeId = landmarkNodeId;
            LandmarkKind = landmarkKind;

            // Il productKey resta una chiave catalogo leggibile, ma viene
            // normalizzato qui per evitare che "Berry" e "berry" generino trace
            // distinte o hash diversi nella memoria.
            ProductKey = string.IsNullOrWhiteSpace(productKey)
                ? string.Empty
                : productKey.Trim().ToLowerInvariant();

            CellX = cellX;
            CellY = cellY;

            // Qualita' minima difensiva: una ricerca prodotta dal job e' comunque
            // un'esperienza soggettiva valida, anche se il producer passa zero.
            SearchQuality01 = searchQuality01 < 0.05f ? 0.05f : Math.Min(1f, searchQuality01);
        }
    }
}
