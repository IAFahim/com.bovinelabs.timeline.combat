using System;
using Unity.Entities;

namespace BovineLabs.Timeline.Combat
{
    public enum CombatActionKind : byte
    {
        None,
        Idle,
        Engaging,
        Attacking,
        Fleeing,
        MaintainingDistance,
        Recovering,
        HitReaction,
        Staggered,
        Grabbed,
        Dying,
        Dead,
    }

    [Flags]
    public enum CombatActionFlags : byte
    {
        None = 0,
        CanCancel = 1 << 0,
        CanCombo = 1 << 1,
        Commited = 1 << 2,
    }

    public struct CombatActionState : IComponentData
    {
        public CombatActionKind Kind;
        public Entity Target;
        public float StartedAt;
        public float MinCommitTime;
        public CombatActionFlags Flags;

        public bool TryGetCanCancel(float currentTime) =>
            (Flags & CombatActionFlags.Commited) == 0 ||
            currentTime - StartedAt >= MinCommitTime;
    }
}
