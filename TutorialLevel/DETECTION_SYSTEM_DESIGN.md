# Detection Meter System — Stealth Game

## 1. System Architecture

```
┌─────────────────────────────────────────────────────────┐
│                    DetectionManager                      │
│  (singleton — routes events, manages HUD, global config) │
└────────┬────────────┬──────────────┬─────────────────────┘
         │            │              │
    ┌────▼────┐  ┌────▼────┐   ┌────▼────┐
    │GuardFSM │  │GuardFSM │   │GuardFSM │  ...
    │         │  │         │   │         │
    │Detection│  │Detection│   │Detection│
    │Meter    │  │Meter    │   │Meter    │
    └────┬────┘  └────┬────┘   └────┬────┘
         │            │              │
    ┌────▼────┐  ┌────▼────┐   ┌────▼────┐
    │WorldUI  │  │WorldUI  │   │WorldUI  │
    │(canvas) │  │(canvas) │   │(canvas) │
    └─────────┘  └─────────┘   └─────────┘
         │            │              │
         └────────────┼──────────────┘
                      │
              ┌───────▼────────┐
              │  HUD_Meter     │
              │ (screen-space) │
              └────────────────┘
```

**Three layers:**
- **DetectionMeter** — component on each NPC; pure data + logic, no UI dependency
- **WorldSpaceDetectionBar** — world-space canvas child of the NPC; follows the guard
- **HUDDetectionMeter** — singleton screen-space canvas; shows the *highest* active detection

---

## 2. Core Component — `DetectionMeter`

### 2.1 Public API

```csharp
public class DetectionMeter : MonoBehaviour
{
    // ── configuration ──
    [Range(0f, 1f)] public float value = 0f;

    public float increaseRate = 0.20f;   // % / second
    public float decreaseRate = 0.10f;   // % / second
    public float cooldownDuration = 2f;  // seconds after hitting 0

    // ── thresholds (0-1 range) ──
    public float suspiciousThreshold = 0.30f;
    public float alertThreshold = 0.70f;

    // ── state (read-only) ──
    public bool isIncreasing  { get; private set; }
    public bool isCoolingDown { get; private set; }
    public float cooldownTimer { get; private set; }

    // ── level enum ──
    public enum AlertLevel { Unaware, Suspicious, Alert }
    public AlertLevel currentLevel { get; private set; }

    // ── events ──
    public event Action<float>              OnValueChanged;     // called every frame value changes
    public event Action<AlertLevel>         OnLevelChanged;     // called on threshold cross
    public event Action                     OnFullyDetected;    // value == 1
    public event Action                     OnCooldownStarted;
    public event Action                     OnCooldownEnded;
}
```

### 2.2 Core Update Loop (pseudocode)

```
Every frame:

  if (cooldownTimer > 0):
      cooldownTimer -= deltaTime
      if (cooldownTimer <= 0):
          isCoolingDown = false
          onCooldownEnded()

  if (playerIsVisible && !isCoolingDown):
      // INCREASE
      value += increaseRate * deltaTime
      isIncreasing = true
  else:
      // DECREASE
      value -= decreaseRate * deltaTime
      isIncreasing = false

  value = clamp(value, 0, 1)

  if (value == 0 && previousFrameValue > 0 && !isCoolingDown):
      isCoolingDown = true
      cooldownTimer = cooldownDuration
      onCooldownStarted()

  newLevel = mapValueToLevel(value)
  if (newLevel != currentLevel):
      currentLevel = newLevel
      onLevelChanged(newLevel)

  if (value >= 1 && previousFrameValue < 1):
      onFullyDetected()

  if (value != previousFrameValue):
      onValueChanged(value)

  previousFrameValue = value
```

### 2.3 Level Mapping

| Range          | Level      | Color  | GuardFSM state        |
|----------------|------------|--------|------------------------|
| 0.00 – 0.30   | Unaware    | Green  | Patrol                 |
| 0.30 – 0.70   | Suspicious | Yellow | Suspicious             |
| 0.70 – 1.00   | Alert      | Red    | Chase                  |

### 2.4 Flow Diagram

```
                    ┌──────────┐
                    │ Unaware  │  ←─ Patrol
                    │ 0-30%    │
                    └────┬─────┘
                         │ player visible
                    ┌────▼─────┐
                    │Suspicious│  ←─ Suspicious state
                    │ 30-70%   │
                    └────┬─────┘
                         │ crosses 70%
                    ┌────▼─────┐
                    │  Alert   │  ←─ Chase state
                    │ 70-100%  │
                    └────┬─────┘
                         │ value == 1
                    ┌────▼─────┐
                    │Detected! │  → GuardFSM.OnFullyDetected
                    └──────────┘

    Player hidden → value decays back through levels
    Hitting 0% → cooldown timer starts (2s default)
    During cooldown → increaseRate is ignored → prevents instant re-detection
```

