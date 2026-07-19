# MUSEUM HEIST — PRE-ALPHA TECHNICAL DESIGN REVIEW

**Review Board**: Gameplay Design (Arkane), AI (IO Interactive), Tech Dir, Software Arch, UX, Level Design, Performance, Audio, Tech Art, External Examiner

**Project**: Unity 6 Stealth Game — ~51 C# scripts, feature complete

**Date**: Pre-QA Final Review

---

## SECTION 1 — EXECUTIVE SUMMARY

### Scores

| Category | Score (1–10) | Notes |
|---|---|---|
| **Architecture** | 6 | Solid foundation undermined by singleton sprawl, OnGUI reliance, and reflection hacks |
| **Gameplay** | 7 | Core loop works. Terminal hacking is genuinely engaging. Lacks fail states, distraction tools, and movement noise — feels half-implemented |
| **Code Quality** | 5 | Inconsistent. Some scripts are clean (DoorController, AuthorizationService). Others are production-dangerous (DebugOverlay, DemoModeController, GuardFSM Update path) |
| **Presentation** | 4 | No lighting, no audio, no post-processing, no main menu, no pause screen. Cannot be shown to an examiner in current state without significant embarrassment |
| **Innovation** | 7 | Layered terminal security (Auth → AuthZ → RBAC) is novel for a student project. The escape lockdown phase is well-conceived |
| **Cybersecurity** | 8 | Genuine standout. The RBAC permission set, role-based command restriction, and multi-step authentication pipeline demonstrate real understanding |
| **Technical Difficulty** | 7 | 5 FSMs, Command Pattern, event-driven SecurityManager, dynamic level generation, ScriptableObject-driven config — above average for final year |
| **Maintainability** | 5 | Singleton dependencies make unit testing impossible. No namespace separation. Folder structure is flat. New features require touching 3–5 files |
| **Replayability** | 3 | Single linear level, no alternate routes, no scoring, no time attack, no difficulty modes |
| **Commercial Potential** | 2 | Core is there but needs 6 months of content, audio, lighting, UI, and bug fixing to be shippable as even an indie title |

### Recommendation

**PASS WITH CHANGES** — The project demonstrates strong technical depth in cybersecurity mechanics and FSM architecture. It is NOT submission-ready. Audio, lighting, UI, and critical bug fixes are required before it can be presented to an examiner.

---

## SECTION 2 — SOFTWARE ARCHITECTURE REVIEW

### SOLID

| Principle | Verdict | Evidence |
|---|---|---|
| **S**ingle Responsibility | ⚠️ Mostly | MissionManager handles objectives, phases, checkpoints, artifact, and alarm listening — 5 responsibilities. DemoModeController is a god class |
| **O**pen/Closed | ⚠️ Partial | FSMs use switch statements — adding a state requires modifying existing code. ActionExecutor is properly extensible via ICommand |
| **L**iskov Substitution | ✅ Good | IInteractable implementations are interchangeable. Guard states are enum-based so LSP is N/A |
| **I**nterface Segregation | ✅ Good | IInteractable is minimal (Interact()). ICommand is minimal (Execute()). No fat interfaces |
| **D**ependency Inversion | ❌ Poor | High-level MissionManager depends directly on SecurityManager, PlayerController, ArtifactController, CheckpointManager — all concrete singletons. No abstractions |

### DRY
**Score: 5/10** — GuardFSM and DoorController both implement FSM via enum+switch. The pattern is duplicated without an abstract `FSMStateMachine<T>` base class. The OnGUI pattern is duplicated in 2 files. `Debug.Log("...")` format strings are repeated.

### Coupling & Cohesion
- **Coupling**: HIGH. 9 singletons create implicit coupling across the entire project. `DemoModeController` references 6 different singletons directly.
- **Cohesion**: MEDIUM. `MissionManager` is a grab-bag of unrelated concerns (objectives, checkpoints, artifact state, alarm handling). `SecurityManager` is well-cohesive (all security registries and alarm logic).

### Dependency Management
- No dependency injection, no Service Locator (beyond singletons), no Zenject/any DI framework.
- Circular dependency risk: `SecurityManager` → via events → `MissionManager` → via Instance → `SecurityManager`. Works at runtime but fragile.
- **Risk**: HIGH. Any singleton initialization order bug will cause silent null refs. Currently works by accident of scene load order.

### Event Architecture
- **Inconsistent**: UnityEvents on DoorConfig, C# events on SecurityManager, no events on GuardFSM or TerminalController.
- **No Event Bus**: Systems that need cross-talk (e.g., alarm level affects guard behavior) must manually wire via Instance references.
- **Unsubscription gap**: MissionManager subscribes to SecurityManager events but only cleans up in OnDestroy (not OnDisable). Scene reload will create duplicate subscriptions.
- **Verdict**: Works for prototype. Will leak in scene reload scenarios.

### FSM Quality
- **GuardFSM**: 6 states, all in one Update() switch. State transitions are clear but state-specific behavior (e.g., investigate vs search) is crammed into case blocks — violates Single Responsibility.
- **DoorController**: 7 states, same pattern. Better organized with helper methods per state.
- **Missing**: No `FSMState` base class, no `OnEnter`/`OnExit` callbacks, no state machine debugger.
- **Risk**: Adding a new state requires careful editing of existing switch — easy to break.

### Command Pattern
- `ICommand` / `ActionExecutor` is clean. Each terminal action is a separate class.
- **Missed opportunity**: Undo/redo not implemented. Command pattern is perfect for "revert terminal state" but not used.
- **Edge case**: `ActionExecutor.Execute()` matches by string ID — no fallback for unknown commands.

### ScriptableObject Usage
- DoorConfig, CameraConfig, TerminalConfig, RolePermissionsConfig, PermissionSet — appropriate use of data-driven design.
- **Missing**: No ScriptableObject-based event channels (GameEventSO pattern from Unity). Would fix the event architecture issues.
- **Warning**: `MuseumLevelGenerator` accesses DoorConfig properties via reflection (`GetFields(BindingFlags.NonPublic)`) — defeats encapsulation.

### Singleton Usage
- 9+ singletons: MissionManager, SecurityManager, PlayerController, InteractionManager, InventoryManager, CheckpointManager, HUDController (?), DemoModeController, AudioManager(?).
- Some justified (MissionManager), many are lazy choices (PlayerController, InventoryManager).
- **Severity**: MAJOR. Prevents unit testing, creates hidden coupling, violates Dependency Inversion.

### Interfaces
- `IInteractable`, `ICommand` — well-designed.
- **Missing**: No `IAlarmListener`, `IDoorUser`, `IGuardController`, `ITerminalHandler`. Would decouple high-level systems.

