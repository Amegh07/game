# Museum Heist — Comprehensive Final Review

> **Phase 5A Update**: See Appendix at end for post-review gameplay enhancements (Elite Guard, Checkpoints, Camera Network, Mission Scoring, HUD Enhancements, Results Screen).



## 1. Gameplay Review

### Core Loop
The game delivers a complete stealth-puzzle loop: observe guard patrols → avoid or disable cameras → hack terminals for credentials → use keycards at locked doors → reach and escape with the artifact. This is tight, proven, and fun. The mission phases (Infiltration → Navigation → Acquisition → Escape) structure progression naturally.

### Strengths
- **Terminal hacking** injects variety. Requiring Authentication → Authorization → RBAC bypass creates a satisfying multi-step puzzle.
- **Camera disabling** (30s timer with auto-re-enable) adds tension without permanent removal of consequences.
- **Guard awareness states** (Idle → Suspicious → Alert → Investigating → Searching → Engaged) give clear feedback.
- **Escape phase** with door lockdown is a strong finale.

### Weaknesses & Fixes
| Issue | Severity | Fix |
|---|---|---|
| No failure state on alarm level 3 (no game over) | **Critical** | MissionManager: `OnAlarmLevelChanged(3)` → trigger `FailMission("Alarm level critical — mission aborted")` |
| Keycards are infinite-use — no tension | Medium | KeycardItem: add `maxUses` (1–3); decrement on successful door open; destroy when exhausted |
| No distraction mechanic (noise pebbles, etc.) | Low | Add `DistractionItem` that implements `IInteractable`; guard investigates sound position |
| Terminal menu requires exact string match | High | `SecurityConsoleUI.ProcessCommand()` needs substring/fuzzy matching OR numbered menu option |
| No movement noise system | Medium | PlayerController: add `noiseRadius` based on movement speed; guards hear within range |
| Camera disable has no animation/feedback | Low | CameraConfig: add `disableDuration` exposed to inspector; play disable VFX on camera |

### Player Feel
The mouse-look + WASD is standard and comfortable. The interaction system (raycast + E + outline) is responsive. The lack of footstep audio and camera-disable effects makes the world feel empty (see Audio/Visual sections).

---

## 2. Architecture Review

### Overall Assessment
The architecture is **solid but heavily Singleton-dependent** with inconsistent event patterns. 9+ classes expose `public static Instance { get; private set; }`. This works for a prototype but creates hidden coupling.

### Singleton Audit
| Class | Justified? | Alternative |
|---|---|---|
| `MissionManager` | Yes — true global mission state | Keep |
| `SecurityManager` | Yes — global alarm/registry hub | Keep |
| `PlayerController` | No — can use FindObjectOfType or DI | Accept for scope |
| `InteractionManager` | No — accessible via PlayerController | Merge or inject |
| `InventoryManager` | No — belongs on PlayerController | Merge |
| `HUDController` | ? Not seen in codebase | -- |
| `CheckpointManager` | Yes — global persistence | Keep |

### Event Pattern Inconsistency
- `SecurityManager` uses `event Action<...>` correctly with null-check invocation.
- `DoorController` uses custom `UnityEvent` on `DoorConfig`.
- `GuardFSM` has no guard-specific events (no `OnGuardStateChanged`).
- `TerminalController` uses no events at all.
- No centralized `GameEventBus` pattern — leads to ad-hoc references.

**Recommendation**: Standardize on UnityEvents for inspector wiring and C# events for code-to-code. Add `GameEventBus` singleton with `Trigger<T>()`/`Listen<T>()` generics for decoupled communication.

### State Machines
- **GuardFSM**: 6-state enum + switch in Update(). Clean but could use a State pattern with separate classes.
- **DoorController**: 7-state enum + switch. Good.
- **MissionManager**: phase enum + switch. Good.

All are maintainable. No rewrite needed — FSM via enum+switch is fine for this scope.

### Dependency Injection
No DI container. Manual wiring in `DemoModeController` and `MissionManager.Start()`. This works but means any new system requires manual `FindObjectOfType` or `Instance` hookup. For final-year project, **acceptable**.

### Code Quality Highlights
- **Clean**: `DoorController`, `MissionManager`, `AuthorizationService`
- **Messy**: `DemoModeController` (monolithic, does everything), `SecurityConsoleUI` (GUI allocation per frame)
- **Dangerous**: `DebugOverlay` and `MissionHUD` use `MonoBehaviour.OnGUI()` with string allocations every frame

**Priority fix**: Move debug HUD to TextMeshPro UI Canvas. `OnGUI()` is deprecated for modern Unity.

