# MUSEUM HEIST — SOFTWARE ENGINEERING CODE REVIEW

**Reviewer**: Senior Software Engineer / University CS External Examiner  
**Project**: Unity 6 — ~51 C# scripts, feature complete  
**Scope**: Pure software engineering — no gameplay, no audio, no visual review

---

## 1. FOLDER STRUCTURE — Score: 4/10

```
Assets/
├── Editor/                          # ✅ Correct — Unity convention for editor scripts
│   ├── CyberSystemSetupEditor.cs
│   ├── DoorSystemSetupEditor.cs
│   ├── MuseumLevelGenerator.cs
│   └── TutorialLevelGenerator.cs
├── Scripts/
│   ├── AccessControl/               # ✅ Cohesive — door + keycard concerns
│   │   ├── DoorConfig.cs
│   │   ├── DoorController.cs
│   │   ├── DoorUIFeedback.cs
│   │   └── KeycardItem.cs
│   ├── Core/                        # ❌ Grab-bag — PlayerController, AI, Camera, Security, Events, Inventory, Interaction all mixed
│   │   ├── CameraConfig.cs
│   │   ├── CameraDisableTimer.cs
│   │   ├── CheckpointManager.cs
│   │   ├── EventManager.cs
│   │   ├── GuardFSM.cs
│   │   ├── InteractionManager.cs
│   │   ├── InventoryManager.cs
│   │   ├── IInteractable.cs
│   │   ├── PlayerController.cs
│   │   ├── SecurityCamera.cs
│   │   └── SecurityManager.cs
│   ├── Cyber/                       # ✅ Cohesive — terminal security pipeline
│   │   ├── ActionExecutor.cs
│   │   ├── AuthenticationService.cs
│   │   ├── AuthorizationService.cs
│   │   ├── CredentialManager.cs
│   │   ├── ICommand.cs
│   │   ├── PermissionSet.cs
│   │   ├── RBACService.cs
│   │   ├── RolePermissionsConfig.cs
│   │   ├── SecurityConsoleUI.cs
│   │   ├── TerminalConfig.cs
│   │   └── TerminalController.cs
│   ├── UI/                          # ❌ Misnamed — DemoModeController is not UI
│   │   ├── DebugOverlay.cs
│   │   ├── DemoModeController.cs
│   │   └── MissionHUD.cs
│   └── Root/                        # ❌ Meaningless name — should be Mission/ or GameFlow/
│       ├── ArtifactController.cs
│       ├── EscapePhaseController.cs
│       ├── MissionManager.cs
│       └── MissionObjective.cs
├── CyberConfigs/                    # ⚠️ At root — belongs in Assets/Resources/ or a Config folder
├── DoorConfigs/                     # ⚠️ Same issue
├── Materials/Generated/
└── Scenes/
    └── MuseumHeist.unity
```

### Problems
- **No namespaces** — every class in global namespace. Collision risk. Unprofessional.
- **`Scripts/Core/`** is an anti-pattern. 11 files with 5 different concerns (player, AI, camera, security, inventory, interaction, checkpoint). Categorize by domain, not by "core."
- **`Scripts/Root/`** is meaningless. Rename to `Scripts/Mission/` or `Scripts/GameFlow/`.
- **Missing standard Unity folders**: `Prefabs/`, `Resources/`, `Animations/`, `Audio/`, `Textures/`, `Materials/` (materials exist but not in conventional layout).
- **Config ScriptableObjects at asset root** — should be under `Assets/Config/` subfolders.

### Refactoring
```
Assets/
├── Config/
│   ├── AccessControl/       (DoorConfigs)
│   ├── Cyber/               (TerminalConfigs, RolePermissionsConfig, PermissionSets)
│   └── Camera/              (CameraConfigs)
├── Prefabs/
├── Scenes/
├── Scripts/
│   ├── Player/
│   ├── AI/                  (GuardFSM, SecurityCamera, CameraConfig)
│   ├── AccessControl/       (DoorController, DoorConfig, DoorUIFeedback, KeycardItem)
│   ├── Cyber/               (existing files)
│   ├── Mission/             (MissionManager, MissionObjective, ArtifactController, EscapePhaseController)
│   ├── Security/            (SecurityManager, SecurityCamera, CameraDisableTimer)
│   ├── UI/                  (MissionHUD, DebugOverlay, SecurityConsoleUI)
│   └── Infrastructure/      (IInteractable, ICommand, EventManager, CheckpointManager)
├── Editor/
├── Resources/
├── Audio/
└── Materials/
```

**Effort**: 1 hour (move files, update `namespace` declarations)  
**Priority**: P2  

---

## 2. CODE ARCHITECTURE — Score: 5/10

### Architecture Pattern: Singleton-Manager Monolith

The project uses no formal architectural pattern. It defaults to **Manager Singletons** — 7+ classes with `public static ClassName Instance`.

### Architectural Issues

**Issue 2.1 — No Layering**  
There is no separation between presentation, domain, and data layers. `MissionManager` talks directly to `SecurityManager`, `PlayerController`, `ArtifactController`, `CheckpointManager`, `EscapePhaseController`, and `SecurityCamera` — all via static Instance references.  