### Namespaces & Folder Structure
- **No namespaces** — all scripts exist in global namespace. Name collisions guaranteed as project grows.
- **Folder structure**: Scripts/, Scripts/AccessControl/, Scripts/Core/, Scripts/Cyber/, Scripts/UI/, Editor/. Adequate but flat.
- **Recommendation**: Add `MuseumHeist.Core`, `MuseumHeist.AccessControl`, `MuseumHeist.Cyber`, `MuseumHeist.UI`, `MuseumHeist.Editor` namespaces.

### Scalability
- **Poor**. Adding a new room type requires modifying MuseumLevelGenerator. Adding a new guard ability requires modifying GuardFSM switch. Adding a new objective type requires modifying MissionManager. The architecture is **brittle** at scale.

### Maintainability Risk Assessment

| Risk | Severity | Mitigation |
|---|---|---|
| Singleton init order | Critical | Add null-guard checks in every Instance accessor |
| OnGUI GC pressure | Critical | Replace with Canvas/TMP before submission |
| Reflection in generator | Major | Add public setter API |
| No event unsubscription | Major | Audit all OnDisable/OnDestroy for += cleanup |
| Switch-statement FSMs | Minor | Acceptable for this scope |

---

## SECTION 3 — GAMEPLAY REVIEW

### Mission Pacing
- **Issue**: Linear. Infiltration → Navigation → Acquisition → Escape is a straight line with no optional objectives or side content.
- **Result**: Feels like a tutorial level, not a full game.
- **Fix**: Add 1 optional side objective (e.g., "Download employee records from a secondary terminal" — rewards intel/lore).

### Stealth
- **Issue**: No movement noise system. Player can sprint directly behind a guard without detection. This breaks immersion.
- **Issue**: No crouch state. Player hitbox is always standing.
- **Issue**: No cover system. Line-of-sight is binary — behind wall = hidden, not behind wall = visible. No lean, no peek.
- **Fix**: Add noise radius to PlayerController (walk=quiet, run=noisy). Add crouch toggle with speed penalty and height reduction. These are script-level changes, not new systems.

### Guard Behavior
- **Issue**: Guards are predictable. Back-and-forth patrol with no variance.
- **Issue**: No coordination. Guards don't react to other guards entering Alert state — breaks believability.
- **Fix**: Add `GuardFSM.OnOtherGuardAlerted(GuardFSM source)` via SecurityManager event broadcast. Searching guards should call for backup.

### Camera Placement — Underused
- Camera detection works but there's no puzzle where cameras create an interesting choice (wait for sweep vs. take a risk vs. disable).
- **Fix**: Place one camera that sweeps across a door keycard reader — player must time their swipe.

### Door Progression — Excellent
- The keycard tier system (Blue → Green → Red → Gold) is well-paced. Each locked door feels like a milestone.
- **Critique**: Keycards have infinite uses. No tension on the 10th door with the same blue keycard. Add `maxUses` (1–3) to KeycardItem.

### Terminal Gameplay — Excellent
- Multi-step (Auth → AuthZ → RBAC bypass) is genuinely satisfying.
- **Issue**: Commands are case-sensitive with no fuzzy matching. Player types "help" → works. "Help" → fails. This will cause frustration in playtesting.
- **Fix**: `SecurityConsoleUI.ProcessCommand()` should use `string.StartsWith(command, StringComparison.OrdinalIgnoreCase)` or a numbered menu.

### Credential Progression
- **Issue**: Authentication codes are found on notes/pickups. If the player misses one, the terminal is permanently unusable with no hint where to look.
- **Fix**: Add fallback: if player fails auth, terminal hints at the room name where the code is located.

### Escape Sequence — Excellent Concept, Underdeveloped
- Lockdown mechanic is strong. But there's no timer pressure (player can sit in the vault forever).
- **Fix**: Add oxygen timer for vault room (60s). Add ticking alarm clock audio. Add escalating music.

### Difficulty Curve
- **Issue**: Flat. The museum level is the same difficulty from room 1 to room 10. No escalation.
- **Fix**: Add more guards and cameras in later rooms. Add a "lieutenant" guard with extended detection range in the artifact room.

### Player Agency
- **Issue**: Zero. One path, one solution, no choices with consequences.
- **Fix**: Add at least one branching moment — "Do you hack the vault door (loud, risky, fast) or find the security guard's keycard (quiet, slow, requires backtrack)?"

### Replayability
- **Issue**: None. Same level, same solution, every time.
- **Fix**: Randomize guard patrol paths (MuseumLevelGenerator assigns waypoints procedurally). Randomize camera sweep directions. Score player on speed, alarms triggered, stealth percentage.

### Risk vs. Reward
- **Issue**: No trade-offs. Hacking a terminal has no risk besides time. Camera disable has no risk. Only interaction with consequence is being seen by a guard.
- **Fix**: Hacking a terminal should have a small chance of triggering an alarm increase (1 in 6). Camera disable should make a noticeable sound that attracts nearby guards.

---

## SECTION 4 — LEVEL DESIGN REVIEW

### Current Layout
```
Entrance → Hall → Gallery1 → Gallery2 → Vault → TerminalRoom → ServerRoom → Archive → Artifact → Exit
```
10 rooms along a single Z-axis line.

### Navigation — POOR
- The level is a corridor. Players cannot get lost, but they also cannot make meaningful navigation decisions.
- **Recommendation**: Connect Hall → ServerRoom via a maintenance tunnel (dark, requires flashlight). Creates a short loop and a meaningful choice: "safe path through guarded galleries" vs. "dangerous path through dark tunnel."

### Sightlines — POOR
- Long narrow corridors mean guards and cameras have clear sightlines for 30+ meters. This makes stealth trivial (see them from far away) AND frustrating (they see you from too far).
- **Recommendation**: Break sightlines with pillars, half-walls, display cases, and columns. Place a large central sculpture in each gallery that provides cover.

### Cover — INADEQUATE
- Rooms are open boxes. No crouch-cover, no behind-desk hiding, no vent entrances.
- **Recommendation**: Add 2–3 cover objects per room (desks, display cases, crates, plants). This enables the peek/lean system.

### Lighting — NON-EXISTENT (see Section 7)
- No lights placed. Scene runs on default ambient. This is a critical presentation blocker.

### Player Flow
- Linear but functional. Player never needs to backtrack significantly (doors open one-way mostly).
- **Issue**: If a player misses a keycard in room 3 and reaches room 7, the backtrack is 4 rooms of empty (already cleared) space. Boring.

### Backtracking — MINIMAL
- The escape phase should force backtracking through altered spaces (locked doors, increased security). Currently it's just "walk back the way you came."
- **Recommendation**: During escape, change guard patrol routes (more aggressive), add new obstacles, lock shortcut doors that were previously open.

