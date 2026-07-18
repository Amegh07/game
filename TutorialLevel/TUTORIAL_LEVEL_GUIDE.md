# Tutorial Level — Stealth Game Whitebox

## Overview

Linear 6-room tutorial level built entirely from Unity primitives.  
Each room: **5w × 5d × 3h units**  
Corridors: **3w × 3d × 3h units**

### Room Order

```
Entrance → Reception → Exhibit Room → Security Room → Vault → Exit
```

### Tutorial Gating

| Room          | What the player learns                                     |
|---------------|------------------------------------------------------------|
| Entrance      | Basic movement (WASD + mouse look)                         |
| Reception     | Guard patrol — observe movement patterns                   |
| Exhibit Room  | Find a keycard, approach locked door                       |
| Security Room | Use computer terminal to disable security camera           |
| Vault         | Collect the artifact (win condition)                       |
| Exit          | Level ends                                                 |

---

## 1. Project Setup

1. Open **Unity Hub** → **Open** → select the `TutorialLevel` folder.  
   *(Unity 2022.3 LTS or later recommended.)*
2. Once the project loads, create the folder structure in the **Project** panel:

```
Assets/
├── Scenes/
├── Scripts/        ← already present
├── Prefabs/
└── Materials/
```

3. **Import the scripts** — the `.cs` files in `Assets/Scripts/` will be compiled automatically.

---

## 2. Scene Setup

1. **File → Save** the current scene as `Assets/Scenes/TutorialLevel.unity`.
2. Set the lighting to **baked whitebox** (or just use the default Directional Light).

---

## 3. Room Layout — Exact Coordinates

All rooms are centered on **X = 0**. Y is the vertical axis (floor = 0, ceiling = 3).  
Z is the linear axis.

### Legend

| Object      | Unity Primitive |
|-------------|-----------------|
| Floor       | Cube (thin)     |
| Ceiling     | Cube (thin)     |
| Side wall   | Cube            |
| End wall    | Cube            |

### 3.1 — Entrance  (z center = -20)

| Object        | Position            | Scale             |
|---------------|---------------------|-------------------|
| Floor         | (0, 0.05, -20)      | (5, 0.1, 5)       |
| Ceiling       | (0, 2.95, -20)      | (5, 0.1, 5)       |
| Left Wall     | (-2.5, 1.5, -20)    | (0.2, 3, 5)       |
| Right Wall    | (2.5, 1.5, -20)     | (0.2, 3, 5)       |
| Back Wall     | (0, 1.5, -22.5)     | (5, 3, 0.2)       |

### 3.2 — Corridor 1  (z center = -16)

| Object        | Position          | Scale             |
|---------------|-------------------|-------------------|
| Floor         | (0, 0.05, -16)    | (3, 0.1, 3)       |
| Ceiling       | (0, 2.95, -16)    | (3, 0.1, 3)       |
| Left Wall     | (-1.5, 1.5, -16)  | (0.2, 3, 3)       |
| Right Wall    | (1.5, 1.5, -16)   | (0.2, 3, 3)       |

### 3.3 — Reception  (z center = -12)

| Object        | Position            | Scale             |
|---------------|---------------------|-------------------|
| Floor         | (0, 0.05, -12)      | (5, 0.1, 5)       |
| Ceiling       | (0, 2.95, -12)      | (5, 0.1, 5)       |
| Left Wall     | (-2.5, 1.5, -12)    | (0.2, 3, 5)       |
| Right Wall    | (2.5, 1.5, -12)     | (0.2, 3, 5)       |

### 3.4 — Corridor 2  (z center = -8)

| Object        | Position          | Scale             |
|---------------|-------------------|-------------------|
| Floor         | (0, 0.05, -8)     | (3, 0.1, 3)       |
| Ceiling       | (0, 2.95, -8)     | (3, 0.1, 3)       |
| Left Wall     | (-1.5, 1.5, -8)   | (0.2, 3, 3)       |
| Right Wall    | (1.5, 1.5, -8)    | (0.2, 3, 3)       |

### 3.5 — Exhibit Room  (z center = -4)

