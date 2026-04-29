using System;
using Unity.Entities;

namespace BovineLabs.Timeline.Combat
{
    [Flags]
    public enum CombatLockFlags : ushort
    {
        None = 0,
        DisableInput = 1 << 0,
        DisableBrain = 1 << 1,
        DisableTurn = 1 << 2,
        DisableAvoidance = 1 << 3,
        SuperArmor = 1 << 4,
        Invulnerable = 1 << 5,
        Rooted = 1 << 6,
    }

    public struct CombatLockRequest : IBufferElementData
    {
        public CombatLockFlags Flags;
        public Entity Source;
        public float RemainingTime;
    }

    public struct ResolvedCombatLock : IComponentData
    {
        public CombatLockFlags Flags;
    }
}