### Environmental Storytelling — NONE
- No notes, no emails, no lore pickups, no set dressing that tells a story.
- **Recommendation**: Add 3–4 readable documents: "Museum Director's Email about security upgrade," "Guard shift schedule," "Artifact acquisition ledger." These are simple string ScriptableObjects placed in the world.

### Room Purpose — ADEQUATE
- Each room has a clear function. Vault, Terminal Room, Server Room, Artifact Chamber — all make sense.
- **Critique**: Gallery1 and Gallery2 are filler. They should have a reason to exist (a camera control panel, a guard break room with keycard on table, a CCTV monitor).

### Alternative Routes — ZERO
- No vents, no crawlspaces, no maintenance corridors, no rooftop paths.
- **Recommendation**: Add one vent entrance in Gallery2 that leads behind a locked door in ServerRoom. Simple box collider trigger that teleports the player. Two hours of work.

### Security Placement — ADEQUATE
- Cameras at room entrances is sensible. Guards patrol gallery spaces, not storage rooms — logical.
- **Critique**: No camera overlaps, no chokepoints covered by multiple security systems, no alarm-triggered lockdown doors mid-level.

---

## SECTION 5 — AI REVIEW

### Guard FSM — FUNCTIONAL BUT SHALLOW

**States**: Idle → Suspicious → Alert → Investigating → Searching → Engaged

| State | Critique |
|---|---|
| Idle | No variation. All guards have the same patrol speed, same waypoint wait time, same idle duration. Add patrol variance (some guards linger at certain waypoints) |
| Suspicious | No feedback to player. Screen should subtly pulse or indicator should appear. Player has no way to know a guard is suspicious unless they see the model's head turn |
| Alert | Transitions correctly. But the alert doesn't propagate to other guards or SecurityManager's alarm level |
| Investigating | Guard walks to last known position. But if player is in shadow vs. light, make it harder/easier. Currently binary |
| Searching | Circular search is adequate. But duration is too short (5s). Player waits it out trivially |
| Engaged | No combat system exists — this state is a dead end. Guard just... exists? Needs to either call for backup (alarm +2), or chase player (speed boost), or trigger mission failure after a timer |

### Camera AI — BASIC
- Detection timer is hardcoded at 2s (SecurityCamera.cs). Not exposed to CameraConfig.
- No camera sweep/ping — all cameras face one direction.
- **Fix**: Expose `detectionTime`, `sweepAngle`, `sweepSpeed` to CameraConfig. Add `bool oscillateRotation` to inspector.

### Alarm Propagation — INCOMPLETE
- SecurityManager has alarm levels. Guards don't react to alarm level changes. A guard at alarm level 3 patrols the same as alarm level 0.
- **Fix**: `GuardFSM.OnAlarmLevelChanged(int level)` → increase patrol speed, widen FOV, reduce detection time, call for reinforcements.

### Search Behavior — NEEDS WORK
- Currently circles last known position. Does not search adjacent rooms, does not look up/down, does not call nearby guards.
- Player can hide 2 meters from the search center and wait 5 seconds. Done.
- **Fix**: Make search multi-phase: 1) Brief look around (2s), 2) Move to last known (3s), 3) Circle (5s), 4) Check cover positions within radius (5s), 5) Return to patrol.

### Suspicion — TOO GENEROUS
- Suspicion triggers when guard "senses" something. The trigger radius and conditions are unclear.
- **Fix**: Add explicit suspicion triggers: sound within 10m (player running), brief visual of player (<0.5s), door opened while guard is nearby.

### Detection Fairness — GENERALLY GOOD
- FOV and range checks are correct. Detection is not instant — the 2s timer is fair.
- **Critique**: No difficulty options. A single guard setup serves all players.

### Pathing — ADEQUATE
- Uses NavMeshAgent. Works.
- **Issue**: If a guard is in Searching state and player leaves the NavMesh (vent/crawlspace), the guard abandons search immediately. Guard should check the entrance of the off-NavMesh area.

### Reaction Timing
- Suspicious → Alert takes some time. Not exposed for tuning.
- **Fix**: Add inspector fields for `timeToAlert`, `timeToSearch`, `timeToReturnToPatrol`.

### Investigation — BASIC
- Walks to last seen position. Does not call out, does not radio.
- **Fix**: Add `AudioSource.PlayOneShot("Hey, who's there?")` on investigation start. Add `GuardRadio` component for voice line triggers.

### Return to Patrol
- Guard finishes search, walks to nearest waypoint, resumes patrol.
- **Issue**: Guard returns to the START of patrol, not the nearest waypoint. Creates a predictable gap.

### Edge Cases
- **Missing**: What happens if guard is Searching and alarm level increases? Currently continues searching. Should escalate to Engaged.
- **Missing**: What happens if two guards Investigate the same location? They should acknowledge each other.
- **Missing**: What if player is detected during Lockdown (Escape phase)? Should be instant fail or extreme pressure.
- **Severity**: MODERATE. These won't crash the game but will produce illogical behavior in QA.

---

## SECTION 6 — USER EXPERIENCE REVIEW

### HUD — CRITICAL ISSUE
- `MissionHUD` and `DebugOverlay` use `MonoBehaviour.OnGUI()`.
- **Problem**: OnGUI is deprecated. It allocates GC every frame. It does not support resolution scaling, text styling beyond basic GUIStyle, or canvas-based layout.
- **Fix**: Replace with `TextMeshProUGUI` on a Canvas. This is the single highest-impact UI change.

### Objective Clarity — ADEQUATE
- Objectives display as text via MissionHUD. Clear wording.
- **Critique**: No completion animation. Objectives just disappear. Add strikethrough + fade for completed objectives.
- **Critique**: No way to see all objectives at once (current + future). Add expandable objective list on Tab key.

### Terminal UI — NEEDS IMPROVEMENT
- Current: scrollable text area with text input.
- Problems:
  - No command history (up arrow to recall).
  - No tab completion.
  - No visual distinction between command output and command input.
  - Case sensitive input.
  - No scroll-to-bottom on new output.
  - No typing animation (green text character-by-character for cyberpunk feel).
- **Severity**: MODERATE. Works but feels lifeless.

### Debug UI — SHOULD BE REMOVED OR HIDDEN
- `DebugOverlay` shows FPS, mission phase, guard states. Useful for development.
- **Fix**: Wrap in `#if UNITY_EDITOR` or `Debug.isDebugBuild` so it is not present in submission build.

### Accessibility — NONE
- No colorblind mode. Alarm levels use green/yellow/red — problematic for deuteranopia.
- No subtitle support (no audio yet, so moot).
- No text size options.
- **Fix**: Use symbols alongside colors for alarm level (shield = safe, exclamation = alert, skull = critical).

