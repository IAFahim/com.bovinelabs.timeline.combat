using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace BovineLabs.Timeline.Combat
{
    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(CombatPhysicsMotorSystem))]
    public partial struct CombatMotionResolveSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (attack, locomotion, navigation, avoidance, forcedState, resolved, locks) in
                SystemAPI.Query<
                    RefRO<AttackMotionAnimated>,
                    RefRO<LocomotionAnimated>,
                    RefRO<NavigationAnimated>,
                    RefRO<AvoidanceAnimated>,
                    RefRO<ForcedMotionState>,
                    RefRW<ResolvedMotion>,
                    RefRO<ResolvedCombatLock>>())
            {
                if (forcedState.ValueRO.IsActive)
                {
                    resolved.ValueRW.Motion = new CombatMotionData
                    {
                        Mode = forcedState.ValueRO.Apply == ForcedMotionApply.Freeze
                            ? CombatMotionMode.Stop
                            : CombatMotionMode.DesiredVelocity,
                        DesiredVelocity = forcedState.ValueRO.Vector,
                        SpeedScale = 1f,
                        AccelerationScale = 1f,
                        BrakeScale = 1f,
                        Flags = CombatMotionFlags.IgnoreAvoidance,
                    };
                    resolved.ValueRW.Source = CombatMotionSource.Forced;
                    continue;
                }

                CombatMotionData motion;
                CombatMotionSource source;

                var attackVal = attack.ValueRO.Value;
                var locoVal = locomotion.ValueRO.Value;
                var navVal = navigation.ValueRO.Value;

                if (attackVal.Mode != CombatMotionMode.None)
                {
                    motion = attackVal;
                    source = CombatMotionSource.Attack;
                }
                else if (locoVal.Mode != CombatMotionMode.None)
                {
                    motion = locoVal;
                    source = CombatMotionSource.Locomotion;
                }
                else if (navVal.Mode != CombatMotionMode.None)
                {
                    motion = navVal;
                    source = CombatMotionSource.Navigation;
                }
                else
                {
                    motion = new CombatMotionData
                    {
                        Mode = CombatMotionMode.Stop,
                        SpeedScale = 1f,
                        AccelerationScale = 1f,
                        BrakeScale = 1f,
                    };
                    source = CombatMotionSource.Idle;
                }

                var avoidVal = avoidance.ValueRO.Value;
                var lockFlags = locks.ValueRO.Flags;

                if (avoidVal.Mode != CombatMotionMode.None &&
                    (motion.Flags & CombatMotionFlags.IgnoreAvoidance) == 0 &&
                    (lockFlags & CombatLockFlags.DisableAvoidance) == 0)
                {
                    var avoid = avoidVal.DesiredVelocity;
                    var maxC = avoidVal.MaxContribution;

                    if (maxC > 0f && math.lengthsq(avoid) > maxC * maxC)
                        avoid = math.normalize(avoid) * maxC;

                    motion.DesiredVelocity += avoid;
                }

                resolved.ValueRW.Motion = motion;
                resolved.ValueRW.Source = source;
            }
        }
    }
}
