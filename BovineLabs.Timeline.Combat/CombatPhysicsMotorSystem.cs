using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;

namespace BovineLabs.Timeline.Combat
{
    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(PhysicsSystemGroup))]
    [UpdateAfter(typeof(CombatMotionResolveSystem))]
    public partial struct CombatPhysicsMotorSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var dt = SystemAPI.Time.DeltaTime;
            if (dt <= 0f) return;

            foreach (var (motionRO, velocityRW, profileRO) in
                SystemAPI.Query<RefRO<ResolvedMotion>, RefRW<PhysicsVelocity>, RefRO<CombatAgentProfile>>())
            {
                var motion = motionRO.ValueRO.Motion;
                var profile = profileRO.ValueRO;
                var current = velocityRW.ValueRO;

                var currentXZ = new float2(current.Linear.x, current.Linear.z);

                float2 targetXZ;
                float accelScale;
                float brakeScale;

                switch (motion.Mode)
                {
                    case CombatMotionMode.Stop:
                    case CombatMotionMode.HoldPosition:
                        targetXZ = float2.zero;
                        accelScale = motion.AccelerationScale;
                        brakeScale = motion.BrakeScale;
                        break;

                    case CombatMotionMode.DesiredVelocity:
                    case CombatMotionMode.DesiredDirection:
                    case CombatMotionMode.ArriveAtPosition:
                    case CombatMotionMode.MaintainDistance:
                    case CombatMotionMode.DashToTarget:
                        targetXZ = new float2(motion.DesiredVelocity.x, motion.DesiredVelocity.z) * motion.SpeedScale;
                        accelScale = motion.AccelerationScale;
                        brakeScale = motion.BrakeScale;
                        break;

                    default:
                        continue;
                }

                var desiredChange = targetXZ - currentXZ;
                var effectiveAccel = math.lengthsq(targetXZ) < math.lengthsq(currentXZ)
                    ? profile.MaxAcceleration * brakeScale
                    : profile.MaxAcceleration * accelScale;

                var maxDelta = effectiveAccel * dt;
                var changeMag = math.length(desiredChange);

                float2 newXZ;
                if (changeMag > maxDelta && changeMag > 0f)
                    newXZ = currentXZ + (desiredChange / changeMag) * maxDelta;
                else
                    newXZ = targetXZ;

                var maxSpeed = profile.MaxSpeed * motion.SpeedScale;
                var speed = math.length(newXZ);
                if (speed > maxSpeed && speed > 0f)
                    newXZ = newXZ * (maxSpeed / speed);

                velocityRW.ValueRW.Linear = new float3(newXZ.x, current.Linear.y, newXZ.y);
            }
        }
    }
}
