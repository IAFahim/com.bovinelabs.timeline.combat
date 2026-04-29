using BovineLabs.Timeline.Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace BovineLabs.Timeline.Combat
{
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    public partial struct CombatTimelineBakingSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var em = state.EntityManager;
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var binding in SystemAPI.Query<RefRO<TrackBinding>>().WithAll<LocomotionAnimated>()
                         .WithOptions(EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IncludePrefab))
            {
                var target = binding.ValueRO.Value;
                if (target == Entity.Null) continue;

                if (!SystemAPI.HasComponent<ResolvedMotion>(target))
                    ecb.AddComponent<ResolvedMotion>(target);
            }

            foreach (var binding in SystemAPI.Query<RefRO<TrackBinding>>().WithAll<FacingAnimated>()
                         .WithOptions(EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IncludePrefab))
            {
                var target = binding.ValueRO.Value;
                if (target == Entity.Null) continue;

                if (!SystemAPI.HasComponent<ResolvedFacing>(target))
                    ecb.AddComponent<ResolvedFacing>(target);
            }

            ecb.Playback(em);
            ecb.Dispose();
        }
    }
}
