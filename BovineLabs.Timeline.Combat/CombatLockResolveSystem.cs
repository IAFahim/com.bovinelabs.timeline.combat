using Unity.Burst;
using Unity.Entities;

namespace BovineLabs.Timeline.Combat
{
    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public partial struct CombatLockResolveSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var dt = SystemAPI.Time.DeltaTime;
            if (dt <= 0f) return;

            foreach (var (requests, resolved) in
                SystemAPI.Query<DynamicBuffer<CombatLockRequest>, RefRW<ResolvedCombatLock>>())
            {
                for (int i = requests.Length - 1; i >= 0; i--)
                {
                    var req = requests[i];
                    req.RemainingTime -= dt;

                    if (req.RemainingTime <= 0f)
                    {
                        requests.RemoveAt(i);
                        continue;
                    }

                    requests[i] = req;
                }

                var flags = CombatLockFlags.None;
                for (int i = 0; i < requests.Length; i++)
                    flags |= requests[i].Flags;

                resolved.ValueRW.Flags = flags;
            }
        }
    }
}
