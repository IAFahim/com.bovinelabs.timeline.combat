using System;
using System.ComponentModel;
using BovineLabs.Reaction.Authoring.Core;
using BovineLabs.Timeline.Authoring;
using BovineLabs.Timeline.EntityLinks;
using BovineLabs.Timeline.EntityLinks.Authoring;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Timeline;

namespace BovineLabs.Timeline.Combat.Authoring
{
    [Serializable]
    [TrackBindingType(typeof(TargetsAuthoring))]
    [TrackClipType(typeof(FleeClip))]
    [TrackClipType(typeof(SeekClip))]
    [TrackClipType(typeof(StopClip))]
    [TrackColor(0.2f, 0.8f, 0.3f)]
    [DisplayName("BovineLabs/Combat/Locomotion")]
    public class LocomotionTrack : DOTSTrack
    {
        [Header("Links")]
        public EntityLinkSchema physicsLink;
        public EntityLinkSchema essenceLink;

        [Header("Locomotion")]
        public float maxSpeed = 10f;
        public float maxAcceleration = 50f;
        public float turnSpeed = 15f;
        public float angularDamping = 10f;
        
        [Header("Sensors")]
        public float sensorRadius = 20f;
        public int maxSensedTargets = 8;

        protected override void Bake(BakingContext context)
        {
            base.Bake(context);

            var entity = context.Target;
            if (entity == Entity.Null) return;

            context.Baker.AddComponent(entity, new CombatAgentProfile
            {
                MaxSpeed = maxSpeed,
                MaxAcceleration = maxAcceleration,
                TurnSpeed = turnSpeed,
                AngularDamping = angularDamping,
                SensorRadius = sensorRadius,
                MaxSensedTargets = maxSensedTargets,
            });

            ushort pLink = 0;
            if (physicsLink != null && EntityLinkAuthoringUtility.TryGetKey(physicsLink, out var pk))
                pLink = pk;
            
            ushort eLink = 0;
            if (essenceLink != null && EntityLinkAuthoringUtility.TryGetKey(essenceLink, out var ek))
                eLink = ek;

            context.Baker.AddComponent(entity, new CombatAgent 
            {
                PhysicsLink = pLink,
                EssenceLink = eLink
            });

            context.Baker.AddComponent<ResolvedMotion>(entity);
            context.Baker.AddComponent<AttackMotionAnimated>(entity);
            context.Baker.AddComponent<LocomotionAnimated>(entity);
            context.Baker.AddComponent<NavigationAnimated>(entity);
            context.Baker.AddComponent<AvoidanceAnimated>(entity);

            context.Baker.AddComponent<FacingAnimated>(entity);
            context.Baker.AddComponent<ResolvedFacing>(entity);

            context.Baker.AddComponent<ResolvedCombatLock>(entity);
            context.Baker.AddBuffer<CombatLockRequest>(entity);

            context.Baker.AddComponent<ForcedMotionState>(entity);
            context.Baker.SetComponentEnabled<ForcedMotionState>(entity, false);
            context.Baker.AddBuffer<ForcedMotionRequest>(entity);
        }
    }
}
