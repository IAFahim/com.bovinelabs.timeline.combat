using BovineLabs.Timeline;
using Unity.Mathematics;

namespace BovineLabs.Timeline.Combat
{
    public readonly struct CombatMotionMixer : IMixer<CombatMotionData>
    {
        public CombatMotionData Lerp(in CombatMotionData a, in CombatMotionData b, in float s)
        {
            return new CombatMotionData
            {
                Mode = s < 0.5f ? a.Mode : b.Mode,
                DesiredVelocity = math.lerp(a.DesiredVelocity, b.DesiredVelocity, s),
                DesiredDirection = math.normalizesafe(math.lerp(a.DesiredDirection, b.DesiredDirection, s)),
                TargetPosition = math.lerp(a.TargetPosition, b.TargetPosition, s),
                SpeedScale = math.lerp(a.SpeedScale, b.SpeedScale, s),
                AccelerationScale = math.lerp(a.AccelerationScale, b.AccelerationScale, s),
                BrakeScale = math.lerp(a.BrakeScale, b.BrakeScale, s),
                ArrivalRadius = math.lerp(a.ArrivalRadius, b.ArrivalRadius, s),
                MaintainDistance = math.lerp(a.MaintainDistance, b.MaintainDistance, s),
                MaxContribution = math.lerp(a.MaxContribution, b.MaxContribution, s),
                Flags = s < 0.5f ? a.Flags : b.Flags,
            };
        }

        public CombatMotionData Add(in CombatMotionData a, in CombatMotionData b)
        {
            var avoid = b.DesiredVelocity;
            var maxC = b.MaxContribution;

            if (maxC > 0f && math.lengthsq(avoid) > maxC * maxC)
                avoid = math.normalize(avoid) * maxC;

            return new CombatMotionData
            {
                Mode = a.Mode != CombatMotionMode.None ? a.Mode : b.Mode,
                DesiredVelocity = a.DesiredVelocity + avoid,
                DesiredDirection = a.DesiredDirection,
                TargetPosition = a.TargetPosition,
                SpeedScale = math.max(a.SpeedScale, b.SpeedScale),
                AccelerationScale = math.max(a.AccelerationScale, b.AccelerationScale),
                BrakeScale = math.max(a.BrakeScale, b.BrakeScale),
                MaxContribution = math.max(a.MaxContribution, b.MaxContribution),
                Flags = a.Flags,
            };
        }
    }
}
