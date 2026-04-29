# AGENTS.md — BovineLabs Timeline Combat

Rules for any AI agent or developer working in `com.bovinelabs.timeline.combat`.

---

## Package Ecosystem

This package lives inside a larger BovineLabs ecosystem. Use these packages. Do NOT reinvent.

| Need | Package | Key Types |
|------|---------|-----------|
| Target resolution | `com.bovinelabs.reaction` | `Targets`, `TargetsCustom`, `Target` enum (Owner/Source/Target/Self/Custom0/Custom1) |
| Stats (speed, accel, HP) | `com.bovinelabs.essence` | `DynamicBuffer<Stat>`, `StatKey`, `StatExtensions.GetValueFloat()` |
| Health / resource pools | `com.bovinelabs.essence` | Intrinsics (clamped by Stats) |
| Timeline clip infrastructure | `com.bovinelabs.timeline` | `DOTSClip`, `DOTSTrack`, `IAnimatedComponent<T>`, `IMixer<T>`, `TrackBlendImpl<T,TC>`, `ClipActive`, `TrackBinding`, `TimelineComponentAnimationGroup` |
| Physics body | `Unity.Physics` | `PhysicsVelocity`, `PhysicsMass`, `PhysicsSystemGroup` (namespace: `Unity.Physics.Systems`) |
| Spatial queries | `com.bovinelabs.spatial` | `NeighborTrackerAuthoring` |
| Navigation | `com.bovinelabs.recast` | NavMesh integration |

---

## Non-Negotiable Invariants

1. **Only `CombatPhysicsMotorSystem` writes `PhysicsVelocity`.** No other system. No exceptions.
2. **No system writes `LocalTransform.Position`.** Ever. Physics owns position.
3. **No `MovementStats.Velocity`.** No duplicate velocity store. `PhysicsVelocity` is truth.
4. **No `TeamId`.** Use `CombatRelationship` (faction bitmasks) or `GroupId`/`ObjectId` from Core.
5. **No `CombatHealth`.** Health is an Essence Intrinsic, not a combat component.
6. **No per-behavior enemy scanning.** Shared sensor pipeline only.
7. **No per-frame allocations in hot paths.** NativeArray/NativeList in try/finally.
8. **Lane defaults always `None` when inactive.** Timeline clips must write `CombatMotionData.None` when condition fails.
9. **Forced motion never blends with desired motion.** Structural bypass.
10. **Facing is separate from movement.** Different lanes, different resolve.
11. **No hidden magic in baking.** Designer adds what they need. Baking systems validate, don't auto-add.
12. **No arbitrary priority numbers.** Structural lane order (Forced > Attack > Locomotion > Navigation > Idle).

---

## Assembly Structure

Follow the physics package pattern:

```
com.bovinelabs.timeline.combat/
  package.json
  BovineLabs.Timeline.Combat.Data/        # Structs, enums, animated components, mixers
  BovineLabs.Timeline.Combat/             # Runtime systems (track systems, resolver, motor)
  BovineLabs.Timeline.Combat.Authoring/   # DOTSClip, DOTSTrack, baking (UNITY_EDITOR only)
  BovineLabs.Timeline.Combat.Tests/       # Pure math tests
```

### asmdef References

**Data** references: `BovineLabs.Timeline.Data`, `BovineLabs.Timeline`, `Unity.*`
**Runtime** references: `Combat.Data`, `Combat.Authoring`, `BovineLabs.Core`, `BovineLabs.Core.Extensions`, `BovineLabs.Reaction.Data`, `Unity.Physics`
**Authoring** references: `Combat.Data`, `BovineLabs.Timeline.Authoring`, `BovineLabs.Reaction.Authoring`, `BovineLabs.Essence.Data`, `UNITY_EDITOR` constraint

---

## Creating a New Clip

1. Create `XClip.cs` in Authoring assembly. Extends `DOTSClip`, implements `ITimelineClipAsset`.
2. Create `XClipData : IComponentData` for baked runtime data.
3. In `Bake()`, set `animated.AuthoredData` on the animated component. Do NOT compute motion here.
4. Add `[TrackClipType(typeof(XClip))]` to the track that hosts it.

```csharp
public class FleeClip : DOTSClip, ITimelineClipAsset
{
    public Target threat = Target.Target;
    public float speed = 8f;

    public override double duration => 1;
    public ClipCaps clipCaps => ClipCaps.Blending | ClipCaps.Looping;

    public override void Bake(Entity clipEntity, BakingContext context)
    {
        context.Baker.AddComponent(clipEntity, new FleeClipData
        {
            Threat = threat,
            Speed = speed,
        });
        base.Bake(clipEntity, context);
    }
}
```

---

## Creating a Track System

Track systems run in `TimelineComponentAnimationGroup`. Pattern:

