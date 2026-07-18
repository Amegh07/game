# Hearing System — Stealth Game

## 1. System Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                       NoiseManager                              │
│  (singleton — routes NoiseEvent to all HearingComponents)       │
│  Register / Unregister listeners                                │
│  Debug visualization (Gizmos + editor overlay)                  │
└──────┬────────────────────────────────────┬─────────────────────┘
       │                                    │
       │ OnNoiseEmitted(NoiseEvent)         │ OnNoiseEmitted(NoiseEvent)
       │                                    │
┌──────▼──────────────┐         ┌──────────▼──────────────┐
│  NoiseEmitter       │         │  HearingComponent        │
│  (Player / objects) │         │  (Guard / NPC)           │
│                     │         │                          │
│ Movement state      │         │ hearingRadius            │
│ Surface detection   │         │ intensity multiplier     │
│ Noise table         │         │ obstruction multiplier   │
│ Emission interval   │         │ audioSuspicionMeter      │
│                     │         │ investigationCooldown    │
│ → emits NoiseEvent  │         │                          │
└─────────────────────┘         │ → feeds into GuardFSM    │
                                │ → triggers Investigate   │
                                │ → triggers Search        │
                                └──────────┬───────────────┘
                                           │
                                ┌──────────▼───────────────┐
                                │       GuardFSM            │
                                │                           │
                                │ Patrol ←→ Suspicious      │
                                │    ←→ Investigate         │
                                │    ←→ Search              │
                                │    ←→ Chase               │
                                │    ←→ ReturnToPatrol      │
                                └───────────────────────────┘
```

### Data Flow — One Noise Event

```
Player moves on metal surface while running
         │
         ▼
NoiseEmitter.ComputeNoise(Movement.Run, Surface.Metal) → intensity = 70
         │
         ▼
NoiseManager.EmitNoise(WorldPosition, intensity, radius, type)
         │
         ▼
    ┌────┴────┐
    │  Poll   │  foreach registered HearingComponent within range:
    │  Nearby │      distance = magnitude(emitterPos - guardPos)
    │ Guards  │      perceived = intensity × distanceFalloff(distance, hearingRadius)
    └────┬────┘      if (obstruction) perceived × obstructionMultiplier
         │           if (perceived > minimumThreshold)
         │               guard.OnNoiseHeard(perceived, emitterPos)
         ▼
    ┌────┴────┐
    │ Hearing │  audioSuspicionMeter += perceived × deltaTime × frequencyMultiplier
    │Component│  if (audioSuspicionMeter > investigateThreshold):
    │         │      GuardFSM.ChangeState(Investigate, noisePosition)
    └─────────┘
```

---

## 2. Core Data Types

### NoiseEvent

```csharp
public struct NoiseEvent
{
    public Vector3 origin;          // world position of the noise
    public float baseIntensity;     // 0 – 100 at source
    public float radius;            // maximum propagation distance
    public NoiseType type;          // footstep, impact, environment, voice
    public GameObject source;       // the emitter GameObject
}
```

### MovementType / SurfaceType

```csharp
public enum MovementType { Idle, CrouchWalk, Walk, Run, Jump, Land, Sprint }

public enum SurfaceType
{
    Concrete, Metal, Wood, Grass, Gravel, Carpet, Tile, Dirt, Stone, Custom
}
```

---

## 3. Component: NoiseEmitter

### 3.1 Public API

```csharp
public class NoiseEmitter : MonoBehaviour
{
    [Header("Movement tracking")]
    public MovementType currentMovement = MovementType.Idle;
    public float stepIntervalWalk = 0.5f;      // seconds between footsteps
    public float stepIntervalRun  = 0.35f;
    public float stepIntervalCrouch = 0.8f;

    [Header("Surface detection")]
    public float surfaceCheckDistance = 0.2f;
    public LayerMask surfaceLayer = -1;

    [Header("Noise profile table")]
    public NoiseProfileEntry[] noiseProfile;   // defined in inspector

    [Header("Emission")]
    public float noiseRadius = 12f;
    public bool enableNoise = true;

