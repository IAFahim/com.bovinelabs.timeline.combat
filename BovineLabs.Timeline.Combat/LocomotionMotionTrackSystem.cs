using BovineLabs.Core.Extensions;
using BovineLabs.Core.Iterators;
using BovineLabs.Core.Jobs;
using BovineLabs.Reaction.Data.Core;
using BovineLabs.Timeline.Combat.Authoring;
using BovineLabs.Timeline.Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace BovineLabs.Timeline.Combat
{
    [UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
    public partial struct LocomotionMotionTrackSystem : ISystem
    {
        private TrackBlendImpl<CombatMotionData, LocomotionAnimated> blendImpl;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            blendImpl.OnCreate(ref state);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            blendImpl.OnDestroy(ref state);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var targetsLookup = SystemAPI.GetComponentLookup<Targets>(true);
            var customsLookup = SystemAPI.GetComponentLookup<TargetsCustom>(true);
            var transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);

            state.Dependency = new ComputeFleeJob
            {
                TargetsLookup = targetsLookup,
                CustomsLookup = customsLookup,
                TransformLookup = transformLookup,
            }.ScheduleParallel(state.Dependency);

            state.Dependency = new ComputeSeekJob
            {
                TargetsLookup = targetsLookup,
                CustomsLookup = customsLookup,
                TransformLookup = transformLookup,
            }.ScheduleParallel(state.Dependency);

            state.Dependency = new ComputeStopJob()
                .ScheduleParallel(state.Dependency);

            state.Dependency = new PrepareJob()
                .ScheduleParallel(state.Dependency);

            var blendData = blendImpl.Update(ref state);

            var resolvedLookup = SystemAPI.GetComponentLookup<ResolvedMotion>();

            state.Dependency = new WriteResolvedJob
            {
                BlendData = blendData,
                ResolvedLookup = resolvedLookup,
            }.ScheduleParallel(blendData, 64, state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(ClipActive))]
        private partial struct PrepareJob : IJobEntity
        {
            private void Execute(ref LocomotionAnimated animated)
            {
                animated.Value = animated.AuthoredData;
            }
        }

        [BurstCompile]
        [WithAll(typeof(ClipActive))]
        private partial struct ComputeFleeJob : IJobEntity
        {
            [ReadOnly] public ComponentLookup<Targets> TargetsLookup;
            [ReadOnly] public ComponentLookup<TargetsCustom> CustomsLookup;
            [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;

            private void Execute(
                in TrackBinding binding,
                in FleeClipData flee,
                ref LocomotionAnimated animated)
            {
                var self = binding.Value;
                if (self == Entity.Null) return;

                if (!TargetsLookup.TryGetComponent(self, out var targets)) return;
                if (!TransformLookup.TryGetComponent(self, out var selfTransform)) return;

                var threatEntity = targets.Get(flee.Threat, self, CustomsLookup);
                if (threatEntity == Entity.Null || !TransformLookup.TryGetComponent(threatEntity, out var threatTransform))
                {
                    animated.AuthoredData = CombatMotionData.None;
                    return;
                }

                var selfPos = selfTransform.Position;
                var threatPos = threatTransform.Position;
                var diff = selfPos - threatPos;
                var distSq = math.lengthsq(diff);

                if (distSq > flee.PanicRadiusSq)
                {
                    animated.AuthoredData = CombatMotionData.None;
                    return;
                }

                var dir = math.normalizesafe(diff);
                var velocity = new float3(dir.x, 0f, dir.z) * flee.Speed;

                animated.AuthoredData = new CombatMotionData
                {
                    Mode = CombatMotionMode.DesiredVelocity,
                    DesiredVelocity = velocity,
                    SpeedScale = flee.SpeedScale,
                    AccelerationScale = flee.AccelerationScale,
                    BrakeScale = 1f,
                    Flags = flee.Flags,
                };
            }
        }

        [BurstCompile]
        [WithAll(typeof(ClipActive))]
        private partial struct ComputeSeekJob : IJobEntity
        {
            [ReadOnly] public ComponentLookup<Targets> TargetsLookup;
            [ReadOnly] public ComponentLookup<TargetsCustom> CustomsLookup;
            [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;

            private void Execute(
                in TrackBinding binding,
                in SeekClipData seek,
                ref LocomotionAnimated animated)
            {
                var self = binding.Value;
                if (self == Entity.Null) return;

                if (!TargetsLookup.TryGetComponent(self, out var targets)) return;
                if (!TransformLookup.TryGetComponent(self, out var selfTransform)) return;

                var targetEntity = targets.Get(seek.Target, self, CustomsLookup);
                if (targetEntity == Entity.Null || !TransformLookup.TryGetComponent(targetEntity, out var targetTransform))
                {
                    animated.AuthoredData = CombatMotionData.None;
                    return;
                }

                var selfPos = selfTransform.Position;
                var targetPos = targetTransform.Position;
                var diff = targetPos - selfPos;
                var dist = math.length(diff);

                if (dist < seek.ArrivalRadius)
                {
                    animated.AuthoredData = new CombatMotionData
                    {
                        Mode = CombatMotionMode.Stop,
                        SpeedScale = 1f,
                        AccelerationScale = 1f,
                        BrakeScale = 2f,
                    };
                    return;
                }

                var dir = diff / dist;
                var velocity = new float3(dir.x, 0f, dir.z) * seek.Speed;

                animated.AuthoredData = new CombatMotionData
                {
                    Mode = CombatMotionMode.DesiredVelocity,
                    DesiredVelocity = velocity,
                    SpeedScale = seek.SpeedScale,
                    AccelerationScale = seek.AccelerationScale,
                    BrakeScale = 1f,
                    Flags = seek.Flags,
                };
            }
        }

        [BurstCompile]
        [WithAll(typeof(ClipActive))]
        private partial struct ComputeStopJob : IJobEntity
        {
            private void Execute(
                in StopClipData stop,
                ref LocomotionAnimated animated)
            {
                animated.AuthoredData = new CombatMotionData
                {
                    Mode = CombatMotionMode.Stop,
                    SpeedScale = 1f,
                    AccelerationScale = 1f,
                    BrakeScale = stop.BrakeScale,
                    Flags = stop.Flags,
                };
            }
        }

        [BurstCompile]
        private struct WriteResolvedJob : IJobParallelHashMapDefer
        {
            [ReadOnly] public NativeParallelHashMap<Entity, MixData<CombatMotionData>>.ReadOnly BlendData;
            [NativeDisableParallelForRestriction] public ComponentLookup<ResolvedMotion> ResolvedLookup;

            public void ExecuteNext(int entryIndex, int jobIndex)
            {
                this.Read(BlendData, entryIndex, out var entity, out var mixData);
                var blended = JobHelpers.Blend<CombatMotionData, CombatMotionMixer>(ref mixData, CombatMotionData.None);

                if (!ResolvedLookup.HasComponent(entity)) return;

                var resolved = ResolvedLookup[entity];
                if (blended.Mode != CombatMotionMode.None)
                {
                    resolved.Motion = blended;
                    resolved.Source = CombatMotionSource.Locomotion;
                }
                ResolvedLookup[entity] = resolved;
            }
        }
    }
}