```csharp
[UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
public partial struct MyTrackSystem : ISystem
{
    private TrackBlendImpl<CombatMotionData, MyAnimated> blendImpl;

    void OnCreate(ref SystemState state) => blendImpl.OnCreate(ref state);
    void OnDestroy(ref SystemState state) => blendImpl.OnDestroy(ref state);

    void OnUpdate(ref SystemState state)
    {
        // 1. Compute job: read clip data, write animated.AuthoredData
        state.Dependency = new ComputeMyJob { ... }.ScheduleParallel(state.Dependency);

        // 2. Blend
        var blendData = blendImpl.Update(ref state);

        // 3. Write blended result to resolved component
        state.Dependency = new WriteJob { BlendData = blendData }
            .ScheduleParallel(blendData, 64, state.Dependency);
    }
}
```

Compute jobs use `[WithAll(typeof(ClipActive))]` and `IJobEntity`.

---

## Target Resolution

Clips resolve targets via `Targets.Get()`:

```csharp
// Required usings:
using BovineLabs.Reaction.Data.Core;

// In compute job:
var targets = TargetsLookup[selfEntity];
var targetEntity = targets.Get(clipData.Target, selfEntity, CustomsLookup);
```

**Designer must add `TargetsAuthoring` on the prefab.** Baking does NOT auto-add it.

---

## Reading Stats from Essence

```csharp
using BovineLabs.Essence.Data;

// In system:
foreach (var (stats, ...) in SystemAPI.Query<DynamicBuffer<Stat>, ...>())
{
    var maxSpeed = stats.GetValueFloat(myStatKey, defaultValue: 5f);
}
```

Do NOT create your own `MovementStats` or `CombatHealth`. Stats come from Essence.

---

## Motion Lane Rules

4 lanes exist. Each is an `IAnimatedComponent<CombatMotionData>`:

| Lane | Animated Component | Purpose |
|------|-------------------|---------|
| Attack | `AttackMotionAnimated` | Charge, lunge, root motion attacks |
| Locomotion | `LocomotionAnimated` | Flee, seek, orbit, backstep, stop |
| Navigation | `NavigationAnimated` | Pathfinding follow |
| Avoidance | `AvoidanceAnimated` | Crowd separation (additive, clamped) |

Resolution order in `CombatMotionResolveSystem`:
1. `ForcedMotionState.IsActive` → bypass everything
2. Attack lane
3. Locomotion lane
4. Navigation lane
5. Default: Stop
6. Add avoidance (clamped by `MaxContribution`, only if no `IgnoreAvoidance` flag)

**Avoidance Add clamp** — only the avoidance vector is clamped, then added. Never clamp the total:

```csharp
// CORRECT:
var avoid = avoidanceData.DesiredVelocity;
if (maxC > 0f && math.lengthsq(avoid) > maxC * maxC)
    avoid = math.normalize(avoid) * maxC;
motion.DesiredVelocity += avoid;

// WRONG — collapses total velocity to avoidance max:
var combined = motion.DesiredVelocity + avoidanceData.DesiredVelocity;
if (math.lengthsq(combined) > maxC * maxC)
    combined = math.normalize(combined) * maxC;
```

---

## Facing

Facing is a separate pipeline:
- `FacingAnimated` → `FacingResolveSystem` → `ResolvedFacing`
- `FaceTarget` resolves entity → position via `LocalTransform` lookup
- `FaceMovement` extracts direction from `ResolvedMotion.Motion.DesiredVelocity`
- CombatLockFlags.DisableTurn suppresses facing

---

## Forced Motion

Forced motion has structural authority, NOT priority numbers:

| Priority | Mode | Beats |
|----------|------|-------|
| 5 | Grabbed | everything |
| 4 | Freeze | launch/knockback |
| 3 | Launch | ground knockback |
| 2 | PullToPosition | normal knockback |
| 1 | VelocityOverride | locomotion |
| 0 | Impulse | nothing |

Forced motion bypasses lane gate but still goes through `CombatPhysicsMotorSystem`. No system writes `PhysicsVelocity` directly.

---

## Source-Stacked Locks

Use `CombatLockRequest` buffer. Multiple sources can lock simultaneously:

```csharp
requests.Add(new CombatLockRequest
{
    Flags = CombatLockFlags.DisableInput | CombatLockFlags.DisableBrain,
    Source = attackEntity,
    RemainingTime = 0.5f,
});
```

`CombatLockResolveSystem` ORs all active flags. Expired requests removed. Never reset whole mask.

---

## C# / Unity Pitfalls

- `[Flags]` requires `using System;`
- `[DisplayName]` requires `using System.ComponentModel;` in Authoring
- `IAnimatedComponent<T>` is in `BovineLabs.Timeline.Data` namespace
- `IMixer<T>` is in `BovineLabs.Timeline` namespace
- `PhysicsSystemGroup` is in `Unity.Physics.Systems` namespace
- No struct field initializers in Unity C# 9 (use static factory)
- No parameterless struct constructors
- DynamicBuffer element assignment in foreach = CS1654. Use `SystemAPI.GetBuffer<T>(entity)` separately
- `IJobEntity` must be sole top-level type in its file
- No `entityInQueryIndex` parameter in IJobEntity.Execute() (Unity Entities 6.x)
- `[Il2CppSetOption]` does not compile. Use `[MethodImpl(AggressiveInlining)]` or omit
- `[MethodImpl]` cannot go on properties (expression-bodied getters)