| Object        | Position            | Scale             |
|---------------|---------------------|-------------------|
| Floor         | (0, 0.05, -4)       | (5, 0.1, 5)       |
| Ceiling       | (0, 2.95, -4)       | (5, 0.1, 5)       |
| Left Wall     | (-2.5, 1.5, -4)     | (0.2, 3, 5)       |
| Right Wall    | (2.5, 1.5, -4)      | (0.2, 3, 5)       |

### 3.6 — Corridor 3  (z center = 0)

| Object        | Position          | Scale             |
|---------------|-------------------|-------------------|
| Floor         | (0, 0.05, 0)      | (3, 0.1, 3)       |
| Ceiling       | (0, 2.95, 0)      | (3, 0.1, 3)       |
| Left Wall     | (-1.5, 1.5, 0)    | (0.2, 3, 3)       |
| Right Wall    | (1.5, 1.5, 0)     | (0.2, 3, 3)       |

### 3.7 — Security Room  (z center = 4)

| Object        | Position            | Scale             |
|---------------|---------------------|-------------------|
| Floor         | (0, 0.05, 4)        | (5, 0.1, 5)       |
| Ceiling       | (0, 2.95, 4)        | (5, 0.1, 5)       |
| Left Wall     | (-2.5, 1.5, 4)      | (0.2, 3, 5)       |
| Right Wall    | (2.5, 1.5, 4)       | (0.2, 3, 5)       |

### 3.8 — Corridor 4  (z center = 8)

| Object        | Position          | Scale             |
|---------------|-------------------|-------------------|
| Floor         | (0, 0.05, 8)      | (3, 0.1, 3)       |
| Ceiling       | (0, 2.95, 8)      | (3, 0.1, 3)       |
| Left Wall     | (-1.5, 1.5, 8)    | (0.2, 3, 3)       |
| Right Wall    | (1.5, 1.5, 8)     | (0.2, 3, 3)       |

### 3.9 — Vault  (z center = 12)

| Object        | Position            | Scale             |
|---------------|---------------------|-------------------|
| Floor         | (0, 0.05, 12)       | (5, 0.1, 5)       |
| Ceiling       | (0, 2.95, 12)       | (5, 0.1, 5)       |
| Left Wall     | (-2.5, 1.5, 12)     | (0.2, 3, 5)       |
| Right Wall    | (2.5, 1.5, 12)      | (0.2, 3, 5)       |

### 3.10 — Corridor 5  (z center = 16)

| Object        | Position          | Scale             |
|---------------|-------------------|-------------------|
| Floor         | (0, 0.05, 16)     | (3, 0.1, 3)       |
| Ceiling       | (0, 2.95, 16)     | (3, 0.1, 3)       |
| Left Wall     | (-1.5, 1.5, 16)   | (0.2, 3, 3)       |
| Right Wall    | (1.5, 1.5, 16)    | (0.2, 3, 3)       |

### 3.11 — Exit  (z center = 20)

| Object        | Position            | Scale             |
|---------------|---------------------|-------------------|
| Floor         | (0, 0.05, 20)       | (5, 0.1, 5)       |
| Ceiling       | (0, 2.95, 20)       | (5, 0.1, 5)       |
| Left Wall     | (-2.5, 1.5, 20)     | (0.2, 3, 5)       |
| Right Wall    | (2.5, 1.5, 20)      | (0.2, 3, 5)       |
| Front Wall    | (0, 1.5, 22.5)      | (5, 3, 0.2)       |

---

## 4. Complete Hierarchy

Build this hierarchy in the **Hierarchy** panel. Create empty GameObjects as parents (set their position to (0,0,0) and scale to (1,1,1)).