    private float stepTimer = 0f;
    private SurfaceType currentSurface = SurfaceType.Concrete;
    private bool wasGrounded = true;
    private Vector3 lastPosition;
}
```

### 3.2 Noise Profile Table

Each entry maps a `(MovementType, SurfaceType)` pair to a base intensity:

```csharp
[System.Serializable]
public struct NoiseProfileEntry
{
    public MovementType movement;
    public SurfaceType  surface;
    [Range(0, 100)] public float intensity;   // noise at source
    public bool autoDetectSurface;            // if true, surface is ignored (use runtime detection)
}
```

**Default table** (used at runtime if no profile entry matches):

| Movement \ Surface | Concrete | Metal | Wood | Grass | Gravel | Carpet | Tile | Dirt | Stone |
|---|---|---|---|---|---|---|---|---|---|
| Idle | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| CrouchWalk | 4 | 10 | 8 | 3 | 15 | 1 | 6 | 3 | 8 |
| Walk | 15 | 30 | 25 | 10 | 40 | 5 | 20 | 10 | 20 |
| Run | 40 | 70 | 55 | 25 | 85 | 15 | 50 | 30 | 45 |
| Sprint | 55 | 85 | 70 | 35 | 100 | 25 | 65 | 40 | 60 |
| Jump | 35 | 55 | 45 | 20 | 65 | 10 | 40 | 25 | 40 |
| Land | 25 | 45 | 35 | 15 | 50 | 8 | 30 | 18 | 30 |

### 3.3 Surface Detection

```csharp
SurfaceType DetectSurface()
{
    Ray ray = new Ray(transform.position + Vector3.up * 0.1f, Vector3.down);
    if (Physics.Raycast(ray, out RaycastHit hit, surfaceCheckDistance + 0.1f, surfaceLayer))
    {
        // Option A: Tag-based
        // hit.collider.tag → "Metal", "Wood" ...

        // Option B: SurfaceIdentifier component on the floor
        SurfaceIdentifier si = hit.collider.GetComponent<SurfaceIdentifier>();
        if (si != null) return si.surfaceType;

        // Option C: Physics Material name
        if (hit.collider.sharedMaterial != null)
            return ParseSurfaceFromMaterialName(hit.collider.sharedMaterial.name);
    }
    return SurfaceType.Concrete; // fallback
}
```

### 3.4 Update Loop

```csharp
void Update()
{
    if (!enableNoise) return;

    DetectMovementType();
    DetectSurface();

    stepTimer -= Time.deltaTime;
    float interval = GetStepInterval();

    if (stepTimer <= 0f && currentMovement != MovementType.Idle)
    {
        stepTimer = interval;
        float intensity = LookupNoise(currentMovement, currentSurface);
        if (intensity > 0)
            NoiseManager.Instance.EmitNoise(
                transform.position,
                intensity,
                noiseRadius,
                NoiseType.Footstep,
                gameObject
            );
    }

    // Jump / Land detection
    bool grounded = IsGrounded();
    if (!wasGrounded && grounded) // just landed
    {
        float intensity = LookupNoise(MovementType.Land, currentSurface);
        if (intensity > 0)
            NoiseManager.Instance.EmitNoise(transform.position, intensity, noiseRadius, NoiseType.Impact, gameObject);
    }
    wasGrounded = grounded;
}
```

---

## 4. Component: HearingComponent

### 4.1 Public API

```csharp
public class HearingComponent : MonoBehaviour
{
    [Header("Hearing")]
    public float hearingRadius = 12f;
    public float hearingAngle = 360f;               // 360 = omni, <360 = directional hearing
    [Range(0, 100)] public float minThreshold = 5f; // ignored below this

    [Header("Multipliers")]
    [Range(0, 1)] public float obstructionMultiplier = 0.3f; // per obstacle
    public LayerMask obstructionMask = -1;

    [Header("Suspicion")]
    public float audioSuspicionMeter = 0f;
    public float suspicionDecayRate = 5f;            // points/second when quiet
    public float investigateThreshold = 25f;          // meter value to trigger investigate
    public float maxSuspicion = 100f;                 // cap
    public float suspicionMultiplier = 1f;            // global modifier

    [Header("Cooldown")]
    public float investigationCooldown = 5f;          // seconds before investigating again
    private float cooldownTimer = 0f;

    [Header("Repeated noise escalation")]
    public int noisesBeforeEscalation = 3;            // loud noises within time window
    public float escalationWindow = 8f;               // seconds for the count
    private int recentNoiseCount = 0;
    private float escalationTimer = 0f;

