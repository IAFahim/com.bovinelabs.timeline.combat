using System;
using Unity.Entities;
using Unity.Mathematics;

namespace BovineLabs.Timeline.Combat
{
    public struct SensedTarget : IBufferElementData
    {
        public Entity Entity;
        public float3 Position;
        public float DistanceSq;
        public float ThreatScore;
        public TargetRelation Relation;
        public SensedTargetFlags Flags;
    }

    public enum TargetRelation : byte { Unknown, Hostile, Friendly, Neutral }

    [Flags]
    public enum SensedTargetFlags : ushort
    {
        None = 0,
        InLineOfSight = 1 << 0,
        InAttackRange = 1 << 1,
        RecentlyDamagedMe = 1 << 2,
    }

    public struct TargetSlot : IBufferElementData
    {
        public Entity Entity;
        public int SlotId;
    }

    public struct CombatTargets : IComponentData { }

    public struct TargetMemory : IComponentData
    {
        public Entity LastTarget;
        public float3 LastKnownPosition;
        public float LastSeenTime;
        public float MemoryDuration;
    }
}
