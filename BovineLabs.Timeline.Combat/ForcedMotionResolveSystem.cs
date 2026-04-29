using Unity.Burst;
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

            foreach (var (_, entity) in
                SystemAPI.Query<RefRO<CombatAgent>>()
                    .WithAll<ForcedMotionState>()
                    .WithEntityAccess())
            {
                if (!SystemAPI.HasBuffer<ForcedMotionRequest>(entity)) continue;

                var requests = SystemAPI.GetBuffer<ForcedMotionRequest>(entity);
                if (requests.Length == 0) continue;

                var bestPriority = -1;
                var bestIndex = -1;

                for (int i = 0; i < requests.Length; i++)
                {
                    var priority = GetPriority(requests[i].Mode);
                    if (priority > bestPriority)
                    {
                        bestPriority = priority;
                        bestIndex = i;
                    }
                }

                if (bestIndex >= 0)
                {
                    var best = requests[bestIndex];
                    SystemAPI.SetComponent(entity, new ForcedMotionState
                    {
                        Mode = best.Mode,
                        Apply = best.Apply,
                        Vector = best.Vector,
                        TargetPosition = best.TargetPosition,
                        Strength = best.Strength,
                        Damping = best.Damping,
                        RemainingTime = best.Duration,
                        Flags = best.Flags,
                    });

                    if (!SystemAPI.IsComponentEnabled<ForcedMotionState>(entity))
                        SystemAPI.SetComponentEnabled<ForcedMotionState>(entity, true);
                }

                requests.Clear();
            }

            foreach (var forcedState in SystemAPI.Query<RefRW<ForcedMotionState>>())
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