    [Header("Events")]
    public event System.Action<Vector3, float> OnNoiseInvestigated; // position, intensity
    public event System.Action<float> OnAudioSuspicionChanged;

    // references
    private GuardFSM guardFSM;
    private DetectionMeter detectionMeter;
    private Vector3 lastNoisePosition;
    private float lastNoiseIntensity;
}
```

### 4.2 Receiving Noise

Called by `NoiseManager` when a noise event is within `hearingRadius`:

```csharp
public void OnNoiseHeard(NoiseEvent noise)
{
    // 1. Directional check (if not 360°)
    Vector3 dirToNoise = (noise.origin - transform.position).normalized;
    float angle = Vector3.Angle(transform.forward, dirToNoise);
    if (angle > hearingAngle * 0.5f) return;

    // 2. Distance falloff
    float dist = Vector3.Distance(transform.position, noise.origin);
    float distanceFactor = 1f - Mathf.Clamp01(dist / hearingRadius);
    float perceived = noise.baseIntensity * distanceFactor;

    // 3. Obstruction (raycast from guard to noise origin)
    if (Physics.Raycast(transform.position + Vector3.up * 0.5f,
                         dirToNoise, out RaycastHit hit, dist, obstructionMask))
    {
        if (hit.collider.gameObject != noise.source)
            perceived *= obstructionMultiplier;
    }

    // 4. Threshold check
    if (perceived < minThreshold) return;

    // 5. Update suspicion
    lastNoisePosition = noise.origin;
    lastNoiseIntensity = perceived;
    audioSuspicionMeter = Mathf.Min(audioSuspicionMeter + perceived * suspicionMultiplier, maxSuspicion);
    OnAudioSuspicionChanged?.Invoke(audioSuspicionMeter);

    // 6. Escalation tracking
    escalationTimer = escalationWindow;
    recentNoiseCount++;

    // 7. React
    ReactToNoise(perceived, noise.origin);
}
```

### 4.3 Reaction Logic

```csharp
void ReactToNoise(float perceivedIntensity, Vector3 noisePosition)
{
    if (guardFSM == null) return;

    switch (guardFSM.currentState)
    {
        case GuardFSM.GuardState.Patrol:
            // First noise → become suspicious
            guardFSM.lastKnownPosition = noisePosition;
            guardFSM.ChangeState(GuardFSM.GuardState.Suspicious);
            break;

        case GuardFSM.GuardState.Suspicious:
            // Update last known position to latest noise
            guardFSM.lastKnownPosition = noisePosition;
            break;

        case GuardFSM.GuardState.Search:
            // New noise during search → re-investigate
            guardFSM.lastKnownPosition = noisePosition;
            guardFSM.ChangeState(GuardFSM.GuardState.Investigate);
            break;

        case GuardFSM.GuardState.Investigate:
            // Update target if noise is from a different location
            guardFSM.lastKnownPosition = noisePosition;
            break;
    }

    // Escalation: too many noises → leapfrog to Chase
    if (recentNoiseCount >= noisesBeforeEscalation &&
        perceivedIntensity > minThreshold * 2f &&
        guardFSM.currentState != GuardFSM.GuardState.Chase)
    {
        guardFSM.ChangeState(GuardFSM.GuardState.Chase);
    }
}
```

### 4.4 Update Loop (decay + cooldown)

```csharp
void Update()
{
    // Decay suspicion when quiet
    if (audioSuspicionMeter > 0f)
    {
        audioSuspicionMeter = Mathf.Max(0f, audioSuspicionMeter - suspicionDecayRate * Time.deltaTime);
        OnAudioSuspicionChanged?.Invoke(audioSuspicionMeter);
    }

    // Cooldown for investigation spam
    if (cooldownTimer > 0f)
        cooldownTimer -= Time.deltaTime;

    // Escalation timer
    if (escalationTimer > 0f)
    {
        escalationTimer -= Time.deltaTime;
        if (escalationTimer <= 0f)
            recentNoiseCount = 0;
    }
}
```

---

## 5. Component: NoiseManager (Singleton)

### 5.1 Public API

```csharp
public class NoiseManager : MonoBehaviour
{
    public static NoiseManager Instance { get; private set; }

    [Header("Debug")]
    public bool showNoiseGizmos = true;
    public float gizmoDuration = 1.5f;

    // Internal
    private List<HearingComponent> listeners = new();
    private List<NoiseDebugInfo> recentNoises = new();

