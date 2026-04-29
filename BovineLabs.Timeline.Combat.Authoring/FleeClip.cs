using BovineLabs.Timeline.Authoring;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Timeline;

namespace BovineLabs.Timeline.Combat.Authoring
{
    public class FleeClip : DOTSClip, ITimelineClipAsset
    {
        [Header("Target")]
        public BovineLabs.Reaction.Data.Core.Target threat = BovineLabs.Reaction.Data.Core.Target.Target;

        [Header("Motion")]
        public float speed = 8f;
        public float speedScale = 1.35f;
        public float accelerationScale = 1.2f;

        [Header("Range")]
        public float panicRadius = 15f;

        [Header("Flags")]
        public bool ignoreAvoidance;
        public bool hardBrakeOnEnd;

        public override double duration => 1;
        public ClipCaps clipCaps => ClipCaps.Blending | ClipCaps.Looping;

        public override void Bake(Entity clipEntity, BakingContext context)
        {
            var flags = CombatMotionFlags.None;
            if (ignoreAvoidance) flags |= CombatMotionFlags.IgnoreAvoidance;
            if (hardBrakeOnEnd) flags |= CombatMotionFlags.HardBrakeOnEnd;

            context.Baker.AddComponent(clipEntity, new FleeClipData
            {
                Threat = threat,
                PanicRadiusSq = panicRadius * panicRadius,
                Speed = speed,
                SpeedScale = speedScale,
                AccelerationScale = accelerationScale,
                Flags = flags,
            });

            base.Bake(clipEntity, context);
        }
    }

    public struct FleeClipData : IComponentData
    {
        public BovineLabs.Reaction.Data.Core.Target Threat;
        public float PanicRadiusSq;
        public float Speed;
        public float SpeedScale;
        public float AccelerationScale;
        public CombatMotionFlags Flags;
    }
}