```
TutorialLevel (scene root)
│
├── _Environment
│   ├── Rooms
│   │   ├── Entrance          (empty, position 0,0,0)
│   │   │   ├── Floor
│   │   │   ├── Ceiling
│   │   │   ├── LeftWall
│   │   │   ├── RightWall
│   │   │   └── BackWall
│   │   ├── Corridor_1        (empty, position 0,0,0)
│   │   │   ├── Floor
│   │   │   ├── Ceiling
│   │   │   ├── LeftWall
│   │   │   └── RightWall
│   │   ├── Reception         (empty, position 0,0,0)
│   │   │   ├── Floor
│   │   │   ├── Ceiling
│   │   │   ├── LeftWall
│   │   │   └── RightWall
│   │   ├── Corridor_2        (empty, position 0,0,0)
│   │   │   ├── Floor
│   │   │   ├── Ceiling
│   │   │   ├── LeftWall
│   │   │   └── RightWall
│   │   ├── ExhibitRoom       (empty, position 0,0,0)
│   │   │   ├── Floor
│   │   │   ├── Ceiling
│   │   │   ├── LeftWall
│   │   │   └── RightWall
│   │   ├── Corridor_3        (empty, position 0,0,0)
│   │   │   ├── Floor
│   │   │   ├── Ceiling
│   │   │   ├── LeftWall
│   │   │   └── RightWall
│   │   ├── SecurityRoom      (empty, position 0,0,0)
│   │   │   ├── Floor
│   │   │   ├── Ceiling
│   │   │   ├── LeftWall
│   │   │   └── RightWall
│   │   ├── Corridor_4        (empty, position 0,0,0)
│   │   │   ├── Floor
│   │   │   ├── Ceiling
│   │   │   ├── LeftWall
│   │   │   └── RightWall
│   │   ├── Vault             (empty, position 0,0,0)
│   │   │   ├── Floor
│   │   │   ├── Ceiling
│   │   │   ├── LeftWall
│   │   │   └── RightWall
│   │   ├── Corridor_5        (empty, position 0,0,0)
│   │   │   ├── Floor
│   │   │   ├── Ceiling
│   │   │   ├── LeftWall
│   │   │   └── RightWall
│   │   └── Exit              (empty, position 0,0,0)
│   │       ├── Floor
│   │       ├── Ceiling
│   │       ├── LeftWall
│   │       ├── RightWall
│   │       └── FrontWall
│   │
│   └── Floors                (optional — all floor cubes as flattened list)
│
├── _Props
│   ├── GuardWaypoints        (empty, position 0,0,0)
│   │   ├── WP1               (empty, position -1.5, 0, -13.5)
│   │   ├── WP2               (empty, position 1.5, 0, -13.5)
│   │   ├── WP3               (empty, position 1.5, 0, -10.5)
│   │   └── WP4               (empty, position -1.5, 0, -10.5)
│   │
│   ├── Keycard               (Cube, position -1.5, 0.5, -4)
│   │   └── (add KeycardPickup script)
│   │
│   ├── LockedDoor            (Cube, position 0, 1.5, -1.5)
│   │   └── (add LockedDoor script)
│   │
│   ├── ComputerTerminal      (Cube, position -1.5, 0.75, 4)
│   │   └── (add ComputerTerminal script)
│   │
│   ├── SecurityCameraMount   (Cylinder, position 0, 2.5, 4)
│   │   └── (add SecurityCamera script)
│   │
│   └── Artifact              (Sphere, position 0, 0.75, 12)
│       └── (add ArtifactCollectible script)
│
├── _Guards
│   └── Guard                 (Capsule, position -1.5, 0.5, -13.5)
│       └── (add GuardPatrol script)
│
├── _Lights
│   ├── Directional Light     (default scene light)
│   ├── PointLight_Entrance
│   ├── PointLight_Reception
│   ├── PointLight_Exhibit
│   ├── PointLight_Security
│   ├── PointLight_Vault
│   └── PointLight_Exit
│
└── _Player
    ├── PlayerControllerRoot  (empty, position 0, 1, -20)
    │   └── (add PlayerController script + CharacterController)
    └── PlayerCamera           (Camera, localPosition 0, 0.7, 0, localRotation 0,0,0)
        └── (add PlayerInteraction script)
```

---

## 5. Step-by-Step Scene Construction

### 5.1 — Create Room Geometry

For each room/corridor use **GameObject → 3D Object → Cube**.

Set each cube's **Transform → Position** and **Transform → Scale** using the tables in Section 3 above.

> **Tip:** Create one room, then duplicate it and adjust position — saves time.

### 5.2 — Create Props

#### Guard Patrol Waypoints

Create 4 empty GameObjects inside `GuardWaypoints`.
Position them at the corners of Reception:

| Waypoint | Position             |
|----------|----------------------|
| WP1      | (-1.5, 0, -13.5)    |
| WP2      | (1.5, 0, -13.5)     |
| WP3      | (1.5, 0, -10.5)     |
| WP4      | (-1.5, 0, -10.5)    |

#### Locked Door

