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
    /// Resources. Se il progetto contiene solo il file sorgente <see cref="Font"/>,
    /// il provider crea una copia TMP runtime e la tiene in cache.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>OfficialFont</b>: carica e mantiene in cache il font ufficiale IBM Plex Medium.</item>
    ///   <item><b>ApplyOfficialFont</b>: applica il font a una label TextMeshProUGUI o TextMeshPro world-space.</item>
    ///   <item><b>TryLoadOfficialFont</b>: cerca path Resources compatibili con il naming previsto.</item>
    ///   <item><b>TryCreateFontAssetFromFont</b>: converte il .ttf IBM Plex in TMP_FontAsset runtime.</item>
    /// </list>
    /// </summary>
    public static class ArcGraphUiFontProvider
    {
        private const string RegularFontResourcePath = "ArcGraph/UI/fonts/IBMPlex/IBMPlexSans-Regular";
        private const string MediumFontResourcePath = "ArcGraph/UI/fonts/IBMPlex/IBMPlexSans-Medium";
        private const string SemiBoldFontResourcePath = "ArcGraph/UI/fonts/IBMPlex/IBMPlexSans-SemiBold";
        private const string BoldFontResourcePath = "ArcGraph/UI/fonts/IBMPlex/IBMPlexSans-Bold";

        private static readonly string[] RegularFallbackFontResourcePaths =
        {
            "ArcGraph/UI/Fonts/IBMPlex/IBMPlexSans-Regular",
            "ArcGraph/UI/fonts/IBMPlexSans-Regular",
            "ArcGraph/UI/Fonts/IBMPlexSans-Regular"
        };

        private static readonly string[] MediumFallbackFontResourcePaths =
        {
            "ArcGraph/UI/Fonts/IBMPlex/IBMPlexSans-Medium",
            "ArcGraph/UI/fonts/IBMPlexSans-Medium",
            "ArcGraph/UI/Fonts/IBMPlexSans-Medium",
            "ArcGraph/UI/Fonts/IBMPlexSans",
            "ArcGraph/UI/Fonts/IBM Plex Sans SDF",
            "ArcGraph/UI/Fonts/IBM_Plex_Sans_SDF"
        };

        private static readonly string[] SemiBoldFallbackFontResourcePaths =
        {
            "ArcGraph/UI/Fonts/IBMPlex/IBMPlexSans-SemiBold",
            "ArcGraph/UI/fonts/IBMPlexSans-SemiBold",
            "ArcGraph/UI/Fonts/IBMPlexSans-SemiBold"
        };

        private static readonly string[] BoldFallbackFontResourcePaths =
        {
            "ArcGraph/UI/Fonts/IBMPlex/IBMPlexSans-Bold",
            "ArcGraph/UI/fonts/IBMPlexSans-Bold",
            "ArcGraph/UI/Fonts/IBMPlexSans-Bold"
        };

        private static TMP_FontAsset _regularFont;
        private static TMP_FontAsset _mediumFont;
        private static TMP_FontAsset _semiBoldFont;
        private static TMP_FontAsset _boldFont;
        private static bool _regularFontLookupCompleted;
        private static bool _mediumFontLookupCompleted;
        private static bool _semiBoldFontLookupCompleted;
        private static bool _boldFontLookupCompleted;

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
                return MediumFont != null ? MediumFont : RegularFont;
            }
        }

        private static TMP_FontAsset RegularFont
        {
            get
            {
                if (!_regularFontLookupCompleted)
                {
                    _regularFont = TryLoadOfficialFont(RegularFontResourcePath, RegularFallbackFontResourcePaths, "IBMPlexSans-Regular_RuntimeTMP");
                    _regularFontLookupCompleted = true;
                }

                return _regularFont;
            }
        }

        private static TMP_FontAsset MediumFont
        {
            get
            {
                if (!_mediumFontLookupCompleted)
                {
                    _mediumFont = TryLoadOfficialFont(MediumFontResourcePath, MediumFallbackFontResourcePaths, "IBMPlexSans-Medium_RuntimeTMP");
                    _mediumFontLookupCompleted = true;
                }

                return _mediumFont;
            }
        }

        private static TMP_FontAsset SemiBoldFont
        {
            get
            {
                if (!_semiBoldFontLookupCompleted)
                {
                    _semiBoldFont = TryLoadOfficialFont(SemiBoldFontResourcePath, SemiBoldFallbackFontResourcePaths, "IBMPlexSans-SemiBold_RuntimeTMP");
                    _semiBoldFontLookupCompleted = true;
                }

                return _semiBoldFont;
            }
        }

        private static TMP_FontAsset BoldFont
        {
            get
            {
                if (!_boldFontLookupCompleted)
                {
                    _boldFont = TryLoadOfficialFont(BoldFontResourcePath, BoldFallbackFontResourcePaths, "IBMPlexSans-Bold_RuntimeTMP");
                    _boldFontLookupCompleted = true;
                }

                return _boldFont;
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
            ApplyOfficialFontToText(label);
        }

        // =============================================================================
        // ApplyOfficialFont
        // =============================================================================
        /// <summary>
        /// <para>
        /// Applica il font ufficiale a una label TextMeshPro world-space.
        /// </para>
        /// </summary>
        public static void ApplyOfficialFont(TextMeshPro label)
        {
            ApplyOfficialFontToText(label);
        }

        private static void ApplyOfficialFontToText(TMP_Text label)
        {
            if (label == null)
                return;

            FontStyles requestedStyle = label.fontStyle;
            TMP_FontAsset font = ResolveFontForStyle(requestedStyle);
            if (font == null)
                return;

            // L'assegnazione resta confinata alla label: non cambia materiali
            // condivisi, non forza stili globali TMP e non produce mutazioni fuori
            // dalla gerarchia UI ArcGraph che sta creando il testo.
            label.font = font;

            // Se usiamo gia' il file SemiBold/Bold reale, togliamo il flag Bold
            // sintetico di TMP. Il falso grassetto e' una delle cause della resa
            // impastata sui testi piccoli del RightInspector e del menu hover.
            if ((requestedStyle & FontStyles.Bold) != 0)
                label.fontStyle = requestedStyle & ~FontStyles.Bold;
        }

        // =============================================================================
        // ResolveFontForStyle
        // =============================================================================
        /// <summary>
        /// <para>
        /// Sceglie il peso IBM Plex corretto in base allo stile richiesto dalla label.
        /// </para>
        /// </summary>
        private static TMP_FontAsset ResolveFontForStyle(FontStyles requestedStyle)
        {
            bool wantsBold = (requestedStyle & FontStyles.Bold) != 0;
            if (wantsBold)
                return SemiBoldFont != null ? SemiBoldFont : BoldFont != null ? BoldFont : OfficialFont;

            return OfficialFont;
        }

        // =============================================================================
        // TryLoadOfficialFont
        // =============================================================================
        /// <summary>
        /// <para>
        /// Cerca un peso IBM Plex specifico nei path Resources previsti.
        /// </para>
        /// </summary>
        private static TMP_FontAsset TryLoadOfficialFont(
            string primaryResourcePath,
            string[] fallbackResourcePaths,
            string runtimeAssetName)
        {
            TMP_FontAsset primaryFont = Resources.Load<TMP_FontAsset>(primaryResourcePath);
            if (primaryFont != null)
                return primaryFont;

            TMP_FontAsset primarySourceFont = TryCreateFontAssetFromFont(primaryResourcePath, runtimeAssetName);
            if (primarySourceFont != null)
                return primarySourceFont;

            for (int i = 0; i < fallbackResourcePaths.Length; i++)
            {
                TMP_FontAsset fallbackFont = Resources.Load<TMP_FontAsset>(fallbackResourcePaths[i]);
                if (fallbackFont != null)
                    return fallbackFont;

                TMP_FontAsset fallbackSourceFont = TryCreateFontAssetFromFont(fallbackResourcePaths[i], runtimeAssetName);
                if (fallbackSourceFont != null)
                    return fallbackSourceFont;
            }

            return null;
        }

        // =============================================================================
        // TryCreateFontAssetFromFont
        // =============================================================================
        /// <summary>
        /// <para>
        /// Carica un file font sorgente da Resources e lo converte in font TMP runtime.
        /// </para>
        /// </summary>
        private static TMP_FontAsset TryCreateFontAssetFromFont(
            string resourcePath,
            string runtimeAssetName)
        {
            Font sourceFont = Resources.Load<Font>(resourcePath);
            if (sourceFont == null)
                return null;

            // Il progetto ha importato IBM Plex come .ttf sotto Resources. Questa
            // conversione permette alla UI UGUI di usarlo subito senza forzare in
            // questa patch la creazione o il versionamento di asset grafici/.meta.
            TMP_FontAsset runtimeFontAsset = TMP_FontAsset.CreateFontAsset(sourceFont);
            if (runtimeFontAsset != null)
            {
                runtimeFontAsset.name = runtimeAssetName;
                runtimeFontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            }

            return runtimeFontAsset;
        }
    }
}
