using System;
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
        }
    }
}