---

## 3. Performance Review

### Immediate Issues (fix before submission)

| Issue | File | Impact |
|---|---|---|
| `OnGUI()` string alloc per frame | `DebugOverlay.cs`, `MissionHUD.cs` | GC pressure — framerate stutter |
| `new GUIStyle()` per frame | `SecurityConsoleUI.cs` | GC alloc — unnecessary |
| `GetComponent<Animator>()` per frame | `GuardFSM.cs` | CPU waste — cache in Awake |
| `GetComponent<Camera>()` in Update | `SecurityCamera.cs` | CPU waste — cache |
| `Debug.Log()` in Update | `SecurityCamera.cs:55` | Console spam — kill |
| Per-frame instantiate in demo | `DemoModeController.cs` | GC alloc |
| `Resources.LoadAll()` at runtime | `MissionManager.cs` | Blocking load — move to Awake |
| `string.Format`/concatenation in Update | Multiple files | Prefer `StringBuilder` or string interpolation (same perf, better readability) |

### Medium Priority
- No object pooling anywhere. Guards, cameras, doors are all placed at design time so pooling is unnecessary. But if pickups/effects are added, pool them.
- `SecurityManager` camera/guard registries use `List<T>` with `Find()` calls — O(n). Fine for < 20 entities.
- `MissionManager` loops over all objectives every frame — add dirty flag or event-driven completion check.

### Architecture-Level Performance
- `MuseumLevelGenerator` uses `FindObjectOfType<SecurityManager>()` then `GetFields(BindingFlags.NonPublic | BindingFlags.Instance)` to set private fields via reflection. **Replace with public setup methods** for performance and safety.

---

## 4. UX / UI Plan

### Current State
- MissionHUD via `OnGUI` — text-only, no health/ammo (stealth game).
- No pause menu, settings, or key rebinding.
- No tutorial UI beyond world text.
- No mini-map or orientation aid.

### Proposed UI Hierarchy
```
MainCanvas (World Space / Screen Space — Overlay)
├── PauseMenu (Panel, initially disabled)
│   ├── Resume button
│   ├── Settings button → Audio sliders, Sensitivity slider
│   └── Quit to Main Menu button
├── MissionHUD (Panel, always visible)
│   ├── Phase indicator (text)
│   ├── Objective list (TextMeshPro — numbered, strikethrough on complete)
│   ├── Alarm level indicator (color-coded: green/yellow/red/critical)
│   └── Timer (escape phase)
├── InteractionPrompt (World-space or center-screen, contextual)
│   └── "Press E to hack terminal" / "Press E to use keycard"
├── TerminalUI (Panel, shown on terminal interaction)
│   ├── Output scroll rect
│   └── Input field
├── MissionComplete/Fail (Panel, full-screen overlay)
│   ├── Outcome text
│   ├── Stats (time, alarms triggered, doors opened)
│   └── Restart / Main Menu buttons
└── Crosshair (Image, simple dot)
```

### Priority Implementation
1. Replace `OnGUI` with `TextMeshProUGUI` on a Canvas (fixes GC, enables styling).
2. Pause menu with Escape key. `Time.timeScale = 0` on pause.
3. Mission complete/fail screen with stats.
4. Mouse sensitivity slider in settings.

---

## 5. Audio Plan

### Current State: No audio system, no AudioManager, no sounds.

### Minimum Viable Audio (rank by impact)

| Sound | Type | Priority |
|---|---|---|
| Footsteps (player) | 2D, surface-dependent | **Critical** — world feels dead without them |
| Footsteps (guards) | 3D, spatial blend | **Critical** — gives positional awareness |
| Camera disable SFX | 2D | High — feedback for successful interaction |
| Terminal access/hack SFX | 2D | High |
| Keycard insert/deny SFX | 3D | High |
| Door open/close/lock SFX | 3D | High |
| Alarm escalation SFX | 2D, global | High |
| Ambient / HVAC drone | 2D, looping | Medium |
| Mission complete fanfare | 2D | Medium |
| UI click / hover | 2D | Low |

### Architecture
```csharp
public class AudioManager : MonoBehaviour {
    public static AudioManager Instance;
    public AudioSource sfx2DSource;
    public AudioSource musicSource;
    public AudioClip[] clips; // Dictionary<string, AudioClip>
    public void Play(string name, Vector3? pos = null) { ... }
}
```
Add `[Range(0,1)] public float masterVolume, sfxVolume, musicVolume` for settings menu.

### Implementation: 2–3 hours. Use free assets from freesound.org (CC0 licenses, credit in build).

---

## 6. Lighting Plan

