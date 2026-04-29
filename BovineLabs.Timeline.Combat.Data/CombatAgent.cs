using Unity.Entities;
using Unity.Mathematics;

namespace BovineLabs.Timeline.Combat
{
    public struct CombatAgent : IComponentData 
    { 
        public ushort PhysicsLink;
        public ushort EssenceLink;
    }

    public struct CombatAgentProfile : IComponentData
    {
        public float MaxSpeed;
        public float MaxAcceleration;
        public float TurnSpeed;
        public float AngularDamping;
        public float SensorRadius;
        public int MaxSensedTargets;
    }
}
