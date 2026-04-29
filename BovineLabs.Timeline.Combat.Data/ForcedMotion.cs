using System;
using Unity.Entities;
using Unity.Mathematics;

namespace BovineLabs.Timeline.Combat
{
    public enum ForcedMotionMode : byte
    {
        Impulse,
        VelocityOverride,
        PullToPosition,
        Freeze,
        Grabbed,
        Launch,
    }

    public enum ForcedMotionApply : byte
    {
        ImpulseOnce,
        VelocityOverride,
        AccelerationToward,
        Freeze,
        PullToTarget,
    }

    [Flags]
    public enum ForcedMotionFlags : ushort
    {
        None = 0,
        ZeroCurrentVelocity = 1 << 0,
        Additive = 1 << 1,
        SuppressAttackMotion = 1 << 2,
        SuppressLocomotion = 1 << 3,
        SuppressNavigation = 1 << 4,
        LockFacing = 1 << 5,
        DisableInput = 1 << 6,
        DisableBrain = 1 << 7,
        IgnoreSuperArmor = 1 << 8,
        PreserveYVelocity = 1 << 9,
    }

    public struct ForcedMotionRequest : IBufferElementData
    {
        public ForcedMotionMode Mode;
        public ForcedMotionApply Apply;
        public float3 Vector;
        public float3 TargetPosition;
        public float Duration;
        public float Damping;
        public float Strength;
        public ForcedMotionFlags Flags;
    }

    public struct ForcedMotionState : IComponentData, IEnableableComponent
    {
        public ForcedMotionMode Mode;
        public ForcedMotionApply Apply;
        public float3 Vector;
        public float3 TargetPosition;
        public float Strength;
        public float Damping;
        public float RemainingTime;
        public ForcedMotionFlags Flags;

        public bool IsActive => RemainingTime > 0f;
    }
}
