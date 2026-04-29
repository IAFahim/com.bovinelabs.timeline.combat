using BovineLabs.Timeline.Data;
using Unity.Entities;
using Unity.Properties;

namespace BovineLabs.Timeline.Combat
{
    public struct AttackMotionAnimated : IAnimatedComponent<CombatMotionData>
    {
        public CombatMotionData AuthoredData;
        [CreateProperty] public CombatMotionData Value { get; set; }
    }

    public struct LocomotionAnimated : IAnimatedComponent<CombatMotionData>
    {
        public CombatMotionData AuthoredData;
        [CreateProperty] public CombatMotionData Value { get; set; }
    }

    public struct NavigationAnimated : IAnimatedComponent<CombatMotionData>
    {
        public CombatMotionData AuthoredData;
        [CreateProperty] public CombatMotionData Value { get; set; }
    }

    public struct AvoidanceAnimated : IAnimatedComponent<CombatMotionData>
    {
        public CombatMotionData AuthoredData;
        [CreateProperty] public CombatMotionData Value { get; set; }
    }
}
