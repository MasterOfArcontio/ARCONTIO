namespace Arcontio.Core
{
    /// <summary>
    /// Marker interface per eventi.
    /// </summary>
    public interface ISimEvent { }

    // Esempi di eventi
    public sealed class NpcBecameHungryEvent : ISimEvent
    {
        public readonly int NpcId;
        public NpcBecameHungryEvent(int npcId) { NpcId = npcId; }
    }

    public sealed class ResourceShortageEvent : ISimEvent
    {
        public readonly string ResourceName;
        public ResourceShortageEvent(string resourceName) { ResourceName = resourceName; }
    }

    public sealed class NpcWasFedEvent : ISimEvent
    {
        public readonly int NpcId;
        public readonly int UsedFood;
        public readonly float HungerAfter;

        public NpcWasFedEvent(int npcId, int usedFood, float hungerAfter)
        {
            NpcId = npcId;
            UsedFood = usedFood;
            HungerAfter = hungerAfter;
        }
    }
}