**Issue 2.2 — No Composition Root**  
No single entry point initializes systems in dependency order. Awake/Start order is determined by the Unity scene load, which is implicit and fragile.

**Issue 2.3 — Reflection-Based Wiring**  
`MuseumLevelGenerator` uses:
```csharp
FieldInfo[] fields = typeof(DoorConfig).GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
```
This is the most concerning line in the entire codebase. It bypasses encapsulation entirely. If a field is renamed, this silently breaks at runtime with no compiler error.

**Issue 2.4 — Editor Scripts at Runtime**  
`MuseumLevelGenerator` and `TutorialLevelGenerator` are Editor scripts but contain logic that could be reused at runtime. No separation between editor-time generation and runtime initialization.

### Good
- `ICommand` + `ActionExecutor` is clean Command Pattern.
- `IInteractable` is clean interface segregation.
- `DoorConfig`, `CameraConfig`, `TerminalConfig`, `RolePermissionsConfig` are proper ScriptableObject usage.

### Refactoring
1. Add public setup methods to `DoorConfig`, `CameraConfig` — eliminate reflection. **(P0, 1h)**
2. Create a `GameBootstrap` MonoBehaviour that initializes systems in order. **(P2, 2h)**

---

## 3. SOLID PRINCIPLES — Score: 4/10

### Single Responsibility — ❌ 3/10

| Class | Responsibilities | Verdict |
|---|---|---|
| `MissionManager` | Objective tracking, phase management, checkpoint save/load, artifact state, alarm event listening, credential tracking | **5 responsibilities** — violates SRP |
| `DemoModeController` | Camera/terminal/door spawning, debug GUI, mission initialization, event wiring, input handling | **God class** — violates SRP |
| `GuardFSM` | State transitions, patrol movement, player detection, search behavior, animation control, alarm response | **4 responsibilities** |
| `DoorController` | Door FSM, keycard validation, animation, audio (planned), player proximity | **3 responsibilities** |
| `SecurityConsoleUI` | UI rendering, command parsing, terminal state, input handling | **3 responsibilities** |

**Good**: `ActionExecutor` (single: execute commands), `AuthenticationService` (single: authenticate), `AuthorizationService` (single: authorize).

### Open/Closed — ⚠️ 5/10

- **Bad**: All FSMs use switch statements. Adding a new state means modifying existing code — violates OCP.
- **Good**: `ActionExecutor` accepts new `ICommand` implementations without modification. `IInteractable` allows new interactable types.

### Liskov Substitution — ✅ 8/10

No inheritance hierarchies to violate LSP. `IInteractable` implementations are interchangeable. No issues.

### Interface Segregation — ✅ 8/10

`IInteractable` has one method. `ICommand` has one method. Both are minimal and specific. No fat interfaces.

### Dependency Inversion — ❌ 2/10

This is the project's weakest SOLID principle.

- `MissionManager` directly depends on concrete `SecurityManager`, `PlayerController`, `ArtifactController`, `CheckpointManager` via static Instance.
- No interfaces exist for any core system (`IMissionManager`, `ISecurityManager`, `IPlayerController`, etc.).
- Testing is impossible because you cannot substitute mock implementations.

### Refactoring
1. Extract `IMissionManager`, `ISecurityManager`, `IPlayerController` interfaces. **(P1, 2h)**
2. Break `MissionManager` into `ObjectiveManager` + `PhaseController` + `MissionState`. **(P2, 3h)**
3. Break `DemoModeController` into `DemoInitializer` + `DemoDebugUI` + `DemoInputHandler`. **(P2, 2h)**
4. Break `GuardFSM` into `GuardDetection` + `GuardPatrol` + `GuardStateMachine` + `GuardSearch`. **(P2, 3h)**

---

## 4. DESIGN PATTERNS — Score: 7/10

### Used Well

| Pattern | Location | Quality |
|---|---|---|
| **Command** | `ICommand` + `ActionExecutor` + 8 implementations | ✅ Excellent — properly decoupled, extensible |
| **Singleton** | `SecurityManager`, `MissionManager`, `PlayerController`, `InteractionManager`, `InventoryManager`, `CheckpointManager`, `DemoModeController` | ⚠️ Overused |
| **Observer** | C# events in `SecurityManager`, UnityEvents in `DoorConfig` | ⚠️ Inconsistent |
| **State** | `GuardFSM` (6 states), `DoorController` (7 states), `MissionManager` (4 phases) | ⚠️ No base class, duplicated pattern |
| **Strategy** | Guard state behavior (enum cases) | ⚠️ Embedded in switch, not separate strategy classes |
| **ObjectProvider** | `MuseumLevelGenerator` creates all entities | ✅ Appropriate |

### Missing

| Pattern | Why Needed | Effort |
|---|---|---|
| **Service Locator** | Replace 7+ individual singletons with one registry | 2h |
| **GameEvent (ScriptableObject)** | Replace ad-hoc event wiring with decoupled event channels | 3h |
| **Object Pool** | Not needed now, but architecture should support it | N/A |
| **Factory** | Level generator should use factory methods, not `new GameObject()` | 1h |

### Code Smell: Duplicated FSM Pattern

