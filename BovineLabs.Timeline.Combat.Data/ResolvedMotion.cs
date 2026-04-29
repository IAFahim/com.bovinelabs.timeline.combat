using System;
using Unity.Entities;
using Unity.Mathematics;

namespace BovineLabs.Timeline.Combat
{
    public enum CombatMotionSource : byte { None, Forced, Attack, Locomotion, Navigation, Idle }

    public struct ResolvedMotion : IComponentData
    {
        public CombatMotionData Motion;
        public CombatMotionSource Source;
    }
}
