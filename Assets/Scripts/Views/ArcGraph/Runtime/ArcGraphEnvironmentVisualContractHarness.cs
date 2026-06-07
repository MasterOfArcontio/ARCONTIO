using System.Collections.Generic;

namespace Arcontio.View.ArcGraph
{
    // =============================================================================
    // ArcGraphEnvironmentVisualContractHarnessResult
    // =============================================================================
    /// <summary>
    /// <para>
    /// Risultato sintetico dello smoke test sui contratti ambientali visuali.
    /// </para>
    ///
    /// <para><b>Principio architetturale: QA senza scena</b></para>
    /// <para>
    /// Il risultato espone solo contatori e flag. Non contiene riferimenti a Unity,
    /// asset, renderer o sistemi simulativi. Serve a verificare che la <c>v0.36a</c>
    /// resti un contratto preparatorio e non diventi rendering produttivo.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>Passed</b>: esito complessivo.</item>
    ///   <item><b>Reason</b>: motivo sintetico.</item>
    ///   <item><b>ContractCount</b>: numero contratti trovati.</item>
    ///   <item><b>EnvironmentLayerCount</b>: contratti appartenenti a layer ambientali.</item>
    ///   <item><b>AnimationCapableCount</b>: layer che potranno usare animazione ArcGraph.</item>
    ///   <item><b>UnityCreationAllowedCount</b>: deve restare zero.</item>
    ///   <item><b>DefaultRegisteredCount</b>: deve restare zero per i placeholder.</item>
    /// </list>
    /// </summary>
    public readonly struct ArcGraphEnvironmentVisualContractHarnessResult
    {
        public readonly bool Passed;
        public readonly string Reason;
        public readonly int ContractCount;
        public readonly int EnvironmentLayerCount;
        public readonly int AnimationCapableCount;
        public readonly int UnityCreationAllowedCount;
        public readonly int DefaultRegisteredCount;

        public ArcGraphEnvironmentVisualContractHarnessResult(
            bool passed,
            string reason,
            int contractCount,
            int environmentLayerCount,
            int animationCapableCount,
            int unityCreationAllowedCount,
            int defaultRegisteredCount)
        {
            Passed = passed;
            Reason = string.IsNullOrWhiteSpace(reason) ? "None" : reason;
            ContractCount = contractCount;
            EnvironmentLayerCount = environmentLayerCount;
            AnimationCapableCount = animationCapableCount;
            UnityCreationAllowedCount = unityCreationAllowedCount;
            DefaultRegisteredCount = defaultRegisteredCount;
        }
    }

    // =============================================================================
    // ArcGraphEnvironmentVisualContractHarness
    // =============================================================================
    /// <summary>
    /// <para>
    /// Harness statico per validare il contratto ambientale visuale <c>v0.36a</c>.
    /// </para>
    ///
    /// <para><b>Principio architetturale: test del perimetro prima del renderer</b></para>
    /// <para>
    /// Lo smoke test costruisce i contratti in memoria e controlla le regole
    /// fondamentali: cinque layer ambientali, snapshot esterni, nessuna creazione
    /// Unity, nessuna registrazione default nei layer foundation. Non legge
    /// <c>World</c>, non crea layer reali e non modifica dirty state.
    /// </para>
    ///
    /// <para><b>Struttura interna:</b></para>
    /// <list type="bullet">
    ///   <item><b>RunDefaultSmoke</b>: scenario unico con i cinque contratti default.</item>
    ///   <item><b>Count flags</b>: diagnostica su animazioni, registrazione e Unity creation.</item>
    /// </list>
    /// </summary>
    public static class ArcGraphEnvironmentVisualContractHarness
    {
        // =============================================================================
        // RunDefaultSmoke
        // =============================================================================
        /// <summary>
        /// <para>
        /// Verifica i contratti ambientali default della <c>v0.36a</c>.
        /// </para>
        ///
        /// <para><b>Scenario minimo</b></para>
        /// <para>
        /// Il test usa una lista riusabile per evitare allocazioni non necessarie
        /// nell'helper principale. Controlla che Vegetation, Water, Light, Effect e
        /// Weather siano presenti e che nessuno dichiari poteri produttivi Unity.
        /// </para>
        /// </summary>
        public static ArcGraphEnvironmentVisualContractHarnessResult RunDefaultSmoke()
        {
            var contracts = new List<ArcGraphEnvironmentVisualLayerContract>(5);
            ArcGraphEnvironmentVisualContractCatalog.AppendDefaultContracts(contracts);

            int environmentLayerCount = 0;
            int animationCapableCount = 0;
            int unityCreationAllowedCount = 0;
            int defaultRegisteredCount = 0;
            bool allRequireExternalSnapshots = true;
            bool hasVegetation = false;
            bool hasWater = false;
            bool hasLight = false;
            bool hasEffect = false;
            bool hasWeather = false;

            for (int i = 0; i < contracts.Count; i++)
            {
                var contract = contracts[i];

                if (contract.IsEnvironmentLayer)
                    environmentLayerCount++;

                if (contract.AllowsArcGraphSpriteAnimation)
                    animationCapableCount++;

                if (contract.AllowsUnityObjectCreation)
                    unityCreationAllowedCount++;

                if (contract.RegisteredByDefault)
                    defaultRegisteredCount++;

                if (!contract.RequiresExternalSnapshots)
                    allRequireExternalSnapshots = false;

                hasVegetation |= contract.LayerId == ArcGraphLayerId.Vegetation;
                hasWater |= contract.LayerId == ArcGraphLayerId.Water;
                hasLight |= contract.LayerId == ArcGraphLayerId.Light;
                hasEffect |= contract.LayerId == ArcGraphLayerId.Effect;
                hasWeather |= contract.LayerId == ArcGraphLayerId.Weather;
            }

            bool passed = contracts.Count == 5
                          && environmentLayerCount == 5
                          && animationCapableCount == 4
                          && unityCreationAllowedCount == 0
                          && defaultRegisteredCount == 0
                          && allRequireExternalSnapshots
                          && hasVegetation
                          && hasWater
                          && hasLight
                          && hasEffect
                          && hasWeather;

            return new ArcGraphEnvironmentVisualContractHarnessResult(
                passed,
                passed ? "EnvironmentVisualContractSmokePassed" : "EnvironmentVisualContractSmokeFailed",
                contracts.Count,
                environmentLayerCount,
                animationCapableCount,
                unityCreationAllowedCount,
                defaultRegisteredCount);
        }
    }
}