### Menus — MISSING
- **No main menu**. Scene starts directly in game.
- **No pause menu**. Escape key does nothing.
- **No settings**. Cannot change resolution, quality, volume, sensitivity.
- **Severity**: CRITICAL. An examiner will notice the lack of a main menu immediately.

### Pause — MISSING
- No pause functionality. Player cannot stop the game.
- **Fix**: Add `PauseManager` singleton (yes, another one, but justified). `Escape` toggles pause. `Time.timeScale = 0`. Show panel with Resume / Settings / Quit.

### Settings — MISSING
- No settings menu. Not even resolution options.
- **Fix**: Add Settings panel with: Resolution dropdown, Fullscreen toggle, Master Volume slider, Mouse Sensitivity slider, Quality preset dropdown.
- Effort: 3–4 hours using Unity's built-in `Screen.resolutions` and `QualitySettings`.

### Feedback
- Interaction prompt ("Press E to interact") appears on raycast hit. Good.
- **Missing**: No controller rumble support.
- **Missing**: No screen flash on damage or detection.
- **Missing**: No hit marker or visual cue when hacking succeeds/fails beyond text.

### Readability
- OnGUI text at default size is small. On 4K displays it will be unreadable.
- **Fix**: TextMeshPro supports auto-size and responsive layout.

### Animation — MINIMAL
- No player animations (no arms, no weapon, no hands interacting).
- No door opening animation — door instantly changes state.
- No guard animations beyond default Unity.
- **Severity**: MODERATE. Acceptable for code-focused project, but an examiner will note the lack of animation.

### Game Feel
- Responsive controls are good.
- **Missing**: Screen shake. Camera bob. Footstep effects. Muzzle flash (no weapons). Speed lines. Drunk effect on alarm level 3.
- **Verdict**: Plays like a prototype. Functional but joyless.

---

## SECTION 7 — VISUAL REVIEW

### Lighting — CRITICAL ISSUE
- **No lights exist in the scene.** The level generator does not create lights. The scene uses only default ambient light.
- **Result**: The game looks like a greybox prototype. An examiner will not take it seriously.
- **Fix**: Add 1 Directional Light (cool white, dim — museum skylight). Add Point Lights in each room (warm, low intensity). Add Spotlights on cameras (colored beam). Simple setup, 1 hour.

### Materials — BASIC
- Generated materials are flat-colored. No normal maps, no roughness variation, no emission.
- **Fix**: Use Unity's Standard shader with tint + metallic/smoothness variation. No new assets needed — just expose properties.

### Museum Atmosphere — NON-EXISTENT
- No dust particles, no god rays, no display case glass, no rope barriers, no informational plaques.
- **Severity**: MINOR for a code-focused project. But atmosphere is what makes a stealth game memorable.

### Emergency Lighting
- **Missing**: Red emergency lights should activate when alarm level reaches 2+. Affects gameplay (changes stealth visibility).
- **Fix**: Add `EmergencyLightingController` that listens to `SecurityManager.OnAlarmLevelChanged` and changes light color/intensity.

### Security Lighting
- **Missing**: Camera spotlights. A visual cone that shows the player where cameras are looking. Currently cameras have no visual indicator.
- **Fix**: Add a cone mesh (transparent blue/red) attached to each camera. Toggle on/off with camera state.

### Post Processing — MISSING
- No Post-Processing Volume or Profile.
- **Fix**: Add PP Volume with:
  - Bloom (on camera spotlights, terminal screens, keycard reader lights)
  - Vignette (intensifies with alarm level)
  - Lift/Gamma/Grading (warm up museum, cool down vault)
  - Chromatic Aberration (on terminal usage)

### Effects — MINIMAL
- No particle effects. No dust motes, no sparks, no smoke.
- No screen effects (scanlines on terminal, glitch on alarm).

### Professional Appearance — POOR
- In current state, this looks like a Week 2 prototype. The lack of lighting, lack of main menu, lack of audio, and OnGUI text make it impossible to present professionally.

---

## SECTION 8 — AUDIO REVIEW

### Current State: SILENT. No AudioManager, no AudioSources, no AudioClips.

### Critical Recommendations

| Sound | Priority | Implementation | Est. Time |
|---|---|---|---|
| Footsteps (player) | **P0** | PlayerController.OnMove → AudioManager.Play("footstep_01", 1.0, pitch variance) | 1h |
| Footsteps (guards) | **P0** | GuardFSM patrol state → AudioManager.Play3D("guard_footstep", transform.position) | 1h |
| Cameras (motor hum) | **P1** | SecurityCamera idle → loop: `hum.wav`, 0.2 volume, spatial blend 1.0 | 30min |
| Cameras (detect SFX) | **P1** | Detection timer reaching 50% → rising pitch beep | 30min |
| Terminal keypress | **P1** | SecurityConsoleUI.OnInputFieldChanged → random click from array | 30min |
| Terminal auth grant/deny | **P1** | ActionExecutor success/failure → one-shot SFX | 15min |
| Door motors | **P1** | DoorController state EnterOpening → loop `door_motor.wav` | 30min |
| Door locked buzz | **P1** | On failed keycard attempt → `buzz.wav` | 15min |
| Vault door heavy | **P1** | Artifact room door → distinct `vault_heavy.wav` | 15min |
| Alarm escalation | **P0** | SecurityManager.OnAlarmLevelChanged → play alarm tier | 30min |
| Ambient drone | **P1** | AudioManager background loop: `museum_ambient.wav`, 0.15 volume | 15min |
| Escape music | **P2** | Dynamic music track that intensifies as player approaches exit | 2h |
| UI hover/click | **P2** | OnGUI/Canvas button events → UI clicks | 30min |

### Architecture

```csharp
// Minimal AudioManager — add to existing project, 150 lines
public class AudioManager : MonoBehaviour {
    public static AudioManager Instance;
    [SerializeField] AudioSource musicSource, ambienceSource;
    [SerializeField] AudioSourcePool sfxPool; // pool of 8 sources
    [Range(0,1)] public float masterVolume, sfxVolume, musicVolume;

    public void PlaySFX(string clipName, float volume = 1f, float pitchVar = 0f) { ... }
    public void PlaySFX3D(string clipName, Vector3 pos, float volume = 1f) { ... }
    public void SetMusic(AudioClip clip, float fadeTime = 1f) { ... }
    public void SetAmbience(AudioClip clip) { ... }
}
```

### Mixing Priorities
1. Player footsteps (presence)
2. Guard footsteps (threat detection)
3. Alarm sounds (tension)
4. UI sounds (feedback)
5. Ambient (atmosphere)
6. Music (emotional drive)

### Asset Sourcing
- Use freesound.org CC0 assets.
- Credit all assets in a `CREDITS.txt` shipped with the build.

---

## SECTION 9 — PERFORMANCE REVIEW

### Critical Issues