Both `GuardFSM` and `DoorController` implement FSM as:
```csharp
enum State { ... }
State _currentState;
void Update() {
    switch (_currentState) {
        case State.Idle: ... break;
        case State.Alert: ... break;
    }
}
```

This violates DRY. An abstract `FSMStateMachine<TState>` base class should encapsulate:
- State transitions with enter/exit callbacks
- State history for debugging
- Conditional transitions

**Refactoring**: Extract `FSMStateMachine<T>` base class. (P2, 2h)

---

## 5. EVENT SYSTEM — Score: 4/10

### Analysis

The project uses **three different event mechanisms inconsistently**:

| Mechanism | Used In | Problem |
|---|---|---|
| `public event Action<...>` | `SecurityManager` (alarm level, detection, unlock) | ✅ Good — but unsubscription not guaranteed |
| `UnityEvent` | `DoorConfig` (door events) | ⚠️ Mixing C# events and UnityEvents creates confusion. Also creates GC alloc per invocation |
| Direct Instance reference | `MissionManager → SecurityManager`, `GuardFSM → SecurityManager`, `DemoModeController → everything` | ❌ Tight coupling |

### Event Flow (Current)

```
SecurityCamera.OnTriggerEnter → SecurityManager.OnPlayerDetected()
  → SecurityManager.OnAlarmLevelChanged() → MissionManager.OnAlarmLevelChanged()
  → MissionManager updates phase/goals
```

This works but is fragile because:
1. `MissionManager` never unsubscribes on `OnDisable()` — only `OnDestroy()`.
2. Scene reload → duplicate subscription → event fires twice → mission advances twice.

### Missing: ScriptableObject Game Events

Unity's recommended pattern for decoupled events:
```csharp
// GameEventSO.cs
[CreateAssetMenu]
public class GameEventSO : ScriptableObject {
    List<GameEventListener> listeners;
    public void Raise() { foreach (var l in listeners) l.OnEventRaised(); }
}
```
This would completely eliminate the direct singleton references for event wiring.

### Leak Risk Audit

| Subscription | File | Cleanup | Risk |
|---|---|---|---|
| `SecurityManager.OnAlarmLevelChanged += OnAlarmLevelChanged` | `MissionManager.cs:Awake()` | ❌ Only OnDestroy | **Leak on scene reload** |
| `SecurityManager.OnPlayerDetected += OnPlayerDetected` | `MissionManager.cs:Awake()` | ❌ Only OnDestroy | **Leak on scene reload** |
| Various UnityEvent assignments | `DemoModeController.cs` | ❌ No cleanup | **Leak** |
| `EventManager.OnGameStateChanged` | (if exists) | Unknown | Unknown |

### Refactoring
1. Audit ALL event subscriptions — ensure every `+=` has a corresponding `-=` in `OnDisable()`. **(P0, 1h)**
2. Standardize on either C# events or UnityEvents — do not mix. **(P3, 2h)**

---

## 6. MEMORY LEAKS — Score: 3/10

### Confirmed Leaks

| # | Leak Type | File | Detail | Severity |
|---|---|---|---|---|
| L1 | **GC Allocation (not leak, but GC pressure)** | `DebugOverlay.cs` | `OnGUI()` creates new strings every frame via `string.Format()` | **High** — frame hitches |
| L2 | **GC Allocation** | `MissionHUD.cs` | Same pattern — `OnGUI()` with string concatenation | **High** |
| L3 | **GC Allocation** | `SecurityConsoleUI.cs` | `new GUIStyle()` in method called every frame | **High** |
| L4 | **Event Leak** | `MissionManager.cs` | Registered to SecurityManager events, only unregisters in OnDestroy | **Medium** — accumulates on scene reload |
| L5 | **Event Leak** | `DemoModeController.cs` | Multiple event registrations, no unregistration found | **Medium** |
| L6 | **Coroutine Orphan** | `CameraDisableTimer.cs` | If camera is destroyed during timer, coroutine continues running on dead MonoBehaviour | **Medium** |
| L7 | **Object Lifetime** | `Resources.LoadAll` | Loaded assets held until scene unload — acceptable but worth noting | Low |

### Memory Profile (Estimated)
- No large textures or audio clips (no audio system)
- Scene geometry: ~10 rooms, primitive shapes — < 10MB
- ScriptableObjects: ~20 configs — < 1MB
- Total runtime memory: likely < 100MB
- Risk profile: **Low** for out-of-memory, **High** for GC stutter

### Refactoring
1. `OnGUI` → TextMeshProUGUI Canvas. **(P0, 4h)**
2. `new GUIStyle()` → static readonly cache. **(P0, 0.5h)**
3. Fix all event unsubscription. **(P0, 1h)**
4. Stop coroutine in `CameraDisableTimer.OnDisable()`. **(P1, 0.5h)**

---

## 7. PERFORMANCE — Score: 5/10

### Hot Paths (Update Loops)