    public void Register(HearingComponent hc) => listeners.Add(hc);
    public void Unregister(HearingComponent hc) => listeners.Remove(hc);

    public void EmitNoise(Vector3 origin, float intensity, float radius,
                          NoiseType type, GameObject source)
    {
        var noise = new NoiseEvent(origin, intensity, radius, type, source);

        foreach (var listener in listeners)
        {
            if (!listener.isActiveAndEnabled) continue;
            float dist = Vector3.Distance(origin, listener.transform.position);
            if (dist <= radius)
                listener.OnNoiseHeard(noise);
        }

        if (showNoiseGizmos)
            recentNoises.Add(new NoiseDebugInfo(origin, radius, Time.time));
    }
}
```

### 5.2 Debug Visualization

```csharp
void OnDrawGizmos()
{
    if (!showNoiseGizmos) return;

    // Recent noise events — expanding rings
    foreach (var n in recentNoises)
    {
        float age = Time.time - n.timestamp;
        if (age > gizmoDuration) continue;
        float progress = age / gizmoDuration;

        Gizmos.color = new Color(1f, 0.5f, 0f, 1f - progress);
        Gizmos.DrawWireSphere(n.origin, n.radius * progress);
    }

    // Guard hearing ranges
    foreach (var hc in listeners)
    {
        if (!hc.isActiveAndEnabled) continue;
        Gizmos.color = new Color(0f, 0.5f, 1f, 0.15f);
        Gizmos.DrawSphere(hc.transform.position, hc.hearingRadius);
    }
}

struct NoiseDebugInfo
{
    public Vector3 origin;
    public float radius;
    public float timestamp;
}
```

---

## 6. State Machine Integration (GuardFSM)

The hearing system modifies how `GuardFSM` enters certain states. The existing FSM transitions are updated:

### 6.1 New transition paths (hearing-driven)

```
                  ┌──────────┐
        ┌────────│  Patrol   │←───────────┐
        │        └─────┬─────┘            │
        │              │ Noise heard      │
        │        ┌─────▼─────────┐        │
        │        │  Suspicious   │        │
        │        └──┬──────┬─────┘        │
        │           │      │              │
        │  visual   │      │ audio        │
        │  meter    │      │ suspicion    │
        │  fills    │      │ reaches      │
        │           │      │ investigate  │
        │    ┌──────▼──┐   │ threshold    │
        │    │  Chase   │   │              │
        │    └────┬─────┘   │              │
        │         │         │              │
        │         │   ┌─────▼──────────┐   │
        │         │   │  Investigate   │   │
        │         │   └───────┬────────┘   │
        │         │           │ arrived    │
        │         │     ┌─────▼──────┐     │
        │         │     │   Search   │     │
        │         │     └─────┬──────┘     │
        │         │           │ not found   │
        │         │     ┌─────▼─────────┐  │
        │         └─────│ReturnToPatrol │──┘
        │               └───────────────┘
```

### 6.2 GuardFSM modifications

```csharp
// In GuardFSM, add a reference to HearingComponent
private HearingComponent hearing;

void Start()
{
    hearing = GetComponent<HearingComponent>();
    if (hearing != null)
    {
        hearing.OnNoiseInvestigated += OnNoiseInvestigation;
    }
}

