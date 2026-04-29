using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace BovineLabs.Timeline.Combat
{
    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(CombatMotionResolveSystem))]
    public partial struct ForcedMotionResolveSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var dt = SystemAPI.Time.DeltaTime;
            if (dt <= 0f) return;

            var stateLookup = SystemAPI.GetComponentLookup<ForcedMotionState>();
            var lockLookup = SystemAPI.GetComponentLookup<ResolvedCombatLock>();

            foreach (var (requests, entity) in
                SystemAPI.Query<DynamicBuffer<ForcedMotionRequest>>()
                    .WithAll<CombatAgent>()
                    .WithEntityAccess())
            {
                var hasBest = false;
                var bestPriority = -1;
                var bestIndex = -1;

                for (int i = 0; i < requests.Length; i++)
                {
                    var req = requests[i];
                    var priority = GetPriority(req.Mode);

                    if (priority > bestPriority ||
                        (priority == bestPriority && !hasBest))
                    {
                        bestPriority = priority;
                        bestIndex = i;
                        hasBest = true;
                    }
                }

                if (hasBest && stateLookup.HasComponent(entity))
                {
                    var best = requests[bestIndex];

                    if (!stateLookup.IsComponentEnabled(entity))
                        stateLookup.SetComponentEnabled(entity, true);

                    stateLookup[entity] = new ForcedMotionState
                    {
                        Mode = best.Mode,
                        Apply = best.Apply,
                        Vector = best.Vector,
                        TargetPosition = best.TargetPosition,
                        Strength = best.Strength,
                        Damping = best.Damping,
                        RemainingTime = best.Duration,
                        Flags = best.Flags,
                    };
                }

                requests.Clear();
            }

            foreach (var (forcedState, entity) in
                SystemAPI.Query<RefRW<ForcedMotionState>>()
                    .WithEntityAccess())
            {
                if (!forcedState.ValueRW.IsActive) continue;

                var fs = forcedState.ValueRW;
                fs.RemainingTime -= dt;

                if (fs.RemainingTime <= 0f)
                {
                    fs.RemainingTime = 0f;
                    fs.Mode = default;
                    fs.Vector = float3.zero;
                    forcedState.ValueRW = fs;

                    if (stateLookup.HasComponent(entity))
                        stateLookup.SetComponentEnabled(entity, false);
                }
                else
                {
                    var damping = math.exp(-fs.Damping * dt);
                    fs.Vector *= damping;
                    forcedState.ValueRW = fs;
                }
            }
        }

        private static int GetPriority(ForcedMotionMode mode) => mode switch
        {
            ForcedMotionMode.Grabbed => 5,
            ForcedMotionMode.Freeze => 4,
            ForcedMotionMode.Launch => 3,
            ForcedMotionMode.PullToPosition => 2,
            ForcedMotionMode.VelocityOverride => 1,
            ForcedMotionMode.Impulse => 0,
            _ => -1,
        };
    }
}