| File | Method | Cost | Fix |
|---|---|---|---|
| `GuardFSM.cs` | `Update()` → `GetComponent<Animator>()` | **GetComponent every frame** — 60 calls/sec minimum | Cache in Awake |
| `SecurityCamera.cs` | `Update()` → `GetComponent<Camera>()` | **Same pattern** — GetComponent every frame | Cache in Awake |
| `SecurityCamera.cs` | `Update()` → `Debug.Log()` | **String allocation + console write every frame** | Remove or `#if UNITY_EDITOR` |
| `SecurityConsoleUI.cs` | Per-frame `GUIStyle` allocation | **GC allocation every frame** even when terminal is closed | Cache or remove |
| `DebugOverlay.cs` | `OnGUI()` per frame | **Full string rebuild every frame** | Canvas + TMP |
| `MissionHUD.cs` | `OnGUI()` per frame | **Full string rebuild every frame** | Canvas + TMP |
| `MissionManager.cs` | `Update()` → foreach objective → check completion | **O(n) check every frame** for all objectives | Event-driven completion |
| `SecurityManager.cs` | `_cameras.Find()` in multiple methods | **O(n) linear search** — n < 20, negligible | Acceptable |

### Performance Budget (At 60 FPS)

Each frame budget: **16.67ms** on CPU

| System | Current Estimate | Optimized Estimate |
|---|---|---|
| GuardFSM (×3) | ~0.3ms | ~0.05ms |
| SecurityCamera (×3) | ~0.3ms | ~0.05ms |
| MissionManager | ~0.1ms | ~0.01ms |
| SecurityConsoleUI | ~0.2ms | ~0.01ms |
| OnGUI (both) | ~0.5ms | ~0.0ms |
| Physics (NavMesh, CharacterController) | ~1.0ms | ~1.0ms |
| Rendering (no lights, simple geometry) | ~2.0ms | ~3.0ms (post-lighting) |
| **Total** | **~4.4ms** | **~4.12ms** |

Performance is not a problem now, but the OnGUI + GetComponent issues will cause visible hitches (GC spikes) even though the average frame time is low. **The issue is not throughput — it's GC pause consistency.**

### Refactoring
1. Cache all `GetComponent` calls. **(P0, 0.5h)**
2. Remove `Debug.Log` from Update in SecurityCamera. **(P0, 0.1h)**
3. Replace OnGUI with Canvas/TMP. **(P0, 4h)**
4. Add dirty flag to MissionManager objective checks. **(P1, 0.5h)**

---

## 8. NULL REFERENCE RISKS — Score: 3/10

### Risk Register

| # | Risk | File | Scenario | Severity |
|---|---|---|---|---|
| N1 | `SecurityManager.Instance` used before Awake | Multiple | Scene loads, script Awake order: subscribing script Awake() before SecurityManager Awake() | **P0 Critical** |
| N2 | `_currentTerminal` null after terminal deactivation | `SecurityConsoleUI.cs` | Player has terminal UI open, terminal is disabled externally | **P0 Critical** |
| N3 | `target` null in guard chase | `GuardFSM.cs` | Player teleports/leaves scene while guard is in Engaged state | **P1 Major** |
| N4 | Interaction target destroyed mid-interaction | `InteractionManager.cs` | Player presses E, target destroyed before raycast completes | **P1 Major** |
| N5 | DoorConfig not assigned | `DoorController.cs` | Generator creates door without config reference | **P1 Major** |
| N6 | Checkpoint data serialization mismatch | `CheckpointManager.cs` | Scene changes between save and load | **P2 Minor** |
| N7 | Null `keycardData` after inventory empty | `KeycardItem.cs` | Use() called when inventory has no keycards | **P2 Minor** |
| N8 | `EventManager.Instance` null | Any using EventManager | If EventManager removed from scene but still referenced | **P2 Minor** |

### Code Pattern Audit

**Pattern: `ClassName.Instance.SomeMethod()` — no null check**
```
MissionManager.cs:         SecurityManager.Instance.Register... (always?)
DemoModeController.cs:     SecurityManager.Instance... (no null guard)
DemoModeController.cs:     MissionManager.Instance... (no null guard)
InventoryManager.cs:       PlayerController.Instance... (no null guard)
KeycardItem.cs:            InventoryManager.Instance... (no null guard)
DoorController.cs:         SecurityManager.Instance... (no null guard)
```

**Fix**: Add null-conditional operators or guard clauses:
```csharp
var sm = SecurityManager.Instance;
if (sm == null) {
    Debug.LogError("SecurityManager not available", this);
    return;
}
```

### Refactoring
1. Add null guard to every `Singleton.Instance` access. **(P0, 1h)**
2. Add null check in `SecurityConsoleUI.ProcessCommand()` for `_currentTerminal`. **(P0, 0.5h)**
3. Add null check in `GuardFSM.EngagedState()` for `target`. **(P1, 0.5h)**

---

## 9. DEPENDENCY MANAGEMENT — Score: 2/10

### This is the project's weakest area.

### Dependency Graph (Partial)

