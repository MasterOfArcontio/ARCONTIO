using System.Collections.Generic;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphLayerStack
    // =============================================================================
    /// <summary>
    /// <para>
    /// Registro ordinato dei layer grafici minimi di <c>arcgraph</c>.
    /// </para>
    ///
    /// <para><b>Principio architetturale: orchestrazione grafica senza World</b></para>
    /// <para>
    /// Lo stack coordina lifecycle e refresh dei layer, ma non legge lo stato
    /// simulativo e non conosce adapter, NPC, oggetti o mappa. Questo lo rende un
    /// primo embrione del mainframe grafico: puo' attivare moduli di presentazione
    /// senza diventare un manager onnisciente o un secondo Decision Layer.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>_layers</b>: lista ordinata dei layer registrati.</item>
    ///   <item><b>Register</b>: aggiunge un layer senza duplicare il layer id.</item>
    ///   <item><b>InitializeAll</b>: passa lo stato render condiviso ai layer.</item>
    ///   <item><b>RefreshDirtyAll</b>: notifica dirty state ai layer visibili.</item>
    ///   <item><b>SetLayerVisible</b>: toggle mirato di un layer.</item>
    ///   <item><b>DisposeAll</b>: cleanup grafico locale.</item>
    /// </list>
    /// </summary>
    public sealed class ArcGraphLayerStack
    {
        private readonly List<IArcGraphLayer> _layers = new();

        public int Count => _layers.Count;

        // =============================================================================
        // Register
        // =============================================================================
        /// <summary>
        /// <para>
        /// Registra un layer nello stack se il suo <c>LayerId</c> non e' gia' presente.
        /// </para>
        ///
        /// <para><b>Modularita' controllata</b></para>
        /// <para>
        /// Lo stack evita duplicati logici, perche' due layer con lo stesso id
        /// renderebbero ambigua la futura composizione visuale. Il metodo non
        /// inizializza automaticamente il layer: il bootstrap grafico deve chiamare
        /// <c>InitializeAll</c> in un punto esplicito.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>layer</b>: layer da aggiungere.</item>
        ///   <item><b>reason</b>: motivo diagnostico sintetico.</item>
        /// </list>
        /// </summary>
        public bool Register(IArcGraphLayer layer, out string reason)
        {
            reason = string.Empty;

            if (layer == null)
            {
                reason = "LayerMissing";
                return false;
            }

            if (layer.LayerId == ArcGraphLayerId.None)
            {
                reason = "InvalidLayerId";
                return false;
            }

            if (ContainsLayer(layer.LayerId))
            {
                reason = "LayerAlreadyRegistered";
                return false;
            }

            _layers.Add(layer);
            reason = "LayerRegistered";
            return true;
        }

        // =============================================================================
        // RegisterDefaultFoundationLayers
        // =============================================================================
        /// <summary>
        /// <para>
        /// Registra i quattro layer minimi previsti dal checkpoint <c>v0.30d</c>.
        /// </para>
        ///
        /// <para><b>Foundation esplicita</b></para>
        /// <para>
        /// Terrain, Object, Actor e Debug sono i layer necessari per iniziare a
        /// sostituire gradualmente la MapGrid. Il metodo crea solo classi passive:
        /// non costruisce sprite, non aggiunge componenti Unity e non collega il
        /// renderer corrente.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Terrain</b>: cache terreno.</item>
        ///   <item><b>Object</b>: cache oggetti.</item>
        ///   <item><b>Actor</b>: cache actor.</item>
        ///   <item><b>Debug</b>: contatori dirty.</item>
        /// </list>
        /// </summary>
        public void RegisterDefaultFoundationLayers()
        {
            Register(new ArcGraphTerrainLayer(), out _);
            Register(new ArcGraphObjectLayer(), out _);
            Register(new ArcGraphActorLayer(), out _);
            Register(new ArcGraphDebugLayer(), out _);
        }

        // =============================================================================
        // RegisterFuturePlaceholderLayers
        // =============================================================================
        /// <summary>
        /// <para>
        /// Registra i layer placeholder futuri previsti dal checkpoint <c>v0.30h</c>.
        /// </para>
        ///
        /// <para><b>Placeholder espliciti, non bootstrap automatico</b></para>
        /// <para>
        /// Water, Vegetation, Light, Weather ed Effect sono slot grafici futuri. Il
        /// metodo li registra solo quando il chiamante lo richiede esplicitamente:
        /// non sono inclusi nei layer foundation di default per evitare che il
        /// runtime attuale sembri avere gia' sistemi ambientali produttivi.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Water</b>: cache visuale liquidi.</item>
        ///   <item><b>Vegetation</b>: cache visuale vegetazione.</item>
        ///   <item><b>Light</b>: cache visuale luce.</item>
        ///   <item><b>Weather</b>: overlay meteo globale o per livello.</item>
        ///   <item><b>Effect</b>: effetti locali come fuoco e fumo.</item>
        /// </list>
        /// </summary>
        public void RegisterFuturePlaceholderLayers()
        {
            Register(new ArcGraphWaterLayer(), out _);
            Register(new ArcGraphVegetationLayer(), out _);
            Register(new ArcGraphLightLayer(), out _);
            Register(new ArcGraphWeatherLayer(), out _);
            Register(new ArcGraphEffectLayer(), out _);
        }

        // =============================================================================
        // InitializeAll
        // =============================================================================
        /// <summary>
        /// <para>
        /// Inizializza tutti i layer registrati con lo stato render condiviso.
        /// </para>
        ///
        /// <para><b>Bootstrap grafico deterministico</b></para>
        /// <para>
        /// L'inizializzazione avviene nell'ordine di registrazione. Non contiene
        /// async, coroutine o update nascosti: il chiamante decide quando il layer
        /// stack entra in vita.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>renderState</b>: stato condiviso passato a ogni layer.</item>
        /// </list>
        /// </summary>
        public void InitializeAll(ArcGraphRenderState renderState)
        {
            for (int i = 0; i < _layers.Count; i++)
                _layers[i].Initialize(renderState);
        }

        // =============================================================================
        // RefreshDirtyAll
        // =============================================================================
        /// <summary>
        /// <para>
        /// Notifica il dirty state corrente a tutti i layer visibili.
        /// </para>
        ///
        /// <para><b>Consumo grafico, non cleanup autoritario</b></para>
        /// <para>
        /// Lo stack non pulisce automaticamente <c>ArcGraphDirtyState</c>. Questa
        /// scelta mantiene separata la notifica dal momento di cleanup, che in futuro
        /// dovra' essere deciso dal mainframe quando tutti i renderer avranno davvero
        /// consumato il dirty.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>renderState</b>: stato dirty da notificare.</item>
        ///   <item><b>IsVisible</b>: i layer nascosti vengono saltati.</item>
        /// </list>
        /// </summary>
        public void RefreshDirtyAll(ArcGraphRenderState renderState)
        {
            for (int i = 0; i < _layers.Count; i++)
            {
                IArcGraphLayer layer = _layers[i];
                if (layer != null && layer.IsVisible)
                    layer.RefreshDirty(renderState);
            }
        }

        // =============================================================================
        // SetLayerVisible
        // =============================================================================
        /// <summary>
        /// <para>
        /// Cambia la visibilita' di un layer registrato.
        /// </para>
        ///
        /// <para><b>Toggle locale</b></para>
        /// <para>
        /// Il metodo agisce solo sul layer grafico trovato. Non modifica lo stato
        /// simulativo e non rimuove snapshot dalla cache del layer.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>layerId</b>: layer da cercare.</item>
        ///   <item><b>visible</b>: nuovo stato visuale.</item>
        /// </list>
        /// </summary>
        public bool SetLayerVisible(ArcGraphLayerId layerId, bool visible)
        {
            for (int i = 0; i < _layers.Count; i++)
            {
                if (_layers[i].LayerId != layerId)
                    continue;

                _layers[i].SetVisible(visible);
                return true;
            }

            return false;
        }

        // =============================================================================
        // TryGetLayer
        // =============================================================================
        /// <summary>
        /// <para>
        /// Cerca un layer registrato per tipo concreto.
        /// </para>
        ///
        /// <para><b>Accesso tipizzato al layer, non al World</b></para>
        /// <para>
        /// Questo helper serve ai test e al futuro mainframe per recuperare cache
        /// visuali specifiche senza esporre la lista mutabile interna. Il tipo
        /// richiesto deve essere un layer, non un sistema simulativo.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>T</b>: tipo di layer cercato.</item>
        ///   <item><b>layer</b>: istanza trovata, se presente.</item>
        /// </list>
        /// </summary>
        public bool TryGetLayer<T>(out T layer) where T : class, IArcGraphLayer
        {
            for (int i = 0; i < _layers.Count; i++)
            {
                if (_layers[i] is T typedLayer)
                {
                    layer = typedLayer;
                    return true;
                }
            }

            layer = null;
            return false;
        }

        // =============================================================================
        // DisposeAll
        // =============================================================================
        /// <summary>
        /// <para>
        /// Esegue cleanup di tutti i layer e svuota lo stack.
        /// </para>
        ///
        /// <para><b>Cleanup della presentazione</b></para>
        /// <para>
        /// DisposeAll non distrugge oggetti nel mondo e non tocca il renderer legacy.
        /// Chiama solo <c>Dispose</c> dei layer registrati e poi libera la lista
        /// locale.
        /// </para>
        ///
        /// <para><b>Struttura interna:</b></para>
        /// <list type="bullet">
        ///   <item><b>Dispose</b>: chiamato su ogni layer non nullo.</item>
        ///   <item><b>_layers</b>: lista svuotata.</item>
        /// </list>
        /// </summary>
        public void DisposeAll()
        {
            for (int i = 0; i < _layers.Count; i++)
                _layers[i]?.Dispose();

            _layers.Clear();
        }

        private bool ContainsLayer(ArcGraphLayerId layerId)
        {
            for (int i = 0; i < _layers.Count; i++)
            {
                if (_layers[i].LayerId == layerId)
                    return true;
            }

            return false;
        }
    }
}