void OnNoiseInvestigation(Vector3 position, float intensity)
{
    lastKnownPosition = position;

    switch (currentState)
    {
        case GuardState.Patrol:
        case GuardState.ReturnToPatrol:
            ChangeState(GuardState.Suspicious);
            break;
        case GuardState.Search:
            ChangeState(GuardState.Investigate);
            break;
    }
}
```

---

## 7. SurfaceIdentifier Component

Attached to each floor primitive to mark its acoustic surface type:

```csharp
public class SurfaceIdentifier : MonoBehaviour
{
    public SurfaceType surfaceType = SurfaceType.Concrete;
}
```

### Generator update (floor creation)

Each floor cube in `CreateRoom` / `CreateCorridor` gets a `SurfaceIdentifier`:

```csharp
// After creating a Floor primitive:
SurfaceIdentifier si = floorObj.AddComponent<SurfaceIdentifier>();
si.surfaceType = roomName switch
{
    "Entrance" => SurfaceType.Concrete,
    "Reception" => SurfaceType.Tile,
    "ExhibitRoom" => SurfaceType.Wood,
    "SecurityRoom" => SurfaceType.Concrete,
    "Vault" => SurfaceType.Metal,
    "Exit" => SurfaceType.Concrete,
    _ => SurfaceType.Concrete
};
```

---

## 8. Configuration Summary

### NoiseEmitter (on Player)

| Parameter | Default | Notes |
|---|---|---|
| `stepIntervalWalk` | 0.5s | seconds between walk footsteps |
| `stepIntervalRun` | 0.35s | seconds between run footsteps |
| `stepIntervalCrouch` | 0.8s | seconds between crouch footsteps |
| `noiseRadius` | 12m | maximum noise propagation |
| `surfaceCheckDistance` | 0.2m | raycast down for surface detection |

### HearingComponent (on Guard)

| Parameter | Default | Notes |
|---|---|---|
| `hearingRadius` | 12m | maximum hearing range |
| `hearingAngle` | 360° | 360 = omnidirectional |
| `minThreshold` | 5 | noise below this is ignored entirely |
| `obstructionMultiplier` | 0.3 | noise reduced by 70% through walls |
| `audioSuspicionMeter` | 0 | current audio-based suspicion |
| `suspicionDecayRate` | 5/s | points lost per second in silence |
| `investigateThreshold` | 25 | meter value to trigger Investigate |
| `maxSuspicion` | 100 | cap for the meter |
| `investigationCooldown` | 5s | minimum time between investigations |
| `noisesBeforeEscalation` | 3 | repeated noises before escalating |
| `escalationWindow` | 8s | time window for counting noises |

### Debug

| Parameter | Default | Notes |
|---|---|---|
| `showNoiseGizmos` | true | noise rings + hearing spheres |
| `gizmoDuration` | 1.5s | how long noise rings persist |

---

## 9. Files to Create / Modify

| File | Action | Purpose |
|---|---|---|
| `Assets/Scripts/NoiseManager.cs` | Create | Singleton event router + debug viz |
| `Assets/Scripts/NoiseEmitter.cs` | Create | Player movement noise generation |
| `Assets/Scripts/HearingComponent.cs` | Create | Guard noise perception + suspicion |
| `Assets/Scripts/SurfaceIdentifier.cs` | Create | Surface type on floor primitives |
| `Assets/Scripts/GuardFSM.cs` | Modify | Subscribe to HearingComponent events |
| `Assets/Editor/TutorialLevelGenerator.cs` | Modify | Attach SurfaceIdentifier to floors, HearingComponent to guard |

---

## 10. Example: Full Cycle

```
Player running across metal grating in Security Room:
  NoiseEmitter detects:  Movement.Run + Surface.Metal → intensity = 70
                          ↓
  NoiseManager emits:    origin=(0, 0, 4), base=70, radius=12, type=Footstep
                          ↓
  Guard in Reception (z=-12):  distance = 16m > 12m → ignored
  Guard in Security (z=4):     distance = 0m ≤ 12m → received
                          ↓
  HearingComponent:
    distanceFactor = 1 - 0/12 = 1.0
    perceived = 70 × 1.0 = 70
    (no obstruction — same room)
    audioSuspicionMeter += 70 → 70
    audioSuspicionMeter (70) > investigateThreshold (25)
                          ↓
  GuardFSM:
    currentState = Patrol
    lastKnownPosition = noiseOrigin
    ChangeState(Suspicious)
                          ↓
  Guard stops patrol, rotates toward noise origin.
  During Suspicious, DetectionMeter also starts filling
  (player is visible running across Security Room).

Combined result: guard heard the footsteps BEFORE seeing the player.
Audio suspicion was enough to trigger Suspicious immediately.
Visual detection fills the meter in parallel.
If player hides behind cover, guard still has audio suspicion
and will investigate the last known position.
```

---

## 11. Implementation Order

1. **SurfaceIdentifier.cs** — simple enum component for floors
2. **NoiseManager.cs** — singleton with register/unregister/emit
3. **NoiseEmitter.cs** — player movement + surface → noise events
4. **HearingComponent.cs** — receive events, suspicion meter, escalation
5. **GuardFSM.cs** — wire up hearing-driven transitions
6. **TutorialLevelGenerator.cs** — attach SurfaceIdentifier to floors, HearingComponent to guard
7. **Test in editor** — run through each surface type, verify guard reactions
