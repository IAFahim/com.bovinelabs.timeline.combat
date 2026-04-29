using System;
using Unity.Mathematics;

namespace BovineLabs.Timeline.Combat
{
    public enum CombatMotionMode : byte
    {
        None,
        DesiredVelocity,
        DesiredDirection,
        ArriveAtPosition,
        MaintainDistance,
        Stop,
        HoldPosition,
        RootDelta,
        DashToTarget,
    }

    [Flags]
    public enum CombatMotionFlags : byte
    {
        None = 0,
        PreserveYVelocity = 1 << 0,
        IgnoreAvoidance = 1 << 1,
        UseCurrentTarget = 1 << 2,
        AllowSlide = 1 << 3,
        HardBrakeOnEnd = 1 << 4,
        IgnoreNavigation = 1 << 5,
    }

    public struct CombatMotionData
    {
        public CombatMotionMode Mode;
        public float3 DesiredVelocity;
        public float3 DesiredDirection;
        public float3 TargetPosition;
        public float SpeedScale;
        public float AccelerationScale;
        public float BrakeScale;
        public float ArrivalRadius;
        public float MaintainDistance;
        public float MaxContribution;
        public CombatMotionFlags Flags;

        public static CombatMotionData None => new()
        {
            Mode = CombatMotionMode.None,
            SpeedScale = 1f,
            AccelerationScale = 1f,
            BrakeScale = 1f,
        };
    }
}