---

## 3. UI Components

### 3.1 World-Space Bar — `WorldSpaceDetectionBar`

**Purpose:** Each guard has a small coloured bar floating above their head. Visible only when `value > 0`.

**Creation (editor / generator):**
```
Guard GameObject
├── Capsule (mesh)
├── GuardFSM (script)
├── DetectionMeter (script)
└── GuardUI (empty, position 0, 2.5, 0)
    └── Canvas (World Space, 200×30)
        ├── Background (Image, dark gray, full width)
        └── Fill (Image, anchored left, width driven by DetectionMeter.value)
```

**Runtime behaviour:**
```
Every frame (via Update or event OnValueChanged):
    fill.rectTransform.SetSizeWithCurrentAnchors(
        Axis.Horizontal,
        backgroundWidth * detectionMeter.value
    )

    // Color lerp based on level
    fill.color = Color.Lerp(Green, Yellow, t)   where t = value mapped 0→1 within 0-0.70
    if (value > 0.70f) fill.color = Color.Lerp(Yellow, Red, (value - 0.70f) / 0.30f)

    // Visibility
    canvas.enabled = value > 0.01f
```

**Billboarding:** Set `Canvas → Render Mode = World Space`, place camera as `eventCamera`.  
OR attach a simple billboard script that rotates toward the main camera each frame:

```csharp
void LateUpdate()
{
    if (Camera.main != null)
        transform.rotation = Camera.main.transform.rotation;
}
```

### 3.2 HUD Element — `HUDDetectionMeter`

**Purpose:** Single screen-space widget showing the *highest* detection meter across all NPCs.  
Serves as the player's primary awareness indicator.

**Canvas layout (Screen Space – Overlay):**
```
Canvas (Screen Space – Overlay)
└── HUD_DetectionMeter
    ├── Border (rounded rect, grey)
    ├── Background (dark translucent)
    ├── Fill (anchored left, width driven by highest value)
    ├── Threshold markers
    │   ├── Marker_Suspicious  (at 30% width)
    │   └── Marker_Alert       (at 70% width)
    └── Label ("DETECTION")
```

**Size & position:** 300×24 pixels, centered at bottom of screen (y offset 40 from bottom).

**Runtime logic:**
```
Every frame:
    highest = max over all active DetectionMeter instances of .value

    fill.width = backgroundWidth * highest
    fill.color = ColorLerp based on highest (same gradient as world bar)

    // Pulse when crossing thresholds
    if (highest just crossed 0.30f || highest just crossed 0.70f):
        flash border white for 0.15s
```

**Discovery of meters:** Use a static registry in `DetectionManager`:

```csharp
public class DetectionManager : MonoBehaviour
{
    public static DetectionManager Instance { get; private set; }
    public List<DetectionMeter> activeMeters = new();

    void Awake() { Instance = this; }

    public void Register(DetectionMeter m)  { activeMeters.Add(m); }
    public void Unregister(DetectionMeter m){ activeMeters.Remove(m); }

    public float GetHighestDetection()
    {
        float max = 0f;
        foreach (var m in activeMeters)
            if (m.value > max) max = m.value;
        return max;
    }
}
```

---

## 4. Configuration Parameters

### DetectionMeter (per-NPC)

| Parameter           | Default | Notes                              |
|---------------------|---------|------------------------------------|
| `increaseRate`      | 0.20    | %/s — fills in 5s if fully visible |
| `decreaseRate`      | 0.10    | %/s — empties in 10s if hidden     |
| `cooldownDuration`  | 2.0     | seconds before re-detection allowed |
| `suspiciousThreshold` | 0.30  | enters yellow band                 |
| `alertThreshold`    | 0.70    | enters red band                    |

### WorldSpaceDetectionBar

| Parameter           | Default | Notes                              |
|---------------------|---------|------------------------------------|
| `barWidth`          | 200     | pixels in world-space canvas       |
| `barHeight`         | 16      | pixels                             |
| `verticalOffset`    | 2.5     | units above guard origin           |
| `unawareColor`      | #00FF00 | green                              |
| `suspiciousColor`   | #FFCC00 | yellow                             |
| `alertColor`        | #FF3300 | red                                |

### HUDDetectionMeter

