using Arcontio.Core.Logging;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Arcontio.Core
{
    /// <summary>
    /// ViewSwitcher basato su InputActions asset (New Input System).
    /// Va messo su ArcontioRuntime (DontDestroyOnLoad), così funziona in qualunque scena.
    /// </summary>
    public sealed class ViewSwitcherInputActions : MonoBehaviour
    {
        [Header("Scene names (devono essere in Build Settings)")]
        [SerializeField] private string atomViewerSceneName = "Scene_AtomViewer";
        [SerializeField] private string mapGridName = "Scene_MapGrid";

        [Header("Startup View")]
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

            LoadIfNotActive(mapGridName);
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
