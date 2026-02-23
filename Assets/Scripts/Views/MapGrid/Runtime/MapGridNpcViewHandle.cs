using UnityEngine;

namespace Arcontio.View.MapGrid
{
    /// <summary>
    /// View-only handle:
    /// mette in chiaro (in modo stabile) l'id dell'NPC a cui appartiene questo GameObject.
    ///
    /// Perché serve:
    /// - evitare parsing da name ("NPC_123")
    /// - evitare di tenere dizionari extra nel tooltip system
    /// - mantenere il binding “World -> View” unidirezionale e semplice.
    /// </summary>
    public sealed class MapGridNpcViewHandle : MonoBehaviour
    {
        public int NpcId;
    }
}