| # | Issue | File | Impact | Fix |
|---|---|---|---|---|
| 1 | **OnGUI string allocation** | DebugOverlay.cs, MissionHUD.cs | **HIGH** — allocates strings every frame causing GC spikes and frame hitches | Replace with TextMeshProUGUI on Canvas |
| 2 | **new GUIStyle() per frame** | SecurityConsoleUI.cs (line ~40-60) | **HIGH** — 1+ KB allocated every frame | Cache GUIStyle or move to TMP |
| 3 | **GetComponent<Animator>() per frame** | GuardFSM.cs (switch, every state) | **MEDIUM** — FindComponent in Update is wasteful | Cache in Awake |
| 4 | **GetComponent<Camera>() per frame** | SecurityCamera.cs (Update loop) | **MEDIUM** — same issue | Cache in Awake |
| 5 | **Debug.Log() in Update** | SecurityCamera.cs:55 | **LOW** — console spam, not release-impact but hides real errors | Remove or wrap in #if UNITY_EDITOR |
| 6 | **Resources.LoadAll at runtime** | MissionManager.cs (Awake or Start) | **MEDIUM** — blocking synchronous load | Move to Awake (already likely there) or use direct references |
| 7 | **Reflection in level generator** | MuseumLevelGenerator.cs | **LOW** — one-time cost during generation | Acceptable for editor tool. Fix the architectural issue separately |

### Moderate Issues

| # | Issue | Details |
|---|---|---|
| 8 | **No object pooling** | Not critical — all objects are placed at design time. Would matter if projectiles or pickups are added |
| 9 | **List<T>.Find() in registries** | SecurityManager uses `_cameras.Find(c => c.ID == id)` — O(n). Max ~20 entities so negligible |
| 10 | **Full objective loop every frame** | MissionManager checks all objectives every Update. Add event-driven completion check instead |
| 11 | **Physics overlap** | No issues found. Guard uses NavMesh. Player uses CharacterController. Clean |

### Draw Calls & Memory
- Not measurable without a Profiler run. The scene is simple (10 rooms, basic geometry, few materials). Likely < 100 draw calls.
- **Risk**: If MuseumLevelGenerator creates unique materials per room, draw calls increase linearly. Use material instancing or shared materials.

### Optimization Priority
1. **P0**: Kill OnGUI allocation (fixes GC spikes entirely)
2. **P1**: Cache all GetComponent calls in Awake
3. **P1**: Remove Debug.Log from Update
4. **P2**: Replace reflection with public API in generator
5. **P2**: Add event-driven objective completion

### FPS Estimate
- Current: Likely 100+ FPS on modern hardware (simple scene, no post-processing, no shadows).
- After full lighting + audio + post-processing: 60 FPS on mid-range, 30 FPS on low-end.
- **Risk**: Bloom + real-time shadows on 10 point lights could drop to 30 on integrated GPUs. Add quality settings.

---

## SECTION 10 — BUG RISK REVIEW

### Null Reference Risks

| # | Risk | File | Scenario | Severity |
|---|---|---|---|---|
| B1 | `target == null` in guard chase | GuardFSM.cs | Guard Engaged state if player leaves scene/teleports | Minor — guard stops, recovers |
| B2 | `SecurityManager.Instance == null` | Any subscribing script | If scene loads before SecurityManager Awake | **Critical** — hard crash |
| B3 | `_currentTerminal` null after terminal deactivation | SecurityConsoleUI.cs | Player interacts with terminal, terminal deactivated while UI open | **Critical** — null ref in ProcessCommand |
| B4 | `DoorConfig == null` on assigned door | DoorController.cs | Generator assigns door without config | Minor — door defaults to locked |
| B5 | `InventoryManager.Instance == null` | KeycardItem.cs | Keycard used before InventoryManager initialized | Minor — check and warn |

### Race Conditions

| # | Risk | Scenario | Severity |
|---|---|---|---|
| B6 | Two guards detect player same frame | SecurityManager receives two OnPlayerDetected calls. Alarm level increments twice | Minor — alarm jumps 2 levels |
| B7 | Camera disable timer + alarm trigger same frame | Guard detects player while camera disable countdown is active | Minor — both fire, order undefined |
| B8 | MissionManager Start() vs SecurityManager Awake() | If MissionManager.Start() calls SecurityManager before SecurityManager.Awake() | **Critical** — null ref |

### Event Leaks

| # | Risk | Detail | Severity |
|---|---|---|---|
| B9 | MissionManager subscribes in Awake, unsubscribes in OnDestroy | Scene reload → old subscription still active → duplicate calls | **Critical** — double objective completion |
| B10 | SecurityCamera.OnDestroy not unregistered | Camera destroyed during gameplay → SecurityManager holds stale reference | Minor — event invocation on destroyed object logs warning |
| B11 | GuardFSM no unsubscription | Same pattern as B9 | **Critical** on scene reload |

### Mission & Flow Bugs

| # | Bug | Scenario | Severity |
|---|---|---|---|
| B12 | Mission phase advance on objective complete triggers multiple times | If objective completion fires twice, phase advances to next erroneously | **Critical** — skips entire phase |
| B13 | Checkpoint restore sets mission phase incorrectly | Player loads checkpoint from Phase 2, but state is at Phase 3 | **Major** — broken game state |
| B14 | EscapePhaseController lockdown triggers before player has artifact | Race condition in mission phase transition | **Critical** — soft lock |

### Door FSM Edge Cases

| # | Bug | Severity |
|---|---|---|
| B15 | Door in Opening state, player leaves range → DoorClosed event never fires | Minor — door stays open |
| B16 | Two keycards used simultaneously on same door | **Critical** — state machine desync |
| B17 | Door locked during escape, player already on other side | Minor — intentional, but need player feedback |

### Camera Edge Cases

| # | Bug | Severity |
|---|---|---|
| B18 | Camera disabled, 30s timer starts, scene unloads → timer orphaned | Minor |
| B19 | Camera re-enables while player is in detection cone → instant detection | **Major** — unfair to player. Add grace period on re-enable |
| B20 | SecurityCamera.OnTriggerEnter called multiple times with same collider | Minor — redundant triggers |

### Terminal Edge Cases

| # | Bug | Severity |
|---|---|---|
| B21 | Terminal command issued before authentication step | **Critical** — executes command without auth check |
| B22 | RBAC bypass attempted but already bypassed | Minor — redundant operation, no feedback |
| B23 | Terminal deactivated while player is in menu | **Critical** — UI state desync |

### Severity Summary

| Severity | Count | Action |
|---|---|---|
| Critical | 8 | Fix before submission |
| Major | 2 | Fix before submission |
| Minor | 11 | Fix if time permits |

---

## SECTION 11 — PROFESSOR EVALUATION

### Examiner Perspective

