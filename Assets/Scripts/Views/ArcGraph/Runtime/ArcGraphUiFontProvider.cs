using TMPro;
using UnityEngine;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphUiFontProvider
    // =============================================================================
    /// <summary>
    /// <para>
    /// Punto unico runtime per applicare il font ufficiale della UI ArcGraph ai testi
    /// TextMeshPro creati da script.
    /// </para>
    ///
    /// <para><b>Principio architetturale: stile centralizzato senza dipendenze world-side</b></para>
    /// <para>
    /// La UI ArcGraph viene costruita progressivamente con prefab e componenti UGUI.
    /// In questa fase esistono ancora alcuni elementi creati a runtime da script:
    /// questo provider evita che ogni pannello decida il font da solo. Il componente
    /// non legge la simulazione, non accede al World e non modifica dati runtime:
    /// assegna soltanto un <see cref="TMP_FontAsset"/> se l'asset e' disponibile in
    /// Resources.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>OfficialFont</b>: carica e mantiene in cache il font ufficiale IBM Plex.</item>
    ///   <item><b>ApplyOfficialFont</b>: applica il font a una label TextMeshProUGUI.</item>
    ///   <item><b>TryLoadOfficialFont</b>: cerca path Resources compatibili con il naming previsto.</item>
    /// </list>
    /// </summary>
    public static class ArcGraphUiFontProvider
    {
        private const string PrimaryFontResourcePath = "ArcGraph/UI/Fonts/IBMPlex";
        private static readonly string[] FallbackFontResourcePaths =
        {
            "ArcGraph/UI/Fonts/IBMPlexSans",
            "ArcGraph/UI/Fonts/IBM Plex Sans SDF",
            "ArcGraph/UI/Fonts/IBM_Plex_Sans_SDF"
        };

        private static TMP_FontAsset _officialFont;
        private static bool _fontLookupCompleted;

        // =============================================================================
        // OfficialFont
        // =============================================================================
        /// <summary>
        /// <para>
        /// Restituisce il font ufficiale IBM Plex se il relativo TMP_FontAsset e'
        /// stato importato sotto Resources.
        /// </para>
        /// </summary>
        public static TMP_FontAsset OfficialFont
        {
            get
            {
                if (!_fontLookupCompleted)
                {
                    _officialFont = TryLoadOfficialFont();
                    _fontLookupCompleted = true;
                }

                return _officialFont;
            }
        }

        // =============================================================================
        // ApplyOfficialFont
        // =============================================================================
        /// <summary>
        /// <para>
        /// Applica il font ufficiale a una label UGUI se l'asset e' disponibile.
        /// </para>
        /// </summary>
        public static void ApplyOfficialFont(TextMeshProUGUI label)
        {
            if (label == null)
                return;

            TMP_FontAsset font = OfficialFont;
            if (font == null)
                return;

            // L'assegnazione resta confinata alla label: non cambia materiali
            // condivisi, non forza stili globali TMP e non produce mutazioni fuori
            // dalla gerarchia UI ArcGraph che sta creando il testo.
            label.font = font;
        }

        // =============================================================================
        // TryLoadOfficialFont
        // =============================================================================
        /// <summary>
        /// <para>
        /// Cerca il TMP FontAsset IBM Plex nei path Resources previsti per ArcGraph.
        /// </para>
        /// </summary>
        private static TMP_FontAsset TryLoadOfficialFont()
        {
            TMP_FontAsset primaryFont = Resources.Load<TMP_FontAsset>(PrimaryFontResourcePath);
            if (primaryFont != null)
                return primaryFont;

            for (int i = 0; i < FallbackFontResourcePaths.Length; i++)
            {
                TMP_FontAsset fallbackFont = Resources.Load<TMP_FontAsset>(FallbackFontResourcePaths[i]);
                if (fallbackFont != null)
                    return fallbackFont;
            }

            return null;
        }
    }
}
