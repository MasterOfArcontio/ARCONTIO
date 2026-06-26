using Arcontio.Core.Logging;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Arcontio.Core
{
    // =============================================================================
    // StartupViewMode
    // =============================================================================
    /// <summary>
    /// <para>
    /// Modalita' con cui il bootstrap runtime decide quale vista caricare dopo
    /// l'avvio persistente.
    /// </para>
    ///
    /// <para><b>Principio architetturale: bootstrap vista configurabile</b></para>
    /// <para>
    /// La vista iniziale non deve piu' essere vincolata per sempre a MapGrid. Il
    /// default resta MapGrid per compatibilita', ma ArcGraph puo' diventare la
    /// vista iniziale appena esiste una scena autonoma dedicata.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>MapGrid</b>: comportamento storico, carica la scena MapGrid.</item>
    ///   <item><b>ArcGraph</b>: carica la futura scena autonoma ArcGraph.</item>
    ///   <item><b>CurrentScene</b>: non cambia scena e lascia operare il bootstrap corrente.</item>
    /// </list>
    /// </summary>
    public enum StartupViewMode
    {
        MapGrid = 0,
        ArcGraph = 1,
        CurrentScene = 2
    }

    /// <summary>
    /// ViewSwitcher basato su InputActions asset (New Input System).
    /// Va messo su ArcontioRuntime (DontDestroyOnLoad), così funziona in qualunque scena.
    /// </summary>
    public sealed class ViewSwitcherInputActions : MonoBehaviour
    {
        [Header("Scene names (devono essere in Build Settings)")]
        [SerializeField] private string atomViewerSceneName = "Scene_AtomViewer";
        [SerializeField] private string mapGridName = "Scene_MapGrid";
        [SerializeField] private string arcGraphSceneName = "Scene_ArcGraph";

        [Header("Startup View")]
        [SerializeField] private StartupViewMode startupViewMode = StartupViewMode.MapGrid;
        [SerializeField] private bool loadMapGridOnStart = true;

        // 1) QUI: sostituisci con il nome della classe generata dal tuo .inputactions
        private ArcontioInputActions _actions;

        private void Awake()
        {
            ArcontioLogger.Debug(
                new LogContext(tick: (int)TickContext.CurrentTickIndex, channel: "ViewSwitcher"),
                new LogBlock(LogLevel.Debug, "log.viewswitcher.awake_start")
            );
            EnsureActions();
        }


        // =============================================================================
        // Start
        // =============================================================================
        /// <summary>
        /// <para>
        /// Porta automaticamente la sessione runtime sulla scena MapGrid quando il
        /// bootstrap ha terminato l'inizializzazione iniziale.
        /// </para>
        ///
        /// <para><b>Principio architetturale: bootstrap della vista senza scelta manuale</b></para>
        /// <para>
        /// Il simulatore continua a nascere nel runtime persistente, ma la vista
        /// operativa di default diventa MapGrid. L'utente conserva comunque gli input
        /// di switch scena esistenti: questo metodo esegue solo la scelta iniziale.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>loadMapGridOnStart</b>: flag ispettore per disattivare il comportamento in debug speciali.</item>
        ///   <item><b>mapGridName</b>: nome scena gia' usato dal comando F2/F-map esistente.</item>
        ///   <item><b>LoadIfNotActive</b>: percorso comune con validazione Build Settings.</item>
        /// </list>
        /// </summary>
        private void Start()
        {
            if (!loadMapGridOnStart)
                return;

            LoadStartupView();
        }

        private void OnEnable()
        {
            EnsureActions();
            _actions.Enable();

            // 2) QUI: sostituisci "Global" e i nomi delle azioni se li hai chiamati diversamente
            _actions.Global.SwitchToAtomViewer.performed += OnSwitchToAtomViewer;
            _actions.Global.SwitchToMap.performed += OnSwitchToMap;
        }

        private void OnDisable()
        {
            if (_actions == null) return;

            _actions.Global.SwitchToAtomViewer.performed -= OnSwitchToAtomViewer;
            _actions.Global.SwitchToMap.performed -= OnSwitchToMap;

            _actions.Disable();
        }

        // =============================================================================
        // EnsureActions
        // =============================================================================
        /// <summary>
        /// <para>
        /// Garantisce che il wrapper generato <see cref="ArcontioInputActions"/> esista
        /// prima che il componente provi ad abilitarlo o a registrare callback.
        /// </para>
        ///
        /// <para><b>Lifecycle difensivo Unity</b></para>
        /// <para>
        /// In condizioni normali <c>Awake</c> inizializza il campo prima di
        /// <c>OnEnable</c>. Questo metodo rende pero' il componente robusto anche nei
        /// casi in cui Unity riabilita lo script, il dominio viene ricaricato o il
        /// campo torna null prima dell'abilitazione. La funzione non modifica lo stato
        /// simulativo: prepara solo l'oggetto input della view/runtime.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>_actions</b>: wrapper New Input System generato dal progetto.</item>
        ///   <item><b>Early return</b>: evita di ricreare input action gia' esistenti.</item>
        ///   <item><b>new ArcontioInputActions</b>: fallback sicuro quando il campo e' null.</item>
        /// </list>
        /// </summary>
        private void EnsureActions()
        {
            if (_actions != null)
                return;

            _actions = new ArcontioInputActions();
        }

        private void OnSwitchToAtomViewer(InputAction.CallbackContext ctx) => LoadIfNotActive(atomViewerSceneName);
        private void OnSwitchToMap(InputAction.CallbackContext ctx) => LoadIfNotActive(mapGridName);

        // =============================================================================
        // LoadStartupView
        // =============================================================================
        /// <summary>
        /// <para>
        /// Applica la scelta iniziale della vista runtime.
        /// </para>
        ///
        /// <para><b>Compatibilita' progressiva</b></para>
        /// <para>
        /// MapGrid resta il default finche' l'operatore non seleziona ArcGraph o
        /// CurrentScene. Questo evita di rompere il bootstrap attuale e consente di
        /// introdurre la futura scena ArcGraph in modo reversibile.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>CurrentScene</b>: non carica scene aggiuntive.</item>
        ///   <item><b>ArcGraph</b>: tenta di caricare la scena ArcGraph configurata.</item>
        ///   <item><b>MapGrid</b>: conserva il percorso storico.</item>
        /// </list>
        /// </summary>
        private void LoadStartupView()
        {
            switch (startupViewMode)
            {
                case StartupViewMode.CurrentScene:
                    return;
                case StartupViewMode.ArcGraph:
                    LoadIfNotActive(arcGraphSceneName);
                    return;
                case StartupViewMode.MapGrid:
                default:
                    LoadIfNotActive(mapGridName);
                    return;
            }
        }

        private static void LoadIfNotActive(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return;

            if (SceneManager.GetActiveScene().name == sceneName)
                return;

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                ArcontioLogger.Error(
                    new LogContext(tick: (int)TickContext.CurrentTickIndex, channel: "ViewSwitcher"),
                    new LogBlock(LogLevel.Error, "log.viewswitcher.scene_missing")
                        .AddField("scene", sceneName)
                );
                return;
            }

            SceneManager.LoadScene(sceneName);
        }

    }
}
