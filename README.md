# Museum Heist

A Unity stealth-puzzle game built around infiltration, security hacking, and tactical escape.

> **Status:** Prototype / pre-submission. Core gameplay is implemented, but polish is incomplete: audio, lighting, menus, and UI still need final work.

---

## Table of Contents

- [Overview](#overview)
- [Core Gameplay](#core-gameplay)
- [Game Systems](#game-systems)
  - [Security & Alarm](#security--alarm)
  - [Terminal Hacking & Cybersecurity](#terminal-hacking--cybersecurity)
  - [Guard AI](#guard-ai)
  - [Cameras & Surveillance](#cameras--surveillance)
  - [Doors, Keycards, & Access Control](#doors-keycards--access-control)
  - [Mission Phases](#mission-phases)
  - [Objectives & Win/Lose Conditions](#objectives--winlose-conditions)
- [Controls](#controls)
- [Architecture](#architecture)
  - [Script Structure](#script-structure)
  - [Design Patterns](#design-patterns)
  - [Events & Communication](#events--communication)
- [Project Status](#project-status)
  - [Completed](#completed)
  - [Needs Work](#needs-work)
  - [Known Issues](#known-issues)
- [Build & Run](#build--run)
- [Project Notes](#project-notes)
- [Future Improvements](#future-improvements)
- [Credits](#credits)

---

## Overview

Museum Heist is a first-person stealth game set in a security-tight museum. The player must infiltrate the museum, bypass surveillance, hack terminals, acquire a valuable artifact, and escape before the guards and alarm system close the doors.

The game is designed around a four-phase mission structure:

1. Infiltration
2. Navigation
3. Acquisition
4. Escape

The main gameplay pillars are:

- Stealth movement and avoidance
- Security camera management
- Terminal hacking with authentication and permissions
- Keycard-based access control
- Guard patrol and alert behavior
- Mission objective tracking and escape tension

---

## Core Gameplay

### High-level loop

- Observe guards and cameras.
- Use the environment, doors, and terminals to progress.
- Avoid direct detection by staying out of sight and minimizing noise.
- Hack security consoles to gain access or disable systems.
- Reach the artifact, take it, and escape before a lockdown forces mission failure.

### Game flow

- Start in a museum lobby or tutorial area.
- Progress through a series of connected rooms, each with security challenges.
- Interact with terminals, doors, and keycards using the same `E` interaction system.
- Complete objectives in order while dealing with alarm escalation and guard attention.

---

## Game Systems

### Security & Alarm

The security system is managed by `SecurityManager` and has a multi-level alarm state.

- Alarm levels are tracked globally.
- Guards and cameras register with the security system.
- Detection by cameras or guards raises alarm levels.
- Alarm escalation triggers gameplay changes such as lockdown and tougher guard behavior.

Current implementation notes:

- Alarm level 3 is the critical failure threshold.
- Camera disable is temporary; cameras re-enable automatically.
- Guards have awareness states that reflect their suspicion.

### Terminal Hacking & Cybersecurity

The terminal system is the game’s most unique feature. It is designed as a layered cybersecurity puzzle with:

- Authentication
- Authorization
- Role-Based Access Control (RBAC)
- Command execution via a command console

Key components:

- `SecurityConsoleUI` handles terminal input and output.
- `AuthenticationService` validates credentials.
- `AuthorizationService` checks permissions.
- `RBACService` enforces role-based command access.
- `ActionExecutor` dispatches terminal actions.

Player interaction with terminals can unlock doors, disable cameras, and alter guard behavior if the correct credentials and permissions are obtained.

### Guard AI

Guards are implemented using a finite state machine in `GuardFSM`.

Guard states include:

- Idle
- Suspicious
- Alert
- Investigating
- Searching
- Engaged

Key behaviors:

- Patrol along waypoints using NavMeshAgent.
- Detect the player by sight and sound.
- Transition from suspicion to full alert when detection thresholds are reached.
- Search the last known player location after losing sight.

The AI is designed to feel predictable but provides the foundation for more complex alert and coordination behavior.

### Cameras & Surveillance

Security cameras are a core stealth hazard.

- Cameras detect the player if within cone and line of sight.
- Each camera has a disable timer and re-enable behavior.
- Cameras register with `SecurityManager` and contribute to alarm escalation.

Current camera features:

- Rotating pan and detection cone.
- Temporary disable with visual feedback planned.
- Security network integration through terminals.

### Doors, Keycards, & Access Control

The museum uses tiered locked doors with keycard requirements.

- Doors are handled by `DoorController` and configured with `DoorConfig`.
- Keycards are items that satisfy door access requirements.
- Doors can be opened, closed, locked, and can trigger events when access is granted or denied.

The intended progression uses multiple keycard tiers to gate player movement and create meaningful choices.

### Mission Phases

The game is structured into four mission phases:

1. **Infiltration** — enter the museum and bypass outer defenses.
2. **Navigation** — move through guarded spaces and find key areas.
3. **Acquisition** — reach the vault or artifact chamber.
4. **Escape** — flee after taking the artifact while the facility goes into lockdown.

Mission phases are tracked by `MissionManager` and drive objective updates and failure conditions.

### Objectives & Win/Lose Conditions

Primary objectives are presented through the HUD and are used to advance mission phases.

Win condition:

- Retrieve the artifact and escape through the exit.

Lose condition:

- Trigger critical alarm level 3 (planned as mission failure).

The current prototype includes:

- Objective tracking and completion.
- Mission phase advancement.
- Escape sequence logic.

---

## Controls

- `WASD` — Move
- `Mouse` — Look around
- `E` — Interact
- `Escape` — Pause menu (planned)

Note: Controller support is a polish item and is not fully implemented yet.

---

## Architecture

### Script Structure

The codebase is organized into several domains:

- `AccessControl` — Doors, keycards, and locks.
- `Core` — Player, interaction, inventory, guard and camera systems.
- `Cyber` — Terminal hacking, authentication, authorization, RBAC.
- `UI` — HUD, terminal UI, debug overlays.
- `Root` — Mission, artifact, escape flow.

Important classes:

- `MissionManager` — mission state, objectives, phase transitions.
- `SecurityManager` — alarm and security registry.
- `GuardFSM` — guard behavior and state transitions.
- `SecurityCamera` — camera detection and disable handling.
- `DoorController` — door state machine.
- `SecurityConsoleUI` — interactive terminal interface.

### Design Patterns

The project uses multiple established patterns:

- Singleton managers for global systems.
- Command pattern for terminal actions (`ICommand`, `ActionExecutor`).
- Observer events for security state changes.
- State machines for guards, doors, and mission phases.
- ScriptableObjects for configuration data.

### Events & Communication

Systems communicate through a mix of:

- C# events in `SecurityManager`.
- UnityEvents on `DoorConfig`.
- Direct singleton references.

This is a functional but currently inconsistent event architecture, and a future improvement is to standardize on a single event bus or ScriptableObject event channels.

---

## Project Status

### Completed

- Core stealth mechanics.
- Guard patrol and search AI.
- Security camera detection.
- Tiered access system with doors and keycards.
- Terminal hacking infrastructure with authentication and RBAC.
- Mission phases and objective tracking.
- Level generation and room-based progression.

### Needs Work

- Main menu / pause menu / settings.
- Audio and sound effects.
- Lighting, post-processing, and visual polish.
- Final HUD replacement of `OnGUI` with TextMeshPro UI.
- Controller support and input management.
- Better event lifecycle management and scene reload safety.
- Added guard coordination and alarm response behavior.

### Known Issues

- No audio system built in yet.
- Scene runs with minimal or no custom lighting.
- `OnGUI` UI components are still in use and need replacement.
- Some debug tools and console output remain in the scene.
- Several design issues are documented in the engineering review.

---

## Build & Run

### Requirements

- Unity Editor: `2024.1` / `6000.5.4f1`
- Platform: Windows
- Project folder: `c:\Users\amegh\game`

### Instructions

1. Open Unity Hub.
2. Add the project folder: `c:\Users\amegh\game`.
3. Open the `game.slnx` or load the Unity project directly.
4. Open the main scene(s): `Assets/Scenes/MuseumHeist.unity` or `Assets/Scenes/Tutorial.unity`.
5. Press Play in the Unity Editor.

> If the project is run outside the editor, make sure all scene references are assigned and that there are no missing script references.

---

## Project Notes

This README is built from the existing project documentation and review notes:

- `MuseumHeist_Review.md`
- `MuseumHeist_TDR.md`
- `MuseumHeist_Engineering_Review.md`

Those docs contain deep design, architecture, bug, and polish guidance.

---

## Future Improvements

The following improvements are planned or recommended:

- Replace `OnGUI` HUD with `Canvas` / `TextMeshPro` UI.
- Implement a proper main menu and pause/settings screens.
- Add audio: footsteps, guards, camera hum, alarm, UI sounds.
- Add lighting and post-processing for atmosphere.
- Add emergency alarm lighting tied to alarm level.
- Add guard coordination and alarm-level-based AI behavior.
- Add a side objective system and alternate routes in the level.
- Add unit tests for the core cybersecurity systems.
- Refactor singletons into interfaces and dependency injection.
- Improve level design with branching paths and verticality.
- Add screen transitions, mission complete/fail screens, and score tracking.

---

## Credits

- Project and game design by the Museum Heist development team.
- Security/terminal mechanics inspired by classic stealth games and cybersecurity concepts.
- Unity project developed in Unity Editor `6000.5.4f1`.

> Asset credits are not yet finalized. Add sound and visual asset authors in `CREDITS.md` once the final build is packaged.