### Current State: Scene created by `MuseumLevelGenerator` with no lights — just default ambient.

### Minimum Viable Lighting
- **1 Directional Light**: simulating dim museum skylights (cool white, 0.3 intensity, angled).
- **Point lights** in each room: dim warm (2500K) at guard patrol paths, slightly brighter near artifacts/terminals.
- **Spotlights** over cameras (red/warm) to indicate detection zone visually.
- **Emission** on terminal screens and keycard readers.

### Implementation
- Cannot modify `MuseumLevelGenerator` per constraint. Instead, create a `LightingSetup` prefab with all lights, placed in scene after generation.
- Alternatively, add `[ExecuteAlways] LightingInstaller` script that finds rooms and spawns lights by tag.

### Visual Quality (Quick Win)
- Enable **Post-processing** volume: add LiftGammaGrad + Bloom + Vignette.
- Bloom on camera spotlights and terminal screens.
- Vignette increases with alarm level (via SecurityManager event).

---

## 7. Level Design Improvements

### Current Museum (10 rooms, linear z-axis)
Layout: Entrance → Hall → Gallery1 → Gallery2 → Vault → TerminalRoom → ServerRoom → Archive → Artifact → Exit

### Issues
- **Linear corridor** — no routing choices. Player always goes A→B.
- **No verticality** — all one flat plane.
- **Guard patrols** are simple back-and-forth on a single axis.
- **No alternate paths** — vents, crawlspaces, maintenance tunnels.
- **Camera placement** is evenly spaced with no clever sight-line puzzles.

### Minimal Changes (high impact, low effort)
1. **Create a loop**: Connect Gallery2 → ServerRoom via a side corridor so player can choose "go through vault (guarded, direct)" or "go through server room (longer, more terminals)."
2. **Add one maintenance tunnel**: A dark alternate path between Hall and Archive — requires finding a flashlight or night-vision item.
3. **Vary guard patrols**: Make one guard patrol a 3-way path instead of 2-way. Add idle pause at each waypoint.
4. **Camera sight-line puzzle**: Place one camera so its cone crosses a keycard door — player must time the door access between sweeps.

### Tutorial Level
The original tutorial (6 rooms) is well-paced for onboarding. Retain as "Training" mode accessible from main menu.

---

## 8. AI Balancing

### Current Guard Config
- Detection Range: 10 units
- FOV: 90 degrees
- Alert → Investigation → Searching → Engaged (attacks)
- Searching: circles last known position, 5s duration

### Balancing Pass

| Parameter | Current | Suggested | Reason |
|---|---|---|---|
| Detection range | 10 | 8 | 10 is forgiving; 8 forces closer play |
| FOV | 90° | 75° | 90 is very wide; tighter FOV rewards flanking |
| Search duration | 5s | 8s | 5s too short — player can wait it out trivially |
| Suspicious → Alert time | ? | 3s | Gives player window to break LOS |
| Move speed (patrol) | ? | 1.5 m/s | Slow patrol = predictable |
| Move speed (chase) | ? | 4.0 m/s | Faster than player (3.5) — danger |

### Camera Balancing
- Current detection time to alarm: ~2s. Keep.
- Add "blind spot" indicator: subtle cone mesh visible in stealth mode.
- Add camera rotation oscillation: some cameras pan 45° left/right (configurable direction via angles).

### Overall Difficulty Curve
- Tutorial: 1 guard, 1 camera, no terminals.
- Museum phase 1 (Rooms 1–4): 1 guard, 1 camera.
- Museum phase 2 (Rooms 5–7): 2 guards, 2 cameras, 1 terminal.
- Museum phase 3 (Rooms 8–10): 3 guards (one with extended range), 3 cameras, 2 terminals.

Currently all entities are placed at generator time. Add `difficultyLevel` parameter to `MuseumLevelGenerator.Generate(int difficulty)`.

---

## 9. Polish Checklist

- [ ] **Screen transitions** — fade to black between mission phases (EscapePhaseController start/end)
- [ ] **Controller support** — basic gamepad input (left stick move, right stick look, A interact)
- [ ] **Mouse sensitivity slider** — stored in PlayerPrefs
- [ ] **Key rebinding** — stretch goal; at minimum show keys in menu
- [ ] **Loading screen** — while MuseumLevelGenerator runs (can take 1–2s)
- [ ] **Main menu** — New Game, Continue, Tutorial, Settings, Quit
- [ ] **Credits screen** — list all assets, sounds (CC0 credits), team
- [ ] **Tooltips** — hover over keycard/terminal in-world shows name
- [ ] **Screen shake** — on alarm level up, on door lockdown
- [ ] **Hitmarker / detection indicator** — red flash on screen edges when guard spots player
- [ ] **Post-processing preset toggle** — on/off in settings (performance option)