| Parameter           | Default | Notes                              |
|---------------------|---------|------------------------------------|
| `anchorMin`         | (0.5,0) | bottom center                      |
| `anchorMax`         | (0.5,0) |                                    |
| `position`          | (0,40)  | 40 px from bottom edge             |
| `width`             | 300     |                                    |
| `height`            | 24      |                                    |
| `flashDuration`     | 0.15    | seconds for threshold-cross flash  |

---

## 5. Integration with GuardFSM

### 5.1 Current GuardFSM detection (simplified)

Currently `GuardFSM` has its own `suspicionMeter` directly inside the state machine. The design goal is to **replace that internal meter** with the `DetectionMeter` component.

### 5.2 Updated GuardFSM states

```
UpdatePatrol():
    if (detectionMeter.value > 0):
        // player is visible (detectionMeter managed by DetectionMeter component)
        ChangeState(Suspicious)

UpdateSuspicious():
    if (detectionMeter.currentLevel >= AlertLevel.Suspicious):
        // already handled by DetectionMeter internally
        if (detectionMeter.value >= detectionMeter.alertThreshold):
            ChangeState(Chase)

UpdateChase():
    if (detectionMeter.value <= 0):
        // player fully hidden, detection decayed to 0
        ChangeState(Investigate)
```

But actually, the cleaner approach is for GuardFSM to subscribe to DetectionMeter events:

```csharp
void Start()
{
    if (detectionMeter == null)
        detectionMeter = GetComponent<DetectionMeter>();

    detectionMeter.OnLevelChanged += OnAlertLevelChanged;
    detectionMeter.OnFullyDetected += OnFullyDetected;
}

void OnAlertLevelChanged(DetectionMeter.AlertLevel level)
{
    switch (level)
    {
        case DetectionMeter.AlertLevel.Suspicious:
            if (currentState == GuardState.Patrol)
                ChangeState(GuardState.Suspicious);
            break;

        case DetectionMeter.AlertLevel.Alert:
            if (currentState == GuardState.Suspicious)
                ChangeState(GuardState.Chase);
            break;
    }
}

void OnFullyDetected()
{
    // Guaranteed to be in Chase state at this point.
    // Could trigger a broadcast alarm, game over check, etc.
}
```

### 5.3 Wiring (in Generator)

```csharp
// Add DetectionMeter to guard
DetectionMeter meter = guardObj.AddComponent<DetectionMeter>();
meter.increaseRate = 0.20f;
meter.decreaseRate = 0.10f;
meter.suspiciousThreshold = 0.30f;
meter.alertThreshold = 0.70f;
meter.cooldownDuration = 2f;

// DetectionMeter reads playerInSight from GuardFSM.
// Option A: DetectionMeter gets playerInSight from GuardFSM's detection
// Option B: DetectionMeter has its own vision check

// World-space bar
// Create a Canvas child of guardObj, add WorldSpaceDetectionBar
```

### 5.4 Connecting DetectionMeter to visibility

The `DetectionMeter` needs to know whether the player is currently visible. There are two approaches:

**Approach A — GuardFSM feeds visibility to DetectionMeter:**

```csharp
// In GuardFSM.Update():
detectionMeter.SetPlayerVisible(playerInSight);
```

**Approach B — DetectionMeter has its own vision cone:**

```csharp
public class DetectionMeter : MonoBehaviour
{
    public Transform player;
    public float visionRange = 10f;
    public float visionAngle = 60f;
    public LayerMask visionMask = -1;

    bool CheckVisibility() { /* same raycast logic as GuardFSM.DetectPlayer */ }
}
```

**Recommendation:** Approach A is cleaner — the GuardFSM already has vision detection logic. The `DetectionMeter` should be a pure meter that receives a `bool visible` each frame.

---

## 6. Example Use Case

### Scenario: Tutorial Level

**Layout:**
```
[Entrance] → [Reception: guard patrols] → [Exhibit] → [Security] → [Vault] → [Exit]
```

**Player actions and DetectionMeter response:**

