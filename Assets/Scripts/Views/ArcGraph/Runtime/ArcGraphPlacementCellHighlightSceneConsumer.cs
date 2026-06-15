using Arcontio.Core;
using Arcontio.View.MapGrid;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphPlacementCellHighlightDiagnostics
    // =============================================================================
    /// <summary>
    /// <para>
    /// Diagnostica sintetica dell'overlay cella per inserimento oggetti ArcGraph.
    /// </para>
    ///
    /// <para><b>Principio architetturale: overlay osservabile e non operativo</b></para>
    /// <para>
    /// La diagnostica dichiara se il consumer ha ricevuto un frame, se il DevTool
    /// legacy era in modalita' inserimento e se la cella visuale e' stata mostrata.
    /// Non contiene riferimenti a GameObject, comandi o stato mutabile del World.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>DidReceiveFrame</b>: il consumer ha ricevuto un frame interattivo.</item>
    ///   <item><b>HighlightEnabled</b>: gate locale del consumer.</item>
    ///   <item><b>HasDevToolsOverlay</b>: presenza del pannello DevTools legacy.</item>
    ///   <item><b>PlacementPreviewActive</b>: tool F3 compatibile con inserimento oggetti.</item>
    ///   <item><b>HasValidCell</b>: cella ArcGraph valida sotto il puntatore.</item>
    ///   <item><b>DidShowHighlight</b>: sprite overlay acceso nel frame.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphPlacementCellHighlightDiagnostics
    {
        public readonly bool DidReceiveFrame;
        public readonly bool HighlightEnabled;
        public readonly bool HasDevToolsOverlay;
        public readonly bool PlacementPreviewActive;
        public readonly bool HasValidCell;
        public readonly bool DidShowHighlight;
        public readonly ArcGraphCellCoord Cell;
        public readonly string Reason;

        public ArcGraphPlacementCellHighlightDiagnostics(
            bool didReceiveFrame,
            bool highlightEnabled,
            bool hasDevToolsOverlay,
            bool placementPreviewActive,
            bool hasValidCell,
            bool didShowHighlight,
            ArcGraphCellCoord cell,
            string reason)
        {
            DidReceiveFrame = didReceiveFrame;
            HighlightEnabled = highlightEnabled;
            HasDevToolsOverlay = hasDevToolsOverlay;
            PlacementPreviewActive = placementPreviewActive;
            HasValidCell = hasValidCell;
            DidShowHighlight = didShowHighlight;
            Cell = cell;
            Reason = string.IsNullOrWhiteSpace(reason) ? "None" : reason;
        }
    }

    // =============================================================================
    // ArcGraphPlacementCellHighlightSceneConsumer
    // =============================================================================
    /// <summary>
    /// <para>
    /// Consumer scena che evidenzia la cella sotto il mouse quando i DevTools sono
    /// in modalita' inserimento oggetto.
    /// </para>
    ///
    /// <para><b>Principio architetturale: ArcGraph mostra, DevTools comandano</b></para>
    /// <para>
    /// Il componente riceve un <c>ArcGraphInteractionFrame</c> gia' calcolato dal
    /// boundary ArcGraph e legge solo un flag read-only del
    /// <c>MapGridRuntimeDevToolsOverlay</c>. Non interpreta click, non piazza muri,
    /// non modifica oggetti, non accoda comandi e non interroga il World. Serve solo
    /// a rendere leggibile dove finira' l'inserimento quando il DevTool legacy e'
    /// attivo.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>ConsumeInteractionFrame</b>: accende/spegne l'highlight in base al frame.</item>
    ///   <item><b>SetDevToolsOverlay</b>: riceve il riferimento legacy dall'installer.</item>
    ///   <item><b>EnsureHighlightRenderer</b>: crea una sola sprite runtime riusabile.</item>
    ///   <item><b>HideHighlight</b>: disattiva il visual senza distruggerlo.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphPlacementCellHighlightSceneConsumer : MonoBehaviour, IArcGraphInteractionFrameConsumer
    {
        [SerializeField] private bool highlightEnabled = true;
        [SerializeField] private MapGridRuntimeDevToolsOverlay devToolsOverlay;
        [SerializeField] private MonoBehaviour spriteResolverBehaviour;
        [SerializeField] private Camera sceneCamera;
        [SerializeField] private bool enableSceneCameraUpdateFallback = true;
        [SerializeField] private Color validPlacementColor = new Color(1f, 0.18f, 0.12f, 0.38f);
        [SerializeField] private Color previewSpriteColor = new Color(1f, 0.05f, 0.05f, 0.58f);
        [SerializeField] private float tileWorldSize = 1f;
        [SerializeField] private float zOffset = -0.035f;
        [SerializeField] private float previewZOffset = -0.045f;
        [SerializeField] private float previewObjectScale = 1f;
        [SerializeField] private int sortingOrder = 250;
        [SerializeField] private int previewSortingOrder = 260;
        [SerializeField] private Vector3 originOffset = Vector3.zero;
        [SerializeField] private string highlightObjectName = "ArcGraphPlacementCellHighlight";
        [SerializeField] private string previewObjectName = "ArcGraphPlacementObjectPreview";
        [SerializeField] private bool logDiagnostics;

        private GameObject _highlightObject;
        private SpriteRenderer _highlightRenderer;
        private Texture2D _highlightTexture;
        private Sprite _highlightSprite;
        private GameObject _previewObject;
        private SpriteRenderer _previewRenderer;
        private ArcGraphPlacementCellHighlightDiagnostics _lastDiagnostics =
            new ArcGraphPlacementCellHighlightDiagnostics(
                false,
                false,
                false,
                false,
                false,
                false,
                new ArcGraphCellCoord(0, 0, 0),
                "NotInitialized");

        public ArcGraphPlacementCellHighlightDiagnostics LastDiagnostics => _lastDiagnostics;
        public bool HighlightEnabled => highlightEnabled;

        // =============================================================================
        // Update
        // =============================================================================
        /// <summary>
        /// <para>
        /// Aggiorna opzionalmente la preview placement usando direttamente la camera
        /// di scena.
        /// </para>
        ///
        /// <para><b>Fallback pratico sullo spazio reale della scena</b></para>
        /// <para>
        /// Il normale frame interattivo ArcGraph passa dal mapper viewport/zoom, che
        /// e' corretto per HUD e selection ma puo' divergere dalla camera Unity
        /// durante il gate MapGrid/ArcGraph provvisorio. Il DevTool F3, invece,
        /// inserisce oggetti nello spazio mappa visibile. Questo fallback converte
        /// mouse -> world -> cella con la camera reale, cosi' la preview resta
        /// agganciata allo stesso spazio in cui l'utente sta piazzando muri, cibo,
        /// porte o oggetti futuri.
        /// </para>
        /// </summary>
        private void Update()
        {
            if (!enableSceneCameraUpdateFallback)
                return;

            if (TryResolveCellFromDevTools(out ArcGraphCellCoord devToolsCell, out _))
            {
                ShowPlacementPreviewAtCell(devToolsCell, CreateSyntheticInteractionFrame(devToolsCell));
                return;
            }

            if (!TryResolveCellFromSceneCamera(out ArcGraphCellCoord cell, out string reason))
            {
                HideAndStore(ArcGraphInteractionFrame.Empty(reason), false, reason);
                return;
            }

            ShowPlacementPreviewAtCell(cell, CreateSyntheticInteractionFrame(cell));
        }

        // =============================================================================
        // ConsumeInteractionFrame
        // =============================================================================
        /// <summary>
        /// <para>
        /// Consuma il frame interattivo e aggiorna la cella evidenziata.
        /// </para>
        ///
        /// <para><b>Filtro a costo basso</b></para>
        /// <para>
        /// Il metodo esce presto quando il gate e' spento, manca il DevTool, il
        /// tool corrente non inserisce oggetti o il puntatore non produce una cella
        /// valida. Solo nel caso positivo aggiorna posizione e colore dello sprite.
        /// </para>
        /// </summary>
        public void ConsumeInteractionFrame(
            ArcGraphInteractionFrame interactionFrame,
            ArcGraphInteractionSceneAdapterDiagnostics diagnostics)
        {
            if (!highlightEnabled)
            {
                HideAndStore(interactionFrame, false, "HighlightDisabled");
                return;
            }

            if (devToolsOverlay == null)
            {
                HideAndStore(interactionFrame, false, "DevToolsOverlayMissing");
                return;
            }

            if (!devToolsOverlay.IsObjectPlacementPreviewActive)
            {
                HideAndStore(interactionFrame, false, "PlacementPreviewInactive");
                return;
            }

            if (!interactionFrame.HasValidCell
                || interactionFrame.IsPointerOverUi
                || devToolsOverlay.IsPointerOverDevToolsWindow)
            {
                HideAndStore(interactionFrame, false, "CellUnavailable");
                return;
            }

            ShowPlacementPreviewAtCell(interactionFrame.Cell, interactionFrame);
        }

        // =============================================================================
        // TryResolveCellFromDevTools
        // =============================================================================
        /// <summary>
        /// <para>
        /// Prova a leggere dal DevTools legacy la cella che il tool F3 userebbe per
        /// piazzare l'oggetto corrente.
        /// </para>
        ///
        /// <para><b>Principio architetturale: una sola verita' per il placement</b></para>
        /// <para>
        /// Durante il gate MapGrid/ArcGraph la camera, lo zoom e il pan possono
        /// attraversare ancora percorsi legacy. Per evitare che la preview ArcGraph
        /// disegni su una cella diversa da quella realmente usata dal click, questo
        /// metodo preferisce la cella esposta dal DevTools. ArcGraph resta comunque
        /// passivo: legge coordinate, non invia comandi e non modifica il mondo.
        /// </para>
        /// </summary>
        private bool TryResolveCellFromDevTools(
            out ArcGraphCellCoord cell,
            out string reason)
        {
            cell = new ArcGraphCellCoord(0, 0, 0);
            reason = "None";

            if (!highlightEnabled)
            {
                reason = "HighlightDisabled";
                return false;
            }

            if (devToolsOverlay == null)
            {
                reason = "DevToolsOverlayMissing";
                return false;
            }

            if (!devToolsOverlay.IsObjectPlacementPreviewActive)
            {
                reason = "PlacementPreviewInactive";
                return false;
            }

            if (!devToolsOverlay.TryGetObjectPlacementPreviewCell(out int x, out int y))
            {
                reason = "DevToolsPlacementCellUnavailable";
                return false;
            }

            cell = new ArcGraphCellCoord(x, y, 0);
            reason = "DevToolsPlacementCellResolved";
            return true;
        }

        // =============================================================================
        // SetDevToolsOverlay
        // =============================================================================
        /// <summary>
        /// <para>
        /// Assegna esplicitamente il DevTools legacy da osservare in sola lettura.
        /// </para>
        /// </summary>
        public void SetDevToolsOverlay(MapGridRuntimeDevToolsOverlay overlay)
        {
            devToolsOverlay = overlay;
        }

        // =============================================================================
        // SetSpriteResolverBehaviour
        // =============================================================================
        /// <summary>
        /// <para>
        /// Assegna il resolver sprite scene-side usato anche dal renderer oggetti.
        /// </para>
        ///
        /// <para><b>Anteprima sullo stesso contratto degli oggetti reali</b></para>
        /// <para>
        /// Il consumer non carica PNG direttamente e non conosce cartelle Unity:
        /// costruisce una <c>ArcGraphSpriteResolveRequest</c> e lascia al resolver
        /// esistente la conversione chiave -> <c>Sprite</c>. Cosi' muri, cibo e
        /// oggetti futuri passano dallo stesso canale visuale.
        /// </para>
        /// </summary>
        public void SetSpriteResolverBehaviour(MonoBehaviour resolverBehaviour)
        {
            spriteResolverBehaviour = resolverBehaviour;
        }

        // =============================================================================
        // SetSceneCamera
        // =============================================================================
        /// <summary>
        /// <para>
        /// Assegna la camera usata dal fallback mouse -> cella.
        /// </para>
        /// </summary>
        public void SetSceneCamera(Camera camera)
        {
            sceneCamera = camera;
        }

        // =============================================================================
        // SetHighlightEnabled
        // =============================================================================
        /// <summary>
        /// <para>
        /// Abilita o disabilita il gate locale dell'overlay.
        /// </para>
        /// </summary>
        public void SetHighlightEnabled(bool enabled)
        {
            highlightEnabled = enabled;
            if (!enabled)
                HideHighlight();
        }

        // =============================================================================
        // EnableHighlightFromInspector
        // =============================================================================
        /// <summary>
        /// <para>
        /// Context menu per abilitare l'highlight durante i test manuali.
        /// </para>
        /// </summary>
        [ContextMenu("ArcGraph/Enable Placement Cell Highlight")]
        public void EnableHighlightFromInspector()
        {
            SetHighlightEnabled(true);
        }

        // =============================================================================
        // DisableHighlightFromInspector
        // =============================================================================
        /// <summary>
        /// <para>
        /// Context menu per disabilitare l'highlight durante i test manuali.
        /// </para>
        /// </summary>
        [ContextMenu("ArcGraph/Disable Placement Cell Highlight")]
        public void DisableHighlightFromInspector()
        {
            SetHighlightEnabled(false);
        }

        // =============================================================================
        // LogPlacementHighlightDiagnosticsFromInspector
        // =============================================================================
        /// <summary>
        /// <para>
        /// Stampa in Console l'ultima diagnostica della preview inserimento.
        /// </para>
        ///
        /// <para><b>Diagnostica manuale per gate visuale</b></para>
        /// <para>
        /// Durante i test ArcGraph l'operatore puo' avere bisogno di capire se la
        /// preview non si vede per assenza tool, cella non risolta, DevTools
        /// mancante o semplice problema di rendering. Questo context menu non
        /// modifica stato runtime: rende solo leggibile l'ultimo motivo registrato.
        /// </para>
        /// </summary>
        [ContextMenu("ArcGraph/Log Placement Cell Highlight Diagnostics")]
        public void LogPlacementHighlightDiagnosticsFromInspector()
        {
            Debug.Log(
                "[ArcGraphPlacementCellHighlightSceneConsumer] " +
                _lastDiagnostics.Reason +
                ", enabled=" + _lastDiagnostics.HighlightEnabled +
                ", devTools=" + _lastDiagnostics.HasDevToolsOverlay +
                ", placement=" + _lastDiagnostics.PlacementPreviewActive +
                ", validCell=" + _lastDiagnostics.HasValidCell +
                ", cell=" + _lastDiagnostics.Cell +
                ", shown=" + _lastDiagnostics.DidShowHighlight);
        }

        private void OnDestroy()
        {
            if (_highlightSprite != null)
                Destroy(_highlightSprite);

            if (_highlightTexture != null)
                Destroy(_highlightTexture);

            _highlightSprite = null;
            _highlightTexture = null;
        }

        private void EnsureHighlightRenderer()
        {
            if (_highlightRenderer != null)
                return;

            _highlightTexture = new Texture2D(1, 1)
            {
                name = "ArcGraphPlacementCellHighlightTexture",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            _highlightTexture.SetPixel(0, 0, Color.white);
            _highlightTexture.Apply();

            _highlightSprite = Sprite.Create(
                _highlightTexture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f),
                1f);
            _highlightSprite.name = "ArcGraphPlacementCellHighlightSprite";

            _highlightObject = new GameObject(highlightObjectName);
            _highlightObject.transform.SetParent(transform, false);
            _highlightRenderer = _highlightObject.AddComponent<SpriteRenderer>();
            _highlightRenderer.sprite = _highlightSprite;
            _highlightRenderer.sortingOrder = sortingOrder;
            _highlightRenderer.color = validPlacementColor;
            _highlightObject.SetActive(false);
        }

        // =============================================================================
        // EnsurePreviewRenderer
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea una sola volta il renderer della preview oggetto e poi lo riusa.
        /// </para>
        ///
        /// <para><b>Pooling minimale per overlay di input</b></para>
        /// <para>
        /// La preview segue il mouse e puo' aggiornarsi ogni frame. Creare e
        /// distruggere GameObject a ogni cella sarebbe inutile: un solo
        /// <c>SpriteRenderer</c> disattivabile e' sufficiente e tiene basso il costo.
        /// </para>
        /// </summary>
        private void EnsurePreviewRenderer()
        {
            if (_previewRenderer != null)
                return;

            _previewObject = new GameObject(previewObjectName);
            _previewObject.transform.SetParent(transform, false);
            _previewRenderer = _previewObject.AddComponent<SpriteRenderer>();
            _previewRenderer.sortingOrder = previewSortingOrder;
            _previewRenderer.color = previewSpriteColor;
            _previewObject.SetActive(false);
        }

        // =============================================================================
        // ShowPlacementPreviewAtCell
        // =============================================================================
        /// <summary>
        /// <para>
        /// Disegna highlight e anteprima oggetto sulla cella risolta.
        /// </para>
        ///
        /// <para><b>Un solo punto di applicazione visuale</b></para>
        /// <para>
        /// Sia il frame interattivo ArcGraph sia il fallback camera passano da qui.
        /// In questo modo colore, scala, sorting e diagnostica restano identici e
        /// non si creano due comportamenti visuali diversi.
        /// </para>
        /// </summary>
        private void ShowPlacementPreviewAtCell(
            ArcGraphCellCoord cell,
            ArcGraphInteractionFrame interactionFrame)
        {
            EnsureHighlightRenderer();
            Vector3 worldPosition = ResolveWorldPosition(cell);

            _highlightObject.transform.position = worldPosition;
            _highlightObject.transform.localScale = new Vector3(tileWorldSize, tileWorldSize, 1f);
            _highlightRenderer.color = validPlacementColor;
            _highlightRenderer.sortingOrder = sortingOrder;
            _highlightObject.SetActive(true);

            bool didShowPreview = TryShowPlacementSpritePreview(cell);
            StoreDiagnostics(
                interactionFrame,
                true,
                didShowPreview ? "PlacementCellAndSpritePreviewShown" : "PlacementCellHighlighted");
        }

        // =============================================================================
        // TryShowPlacementSpritePreview
        // =============================================================================
        /// <summary>
        /// <para>
        /// Prova a mostrare lo sprite dell'oggetto che il DevTool inserirebbe nella
        /// cella indicata.
        /// </para>
        ///
        /// <para><b>Preview come lettura passiva del catalogo</b></para>
        /// <para>
        /// Il metodo legge il <c>defId</c> attivo dal DevTool, recupera la
        /// <c>ObjectDef</c> dal <c>World</c> e chiede al resolver ArcGraph lo sprite.
        /// Non accoda comandi e non crea istanze mondo: produce soltanto un fantasma
        /// grafico rosso e semi-trasparente.
        /// </para>
        /// </summary>
        private bool TryShowPlacementSpritePreview(ArcGraphCellCoord cell)
        {
            if (devToolsOverlay == null
                || !devToolsOverlay.TryGetActiveObjectPlacementPreviewDefId(out string defId)
                || string.IsNullOrWhiteSpace(defId))
            {
                HidePreview();
                return false;
            }

            World world = MapGridWorldProvider.TryGetWorld();
            if (world == null || !world.TryGetObjectDef(defId, out ObjectDef def) || def == null)
            {
                HidePreview();
                return false;
            }

            IArcGraphSpriteResolver spriteResolver = spriteResolverBehaviour as IArcGraphSpriteResolver;
            if (spriteResolver == null)
            {
                HidePreview();
                return false;
            }

            string spriteKey = ResolvePreviewSpriteKey(world, def, cell);
            if (string.IsNullOrWhiteSpace(spriteKey))
            {
                HidePreview();
                return false;
            }

            var request = new ArcGraphSpriteResolveRequest(
                ArcGraphRenderItemKind.Object,
                -1,
                spriteKey,
                def.Id,
                false);

            if (!spriteResolver.TryResolveSprite(request, out Sprite sprite) || sprite == null)
            {
                HidePreview();
                return false;
            }

            EnsurePreviewRenderer();

            _previewObject.transform.position = ResolveObjectPreviewWorldPosition(cell, def, sprite);
            _previewObject.transform.localScale = Vector3.one * ResolvePositiveScale(previewObjectScale);
            _previewRenderer.sprite = sprite;
            _previewRenderer.color = previewSpriteColor;
            _previewRenderer.sortingOrder = previewSortingOrder;
            _previewObject.SetActive(true);
            return true;
        }

        // =============================================================================
        // TryResolveCellFromSceneCamera
        // =============================================================================
        /// <summary>
        /// <para>
        /// Risolve la cella sotto il mouse usando la camera Unity reale.
        /// </para>
        ///
        /// <para><b>Contratto locale al gate F3</b></para>
        /// <para>
        /// Il metodo non interpreta click e non produce comandi. Serve solo a sapere
        /// dove disegnare l'anteprima se il mapper interattivo ArcGraph non e'
        /// ancora perfettamente sincronizzato con camera, zoom e pan della scena.
        /// </para>
        /// </summary>
        private bool TryResolveCellFromSceneCamera(
            out ArcGraphCellCoord cell,
            out string reason)
        {
            cell = new ArcGraphCellCoord(0, 0, 0);
            reason = "None";

            if (!highlightEnabled)
            {
                reason = "HighlightDisabled";
                return false;
            }

            if (devToolsOverlay == null)
            {
                reason = "DevToolsOverlayMissing";
                return false;
            }

            if (!devToolsOverlay.IsObjectPlacementPreviewActive)
            {
                reason = "PlacementPreviewInactive";
                return false;
            }

            if (devToolsOverlay.IsPointerOverDevToolsWindow)
            {
                reason = "PointerOverDevToolsWindow";
                return false;
            }

            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                reason = "MouseMissing";
                return false;
            }

            Camera camera = ResolveSceneCamera();
            if (camera == null)
            {
                reason = "SceneCameraMissing";
                return false;
            }

            Vector2 screenPosition = mouse.position.ReadValue();
            float planeDistance = ResolveWorldPlaneDistance(camera);
            Vector3 worldPosition = camera.ScreenToWorldPoint(new Vector3(
                screenPosition.x,
                screenPosition.y,
                planeDistance));

            float safeTileWorldSize = ResolvePositiveScale(tileWorldSize);
            int x = Mathf.FloorToInt((worldPosition.x - originOffset.x) / safeTileWorldSize);
            int y = Mathf.FloorToInt((worldPosition.y - originOffset.y) / safeTileWorldSize);

            World world = MapGridWorldProvider.TryGetWorld();
            if (world != null
                && (x < 0 || y < 0 || x >= world.MapWidth || y >= world.MapHeight))
            {
                reason = "CellOutOfWorld";
                return false;
            }

            cell = new ArcGraphCellCoord(x, y, 0);
            reason = "SceneCameraCellResolved";
            return true;
        }

        // =============================================================================
        // ResolvePreviewSpriteKey
        // =============================================================================
        /// <summary>
        /// <para>
        /// Risolve la sprite key da usare per l'anteprima dell'oggetto.
        /// </para>
        ///
        /// <para><b>Stessa convenzione degli oggetti reali</b></para>
        /// <para>
        /// Gli oggetti normali usano il path dichiarato in <c>object_defs</c>. I
        /// muri, che sono cardinali, costruiscono invece una chiave
        /// <c>sheet#subSprite</c> basata sui muri vicini gia' presenti, cosi'
        /// l'anteprima assomiglia alla variante che verra' renderizzata dopo il
        /// click.
        /// </para>
        /// </summary>
        private string ResolvePreviewSpriteKey(
            World world,
            ObjectDef def,
            ArcGraphCellCoord cell)
        {
            if (def == null || string.IsNullOrWhiteSpace(def.Id))
                return string.Empty;

            string baseSpriteKey = ResolveBaseSpriteKey(def);
            if (string.IsNullOrWhiteSpace(baseSpriteKey))
                return string.Empty;

            // I muri sono l'unico oggetto che oggi cambia sprite in base ai vicini.
            // Per la preview usiamo la stessa convenzione della queue oggetti:
            // maschera N/W/S/E e sub-sprite dentro la striscia PNG sliced.
            if (!IsVisualKind(def.Visual, "wall"))
                return baseSpriteKey;

            string mask = ResolveProspectiveWallMask(world, def, cell);
            var previewSnapshot = new ArcGraphObjectVisualSnapshot(
                int.MaxValue,
                def.Id,
                cell,
                baseSpriteKey,
                false,
                0,
                -1,
                ResolvePositive(def.FootprintWidth, 1),
                ResolvePositive(def.FootprintHeight, 1),
                def.Visual?.VisualKind ?? string.Empty,
                def.Visual?.ResolverKey ?? string.Empty,
                ResolveNonNegative(def.Visual?.WidthPixels ?? 0),
                ResolveNonNegative(def.Visual?.HeightPixels ?? 0),
                ResolveNonNegative(def.Visual?.BaseWidthPixels ?? 0),
                ResolveNonNegative(def.Visual?.BaseHeightPixels ?? 0),
                def.Visual?.BaseMiniTileMask ?? string.Empty,
                def.Visual?.Pivot ?? string.Empty,
                def.Visual?.OffsetX ?? 0,
                def.Visual?.OffsetY ?? 0,
                def.Visual?.FadeWhenActorBehind ?? false,
                def.Visual?.UseShadow ?? false);

            string subSpriteName = ArcGraphWallCardinalResolver.ResolveSubSpriteName(
                baseSpriteKey,
                mask,
                previewSnapshot);

            return string.IsNullOrWhiteSpace(subSpriteName)
                ? baseSpriteKey
                : baseSpriteKey + "#" + subSpriteName;
        }

        // =============================================================================
        // ResolveProspectiveWallMask
        // =============================================================================
        /// <summary>
        /// <para>
        /// Calcola la maschera cardinale che avrebbe un muro se venisse inserito
        /// nella cella sotto il mouse.
        /// </para>
        ///
        /// <para><b>Regola locale e leggibile</b></para>
        /// <para>
        /// La preview non ricostruisce una queue completa: guarda solo nord,
        /// ovest, sud ed est sullo stesso piano logico 2D usato oggi dal tool F3.
        /// Questo basta per mostrare il potenziale collegamento senza introdurre
        /// un secondo sistema di piazzamento.
        /// </para>
        /// </summary>
        private string ResolveProspectiveWallMask(
            World world,
            ObjectDef previewDef,
            ArcGraphCellCoord cell)
        {
            bool north = HasCompatibleWallAt(world, previewDef, cell.X, cell.Y + 1);
            bool west = HasCompatibleWallAt(world, previewDef, cell.X - 1, cell.Y);
            bool south = HasCompatibleWallAt(world, previewDef, cell.X, cell.Y - 1);
            bool east = HasCompatibleWallAt(world, previewDef, cell.X + 1, cell.Y);

            return (north ? "1" : "0")
                   + (west ? "1" : "0")
                   + (south ? "1" : "0")
                   + (east ? "1" : "0");
        }

        // =============================================================================
        // HasCompatibleWallAt
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica se una cella vicina contiene un muro compatibile con quello in
        /// preview.
        /// </para>
        ///
        /// <para><b>Compatibilita' per famiglia visuale</b></para>
        /// <para>
        /// Due muri si collegano solo se appartengono alla stessa famiglia
        /// <c>ResolverKey</c>. Questo evita che futuri muri di pietra, legno o
        /// mattoni si aggancino automaticamente tra loro solo perche' condividono
        /// <c>VisualKind = wall</c>.
        /// </para>
        /// </summary>
        private static bool HasCompatibleWallAt(
            World world,
            ObjectDef previewDef,
            int cellX,
            int cellY)
        {
            if (world == null || previewDef == null)
                return false;

            int objectId = world.GetObjectAt(cellX, cellY);
            if (objectId <= 0
                || !world.Objects.TryGetValue(objectId, out WorldObjectInstance instance)
                || instance == null
                || instance.IsHeld
                || !world.TryGetObjectDef(instance.DefId, out ObjectDef existingDef)
                || existingDef == null)
            {
                return false;
            }

            return IsVisualKind(existingDef.Visual, "wall")
                   && string.Equals(
                       ResolveWallFamilyKey(existingDef),
                       ResolveWallFamilyKey(previewDef),
                       StringComparison.Ordinal);
        }

        // =============================================================================
        // ResolveObjectPreviewWorldPosition
        // =============================================================================
        /// <summary>
        /// <para>
        /// Calcola la posizione world-space della preview usando pivot, footprint e
        /// offset visuali dell'oggetto.
        /// </para>
        ///
        /// <para><b>Allineamento con il renderer oggetti</b></para>
        /// <para>
        /// I muri 32x83 e gli oggetti futuri piu' alti della cella devono poggiare
        /// sulla stessa base usata dal renderer reale. Per questo la preview applica
        /// la stessa compensazione del pivot basso, invece di limitarsi a centrare
        /// lo sprite nella cella.
        /// </para>
        /// </summary>
        private Vector3 ResolveObjectPreviewWorldPosition(
            ArcGraphCellCoord cell,
            ObjectDef def,
            Sprite sprite)
        {
            ObjectVisualDef visual = def?.Visual;
            int footprintWidth = ResolvePositive(def?.FootprintWidth ?? 0, 1);
            int footprintHeight = ResolvePositive(def?.FootprintHeight ?? 0, 1);
            string pivot = visual?.Pivot ?? string.Empty;

            float worldX = (cell.X + (footprintWidth * 0.5f)) * tileWorldSize;
            if (IsPivot(pivot, "bottom_left"))
                worldX = cell.X * tileWorldSize;
            else if (IsPivot(pivot, "bottom_right"))
                worldX = (cell.X + footprintWidth) * tileWorldSize;

            float worldY = (cell.Y + (footprintHeight * 0.5f)) * tileWorldSize;
            worldX += ConvertPixelOffsetToWorld(visual?.OffsetX ?? 0, visual?.BaseWidthPixels ?? 0);
            worldY += ConvertPixelOffsetToWorld(visual?.OffsetY ?? 0, visual?.BaseHeightPixels ?? 0);

            Vector3 position = originOffset + new Vector3(worldX, worldY, previewZOffset);
            position += ResolveSpritePivotCompensation(pivot, sprite, ResolvePositiveScale(previewObjectScale));
            return position;
        }

        // =============================================================================
        // ResolveSpritePivotCompensation
        // =============================================================================
        /// <summary>
        /// <para>
        /// Compensa il pivot importato da Unity quando il catalogo dichiara un pivot
        /// basso come <c>bottom_center</c>.
        /// </para>
        /// </summary>
        private static Vector3 ResolveSpritePivotCompensation(
            string pivot,
            Sprite sprite,
            float objectScale)
        {
            if (sprite == null)
                return Vector3.zero;

            float safeScale = ResolvePositiveScale(objectScale);
            Bounds bounds = sprite.bounds;
            float x = 0f;
            float y = 0f;

            if (IsPivot(pivot, "bottom_left"))
                x = -bounds.min.x * safeScale;
            else if (IsPivot(pivot, "bottom_right"))
                x = -bounds.max.x * safeScale;

            if (IsBottomPivot(pivot))
                y = -bounds.min.y * safeScale;

            return new Vector3(x, y, 0f);
        }

        private Vector3 ResolveWorldPosition(ArcGraphCellCoord cell)
        {
            return originOffset + new Vector3(
                (cell.X + 0.5f) * tileWorldSize,
                (cell.Y + 0.5f) * tileWorldSize,
                zOffset);
        }

        // =============================================================================
        // CreateSyntheticInteractionFrame
        // =============================================================================
        /// <summary>
        /// <para>
        /// Crea un frame diagnostico valido quando la cella arriva dal fallback
        /// camera invece che dal boundary interattivo ArcGraph.
        /// </para>
        /// </summary>
        private static ArcGraphInteractionFrame CreateSyntheticInteractionFrame(
            ArcGraphCellCoord cell)
        {
            var coordinate = new ArcGraphViewCoordinateResult(
                true,
                cell,
                0f,
                0f,
                new ArcGraphViewCellRect(cell.X, cell.Y, cell.X + 1, cell.Y + 1),
                "SceneCameraFallback");

            return new ArcGraphInteractionFrame(
                ArcGraphViewInputFrame.Empty(),
                coordinate,
                ArcGraphInteractionTargetKind.Cell,
                cell,
                -1,
                -1,
                true,
                false,
                "SceneCameraFallback");
        }

        // =============================================================================
        // ResolveSceneCamera
        // =============================================================================
        /// <summary>
        /// <para>
        /// Restituisce la camera esplicita o, in fallback, <c>Camera.main</c>.
        /// </para>
        /// </summary>
        private Camera ResolveSceneCamera()
        {
            if (sceneCamera != null)
                return sceneCamera;

            return Camera.main;
        }

        // =============================================================================
        // ResolveWorldPlaneDistance
        // =============================================================================
        /// <summary>
        /// <para>
        /// Calcola la distanza da usare con <c>ScreenToWorldPoint</c> per il piano
        /// Z della mappa ArcGraph.
        /// </para>
        /// </summary>
        private float ResolveWorldPlaneDistance(Camera camera)
        {
            if (camera == null)
                return 0f;

            float distance = originOffset.z - camera.transform.position.z;
            return Mathf.Abs(distance) > 0.001f
                ? Mathf.Abs(distance)
                : Mathf.Max(0.001f, camera.nearClipPlane);
        }

        private void HideAndStore(
            ArcGraphInteractionFrame interactionFrame,
            bool didShowHighlight,
            string reason)
        {
            HideHighlight();
            StoreDiagnostics(interactionFrame, didShowHighlight, reason);
        }

        private void HideHighlight()
        {
            if (_highlightObject != null)
                _highlightObject.SetActive(false);

            HidePreview();
        }

        // =============================================================================
        // HidePreview
        // =============================================================================
        /// <summary>
        /// <para>
        /// Spegne il GameObject della preview senza distruggerlo.
        /// </para>
        /// </summary>
        private void HidePreview()
        {
            if (_previewObject != null)
                _previewObject.SetActive(false);
        }

        // =============================================================================
        // ResolveBaseSpriteKey
        // =============================================================================
        /// <summary>
        /// <para>
        /// Risolve il path base dello sprite ArcGraph partendo dalla definizione
        /// oggetto.
        /// </para>
        /// </summary>
        private string ResolveBaseSpriteKey(ObjectDef def)
        {
            if (def == null)
                return string.Empty;

            string spriteKey = def.ResolveArcGraphSpritePath();
            if (!string.IsNullOrWhiteSpace(spriteKey))
                return spriteKey.Trim();

            return string.IsNullOrWhiteSpace(def.Id)
                ? string.Empty
                : "MapGrid/Sprites/Objects/" + def.Id.Trim();
        }

        // =============================================================================
        // ResolveWallFamilyKey
        // =============================================================================
        /// <summary>
        /// <para>
        /// Restituisce la famiglia visuale usata per decidere se due muri possono
        /// collegarsi graficamente.
        /// </para>
        /// </summary>
        private static string ResolveWallFamilyKey(ObjectDef def)
        {
            if (def == null)
                return string.Empty;

            if (def.Visual != null && !string.IsNullOrWhiteSpace(def.Visual.ResolverKey))
                return def.Visual.ResolverKey.Trim();

            return string.IsNullOrWhiteSpace(def.Id) ? string.Empty : def.Id.Trim();
        }

        // =============================================================================
        // ConvertPixelOffsetToWorld
        // =============================================================================
        /// <summary>
        /// <para>
        /// Converte un offset espresso in pixel catalogo in un offset world-space.
        /// </para>
        /// </summary>
        private float ConvertPixelOffsetToWorld(
            int offsetPixels,
            int basePixels)
        {
            if (offsetPixels == 0)
                return 0f;

            int safeBasePixels = basePixels > 0 ? basePixels : 32;
            return offsetPixels * (tileWorldSize / safeBasePixels);
        }

        // =============================================================================
        // IsVisualKind
        // =============================================================================
        /// <summary>
        /// <para>
        /// Confronta la categoria visuale di una definizione oggetto con una stringa
        /// attesa.
        /// </para>
        /// </summary>
        private static bool IsVisualKind(
            ObjectVisualDef visual,
            string expected)
        {
            return visual != null
                   && string.Equals(
                       visual.VisualKind ?? string.Empty,
                       expected ?? string.Empty,
                       StringComparison.OrdinalIgnoreCase);
        }

        // =============================================================================
        // IsBottomPivot
        // =============================================================================
        /// <summary>
        /// <para>
        /// Indica se una convenzione pivot appartiene alla famiglia dei pivot bassi.
        /// </para>
        /// </summary>
        private static bool IsBottomPivot(
            string pivot)
        {
            return IsPivot(pivot, "bottom_center")
                   || IsPivot(pivot, "bottom_left")
                   || IsPivot(pivot, "bottom_right");
        }

        // =============================================================================
        // IsPivot
        // =============================================================================
        /// <summary>
        /// <para>
        /// Confronta due convenzioni pivot ignorando maiuscole e minuscole.
        /// </para>
        /// </summary>
        private static bool IsPivot(
            string pivot,
            string expected)
        {
            return string.Equals(
                pivot ?? string.Empty,
                expected ?? string.Empty,
                StringComparison.OrdinalIgnoreCase);
        }

        // =============================================================================
        // ResolvePositive
        // =============================================================================
        /// <summary>
        /// <para>
        /// Normalizza un intero che deve essere positivo.
        /// </para>
        /// </summary>
        private static int ResolvePositive(
            int value,
            int fallback)
        {
            return value > 0 ? value : fallback;
        }

        // =============================================================================
        // ResolveNonNegative
        // =============================================================================
        /// <summary>
        /// <para>
        /// Normalizza un intero che puo' essere zero ma non negativo.
        /// </para>
        /// </summary>
        private static int ResolveNonNegative(
            int value)
        {
            return value < 0 ? 0 : value;
        }

        // =============================================================================
        // ResolvePositiveScale
        // =============================================================================
        /// <summary>
        /// <para>
        /// Normalizza una scala Unity evitando valori nulli o negativi.
        /// </para>
        /// </summary>
        private static float ResolvePositiveScale(
            float value)
        {
            return value > 0f ? value : 1f;
        }

        private void StoreDiagnostics(
            ArcGraphInteractionFrame interactionFrame,
            bool didShowHighlight,
            string reason)
        {
            bool placementActive = devToolsOverlay != null && devToolsOverlay.IsObjectPlacementPreviewActive;
            _lastDiagnostics = new ArcGraphPlacementCellHighlightDiagnostics(
                true,
                highlightEnabled,
                devToolsOverlay != null,
                placementActive,
                interactionFrame.HasValidCell,
                didShowHighlight,
                interactionFrame.Cell,
                reason);

            if (!logDiagnostics)
                return;

            Debug.Log(
                "[ArcGraphPlacementCellHighlightSceneConsumer] " +
                _lastDiagnostics.Reason +
                ", enabled=" + _lastDiagnostics.HighlightEnabled +
                ", devTools=" + _lastDiagnostics.HasDevToolsOverlay +
                ", placement=" + _lastDiagnostics.PlacementPreviewActive +
                ", cell=" + _lastDiagnostics.Cell +
                ", shown=" + _lastDiagnostics.DidShowHighlight);
        }
    }
}
