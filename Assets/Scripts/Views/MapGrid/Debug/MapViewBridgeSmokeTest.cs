using UnityEngine;
using Arcontio.Core;

namespace Arcontio.Views
{
    public sealed class MapViewBridgeSmokeTest : MonoBehaviour
    {
        private void Start()
        {
            var host = SimulationHost.Instance;
            if (host == null)
            {
                Debug.LogError("[MapView] SimulationHost.Instance � null. Non stai usando Bootstrap o runtime non persistente.");
                return;
            }

            Debug.Log($"[MapView] Connesso. Tick attuale = {host.TickIndex}, NPC = {host.World.NpcDna.Count}");
        }
    }
}
