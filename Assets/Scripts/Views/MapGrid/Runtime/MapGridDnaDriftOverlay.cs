using UnityEngine;
using Arcontio.Core;

namespace Arcontio.View.MapGrid
{
    /// <summary>
    /// MapGridDnaDriftOverlay — sezione "DNA DRIFT" nella card NPC.
    ///
    /// Mostra per ogni asse (PREFERENZE / COMPETENZA / OBBLIGO):
    ///   - una riga per ogni dominio attivo (Agriculture → Exploration)
    ///   - una barra colorata che rappresenta la distanza DNA ↔ NpcProfile
    ///     verde (0) → giallo (0.5) → rosso (1)
    ///   - etichetta testuale del dominio e valore numerico
    ///   - riga totale pesata in fondo a ogni asse
    ///
    /// Il pannello è posizionato immediatamente sotto il tooltip esistente
    /// (che è uGUI Canvas-based). Questo overlay usa IMGUI (OnGUI) come il
    /// MapGridRuntimeDevToolsOverlay — stessa convenzione del progetto.
    ///
    /// Lifecycle:
    ///   - Show(dna, profile, screenPos) → visibile al prossimo OnGUI
    ///   - Hide()                         → pannello nascosto
    ///   - Tick a ogni frame dal MapGridWorldView quando c'è un NPC hovered
    /// </summary>
    public sealed class MapGridDnaDriftOverlay : MonoBehaviour
    {
        // ── Costanti layout ───────────────────────────────────────────────────
        private const float PanelW          = 560f;
        private const float HeaderH         = 22f;
        private const float AxisTitleH      = 20f;
        private const float RowH            = 18f;
        private const float TotalRowH       = 20f;
        private const float BarX            = 180f;  // x da sinistra del pannello dove inizia la barra
        private const float BarW            = 240f;  // larghezza massima barra
        private const float BarH            = 12f;
        private const float LabelW          = 170f;
        private const float ValueLabelW     = 48f;
        private const float PaddingX        = 10f;
        private const float PaddingY        = 8f;

        // 8 domini validi (Agriculture..Exploration) × 3 assi + header + totali + spaziature
        private const int   DomainsCount    = 8; // DomainKind.COUNT - 1
        private const float AxisBlockH      = AxisTitleH + DomainsCount * RowH + TotalRowH + 6f;
        private const float PanelH          = PaddingY * 2 + HeaderH + 8f + AxisBlockH * 3 + 12f;

        private const float GapBelowTooltip = 4f;

        // ── Stato visibilità ──────────────────────────────────────────────────
        private bool              _visible;
        private Vector2           _anchorScreen; // angolo in alto a sinistra del tooltip
        private NpcDnaProfile     _dna;
        private NpcProfile        _profile;
        private DnaDistanceResult _result;

        // ── Texture cached (1×1) ─────────────────────────────────────────────
        private Texture2D _texGray;
        private Texture2D _texGreen;
        private Texture2D _texYellow;
        private Texture2D _texRed;
        private Texture2D _texBackground;

        // ── GUIStyle cached ───────────────────────────────────────────────────
        private bool     _stylesInit;
        private GUIStyle _styleTitle;
        private GUIStyle _styleAxisTitle;
        private GUIStyle _styleDomain;
        private GUIStyle _styleValue;
        private GUIStyle _styleTotal;

        // ── Nomi domini (ordinati come DomainKind 1..8) ───────────────────────
        private static readonly string[] DomainNames =
        {
            "Agricoltura",
            "Costruzione",
            "Sicurezza",
            "Artigianato",
            "Magazzino",
            "Governo",
            "Sociale",
            "Esplorazione"
        };

        // ─────────────────────────────────────────────────────────────────────
        // Unity lifecycle
        // ─────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            _texGray       = MakeTex(new Color(0.45f, 0.45f, 0.45f, 1f));
            _texGreen      = MakeTex(new Color(0.22f, 0.75f, 0.22f, 1f));
            _texYellow     = MakeTex(new Color(0.90f, 0.78f, 0.10f, 1f));
            _texRed        = MakeTex(new Color(0.85f, 0.18f, 0.18f, 1f));
            _texBackground = MakeTex(new Color(0.05f, 0.05f, 0.08f, 0.90f));
        }

        private void OnDestroy()
        {
            Destroy(_texGray);
            Destroy(_texGreen);
            Destroy(_texYellow);
            Destroy(_texRed);
            Destroy(_texBackground);
        }