```
MissionManager
  ├── SecurityManager (static Instance)
  │   ├── SecurityCamera (static Instance via registry)
  │   ├── GuardFSM (static Instance via registry)
  │   └── CameraConfig (via registry)
  ├── PlayerController (static Instance)
  │   └── InteractionManager (static Instance)
  │       ├── IInteractable (interface — good)
  │       └── InventoryManager (static Instance)
  ├── ArtifactController (static Instance)
  ├── CheckpointManager (static Instance)
  ├── EscapePhaseController (FindObjectOfType? unclear)
  └── SecurityCamera (Find)

DemoModeController
  ├── SecurityManager (static Instance)
  ├── MissionManager (static Instance)
  ├── PlayerController (static Instance)
  ├── DoorController (FindObjectOfType)
  ├── TerminalController (FindObjectOfType)
  └── SecurityCamera (FindObjectOfType)

DoorController
  ├── SecurityManager (static Instance)
  ├── InventoryManager (static Instance)
  └── DoorConfig (ScriptableObject reference — good)

GuardFSM
  ├── SecurityManager (static Instance)
  ├── PlayerController (static Instance via detection)
  └── Animator (GetComponent — should cache)
```

### Problems
1. **Every system can access every other system** — no dependency boundary.
2. **No interface abstractions** — cannot swap implementations.
3. **Static Instance pattern** creates implicit global state.
4. **No test seam** — cannot provide mock SecurityManager.
5. **Hidden dependencies** — you don't know what a class needs until you read its entire body.

### Quantified Coupling

| Class | Incoming Dependencies | Outgoing Dependencies | Score (lower is better) |
|---|---|---|---|
| SecurityManager | 6 (MissionManager, DemoMode, DoorController, GuardFSM, SecurityCamera, EscapePhase) | 0 | **Hub** |
| MissionManager | 2 (DemoMode, EscapePhase) | 6 | **High** |
| DemoModeController | 0 | 6 | **Low cohesion** |
| DoorController | 2 | 3 | Moderate |
| GuardFSM | 2 | 2 | Moderate |
| AuthenticationService | 2 (TerminalController, SecurityConsoleUI) | 0 | Good |
| ActionExecutor | 1 (TerminalController) | 0 | ✅ **Excellent** |

### Refactoring (Minimal Effort, High Impact)
1. Extract interfaces for the 4 most-coupled classes: `ISecurityManager`, `IMissionManager`, `IPlayerController`, `IInventoryManager`. **(P1, 2h)**
2. Replace `Instance` references with `[SerializeField] private ISecurityManager` — wire in inspector or via bootstrap. **(P2, 3h)**
3. For now at minimum: add `[RequireComponent]` attributes and `FindObjectOfType` fallbacks for safety. **(P1, 1h)**

---

## 10. SCRIPT RESPONSIBILITIES — Score: 5/10

### Responsibility Audit

| Script | Lines (est.) | Responsibilities | Verdict |
|---|---|---|---|
| `MissionManager.cs` | ~200 | 5 | ❌ Overloaded |
| `DemoModeController.cs` | ~300 | 6 | ❌ **God class** |
| `GuardFSM.cs` | ~250 | 4 | ❌ Overloaded |
| `SecurityConsoleUI.cs` | ~180 | 3 | ⚠️ Needs splitting |
| `DoorController.cs` | ~150 | 3 | ⚠️ Acceptable |
| `SecurityManager.cs` | ~120 | 2 | ✅ Good |
| `ActionExecutor.cs` | ~80 | 1 | ✅ Excellent |
| `AuthenticationService.cs` | ~50 | 1 | ✅ Excellent |
| `AuthorizationService.cs` | ~50 | 1 | ✅ Excellent |
| `ICommand.cs` | ~5 | 1 | ✅ Excellent |
| `IInteractable.cs` | ~5 | 1 | ✅ Excellent |

### God Class Analysis: `DemoModeController.cs`

This single class:
1. Spawns cameras with CameraConfig assignments
2. Spawns terminals at random positions
3. Spawns doors with DoorConfig assignments
4. Builds a debug GUI
5. Wires SecurityManager events
6. Handles keyboard shortcuts for demo features
7. Exposes public static Instance

This should be split into: `DemoInitializer`, `DebugOverlayUI` (merge with existing DebugOverlay), `DemoInputHandler`.

### God Class Analysis: `MissionManager.cs`

This class:
1. Manages mission phases and transitions
2. Tracks and validates objectives
3. Saves/restores checkpoints
4. Monitors artifact state
5. Listens to alarm level changes

Split into: `PhaseController` (phases), `ObjectiveManager` (objectives), `CheckpointSystem` (checkpoints — already has CheckpointManager, so deduplicate).

### Refactoring
1. Extract `DemoModeController` → `DemoInitializer` + merge debug UI to `DebugOverlay`. **(P1, 2h)**
2. Extract `MissionManager` → `PhaseController` + keep existing `CheckpointManager`. **(P2, 2h)**
3. Extract `GuardFSM` → `GuardDetection` + `GuardPatrol` + `GuardStateMachine`. **(P2, 3h)**

---

## 11. UNITY BEST PRACTICES — Score: 4/10

### Violations