As an External Examiner for a Final Year Computer Science project, I would evaluate this work across the following dimensions:

#### Software Engineering (7/10)
**Demonstrates**: Clear understanding of separation of concerns, event-driven architecture, FSM implementation, Command pattern, ScriptableObject configuration, and interface-based design.
**Penalties**: No unit tests. No dependency injection. 9 singletons create coupling. No namespaces. OnGUI in production code. The reflection-based private field assignment in MuseumLevelGenerator would be flagged in code review as a design smell.

**Viva Question**: "Why did you choose to use reflection to set private fields in MuseumLevelGenerator rather than providing a public API? What does this say about the encapsulation of your DoorConfig class?"

#### Artificial Intelligence (6/10)
**Demonstrates**: Functional FSM with 6 states. NavMesh-based pathfinding. Context-aware transitions.
**Penalties**: No learning, no adaptation, no coordination, no hierarchical AI. Guard behavior is entirely deterministic. The Searching state is rudimentary. No anti-player-pattern awareness.

**Viva Question**: "Your guards have no awareness of alarm level changes. How would you extend the GuardFSM to respond to a global alarm state, and what pattern would you use to communicate this without coupling GuardFSM to SecurityManager directly?"

#### Cybersecurity (9/10)
**Demonstrates**: Genuine understanding of authentication vs. authorization separation. RBAC with role-based permission sets. Command restriction by roles. Multi-step terminal security pipeline.
**This is the project's standout feature.** The cyber operations system is above and beyond what is expected of a final year project.

**Viva Question**: "Explain the difference between authentication and authorization as implemented in your terminal system. How does your RBAC implementation prevent privilege escalation?"

#### Architecture (6/10)
**Demonstrates**: Understanding of modular design, interfaces, events, ScriptableObjects as data containers, and separation of mechanical concerns (AccessControl vs Cyber vs Core).
**Penalties**: No namespace isolation. Monolithic MissionManager. Singleton abuse prevents testing. No service locator pattern. Event subscription lifecycle not properly managed.

**Viva Question**: "If I asked you to add a save/load system, what components of your architecture would need to change and why? Walk me through the impact analysis."

#### Unity Proficiency (7/10)
**Demonstrates**: Competent use of MonoBehaviour lifecycle, NavMeshAgent, CharacterController, OnTriggerEnter, Input system, Raycast interaction, Coroutine timing, ScriptableObject workflow.
**Penalties**: OnGUI in 2024+ is unacceptable. No Post-Processing. Resources.LoadAll instead of direct references. No object pooling awareness (though not needed here).

**Viva Question**: "Why is OnGUI considered problematic in modern Unity development? How would you refactor your HUD to address these concerns?"

#### Object-Oriented Design (6/10)
**Demonstrates**: Interface segregation (IInteractable, ICommand), inheritance, composition over inheritance (FSM components).
**Penalties**: DemoModeController violates Single Responsibility. MissionManager violates Single Responsibility. No abstract base classes for FSM (duplicated enum+switch pattern). Heavy use of static state.

**Viva Question**: "DemoModeController handles initialization, spawning, debugging, and UI. How would you refactor it to better follow the Single Responsibility Principle?"

#### Design Patterns (8/10)
**Demonstrates**: Singleton, Observer (events), Command (ICommand + ActionExecutor), State (FSM enums), Strategy (guard states).
**Strong showing**. An examiner will be impressed by the Command pattern usage in terminal actions and the Observer pattern in SecurityManager events.

**Viva Question**: "You used the Command pattern for terminal actions. What advantages does this give you over a simple switch statement, and why didn't you apply the same pattern to guard states?"

#### Problem Solving (7/10)
**Demonstrates**: Working solutions to complex problems: multi-step authentication flow, camera detection with timer, escape phase with door lockdown, alarm system with escalation.
**Penalties**: No graceful failure states. No handling of edge cases (concurrent door access, terminal session while in pause menu).

**Viva Question**: "What happens if the player is detected by a guard while the camera disable timer is counting down? How does your system resolve these overlapping states?"

#### Innovation (8/10)
**Demonstrates**: The layered terminal hacking system is genuinely innovative for a student project. The RBAC bypass mechanic teaches the player about real security concepts. The escape lockdown changes the level state in an interesting way.
**Viva Question**: "What inspired the multi-step terminal security system, and how does it model real-world cybersecurity concepts?"

### Overall Verdict

This project would receive a **strong Upper Second (2:1)** with potential for **First (1st)** classification if the audio, lighting, UI, and bug fixes are completed before submission.

**Strengths**: Cybersecurity mechanics, architecture documentation potential, FSM variety, Command pattern implementation.
**Weaknesses**: Polish (no audio, no lighting, no UI), bug surface (event leaks, null refs), lack of testing, singleton coupling.

**Do not submit in current state.** The examiner will be impressed by the code concepts but dismayed by the lack of presentation polish.

---

## SECTION 12 — FINAL POLISH PLAN

### Gameplay Polish
- [ ] Add game-over state on alarm level 3
- [ ] Add keycard use limits (1–3 uses per card)
- [ ] Add movement noise radius to PlayerController
- [ ] Add crouch state (toggle, speed penalty, height change)
- [ ] Make terminal commands case-insensitive
- [ ] Add 1 optional side objective (secondary terminal hack)
- [ ] Add fail text hints when terminal auth code is missing
- [ ] Add score screen on mission complete (time, alarms, stealth)

### Audio Polish
- [ ] Create AudioManager (singleton, object-pooled AudioSources)
- [ ] Player footstep SFX (walk + run variants)
- [ ] Guard footstep SFX (3D spatial)
- [ ] Camera motor hum + detection beep
- [ ] Terminal keypress sounds (UI click array)
- [ ] Door motor + lock buzz SFX
- [ ] 3-tier alarm sounds (escalating intensity)
- [ ] Ambient museum drone (low loop)
- [ ] Escape phase music track

### Visual Polish
- [ ] Add Directional Light (museum skylight)
- [ ] Add Point Lights per room (warm dim)
- [ ] Add camera spotlights (cone mesh + light)
- [ ] Add Post-Processing Volume (Bloom, Vignette, ACES Tonemapping)
- [ ] Add red emergency lights on alarm level 2+
- [ ] Add emission materials on terminal screens, keycard readers
- [ ] Add 1–2 particle effects (dust motes, terminal glitch)

### Animation Polish
- [ ] Add door open/close animation (simple rotation tween)
- [ ] Add guard head-turn toward investigation point
- [ ] Add camera oscillation (sweep left/right)