- Create a **Cube** at **(0, 1.5, -1.5)** with scale **(0.1, 2.5, 1.2)**.
- Position it as a door blocking Corridor 3 (between Exhibit and Security).
  It should overlap the left wall slightly, with the hinge on the left side.
- Add the `LockedDoor` script to it.

#### Keycard Pickup

- Create a **Cube** at **(-1.5, 0.5, -4)** with scale **(0.3, 0.05, 0.2)**.
- This is inside the Exhibit Room, on a small pedestal (a Cube at (-1.5, 0.25, -4) scale (0.5, 0.1, 0.5)).
- Add the `KeycardPickup` script.  
- Drag the `LockedDoor` object into the `Target Door` slot in the Inspector.

#### Security Camera

- Create a **Cylinder** at **(0, 2.5, 4)** with scale **(0.3, 0.3, 0.3)**.
- Add a **Cone** for the FOV visual:
  - **GameObject → 3D Object → Cube**, position **(0, 2.5, 4.5)**, scale **(0.1, 0.1, 1)**.
  - (A cube stretched into a thin rectangular "beam" represents the camera FOV.)
- Add the `SecurityCamera` script to the Cylinder.

#### Computer Terminal

- Create a **Cube** at **(-1.5, 0.75, 4)** with scale **(0.8, 0.1, 0.6)** — the desk.
- Create a **Cube** at **(-1.5, 1, 4.2)** with scale **(0.5, 0.4, 0.05)** — the screen.
- Add a second box for the base: **(-1.5, 0.9, 4)** scale **(0.3, 0.05, 0.3)**.
- Add the `ComputerTerminal` script to the screen cube.
- Drag the SecurityCamera object into the `Target Camera` slot.

#### Artifact

- Create a **Sphere** at **(0, 0.75, 12)** with scale **(0.5, 0.5, 0.5)**.
- It should be on a pedestal: Cube at **(0, 0.35, 12)** scale **(0.8, 0.2, 0.8)**.
- Add the `ArtifactCollectible` script to the sphere.

---

## 6. Prefab Creation

Drag these objects from the Hierarchy into `Assets/Prefabs/` to create prefabs:

| Object              | Prefab Name         | Root Object (drag this)                     |
|---------------------|---------------------|---------------------------------------------|
| Guard (capsule)     | `Guard.prefab`      | The capsule with `GuardPatrol` script       |
| Security Camera     | `SecurityCamera.prefab` | The cylinder with `SecurityCamera` script |
| Locked Door         | `LockedDoor.prefab` | The door cube with `LockedDoor` script      |
| Computer Terminal   | `ComputerTerminal.prefab` | The screen cube with `ComputerTerminal` |
| Artifact            | `Artifact.prefab`   | The sphere with `ArtifactCollectible`       |

After creating prefabs, you can replace the scene instances with the prefab instances.  
**Open each prefab** in Prefab Mode to adjust positioning as needed.

---

## 7. Player Setup

1. Create an empty GameObject named `_Player` at **(0, 0, 0)**.
2. Inside it, create an empty `PlayerControllerRoot` at **(0, 1, -19.5)** (inside Entrance).
3. Add **Component → CharacterController** to `PlayerControllerRoot`.  
   Set **Height = 1.8**, **Radius = 0.4**.
4. Add `PlayerController` script.
5. Create a **Camera** as a child of `PlayerControllerRoot` at localPosition **(0, 0.7, 0)**.
6. Add `PlayerInteraction` script to the Camera.

---

## 8. Script Configuration Summary

### GuardPatrol — on Guard Capsule

| Field         | Value            |
|---------------|------------------|
| Waypoints     | Array size 4     |
|               | [0] → WP1        |
|               | [1] → WP2        |
|               | [2] → WP3        |
|               | [3] → WP4        |
| Move Speed    | 2                |
| Rotation Speed| 5                |
| Loop          | true             |

### SecurityCamera — on Camera Cylinder

| Field           | Value |
|-----------------|-------|
| Rotation Angle  | 45    |
| Rotation Speed  | 30    |
| Detection Range | 8     |
| Field of View   | 60    |

### LockedDoor — on Door Cube

| Field       | Value |
|-------------|-------|
| Is Locked   | true  |
| Open Angle  | 90    |
| Open Speed  | 2     |