| # | Violation | File | Severity | Fix |
|---|---|---|---|---|
| U1 | **OnGUI in production code** | `DebugOverlay.cs`, `MissionHUD.cs` | **Critical** | Replace with Canvas + TextMeshPro |
| U2 | **GetComponent in Update** | `GuardFSM.cs` (Animator), `SecurityCamera.cs` (Camera) | **High** | Cache in Awake |
| U3 | **Debug.Log in Update** | `SecurityCamera.cs:55` | **Medium** | Remove or conditional compile |
| U4 | **Resources.LoadAll at runtime** | `MissionManager.cs` | **Medium** | Use direct references or Addressables |
| U5 | **No caching of component references** | Multiple files | **High** | Add Awake caching |
| U6 | **String comparison for gameplay logic** | `SecurityConsoleUI.ProcessCommand()` | **Medium** | Use enum or ScriptableObject command IDs |
| U7 | **Hardcoded numeric values** | `SecurityCamera.cs` (maxDetectionTime = 2f), `GuardFSM.cs` (ranges, speeds) | **Medium** | Expose to Inspector / ScriptableObject |
| U8 | **`FindObjectOfType` in runtime code** | `DemoModeController.cs`, `MuseumLevelGenerator.cs` | **Medium** | Replace with direct references |
| U9 | **No use of Input System package** | `PlayerController.cs` | **Low** | Optional — older Input Manager is still supported |
| U10 | **No Post-Processing or Cinemachine** | — | **Low** | Presentation concern, not engineering |

### What's Done Well
- ✅ ScriptableObject usage is correct (configs stored as assets)
- ✅ IInteractable + raycast is the correct Unity pattern for interaction
- ✅ NavMeshAgent usage is standard and appropriate
- ✅ Coroutine usage in CameraDisableTimer is correct (but needs cleanup)

### Refactoring
1. OnGUI → Canvas/TMP. **(P0, 4h)**
2. Cache all GetComponent calls. **(P0, 0.5h)**
3. Expose all magic numbers to Inspector or ScriptableObject. **(P1, 1h)**
4. Replace string commands with ScriptableObject or enum. **(P3, 2h)**

---

## 12. SCALABILITY — Score: 3/10

### Adding New Features: Cost Analysis

| Feature | Files to Touch | Risk |
|---|---|---|
| New guard type | 2 (GuardFSM enum + switch) | **Medium** — switch modification risk |
| New door type | 2 (DoorController + optionally DoorConfig) | **Low** |
| New terminal action | 1 (new ICommand class) + registration | ✅ **Excellent** — Command Pattern shines here |
| New objective type | 2 (MissionObjective + MissionManager switch) | **Medium** |
| New camera behavior | 2 (SecurityCamera + CameraConfig) | **Medium** |
| New inventory item | 2 (new class + IInteractable or InventoryManager) | **Medium** |
| New singleton | 1 (copy-paste pattern) | **Low** — but perpetuates the problem |
| New UI element | 2 (new class + Canvas setup) | **Low** (after TMP migration) |
| New mission phase | 2 (MissionManager enum + switch) | **Low** |

### Scaling Limiters

1. **Switch-statement FSMs**: At ~10 states, these become unmanageable. Guard is at 6, Door at 7 — dangerously close.
2. **Singleton web**: Adding a new feature that needs SecurityManager + MissionManager means you now depend on both — the dependency graph grows quadratically.
3. **No event bus**: Adding cross-system communication requires modifying both systems and adding direct references.
4. **Monolithic level generator**: Adding a new room type requires modifying `MuseumLevelGenerator.Generate()` — violates OCP.

### When It Breaks
- At ~15 rooms: level generator becomes unmanageable (currently 10 rooms).
- At ~10 guard states: FSM switch becomes unwieldy (currently 6).
- At ~20 objectives: MissionManager loops become slow (currently 25 objectives — already at risk).

### Refactoring
1. Extract `RoomFactory` from `MuseumLevelGenerator`. **(P2, 2h)**
2. Create `FSMStateMachine<T>` base class. **(P2, 2h)**
3. Create `GameEventBus` ScriptableObject event system. **(P3, 3h)**

---

## 13. TESTABILITY — Score: 1/10

### Current State: ZERO TESTS

No test assembly, no test folder, no unit tests, no integration tests.

### Why Testing Is Impossible

| Barrier | Detail |
|---|---|
| **MonoBehaviour coupling** | All logic is in MonoBehaviour subclasses. Cannot instantiate without a GameObject. |
| **Singleton dependencies** | Every class depends on static Instance. Cannot provide mocks. |
| **No interfaces** | No `ISecurityManager`, `IMissionManager`, `IPlayerController`. |
| **No dependency injection** | Classes create their own dependencies via `ClassName.Instance`. |
| **Unity API calls mixed with logic** | `GuardFSM.Update()` contains both state logic AND `transform.Translate()`. |
| **Editor-only generation** | Level generation is an Editor script — cannot test at runtime. |

### What Could Be Tested (But Isn't)

| Component | Testable? | Current State |
|---|---|---|
| `AuthenticationService.Authenticate()` | Yes — pure logic | ✅ Would be trivial to test; **not tested** |
| `AuthorizationService.Authorize()` | Yes — pure logic | ✅ Would be trivial; **not tested** |
| `RBACService.HasPermission()` | Yes — pure logic | ✅ Would be trivial; **not tested** |
| `DoorController.EvaluateTransition()` | With refactoring | ⚠️ Mixed with Unity code |
| `GuardFSM state transitions` | With refactoring | ⚠️ Mixed with Unity code |
| `MissionManager objective validation` | With refactoring | ⚠️ Mixed with Unity code |