---

## 10. Bug Checklist

Found and verified:

- [x] **D1**: `MuseumLevelGenerator` uses `BindingFlags.NonPublic` to set private fields — reflects fragility; add public setter API
- [ ] **D2**: `SecurityManager.RegisterCamera()` uses `TryAdd` but then immediately `_cameras[dto.ID] = dto` — redundant; `TryAdd` already sets; also no check if camera already registered (no warning)
- [ ] **D3**: `EventManager` + `EventManager` references — likely naming collision from drag; `EventManager.OnAlarmLevelChanged` used but class may conflict
- [ ] **D4**: `DemoModeController` spawns GUIs via `new Vector3(Random.value * ...)` — random positions may overlap
- [ ] **D5**: No `OnDisable()` unsubscription in `MissionManager`, `GuardFSM`, `SecurityCamera` — potential memory leak / missing null checks
- [ ] **D6**: `SecurityCamera` hardcoded `maxDetectionTime = 2f` — not exposed to CameraConfig
- [ ] **D7**: `KeycardItem` has no validity check on destruction path — calling `Use()` on destroyed keycard
- [ ] **D8**: `TerminalController` requires exact `terminalID` match for `ActionExecutor.Execute` — case-sensitive, no error feedback

---

## 11. Professor Presentation Checklist

### What to Emphasize
- **Architecture documentation**: System Interaction Diagram (show how MissionManager orchestrates SecurityManager → DoorController → GuardFSM via events). Print as poster or include in slides.
- **FSM complexity**: 5 separate finite state machines (Guard, Door, Mission, Camera, Player). Show the Door FSM diagram (7 states) as a standout.
- **Security system layering**: Authentication → Authorization → RBAC → 8 separate actions. This demonstrates understanding of real-world cybersecurity concepts.
- **ScriptableObject configuration**: DoorConfig, CameraConfig, TerminalConfig, RolePermissionsConfig — demonstrate data-driven architecture.
- **Design patterns**: Singleton, Observer (events), Command (ActionExecutor), State (FSMs), Strategy (different guard states).

### Presentation Tips
1. **Live demo**: Start with Tutorial scene (guaranteed to work). Then Museum scene. Have backup of both scenes pre-loaded.
2. **Show the code**: Open `GuardFSM.cs` and `DoorController.cs` side-by-side to show FSM pattern reuse.
3. **Show the inspector**: Click a DoorController GameObject to show DoorConfig slot — demonstrate config-driven design.
4. **Fail gracefully**: If a demo bug occurs, explain what *should* happen and why it's architecturally sound.
5. **Slides should include**: System architecture diagram, FSM state diagrams, Security system flow, Screenshots of in-game.

### Potential Demo Failures to Mitigate
- Scene generation creates objects with no lights = dark scene. Pre-bake lighting or add lights to scene before demo.
- `SecurityCamera` has `target` reference from generator that may be stale — test each camera before demo.
- `DemoModeController` adds debug GUI objects that have no cleanup — hide or disable in final build.

---

## 12. Final Submission Checklist

- [ ] Build runs without errors
- [ ] No `Debug.Log()` spam in build (comment out dev logs)
- [ ] All ScriptableObject configs are assigned in `Resources/` or in scene
- [ ] Scene loads in < 5 seconds
- [ ] Resolution and fullscreen settings work
- [ ] No missing script references in scene (check all GameObjects)
- [ ] All public text is final (no placeholder strings)
- [ ] Build size reasonable (< 500 MB with assets)
- [ ] `StreamingAssets` / `Resources` folder has only required files
- [ ] `PlayerPrefs` used for settings (sensitivity, volume) — not required but nice
- [ ] Quit button works (Application.Quit())
- [ ] Alt+F4 / window close doesn't crash
- [ ] Executable name is professional (not "UnityBuild" or "My Game")
- [ ] Submission ZIP contains: build + README + source code + 2-page architecture document

---

## 13. Prioritized Roadmap

### Phase A — Critical Bugs & GC Fixes (1–2 days)
1. Fix `OnGUI` → TextMeshPro in MissionHUD and DebugOverlay (blocking GC issue)
2. Cache `GetComponent` calls in GuardFSM, SecurityCamera (perf)
3. Remove `Debug.Log()` from Update loops
4. Add game-over on alarm level 3
5. Fix `MuseumLevelGenerator` reflection → public API
6. Fix `SecurityManager.RegisterCamera()` TryAdd redundancy

