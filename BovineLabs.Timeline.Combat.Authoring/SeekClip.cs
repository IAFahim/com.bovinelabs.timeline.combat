using BovineLabs.Timeline.Authoring;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Timeline;

namespace BovineLabs.Timeline.Combat.Authoring
{
    public class SeekClip : DOTSClip, ITimelineClipAsset
    {
        [Header("Target")]
        public BovineLabs.Reaction.Data.Core.Target target = BovineLabs.Reaction.Data.Core.Target.Target;

        [Header("Motion")]
        public float speed = 5f;
        public float speedScale = 1f;
        public float accelerationScale = 1f;

        [Header("Arrival")]
        public float arrivalRadius = 1f;

        [Header("Flags")]
        public bool ignoreAvoidance;

        public override double duration => 1;
        public ClipCaps clipCaps => ClipCaps.Blending | ClipCaps.Looping;

        public override void Bake(Entity clipEntity, BakingContext context)
        {
            var flags = CombatMotionFlags.None;
            if (ignoreAvoidance) flags |= CombatMotionFlags.IgnoreAvoidance;

            context.Baker.AddComponent(clipEntity, new SeekClipData
            {
                Target = target,
                Speed = speed,
                SpeedScale = speedScale,
                AccelerationScale = accelerationScale,
                ArrivalRadius = arrivalRadius,
                Flags = flags,
            });

            base.Bake(clipEntity, context);
        }
    }

    public struct SeekClipData : IComponentData
    {
        public BovineLabs.Reaction.Data.Core.Target Target;
        public float Speed;
        public float SpeedScale;
        public float AccelerationScale;
        public float ArrivalRadius;
        public CombatMotionFlags Flags;
    }
}
