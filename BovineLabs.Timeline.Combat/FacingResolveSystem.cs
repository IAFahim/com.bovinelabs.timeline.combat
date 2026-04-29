using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace BovineLabs.Timeline.Combat
{
    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(CombatMotionResolveSystem))]
    public partial struct FacingResolveSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);

            foreach (var (facingRO, resolvedFacingRW, resolvedMotionRO, locksRO) in
                SystemAPI.Query<
                    RefRO<FacingAnimated>,
                    RefRW<ResolvedFacing>,
                    RefRO<ResolvedMotion>,
                    RefRO<ResolvedCombatLock>>())
            {
                if ((locksRO.ValueRO.Flags & CombatLockFlags.DisableTurn) != 0)
                {
                    resolvedFacingRW.ValueRW = ResolvedFacing.None;
                    continue;
                }

                var data = facingRO.ValueRO.Value;

                switch (data.Mode)
                {
                    case FacingMode.FaceTarget:
                    {
                        if (data.Target == Entity.Null ||
                            !transformLookup.TryGetComponent(data.Target, out var targetTransform))
                        {
                            resolvedFacingRW.ValueRW = ResolvedFacing.None;
                            continue;
                        }

                        data.Direction = targetTransform.Position;
                        data.Mode = FacingMode.FacePosition;
                        resolvedFacingRW.ValueRW = new ResolvedFacing { Value = data };
                        break;
                    }

                    case FacingMode.FaceMovement:
                    {
                        var vel = resolvedMotionRO.ValueRO.Motion.DesiredVelocity;
                        var xz = new float3(vel.x, 0f, vel.z);
                        if (math.lengthsq(xz) < 0.0001f)
                        {
                            resolvedFacingRW.ValueRW = ResolvedFacing.None;
                            continue;
                        }

                        data.Direction = math.normalize(xz);
                        data.Mode = FacingMode.FaceDirection;
                        resolvedFacingRW.ValueRW = new ResolvedFacing { Value = data };
                        break;
                    }

                    case FacingMode.None:
                        resolvedFacingRW.ValueRW = ResolvedFacing.None;
                        break;

                    default:
                        resolvedFacingRW.ValueRW = new ResolvedFacing { Value = data };
                        break;
                }
            }
        }
    }
}