### UI Polish
- [ ] Replace all OnGUI with TextMeshProUGUI on Canvas
- [ ] Create PauseMenu (Resume, Settings, Quit)
- [ ] Create MainMenu (New Game, Tutorial, Settings, Quit)
- [ ] Create Settings panel (Resolution, Fullscreen, Volume, Sensitivity)
- [ ] Create MissionComplete/Fail screens with stats
- [ ] Add loading screen during level generation
- [ ] Add interaction tooltip (E to interact) with fade
- [ ] Wrap DebugOverlay in #if UNITY_EDITOR

### Bug Fixes
- [ ] Fix all 8 critical bugs from Section 10
- [ ] Fix all 2 major bugs from Section 10
- [ ] Audit all Subscribe/Unsubscribe pairs for scene reload safety
- [ ] Audit all GetComponent calls — cache in Awake
- [ ] Fix MuseumLevelGenerator reflection → public API
- [ ] Fix SecurityManager.RegisterCamera TryAdd redundancy
- [ ] Add null guards to all Singleton.Instance accessors

### Optimization
- [ ] Replace OnGUI → Canvas (fixes GC)
- [ ] Fix GUIStyle allocation per frame
- [ ] Cache GetComponent calls
- [ ] Remove Debug.Log from Update
- [ ] Add event-driven objective completion check

### Presentation Polish
- [ ] Create architecture diagram (Visio/draw.io)
- [ ] Create FSM state diagrams for all 5 FSMs
- [ ] Create cybersecurity system flow diagram
- [ ] Write 2-page architecture document
- [ ] Write README with build instructions
- [ ] Add credits file for all assets
- [ ] Ensure build runs without console errors
- [ ] Test at 1920x1080 and 2560x1440

---

## SECTION 13 — ROADMAP

### Legend

| Category | Effort |
|---|---|
| 🟢 **P0 — Critical** | Must do before submission |
| 🟡 **P1 — High** | Should do before submission |
| 🟠 **P2 — Medium** | Nice to have |
| 🔴 **P3 — Low** | If time permits |

### Week 1: Foundation Fixes (Estimated: 5 days)

| Day | Task | Priority | Effort | ROI |
|---|---|---|---|---|
| 1 | Fix OnGUI → Canvas/TMP (MissionHUD, DebugOverlay) | 🟢 P0 | 4h | **Extreme** — fixes GC, enables styling |
| 1 | Cache all GetComponent calls in Awake | 🟢 P0 | 1h | **High** — free perf gain |
| 1 | Remove Debug.Log from Update loops | 🟢 P0 | 0.5h | **High** — cleanup |
| 2 | Fix MuseumLevelGenerator reflection → public setters | 🟢 P0 | 1h | **High** — architectural integrity |
| 2 | Fix SecurityManager.RegisterCamera duplication | 🟢 P0 | 0.5h | **Medium** — correctness |
| 2 | Fix all 8 critical bugs from Section 10 | 🟢 P0 | 4h | **Extreme** — prevents crashes |
| 3 | Add game-over on alarm level 3 | 🟡 P1 | 1h | **High** — core gameplay fix |
| 3 | Add terminal case-insensitive matching | 🟡 P1 | 0.5h | **High** — UX fix |
| 3 | Add PauseManager + PauseMenu (Escape key) | 🟡 P1 | 3h | **Extreme** — examiners will check |
| 4 | Create MainMenu scene | 🟡 P1 | 3h | **Extreme** — required for submission |
| 4 | Add Settings panel (resolution, volume, sensitivity) | 🟡 P1 | 3h | **High** — professionalism |
| 5 | Create MissionComplete/Fail screen | 🟡 P1 | 2h | **High** — closes gameplay loop |
| 5 | Wrap DebugOverlay in #if UNITY_EDITOR | 🟡 P1 | 0.5h | **Medium** — submission hygiene |

### Week 2: Audio & Visuals (Estimated: 5 days)

| Day | Task | Priority | Effort | ROI |
|---|---|---|---|---|
| 6 | Implement AudioManager | 🟡 P1 | 3h | **Extreme** — audio transforms game feel |
| 6 | Player footsteps (walk + run) | 🟡 P1 | 1h | **Extreme** — most impactful sound |
| 6 | Guard footsteps (3D spatial) | 🟡 P1 | 1h | **Extreme** — gameplay audio |
| 7 | Terminal + door + camera SFX | 🟡 P1 | 3h | **High** — feedback sounds |
| 7 | Alarm escalation sounds (3 tiers) | 🟡 P1 | 1h | **High** — tension |
| 7 | Ambient museum drone | 🟠 P2 | 0.5h | **Medium** — atmosphere |
| 8 | Add Directional Light + Point Lights per room | 🟡 P1 | 2h | **Extreme** — visual transformation |
| 8 | Add camera spotlights (cone mesh + light) | 🟡 P1 | 1h | **High** — gameplay visibility |
| 8 | Add Post-Processing Volume (Bloom, Vignette, Tonemapping) | 🟡 P1 | 1h | **Extreme** — presentation quality |
| 9 | Add emergency red lights on alarm | 🟠 P2 | 1h | **Medium** — atmosphere |
| 9 | Add emission materials (terminals, keycard readers) | 🟠 P2 | 0.5h | **Medium** — visual polish |
| 9 | Add AI voice lines (guard radio calls) | 🟠 P2 | 1h | **Medium** — immersion |
| 10 | Bug fix audit — fix remaining major + minor bugs | 🟢 P0 | 4h | **High** — stability |
| 10 | Event subscription audit (fix leaks) | 🟢 P0 | 2h | **High** — scene reload safety |

### Week 3: Gameplay Polish & Presentation (Estimated: 4 days)

| Day | Task | Priority | Effort | ROI |
|---|---|---|---|---|
| 11 | Add keycard use limits (1–3 uses) | 🟠 P2 | 1h | **Medium** — tension |
| 11 | Add movement noise radius | 🟠 P2 | 1h | **Medium** — stealth depth |
| 11 | Add crouch toggle | 🟠 P2 | 2h | **Medium** — gameplay depth |
| 12 | Add level design loops (1 alternate path in museum) | 🟠 P2 | 3h | **Medium** — replayability |
| 12 | Add 1 optional side objective | 🟠 P2 | 1h | **Medium** — content depth |
| 12 | Adjust difficulty curve (more guards later) | 🟠 P2 | 1h | **Medium** — pacing |
| 13 | Create architecture diagrams + FSM diagrams | 🟡 P1 | 4h | **Extreme** — examiners love diagrams |
| 13 | Write 2-page architecture document | 🟡 P1 | 3h | **Extreme** — submission requirement |
| 14 | Full playthrough — log every issue found | 🟢 P0 | 2h | **Extreme** — catch remaining bugs |
| 14 | Fix all issues from playthrough | 🟢 P0 | 4h | **Extreme** |
| 14 | Build testing (3 resolutions, fullscreen/windowed) | 🟢 P0 | 1h | **High** — submission readiness |
| 14 | Create submission ZIP + README + CREDITS | 🟢 P0 | 1h | **Extreme** — submission |