| Time | Player action                          | Guard state  | Meter value | Level      | UI                     |
|------|----------------------------------------|--------------|-------------|------------|------------------------|
| 0s   | Spawns in Entrance                     | Patrol       | 0%          | Unaware    | Hidden                 |
| 10s  | Enters Reception, visible briefly      | Suspicious   | 0% → 15%   | Unaware → Suspicious | Bar appears, yellow |
| 11s  | Hides behind corner                    | Suspicious   | 15% → 10%   | Suspicious | Yellow, decreasing     |
| 12s  | Peeks out for 0.5s                     | Suspicious   | 10% → 20%   | Suspicious | Yellow, increasing     |
| 13s  | Hides again                            | Suspicious   | 20% → 0%    | Suspicious → Unaware | Bar fades            |
| 13s  | Cooldown starts (2s)                   | Investigate  | 0%          | Unaware    | Hidden, *cannot rise*  |
| 15s  | Cooldown ends                          | Search       | 0%          | Unaware    | Hidden                 |
| 16s  | Walks into open, stays visible 3s      | Suspicious   | 0% → 60%   | Suspicious | Yellow, rising fast    |
| 19s  | Still visible, 1 more second           | Suspicious   | 60% → 80%   | Alert      | Red, pulsing           |
| 20s  | Detection reaches 100%                 | Chase        | 100%        | Alert      | Full red, flash        |

### Configuration used in this scenario:

```
increaseRate       = 0.20   (20%/s  → 5s to full from 0)
decreaseRate       = 0.10   (10%/s  → 10s to empty from full)
cooldownDuration   = 2.0s
suspiciousThreshold = 0.30
alertThreshold     = 0.70
```

---

## 7. Files to Create / Modify

| File | Action | Purpose |
|------|--------|---------|
| `Assets/Scripts/DetectionMeter.cs` | **Create** | Core meter logic |
| `Assets/Scripts/DetectionManager.cs` | **Create** | Singleton registry, HUD feed |
| `Assets/Scripts/WorldSpaceDetectionBar.cs` | **Create** | World-space UI billboard |
| `Assets/Scripts/HUDDetectionMeter.cs` | **Create** | Screen-space HUD widget |
| `Assets/Scripts/GuardFSM.cs` | Modify | Subscribe to DetectionMeter events, replace internal meter |
| `Assets/Scripts/IAlertable.cs` | **Create** (optional) | Interface for any NPC that can receive detection |
| `Assets/Editor/TutorialLevelGenerator.cs` | Modify | Attach DetectionMeter + WorldSpaceDetectionBar to guard |

---

## 8. Implementation Order

1. **DetectionMeter.cs** — core logic, no UI dependencies
2. **GuardFSM modifications** — replace `suspicionMeter` with `DetectionMeter` events
3. **WorldSpaceDetectionBar.cs** — UI billboard on guard
4. **DetectionManager.cs** — singleton, register meters, expose highest value
5. **HUDDetectionMeter.cs** — screen-space bar driven by DetectionManager
6. **Generator update** — wire DetectionMeter + WorldSpaceDetectionBar onto guard prefab

---

## 9. Appendix: Pseudocode Summary

```csharp
// DetectionMeter.cs — per-NPC
void Update()
{
    if (cooldownTimer > 0)
        cooldownTimer -= Time.deltaTime;
    if (cooldownTimer <= 0 && isCoolingDown)
    {
        isCoolingDown = false;
        OnCooldownEnded?.Invoke();
    }

    float prev = value;

    if (playerVisible && !isCoolingDown)
        value = Mathf.MoveTowards(value, 1f, increaseRate * Time.deltaTime);
    else
        value = Mathf.MoveTowards(value, 0f, decreaseRate * Time.deltaTime);

    // Detect full decay → start cooldown
    if (value <= 0f && prev > 0f && !isCoolingDown)
    {
        isCoolingDown = true;
        cooldownTimer = cooldownDuration;
        OnCooldownStarted?.Invoke();
    }

    // Level change
    AlertLevel newLevel = LevelForValue(value);
    if (newLevel != currentLevel)
    {
        currentLevel = newLevel;
        OnLevelChanged?.Invoke(newLevel);
    }

    // Full detection
    if (value >= 1f && prev < 1f)
        OnFullyDetected?.Invoke();

    if (Mathf.Abs(value - prev) > 0.001f)
        OnValueChanged?.Invoke(value);
}

// WorldSpaceDetectionBar.cs
void OnEnable() { detectionMeter.OnValueChanged += UpdateBar; }
void OnDisable(){ detectionMeter.OnValueChanged -= UpdateBar; }

void UpdateBar(float v)
{
    fill.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, bgWidth * v);
    fill.color = Gradient(v);
    canvas.enabled = v > 0.01f;
}

// DetectionManager.cs — singleton
float GetHighestDetection()
{
    float max = 0;
    foreach (var m in meters)
        if (m.isActiveAndEnabled && m.value > max) max = m.value;
    return max;
}

// HUDDetectionMeter.cs
void Update()
{
    float h = DetectionManager.Instance.GetHighestDetection();
    fill.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, bgWidth * h);
    fill.color = Gradient(h);
}
```
