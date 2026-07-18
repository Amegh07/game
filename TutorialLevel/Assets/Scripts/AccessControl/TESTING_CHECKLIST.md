# Testing Checklist: Door & Keycard System

## Setup Tests

- [ ] `Tools → Museum Heist → Create Example Door Configs` generates 7 `.asset` files in `Assets/DoorConfigs/`
- [ ] Each DoorConfig asset opens in Inspector with all fields visible and editable
- [ ] `DoorController` component appears with doorID, config slot, and debug toggle
- [ ] `InventoryManager` singleton appears in scene hierarchy with DontDestroyOnLoad (if configured)

## Basic Door Tests

- [ ] Door starts in correct state (Locked if `startsLocked = true`, Unlocked if `false`)
- [ ] Interacting with a Locked door without the required keycard shows "ACCESS DENIED" feedback
- [ ] Interacting with a Locked door without the required keycard plays accessDeniedSound
- [ ] `OnAccessDenied` event fires with correct doorID and KeycardType
- [ ] Interacting with a Locked door WITH the required keycard shows "ACCESS GRANTED" feedback
- [ ] `OnDoorUnlocked` event fires
- [ ] Door transitions to Unlocked then Opening state
- [ ] Transform animation smoothly rotates door to openAngle over openSpeed duration
- [ ] `OnDoorOpened` event fires after animation completes
- [ ] Door auto-closes after autoCloseDelay seconds (if canAutoClose = true)
- [ ] `OnDoorClosed` event fires, then `OnDoorLocked` if startsLocked was true
- [ ] Interacting with an Open door immediately closes it (cancels auto-close timer)
- [ ] Cancelled auto-close does not fire OnDoorOpened again

## Edge Case Tests

- [ ] Rapid E-spam (repeated Interact calls) does not double-animate or break state machine
- [ ] Door with no DoorConfig assigned logs a warning and does nothing on Interact
- [ ] Door with no AudioSource plays no sounds (no NRE)
- [ ] DoorUIFeedback with no instance logs no errors (null-conditional operator)
- [ ] InventoryManager with no instance — KeycardItem silently fails (no NRE)
- [ ] Player can interact from any angle within interactionRange
- [ ] Door does not open when in LockedDown or Disabled state

## Keycard Tests

- [ ] KeycardItem floats and rotates when active
- [ ] Interacting with KeycardItem adds correct KeycardType to InventoryManager
- [ ] `OnKeycardAdded` event fires on InventoryManager
- [ ] KeycardItem disappears (Destroy or SetActive(false)) after pickup
- [ ] Multiple keycards — InventoryManager tracks all collected types
- [ ] InventoryManager.HasKeycard returns true for collected, false for uncollected
- [ ] InventoryManager.Clear() empties all keycards

## Lockdown / Security Tests

- [ ] Door with `lockOnAlert = true` locks when SecurityManager alarm reaches Alert
- [ ] Door with `lockOnAlert = false` ignores Alert level
- [ ] Door with `lockDuringLockdown = true` transitions to LockedDown when SecurityManager reaches Lockdown
- [ ] LockedDown door shows magenta gizmo and cannot be interacted with
- [ ] Door with `isEmergencyExit = true` unlocks and opens during Lockdown
- [ ] Door with `lockDuringLockdown = false` ignores Lockdown (e.g., public doors)
- [ ] Door returns to normal state when SecurityManager alarm returns to Normal
- [ ] Door with `startsLocked = true` re-locks after lockdown ends
- [ ] Door with `startsLocked = false` stays unlocked after lockdown ends

## Animation Tests

- [ ] Door with no Animator uses transform-based rotation (Slerp)
- [ ] Door with Animator controller assigned triggers openTrigger/closeTrigger parameters
- [ ] Animator controller clip length determines animation duration (matched by name)
- [ ] `stopRotationOnDetection` (CameraConfig) does NOT affect doors — doors have their own animation

## Gizmo Tests

- [ ] Wireframe cube appears around door in Scene view
- [ ] Wireframe color matches state (red = Locked, green = Unlocked, yellow = Opening, cyan = Open, orange = Closing, gray = Disabled, magenta = LockedDown)
- [ ] State label shows doorID, doorName, CurrentState, requiredKeycard
- [ ] Open direction ray appears in Edit mode
- [ ] Gizmos toggle off when `showGizmos = false`

## Stress Tests

- [ ] 50+ doors in a single scene — no frame-rate issues
- [ ] 10+ doors animating simultaneously — no coroutine leaks
- [ ] All doors subscribing to SecurityManager — no duplicate event subscriptions
- [ ] Keycard collected → 50 doors with that keycard unlock correctly
- [ ] Rapid alarm level changes (Normal → Alert → Lockdown → Normal → ...) — doors follow correctly

## Regression Tests

- [ ] Existing LockedDoor (legacy system) continues to work
- [ ] TutorialLevelGenerator still generates scene with LockedDoor
- [ ] Existing SecurityManager.UnlockDoor/LockDoor still work with LockedDoor instances
- [ ] Existing GuardFSM alarm responses unchanged
- [ ] Existing SecurityCamera detection unchanged