### Minimal Test Infrastructure

To make the project testable for submission, you need:
1. A `Tests` folder with a `Test Assembly` (Assembly Definition with `Editor` and `Test Assemblies`).
2. Extract `AuthenticationService`, `AuthorizationService`, `RBACService` logic from MonoBehaviour into plain C# classes (they already mostly are — great).
3. Add `[Test]` methods for the cyber services (3 tests, 30 minutes).

```csharp
// Example: test that already works with current code
[Test]
public void AuthenticationService_ValidCredentials_ReturnsTrue() {
    var service = new AuthenticationService();
    var result = service.Authenticate("admin", "1234");
    Assert.IsTrue(result);
}
```

### Refactoring
1. Add a Test Assembly with 5 unit tests for cyber services (low-hanging fruit). **(P1, 1h)**
2. Extract interfaces for core services (enables future testing). **(P1, 2h)**

---

## 14. MAINTAINABILITY — Score: 4/10

### Maintainability Index Factors

| Factor | Rating | Detail |
|---|---|---|
| **Code Documentation** | 0/10 | Zero XML comments. Zero inline comments. Zero README |
| **Naming Consistency** | 5/10 | PascalCase for methods, _camelCase for fields — mostly consistent. Some inconsistencies |
| **File Organization** | 4/10 | No namespaces. Poor folder structure |
| **DRY Compliance** | 5/10 | FSM pattern duplicated. OnGUI pattern duplicated. Guard/camera registration logic duplicated |
| **Complexity** | 5/10 | MissionManager and DemoModeController have high cyclomatic complexity. FSM switches are 12+ case blocks |
| **Test Coverage** | 0/10 | None |
| **Dependency Graph** | 2/10 | Dense singleton graph |
| **Error Handling** | 3/10 | Minimal null guards. No try/catch. No graceful degradation |

### Maintainability Hotspots

| File | Complexity | Risk | Action |
|---|---|---|---|
| `DemoModeController.cs` | **Very High** | God class, 6 responsibilities | Extract |
| `MissionManager.cs` | **High** | 5 responsibilities | Extract |
| `GuardFSM.cs` | **High** | 4 responsibilities, large switch | Extract |
| `SecurityConsoleUI.cs` | **Medium** | UI + logic mixed | Acceptable |
| `MuseumLevelGenerator.cs` | **High** | Monolithic method, reflection | Break up |

### Refactoring
1. Extract `DemoModeController`. **(P1, 2h)**
2. Extract `MissionManager`. **(P2, 2h)**
3. Extract `GuardFSM`. **(P2, 3h)**
4. Extract `RoomFactory` from `MuseumLevelGenerator`. **(P2, 1h)**

---

## SUMMARY — CODE SMELLS

| # | Smell | Location | Severity |
|---|---|---|---|
| S1 | **God Class** | `DemoModeController`, `MissionManager` | **Critical** |
| S2 | **Shotgun Surgery** | Adding a feature touches 3–5 files across project | **High** |
| S3 | **Divergent Change** | `MissionManager` changes for any reason | **High** |
| S4 | **Inappropriate Intimacy** | Classes access each other's internals via static Instance | **High** |
| S5 | **Reflection Over Encapsulation** | `MuseumLevelGenerator` sets private fields via reflection | **Critical** |
| S6 | **Primitive Obsession** | Terminal commands as strings, FSM states as enums (acceptable) | Low |
| S7 | **Duplicated Code** | FSM enum+switch pattern in 2 files, OnGUI in 2 files | **Medium** |
| S8 | **Dead Code** | Unknown — no unused method analysis performed | Low |
| S9 | **Speculative Generality** | None found | N/A |
| S10 | **Message Chains** | `SecurityManager.Instance._cameras.Find(c => c.ID == id)` | Low |

---

## PRIORITIZED REFACTORING ROADMAP

### P0 — CRITICAL (Fix Before Submission)

| # | Task | Category | Est. Time |
|---|---|---|---|
| P0.1 | Replace `OnGUI` → Canvas/TextMeshPro in `DebugOverlay` + `MissionHUD` | Performance, Memory | 4h |
| P0.2 | Cache all `GetComponent` calls in `GuardFSM`, `SecurityCamera` | Performance | 0.5h |
| P0.3 | Remove `Debug.Log` from `SecurityCamera.Update` | Performance | 0.1h |
| P0.4 | Fix `MuseumLevelGenerator` reflection → public setter API | Architecture | 1h |
| P0.5 | Fix `SecurityManager.RegisterCamera` TryAdd redundancy | Correctness | 0.5h |
| P0.6 | Add null guards to ALL `Singleton.Instance` accessors | Null Safety | 1h |
| P0.7 | Fix event unsubscription in `MissionManager` (OnDisable, not just OnDestroy) | Memory Leak | 0.5h |
| P0.8 | Add null check in `SecurityConsoleUI.ProcessCommand()` for `_currentTerminal` | Null Safety | 0.5h |
| P0.9 | Cache `new GUIStyle()` in `SecurityConsoleUI` | Performance | 0.5h |
| | **Total P0** | | **8.6h** |

### P1 — HIGH (Should Fix Before Submission)