### ComputerTerminal — on Screen Cube

| Field             | Value                                     |
|-------------------|-------------------------------------------|
| Target Camera     | Drag SecurityCamera here                  |
| Screen Renderer   | Drag the Screen's MeshRenderer here       |

### KeycardPickup — on Keycard Cube

| Field      | Value                               |
|------------|-------------------------------------|
| Target Door| Drag LockedDoor object here         |

### ArtifactCollectible — on Sphere

*(No references needed — defaults work.)*

### PlayerController — on PlayerControllerRoot

| Field            | Value |
|------------------|-------|
| Move Speed       | 5     |
| Mouse Sensitivity| 2     |

### PlayerInteraction — on Camera

| Field             | Value             |
|-------------------|-------------------|
| Interaction Range | 3                 |
| Interact Key      | E                 |
| Interaction Layer | Everything        |

---

## 9. Materials (Optional)

To differentiate rooms, create basic Materials:

1. **Assets → Create → Material** for each room group:
   - `Mat_Entrance` — dark gray
   - `Mat_Reception` — blue
   - `Mat_Exhibit` — green
   - `Mat_Security` — red
   - `Mat_Vault` — gold/yellow
   - `Mat_Exit` — white

2. Assign by dragging onto the floor/ceiling/wall cubes.

---

## 10. Testing

1. Press **Play**.
2. Walk through Entrance → Reception (watch guard patrol).
3. Enter Exhibit Room → pick up keycard.
4. Approach locked door → press **E** to open.
5. Enter Security Room → interact with terminal → camera stops.
6. Enter Vault → collect artifact.
7. Walk to Exit.

**Expected behavior:**
- Guard patrols waypoints.
- Camera oscillates left/right.
- Door stays locked until keycard is collected.
- Terminal disables camera.
- Artifact floats/rotates and disappears when collected.

---

## 11. Extending the Tutorial

| Feature                          | How to add                                                    |
|----------------------------------|---------------------------------------------------------------|
| Alert state                      | Add an `OnTriggerEnter` to the camera's FOV cone              |
| Guard vision cone                | Add a child capsule + trigger collider to the Guard            |
| Sound effects                    | Use `AudioSource.PlayOneShot()` in `Interact()` methods        |
| UI prompts (crosshair, text)     | Add a Canvas with world-space or screen-space UI elements      |
| Respawn / fail state             | Check distance to guard in `Update()` and reposition player    |
| Minimap                          | Add a second Camera with `RenderTexture` for top-down view     |

---

## Appendix A: Quick-Reference Coordinates

| Element       | Position           | Scale             |
|---------------|--------------------|-------------------|
| Entrance      | center (0,1.5,-20) | (5,3,5)           |
| Corridor 1    | center (0,1.5,-16) | (3,3,3)           |
| Reception     | center (0,1.5,-12) | (5,3,5)           |
| Corridor 2    | center (0,1.5,-8)  | (3,3,3)           |
| Exhibit Room  | center (0,1.5,-4)  | (5,3,5)           |
| Corridor 3    | center (0,1.5,0)   | (3,3,3)           |
| Security Room | center (0,1.5,4)   | (5,3,5)           |
| Corridor 4    | center (0,1.5,8)   | (3,3,3)           |
| Vault         | center (0,1.5,12)  | (5,3,5)           |
| Corridor 5    | center (0,1.5,16)  | (3,3,3)           |
| Exit          | center (0,1.5,20)  | (5,3,5)           |

---

## Appendix B: Script API Reference

| Script                | Public Methods                                      |
|-----------------------|-----------------------------------------------------|
| `GuardPatrol`         | *(none — runs automatically)*                       |
| `SecurityCamera`      | `SetActiveState(bool state)`                        |
| `LockedDoor`          | `Unlock()`, `Open()`, `Close()`, `Toggle()`, `Interact()` |
| `ComputerTerminal`    | `Interact()`                                        |
| `KeycardPickup`       | `Interact()`                                        |
| `ArtifactCollectible` | `Interact()`                                        |
| `PlayerController`    | *(none — runs automatically)*                       |
| `PlayerInteraction`   | *(none — runs automatically)*                       |

All interactable objects implement `IInteractable` and respond to the **E** key when the player is looking at them within range.
