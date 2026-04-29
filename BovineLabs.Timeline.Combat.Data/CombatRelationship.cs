using Unity.Entities;
using Unity.Mathematics;

namespace BovineLabs.Timeline.Combat
{
    public struct CombatRelationship : IComponentData
    {
        public int FactionId;
        public uint HostileFactions;
        public uint FriendlyFactions;

        public bool IsHostileTo(CombatRelationship other) =>
            (HostileFactions & (1u << other.FactionId)) != 0;

        public bool IsFriendlyTo(CombatRelationship other) =>
            FactionId == other.FactionId && FactionId != 0 ||
            (FriendlyFactions & (1u << other.FactionId)) != 0;
    }
}
