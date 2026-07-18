# Migration Notes: Cyber Operations System

## Overview

The Cyber Operations System (`MuseumHeist.Cyber`) adds a realistic security terminal interface to Museum Heist. It uses enterprise cybersecurity concepts: authentication, authorization, RBAC, session management, command pattern, and a professional Security Console UI.

**No existing gameplay systems were modified** except two minimal additive changes:
- `SecurityManager.cs`: Added 2 events + 2 methods for door control routing (non-breaking)
- `DoorController.cs`: Added subscription to the new door events (necessary for integration)

## New Files

### Core (Assets/Scripts/Cyber/Core/)
| File | Purpose |
|------|---------|
| `UserRole.cs` | Enum: Guest, Maintenance, Staff, Curator, SecurityOfficer, Administrator |
| `CredentialType.cs` | Enum: Keycard, PIN, EmployeeCredentials, SmartCard, USBCertificate |
| `Permissions.cs` | 13 permission string constants (extensible) |
| `NetworkSession.cs` | Session data object with role, permissions, timestamps |

### Authentication (Assets/Scripts/Cyber/Authentication/)
| File | Purpose |
|------|---------|
| `AuthenticationService.cs` | Validates credentials, returns auth result with role |
| `CredentialManager.cs` | Singleton tracking collected credentials |
| `CredentialItem.cs` | Physical pickup (IInteractable) that adds credentials |

### Authorization (Assets/Scripts/Cyber/Authorization/)
| File | Purpose |
|------|---------|
| `PermissionSet.cs` | ScriptableObject holding a list of permission strings |
| `RolePermissionsConfig.cs` | ScriptableObject mapping UserRole → PermissionSet |
| `AuthorizationService.cs` | Builds permission set for authenticated role |

### Terminal (Assets/Scripts/Cyber/Terminal/)
| File | Purpose |
|------|---------|
| `TerminalConfig.cs` | ScriptableObject with terminal parameters, actions, connected systems |
| `TerminalController.cs` | Main terminal logic: connect, authenticate, execute actions |
| `TerminalConnectionPoint.cs` | Physical interactable (IInteractable) on terminal |
| `LaptopController.cs` | Player component, bridges between player and terminals |
| `TerminalLogEntry.cs` | Struct for audit log entries |
| `TerminalLog.cs` | In-memory audit log with max entry cap |

### Actions (Assets/Scripts/Cyber/Actions/)
| File | Purpose |
|------|---------|
| `ITerminalAction.cs` | Command pattern interface + TerminalActionContext |
| `ActionExecutor.cs` | Registry + executor for terminal actions |
| `TerminalActions.cs` | 8 concrete action implementations |

### UI (Assets/Scripts/Cyber/UI/)
| File | Purpose |
|------|---------|
| `SecurityConsoleUI.cs` | Professional OnGUI console interface |

### Editor (Assets/Editor/)
| File | Purpose |
|------|---------|
| `CyberSystemSetupEditor.cs` | Generates example ScriptableObject assets |

## Integration Points

### SecurityManager (modified — additive only)
- `OnDoorUnlockRequested` (event, string doorID) — fired by Terminal actions
- `OnDoorLockRequested` (event, string doorID) — fired by Terminal actions
- `RequestDoorUnlock(string)` — fires the event
- `RequestDoorLock(string)` — fires the event
- These route to `DoorController` subscribers (not legacy LockedDoor)

### DoorController (modified — additive only)
- New handlers `HandleDoorUnlockRequest` / `HandleDoorLockRequest`
- Subscribe to SecurityManager's new events
- Match by doorID: only react if the doorID matches

### SecurityManager (existing — unchanged)
- `DisableCamera(string id)` — called by DisableCameraAction
- `SetAlarmLevel(AlarmLevel)` — called by EmergencyLockdownAction, MaintenanceModeAction
- `ResetAlarm()` — called by ResetAlarmAction, ShutdownSecurityAction
- `LockAllDoors()` — called by ShutdownSecurityAction