### Phase B — Audio & Lighting (2–3 days)
7. Implement `AudioManager` with event-driven play calls
8. Add footsteps to PlayerController and GuardFSM
9. Add terminal, door, camera, alarm SFX
10. Add directional + point lights to museum scene
11. Add post-processing volume with bloom + vignette

### Phase C — UX & UI (2–3 days)
12. Convert HUD to TextMeshPro Canvas
13. Add pause menu with settings (sensitivity, volume sliders)
14. Add mission complete/fail screen with stats
15. Add main menu scene (New Game, Tutorial, Settings, Quit)
16. Add loading screen while generator runs

### Phase D — AI & Balancing (1–2 days)
17. Tune guard detection range, FOV, search duration
18. Add camera pan oscillation
19. Adjust difficulty curve across museum phases
20. Add one alternate path / loop in level layout

### Phase E — Final Polish (2–3 days)
21. Screen transitions (fade in/out)
22. Screen shake on alarm events
23. Tooltips on interactables
24. Controller support
25. Credits screen
26. Build testing across resolutions
27. Bug bash — walk through entire game 3 times, log every issue
28. Create architecture document for professor submission
29. Final build + submission ZIP

**Total estimate**: ~10–14 days for a solo developer working evenings/weekends.

---

## Appendix: Metrics Snapshot

| Metric | Value |
|---|---|
| Total C# scripts | 50 (runtime) + 4 (editor) |
| Total lines of code (approx) | ~6,200 |
| State machines | 5 (Guard, Door, Mission, Camera, Player) |
| FSM states combined | ~28 |
| ScriptableObject types | 5 (DoorConfig, CameraConfig, TerminalConfig, RolePermissionsConfig, PermissionSet) |
| IInteractable implementations | 6 (KeycardItem, TerminalController, SecurityCamera, NotePickup, CheckpointTrigger, ArtifactController) |
| Design patterns used | Singleton (11×), Observer, Command, State, Strategy, ObjectProvider |
| Scenes | 2 (Tutorial, MuseumHeist) + planned MainMenu |

---

## Appendix B: Phase 5A — Post-Review Gameplay Features

### Elite Guard
- `GuardFSM.isElite` bool overrides patrol speed (2.5+), vision range (14m), FOV (80°), suspicion time (1.2s), chase speed (6.5), hearing radius (18m).
- One elite guard (dark red) placed in vault antechamber with 5-waypoint patrol.
- File: `Assets/Scripts/GuardFSM.cs:23`, placement in `MuseumGameplayPlacer.MakeEliteGuard()`.

### Checkpoints
- `CheckpointTrigger` MonoBehaviour saves progress on trigger enter.
- `CheckpointManager` extended with `RegisterCheckpoint()` / `ForceCheckpoint()`.
- 4 zones: Lobby, Security Office, Vault Corridor entrance, Escape.
- Files: `Assets/Scripts/CheckpointTrigger.cs` (new), `Assets/Scripts/CheckpointManager.cs`.

### Camera Network Disable
- `SecurityManager.DisableCameraGroup(string[] ids)` batch-disables cameras.
- `DisableCameraGroupAction` parses semicolon-delimited IDs from `TargetID`.
- Registered via `ActionExecutor.RegisterAction()` in level startup.
- Security terminal action disables `camera_east`, `camera_west`, `camera_lobby` in one click.
- Files: `Assets/Scripts/SecurityManager.cs`, `Assets/Scripts/Cyber/Actions/TerminalActions.cs`.

### Mission Scoring
- `MissionScorer` singleton tracks: camera detections, guard encounters, alarms, time, secondary objectives.
- Rating: S (≥95), A (≥75), B (≥50), C (≥25), D.
- Deductions: −15/camera, −20/guard, −25/alarm, −2/10s overtime.
- File: `Assets/Scripts/MissionScorer.cs` (new).

### HUD Enhancements
- **Stealth Indicator**: bottom-right eye icon (green/yellow/red) scanning guards+cameras within 12m.
- **Detection Meter**: center-screen progress bar with source label when being detected.
- File: `Assets/Scripts/UI/MissionHUD.cs`.

### Results Screen
- OnGUI panel triggered after `OnMissionCompleted` (1.5s delay).
- Shows rating letter, color, descriptive label, stat breakdown.
- Dismiss with Space.
- File: `Assets/Scripts/UI/ResultsScreen.cs` (new).

### Bootstrap Wiring
- Both `MissionScorer` and `ResultsScreen` added to `GameBootstrapper.EnsureManagers()`.
- Scoring auto-starts on first objective via `OnObjectiveStarted` event.