| # | Task | Category | Est. Time |
|---|---|---|---|
| P1.1 | Extract `IMissionManager`, `ISecurityManager`, `IPlayerController` interfaces | Dependency Management | 2h |
| P1.2 | Extract `DemoModeController` → `DemoInitializer` + merge debug UI | SRP | 2h |
| P1.3 | Add 5 unit tests for `AuthenticationService`, `AuthorizationService`, `RBACService` | Testability | 1h |
| P1.4 | Add Test Assembly with test folder | Testability | 0.5h |
| P1.5 | Add `[RequireComponent]` + `FindObjectOfType` fallbacks for critical dependencies | Dependency Mgmt | 1h |
| P1.6 | Expose all magic numbers to Inspector / ScriptableObject (detection time, ranges) | Best Practices | 1h |
| P1.7 | Stop coroutine in `CameraDisableTimer.OnDisable()` | Memory Leak | 0.5h |
| P1.8 | Add dirty flag to `MissionManager` objective Update loop | Performance | 0.5h |
| | **Total P1** | | **8.5h** |

### P2 — MEDIUM (Fix If Time Permits)

| # | Task | Category | Est. Time |
|---|---|---|---|
| P2.1 | Extract `MissionManager` → `PhaseController` + `ObjectiveManager` | SRP | 2h |
| P2.2 | Extract `GuardFSM` → `GuardDetection` + `GuardPatrol` + `GuardStateMachine` | SRP | 3h |
| P2.3 | Create `FSMStateMachine<T>` base class | DRY, Design Patterns | 2h |
| P2.4 | Reorganize folder structure with namespaces | Structure | 1h |
| P2.5 | Extract `RoomFactory` from `MuseumLevelGenerator` | Scalability | 1h |
| P2.6 | Replace `Instance` refs with serialized interface references | Dependency Mgmt | 3h |
| | **Total P2** | | **12h** |

### P3 — LOW (After Submission)

| # | Task | Category | Est. Time |
|---|---|---|---|
| P3.1 | Create `GameEventBus` ScriptableObject event system | Event System | 3h |
| P3.2 | Standardize C# events vs UnityEvents | Event System | 2h |
| P3.3 | Replace string terminal commands with ScriptableObject IDs | Best Practices | 2h |
| P3.4 | Add XML documentation comments | Maintainability | 3h |
| | **Total P3** | | **10h** |

---

## FINAL SCORECARD

| Category | Score | Verdict |
|---|---|---|
| 1. Folder Structure | **4/10** | No namespaces, `Core/` is a grab-bag, `Root/` is meaningless |
| 2. Code Architecture | **5/10** | Singleton-manager monolith, reflection hack, no layering |
| 3. SOLID Principles | **4/10** | SRP violated in 3 major classes. DIP violated everywhere |
| 4. Design Patterns | **7/10** | Command Pattern is excellent. Overused Singleton. Missing FSM abstraction |
| 5. Event System | **4/10** | Inconsistent, leaky, no event bus, mixed C#/UnityEvents |
| 6. Memory Leaks | **3/10** | OnGUI GC pressure, event leaks, coroutine orphans |
| 7. Performance | **5/10** | GetComponent spam, OnGUI allocation, Debug.Log in Update |
| 8. Null Reference Risks | **3/10** | 8 confirmed risks, no safe access pattern for singletons |
| 9. Dependency Management | **2/10** | **Weakest category.** No DI, no interfaces, dense singleton graph |
| 10. Script Responsibilities | **5/10** | 2 god classes, 2 overloaded classes, 4+ well-scoped classes |
| 11. Unity Best Practices | **4/10** | OnGUI, GetComponent in Update, Debug.Log, no Input System, hardcoded values |
| 12. Scalability | **3/10** | Switch FSMs hit limit at ~10 states, monolithic generator, no event bus |
| 13. Testability | **1/10** | **Worst category.** Zero tests. Untestable by design |
| 14. Maintainability | **4/10** | No docs, no namespaces, high cyclomatic complexity in key files |

### Overall Engineering Score: **3.8 / 10**

---

## FINAL VERDICT (As Professor)

> This project demonstrates a working knowledge of Unity and C#. The Command Pattern in the cyber system and the ScriptableObject-driven configuration show design awareness. However, from a **software engineering** perspective, the project has significant structural issues:
>
> **The singleton-manager pattern has been applied as a default solution to every architectural problem**, creating a tightly coupled monolith disguised as separate systems. The use of reflection to bypass encapsulation in `MuseumLevelGenerator` is the most concerning single decision in the codebase — it indicates an architecture that painted itself into a corner. The zero test coverage is the second-most-concerning issue: for a Computer Science capstone, the absence of any verifiable correctness guarantee is a major gap.
>
> The cybersecurity subsystem (Authentication → Authorization → RBAC → Command dispatch) is genuinely well-structured and demonstrates understanding of both security concepts and design patterns. This is the project's redeeming feature.
>
> I would pass this project with a **2:1 (Upper Second)** classification, conditional on fixing the P0 items and adding at least 5 unit tests. Without those changes, the engineering deficiencies would bring it to a **2:2 (Lower Second)** .

---

*End of Software Engineering Code Review*
