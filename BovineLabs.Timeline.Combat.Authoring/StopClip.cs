using BovineLabs.Timeline.Authoring;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Timeline;

namespace BovineLabs.Timeline.Combat.Authoring
{
    public class StopClip : DOTSClip, ITimelineClipAsset
    {
        [Header("Braking")]
        public float brakeScale = 2f;
        public bool hardBrakeOnEnd = true;

        public override double duration => 0.5;
        public ClipCaps clipCaps => ClipCaps.Blending;

        public override void Bake(Entity clipEntity, BakingContext context)
        {
            var flags = CombatMotionFlags.None;
            if (hardBrakeOnEnd) flags |= CombatMotionFlags.HardBrakeOnEnd;

            context.Baker.AddComponent(clipEntity, new StopClipData
            {
                BrakeScale = brakeScale,
                Flags = flags,
            });

            base.Bake(clipEntity, context);
        }
    }

    public struct StopClipData : IComponentData
    {
        public float BrakeScale;
        public CombatMotionFlags Flags;
    }
}
