using System;
using BovineLabs.Timeline.Data;
using Unity.Entities;
using Unity.Mathematics;

namespace BovineLabs.Timeline.Combat
{
    public enum FacingMode : byte
    {
        None,
        FaceMovement,
        FaceTarget,
        FaceDirection,
        FacePosition,
        LockCurrent,
    }

    [Flags]
    public enum FacingFlags : ushort
    {
        None = 0,
        UseLookAt = 1 << 0,
        InstantSnap = 1 << 1,
        PreservePitch = 1 << 2,
    }

    public struct FacingData
    {
        public FacingMode Mode;
        public Entity Target;
        public float3 Direction;
        public float3 Position;
        public float TurnSpeedScale;
        public float AngularDampingScale;
        public FacingFlags Flags;

        public static FacingData None => new()
        {
            Mode = FacingMode.None,
            TurnSpeedScale = 1f,
            AngularDampingScale = 1f,
        };
    }

    public struct FacingAnimated : IAnimatedComponent<FacingData>
    {
        public FacingData AuthoredData;
        [Unity.Properties.CreateProperty] public FacingData Value { get; set; }
    }

    public struct ResolvedFacing : IComponentData
    {
        public FacingData Value;
        public static ResolvedFacing None => new() { Value = FacingData.None };
    }
}