        private void OnGUI()
        {
            if (!_visible || _dna.Identity.Name == null || _profile == null)
                return;

            EnsureStyles();

            // Posizionamento: sotto il tooltip uGUI.
            //
            // Sistemi di coordinate:
            //   _anchorScreen = PointerScreenPos da InputSystem → y=0 in BASSO (screen space)
            //   IMGUI GUI.DrawTexture/Label         → y=0 in ALTO  (GUI space)
            //
            // Il tooltip uGUI ha pivot=(0,0) e anchoredPosition=pointerPos:
            //   → il suo bordo INFERIORE è al cursore in screen space
            //   → in GUI space corrisponde a:  Screen.height - _anchorScreen.y
            //
            // Il pannello DRIFT parte appena sotto quel bordo (+ gap).
            float panelX = _anchorScreen.x;
            float panelY = Screen.height - _anchorScreen.y + GapBelowTooltip;

            // Clamp per restare nel viewport
            panelX = Mathf.Clamp(panelX, 0f, Screen.width  - PanelW);
            panelY = Mathf.Clamp(panelY, 0f, Screen.height - PanelH);

            var panelRect = new Rect(panelX, panelY, PanelW, PanelH);

            // Sfondo
            GUI.DrawTexture(panelRect, _texBackground, ScaleMode.StretchToFill);

            float cursor = panelY + PaddingY;

            // Intestazione
            GUI.Label(new Rect(panelX + PaddingX, cursor, PanelW - PaddingX * 2, HeaderH),
                      "DNA DRIFT", _styleTitle);
            cursor += HeaderH + 8f;

            // ── Asse PREFERENZE ────────────────────────────────────────────────
            float prefDist = _result.PreferenceDistance;
            cursor = DrawAxis("PREFERENZE", panelX, cursor,
                _dna.Preferences.Seeds, _profile.Preference.Values, prefDist);

            // ── Asse COMPETENZA ────────────────────────────────────────────────
            // Per competenza la "distanza" è cap - current (sottoutilizzo del potenziale).
            float compDist = _result.CompetenceDistance;
            cursor = DrawAxisCompetence("COMPETENZA", panelX, cursor,
                _dna.Capacities.CompetenceCap, _profile.Competence.Values, compDist);

            // ── Asse OBBLIGO ───────────────────────────────────────────────────
            float oblDist = _result.ObligationDistance;
            cursor = DrawAxis("OBBLIGO", panelX, cursor,
                _dna.ObligationFrame.Seeds, _profile.Obligation.Values, oblDist);
        }

        // ─────────────────────────────────────────────────────────────────────
        // API pubblica
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Mostra il pannello DNA DRIFT per l'NPC correntemente hovered.
        /// screenPos: coordinate schermo (angolo superiore sinistro del tooltip uGUI).
        /// </summary>
        public void Show(NpcDnaProfile dna, NpcProfile profile, Vector2 screenPos)
        {
            _dna           = dna;
            _profile       = profile;
            _anchorScreen  = screenPos;
            _result        = NpcDnaDistance.Compute(dna, profile);
            _visible       = true;
        }