### Time Budget Totals

| Priority | Hours | % |
|---|---|---|
| 🟢 P0 — Critical | 26.5h | 39% |
| 🟡 P1 — High | 26.5h | 39% |
| 🟠 P2 — Medium | 15h | 22% |
| 🔴 P3 — Low | 0h | 0% |
| **Total** | **68h** | **100%** |

At 4h/day (evenings + weekends), this is **17 days** of work.

### ROI Ranking (Highest Impact First)

1. OnGUI → Canvas/TMP (fixes GC, enables UI) — 4h
2. Main menu + Pause menu — 6h
3. AudioManager + footsteps — 4h
4. Lighting + Post-Processing — 4h
5. Architecture diagrams — 4h
6. Critical bug fixes — 6h
7. Bug playthrough + fix — 6h
8. Settings panel — 3h
9. Mission complete/fail screen — 2h
10. Terminal UX improvements — 0.5h
11. Game-over on alarm 3 — 1h
12. Keycard limits — 1h
13. Alternate path — 3h
14. Movement noise — 1h
15. Crouch — 2h
16. Emergency lights — 1h
17. Guard voice lines — 1h
18. Optional objective — 1h
19. Difficulty curve — 1h
20. Emission materials — 0.5h

---

---

## PHASE 5A — POST-REVIEW GAMEPLAY ENHANCEMENTS

The following features were implemented after the initial review:

### Elite Guard (`GuardFSM.isElite`)
- New `isElite` bool field on GuardFSM; when true, overrides patrol speed (2.5+), vision range (14m), FOV (80°), suspicion time (1.2s), chase speed (6.5), and hearing radius (18m).
- One elite guard placed in the vault antechamber with a 5-waypoint patrol loop.
- Rendered in dark red to distinguish from standard blue guards.

### Checkpoints (`CheckpointTrigger`)
- New `CheckpointTrigger` MonoBehaviour: box collider trigger that saves progress when the player enters the zone.
- `CheckpointManager` extended with `RegisterCheckpoint()` and `ForceCheckpoint()` for runtime checkpoint registration.
- 4 checkpoints placed: Lobby, Security Office, Vault Corridor entrance, and Escape zone.

### Camera Network Disable (`DisableCameraGroup`)
- `SecurityManager.DisableCameraGroup(string[] ids)` disables multiple cameras at once and returns the count.
- New `DisableCameraGroupAction` terminal action parses semicolon-delimited camera IDs from `TargetID`.
- Registered at level start via `ActionExecutor.RegisterAction("DisableCameraGroup", ...)`.
- Security Office terminal now offers "Disable Camera Network (East+West)" — disables `camera_east`, `camera_west`, and `camera_lobby` simultaneously.

### Mission Scoring (`MissionScorer`)
- New singleton tracks: camera detections, guard encounters, alarms triggered, mission completion time, and secondary objective completion.
- Subscribes to `SecurityManager.OnTriggerReported` and `MissionManager.OnMissionCompleted`.
- Rating tiers: S (95+), A (75+), B (50+), C (25+), D (<25).
- Score deduction: -15 per camera detection, -20 per guard encounter, -25 per alarm, -2 per 10s over time limit.

### HUD Enhancements (`MissionHUD`)
- **Stealth Indicator**: Eye icon in bottom-right shows visibility level (green=hidden, yellow=cautious, red=detected). Scans nearby guards and cameras within 12m.
- **Detection Meter**: Center-screen progress bar when a guard or camera is building detection. Shows source ("GUARD", "CAMERA") and flashes "DETECTED!" when full.
- Both indicators use the existing OnGUI system and can be migrated to Canvas/TMP in a future pass.

### Results Screen (`ResultsScreen`)
- New OnGUI panel that appears after `OnMissionCompleted` with a 1.5s delay.
- Displays: rating letter (S/A/B/C/D) with color, descriptive label, stat breakdown (detections, alarms, time, secondary objectives).
- Dismissed with Space key.

### Scoring Update
- `MissionScorer.StartScoring()` is triggered by the first `OnObjectiveStarted` event from MissionManager.
- Bootstrapped via `GameBootstrapper` as a singleton alongside existing managers.

---

## FINAL SCORECARD

| Category | Score (1–10) | Verdict |
|---|---|---|
| **Architecture** | 6 | Solid patterns undermined by singleton coupling and reflection hack |
| **Gameplay** | 7.5 | Elite guard, checkpoints, scoring, camera network, and results screen added post-review. Still lacks fail states and player agency |
| **Performance** | 5 | OnGUI and GetComponent spam will cause visible hitches. Fixable in a day |
| **Visuals** | 2 | No lighting, no post-processing, no atmosphere. Looks like a greybox |
| **Audio** | 0 | Complete absence. Single biggest impact investment |
| **UX** | 4 | HUD enhancements (stealth indicator, detection meter, results screen) improve feedback. OnGUI remains |
| **AI** | 5.5 | Elite guard variant adds depth. Standard guards still lack coordination and alarm awareness |
| **Code Quality** | 5 | Inconsistent. Some files are excellent, others are production-dangerous |
| **Presentation** | 3 | Cannot submit in current state. Missing main menu, audio, lighting, and UI framework |
| **Cybersecurity (bonus)** | 8 | Genuine standout. Multi-step auth + RBAC is above final-year expectations |

### Overall Score: **5.2 / 10**

### Final Verdict

**Would this project stand out among typical final-year Computer Science projects?**

Yes — but only if the Week 1 and Week 2 roadmaps are completed.

The **cybersecurity mechanics** (Authentication → Authorization → RBAC command restriction) are genuinely above average. The **FSM architecture** (5 separate state machines) demonstrates appropriate complexity. The **Command pattern** implementation in terminal actions shows design pattern understanding beyond what most students demonstrate.

However, in its current state, the project would be **graded down significantly** for lack of audio, absence of lighting, OnGUI in production code, and missing main menu/pause functionality. An examiner's first impression is visual and auditory — a silent, dark, greyboxed game with system text as UI does not inspire confidence regardless of code quality.

The gap between the code's technical merit and the game's presentation quality is the project's single biggest risk.

**Recommendation**: Invest 4 days in Critical (P0) items, then 4 days in High (P1) audio/lighting. After those 8 days, the project is submission-ready and competitive for a First classification.

---

*End of Technical Design Review*

*Review Board: Senior Gameplay Designer (Arkane), Senior AI Engineer (IO Interactive), Senior Unity Technical Director, Senior Software Architect, Senior UX Designer, Senior Level Designer, Senior Performance Engineer, Senior Audio Director, Senior Technical Artist, University CS External Examiner*