### PlayerInteraction (existing — unchanged)
- `IInteractable` implemented by `TerminalConnectionPoint` and `CredentialItem`

### No changes to:
- GuardFSM, SecurityCamera, CameraConfig — untouched
- MissionManager — untouched
- PlayerController — untouched
- KeycardItem, InventoryManager, DoorConfig, DoorState, KeycardType — untouched

## Scene Setup

1. **Tools → Museum Heist → Cyber → Create Example Assets** (generates all SO assets)
2. Add `AuthenticationService` to scene
3. Add `AuthorizationService` to scene (assign RolePermissionsConfig)
4. Add `CredentialManager` to scene
5. Add `LaptopController` to PlayerCamera GameObject
6. Add `SecurityConsoleUI` to PlayerCamera GameObject (or a Canvas)
7. For each terminal: place a GameObject with `TerminalConnectionPoint`, `TerminalController`, and `TerminalLog`
8. Assign TerminalConfig to each TerminalController
9. For each keycard: use `CredentialItem` (instead of legacy KeycardItem for the new system)

## Flow

```
Player presses E on TerminalConnectionPoint
  → TerminalConnectionPoint.Interact()
  → LaptopController.Connect(terminal)
  → TerminalController accepts connection
  → Player selects credential (automatic via detection)
  → AuthenticationService.Authenticate(credentialID)
  → AuthorizationService.Authorize(role, terminal)
  → NetworkSession created with permissions
  → SecurityConsoleUI.Show(terminal)
  → Player clicks action button
  → TerminalController.ExecuteAction(entry)
  → ActionExecutor.ExecuteAction(entry, session)
  → Concrete action calls SecurityManager
  → Result logged to TerminalLog
  → Console UI refreshes
```

## Architecture Diagram

```
┌─────────────┐     ┌──────────────────┐     ┌──────────────────┐
│ Credential  │────>│ Authentication   │────>│ Authorization    │
│ Item        │     │ Service          │     │ Service          │
│ (pickup)    │     │                  │     │                  │
└─────────────┘     └──────────────────┘     └──────────────────┘
       │                    │                         │
       ▼                    ▼                         ▼
┌─────────────┐     ┌──────────────────┐     ┌──────────────────┐
│ Credential  │     │  NetworkSession  │     │  PermissionSet   │
│ Manager     │     │  (User, Role,    │     │  (RBAC config)   │
│             │     │   Permissions)   │     │                  │
└─────────────┘     └──────────────────┘     └──────────────────┘
                           │
                           ▼
┌─────────────┐     ┌──────────────────┐     ┌──────────────────┐
│ Terminal    │────>│ Terminal         │────>│ ActionExecutor   │
│ Connection  │     │ Controller       │     │ (Command Pattern)│
│ Point       │     │                  │     │                  │
└─────────────┘     └──────────────────┘     └──────────────────┘
       │                    │                         │
       ▼                    ▼                         ▼
┌─────────────┐     ┌──────────────────┐     ┌──────────────────┐
│ Laptop      │     │ SecurityConsole  │     │ TerminalAction   │
│ Controller  │     │ UI (OnGUI)       │     │ (concrete impl)  │
│ (on player) │     │                  │     │                  │
└─────────────┘     └──────────────────┘     └──────────────────┘
                                                    │
                                                    ▼
                                          ┌──────────────────┐
                                          │ SecurityManager  │
                                          │ (single source   │
                                          │  of truth)       │
                                          └──────────────────┘
                                                    │
                                          ┌─────────┴─────────┐
                                          ▼                   ▼
                                   ┌──────────┐      ┌──────────────┐
                                   │ Cameras  │      │ Doors        │
                                   │ (via     │      │ (via events) │
                                   │  Disable │      │              │
                                   │  Camera) │      │              │
                                   └──────────┘      └──────────────┘
```