        /// <summary>Nasconde il pannello.</summary>
        public void Hide()
        {
            _visible = false;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Helpers di rendering
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Disegna un blocco asse (seeds vs current) — usato per Preferenze e Obblighi.
        /// Restituisce il nuovo cursore Y.
        /// </summary>
        private float DrawAxis(
            string axisLabel,
            float  panelX,
            float  cursorY,
            float[] seeds,
            float[] current,
            float  axisDist)
        {
            // Titolo asse
            GUI.Label(new Rect(panelX + PaddingX, cursorY, PanelW - PaddingX * 2, AxisTitleH),
                      axisLabel, _styleAxisTitle);
            cursorY += AxisTitleH;

            // Righe per dominio (Agriculture=1 .. Exploration=8)
            for (int d = 1; d < (int)DomainKind.COUNT; d++)
            {
                int   nameIdx  = d - 1;
                float seed     = (seeds   != null && d < seeds.Length)   ? seeds[d]   : 0f;
                float curr     = (current != null && d < current.Length) ? current[d] : 0f;
                float dist     = Mathf.Abs(seed - curr);

                DrawDomainRow(panelX, cursorY, DomainNames[nameIdx], seed, curr, dist);
                cursorY += RowH;
            }

            // Riga totale asse
            DrawTotalRow(panelX, cursorY, axisDist);
            cursorY += TotalRowH + 6f;

            return cursorY;
        }

        /// <summary>
        /// Variante per Competenza: distanza = cap - current (sottoutilizzo potenziale).
        /// Restituisce il nuovo cursore Y.
        /// </summary>
        private float DrawAxisCompetence(
            string axisLabel,
            float  panelX,
            float  cursorY,
            float[] caps,
            float[] current,
            float  axisDist)
        {
            GUI.Label(new Rect(panelX + PaddingX, cursorY, PanelW - PaddingX * 2, AxisTitleH),
                      axisLabel, _styleAxisTitle);
            cursorY += AxisTitleH;

            for (int d = 1; d < (int)DomainKind.COUNT; d++)
            {
                int   nameIdx  = d - 1;
                float cap      = (caps    != null && d < caps.Length)    ? caps[d]    : 1f;
                float curr     = (current != null && d < current.Length) ? current[d] : 0f;
                float dist     = Mathf.Abs(cap - curr);  // sottoutilizzo

                DrawDomainRow(panelX, cursorY, DomainNames[nameIdx], cap, curr, dist);
                cursorY += RowH;
            }

            DrawTotalRow(panelX, cursorY, axisDist);
            cursorY += TotalRowH + 6f;

            return cursorY;
        }

        private void DrawDomainRow(float panelX, float y, string domainName,
                                   float reference, float current, float dist)
        {
            float x = panelX + PaddingX;

            // Etichetta dominio
            GUI.Label(new Rect(x, y + 2f, LabelW, RowH), domainName, _styleDomain);

            // Barra distanza
            float barFill = Mathf.Clamp01(dist);
            DrawBar(new Rect(x + BarX, y + (RowH - BarH) * 0.5f, BarW, BarH),
                    barFill, _texGray);

            // Valore: "seed→curr  Δ=0.00"
            string valTxt = $"{reference:0.00}→{current:0.00}";
            GUI.Label(new Rect(x + BarX + BarW + 6f, y, ValueLabelW + 30f, RowH),
                      valTxt, _styleValue);
        }

        private void DrawTotalRow(float panelX, float y, float total)
        {
            float x = panelX + PaddingX;

            GUI.Label(new Rect(x, y + 2f, LabelW, TotalRowH), "Totale asse", _styleTotal);

            float barFill = Mathf.Clamp01(total);
            DrawBar(new Rect(x + BarX, y + (TotalRowH - BarH) * 0.5f, BarW, BarH),
                    barFill, null); // null = usa colore dinamico

            string valTxt = total.ToString("0.000");
            GUI.Label(new Rect(x + BarX + BarW + 6f, y, ValueLabelW, TotalRowH),
                      valTxt, _styleTotal);
        }

        /// <summary>
        /// Disegna una barra con riempimento [0,1].
        /// Se bgTex è null usa colore dinamico (verde→giallo→rosso); altrimenti usa bgTex come sfondo.
        /// </summary>
        private void DrawBar(Rect r, float fill, Texture2D bgTex)
        {
            // Sfondo grigio scuro
            GUI.DrawTexture(new Rect(r.x, r.y, r.width, r.height),
                            _texGray, ScaleMode.StretchToFill);

            if (fill > 0.001f)
            {
                Texture2D fillTex = bgTex != null ? bgTex : PickBarColor(fill);
                GUI.DrawTexture(new Rect(r.x, r.y, r.width * fill, r.height),
                                fillTex, ScaleMode.StretchToFill);
            }
        }

        private Texture2D PickBarColor(float fill)
        {
            if (fill < 0.35f) return _texGreen;
            if (fill < 0.65f) return _texYellow;
            return _texRed;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Styles e texture
        // ─────────────────────────────────────────────────────────────────────

        private void EnsureStyles()
        {
            if (_stylesInit) return;
            _stylesInit = true;

            _styleTitle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 13,
                fontStyle = FontStyle.Bold,
                normal    = { textColor = new Color(0.95f, 0.85f, 0.40f) }
            };

            _styleAxisTitle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 11,
                fontStyle = FontStyle.Bold,
                normal    = { textColor = new Color(0.75f, 0.90f, 1.00f) }
            };

            _styleDomain = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                normal   = { textColor = new Color(0.85f, 0.85f, 0.85f) }
            };

            _styleValue = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                normal   = { textColor = new Color(0.70f, 0.70f, 0.70f) }
            };

            _styleTotal = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 11,
                fontStyle = FontStyle.Bold,
                normal    = { textColor = new Color(1.00f, 1.00f, 1.00f) }
            };
        }

        private static Texture2D MakeTex(Color c)
        {
            var t = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            t.SetPixel(0, 0, c);
            t.Apply();
            return t;
        }
    }
}
