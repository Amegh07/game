using UnityEngine;
using System.Collections;

namespace MuseumHeist.AccessControl
{
    [RequireComponent(typeof(BoxCollider))]
    public class DoorController : MonoBehaviour, IInteractable
    {
        [SerializeField] private string doorID = "";
        [SerializeField] private DoorConfig config;

        [Header("Debug")]
        [SerializeField] private bool showGizmos = true;

        public DoorState CurrentState { get; private set; } = DoorState.Locked;

        public event System.Action<string> OnDoorOpened;
        public event System.Action<string> OnDoorClosed;
        public event System.Action<string> OnDoorLocked;
        public event System.Action<string> OnDoorUnlocked;
        public event System.Action<string, KeycardType> OnAccessDenied;

        private Quaternion closedRotation;
        private Quaternion openRotation;
        private Coroutine animationRoutine;
        private Coroutine autoCloseRoutine;
        private Animator doorAnimator;
        private AudioSource audioSource;
        private Collider doorCollider;
        private bool initialized;

        void Awake()
        {
            doorAnimator = GetComponent<Animator>();
            audioSource = GetComponent<AudioSource>();
            doorCollider = GetComponent<BoxCollider>();
            animationRoutine = null;
            autoCloseRoutine = null;
        }

        void Start()
        {
            closedRotation = transform.localRotation;
            openRotation = closedRotation * Quaternion.Euler(0f, config != null ? config.openAngle : 90f, 0f);

            if (config != null)
            {
                CurrentState = config.startsLocked ? DoorState.Locked : DoorState.Unlocked;
            }
            else
            {
                CurrentState = DoorState.Locked;
            }

            initialized = true;
            SubscribeToSecurityManager();
        }

        void OnEnable()
        {
            if (initialized) SubscribeToSecurityManager();
        }

        void OnDisable()
        {
            UnsubscribeFromSecurityManager();
        }

        void OnDestroy()
        {
            UnsubscribeFromSecurityManager();
            StopAllCoroutines();
        }

        private void SubscribeToSecurityManager()
        {
            if (SecurityManager.Instance != null)
            {
                SecurityManager.Instance.OnAlarmLevelChanged -= HandleAlarmChange;
                SecurityManager.Instance.OnAlarmLevelChanged += HandleAlarmChange;
                SecurityManager.Instance.OnDoorUnlockRequested -= HandleDoorUnlockRequest;
                SecurityManager.Instance.OnDoorUnlockRequested += HandleDoorUnlockRequest;
                SecurityManager.Instance.OnDoorLockRequested -= HandleDoorLockRequest;
                SecurityManager.Instance.OnDoorLockRequested += HandleDoorLockRequest;
            }
        }

        private void UnsubscribeFromSecurityManager()
        {
            if (SecurityManager.Instance != null)
            {
                SecurityManager.Instance.OnAlarmLevelChanged -= HandleAlarmChange;
                SecurityManager.Instance.OnDoorUnlockRequested -= HandleDoorUnlockRequest;
                SecurityManager.Instance.OnDoorLockRequested -= HandleDoorLockRequest;
            }
        }

        private void HandleDoorUnlockRequest(string targetDoorID)
        {
            if (doorID != targetDoorID) return;
            if (CurrentState == DoorState.LockedDown || CurrentState == DoorState.Disabled) return;

            SetState(DoorState.Unlocked);
            OnDoorUnlocked?.Invoke(doorID);
            BeginOpen();
        }

        private void HandleDoorLockRequest(string targetDoorID)
        {
            if (doorID != targetDoorID) return;
            if (CurrentState == DoorState.LockedDown) return;

            CancelCurrentAnimation();
            CancelAutoClose();
            SetState(DoorState.Locked);
            OnDoorLocked?.Invoke(doorID);
        }

        public void Interact()
        {
            if (config == null)
            {
                Debug.LogWarning($"DoorController '{doorID}': No DoorConfig assigned.", this);
                return;
            }

            switch (CurrentState)
            {
                case DoorState.Locked:
                    TryUnlock();
                    break;

                case DoorState.Unlocked:
                    BeginOpen();
                    break;

                case DoorState.Open:
                    BeginClose();
                    break;

                case DoorState.LockedDown:
                    PlaySound(config.accessDeniedSound);
                    OnAccessDenied?.Invoke(doorID, config.requiredKeycard);
                    DoorUIFeedback.Instance?.ShowDoorLocked(config.doorName);
                    break;

                case DoorState.Disabled:
                    break;

                default:
                    break;
            }
        }

        private void TryUnlock()
        {
            bool hasRequiredCard = InventoryManager.Instance != null &&
                                   InventoryManager.Instance.HasKeycard(config.requiredKeycard);

            if (hasRequiredCard)
            {
                SetState(DoorState.Unlocked);
                OnDoorUnlocked?.Invoke(doorID);
                PlaySound(config.unlockSound);
                DoorUIFeedback.Instance?.ShowAccessGranted(config.doorName);
                BeginOpen();
            }
            else
            {
                PlaySound(config.accessDeniedSound);
                OnAccessDenied?.Invoke(doorID, config.requiredKeycard);
                DoorUIFeedback.Instance?.ShowAccessDenied(config.doorName, config.requiredKeycard);
            }
        }

        private void BeginOpen()
        {
            if (CurrentState == DoorState.LockedDown || CurrentState == DoorState.Disabled) return;
            if (CurrentState == DoorState.Opening || CurrentState == DoorState.Open) return;

            SetState(DoorState.Opening);
            CancelCurrentAnimation();
            animationRoutine = StartCoroutine(AnimateOpen());
        }

        private void BeginClose()
        {
            if (CurrentState == DoorState.Closing || CurrentState == DoorState.Locked) return;

            SetState(DoorState.Closing);
            CancelCurrentAnimation();
            CancelAutoClose();
            animationRoutine = StartCoroutine(AnimateClose());
        }

        private IEnumerator AnimateOpen()
        {
            PlaySound(config.openSound);

            if (doorAnimator != null && doorAnimator.runtimeAnimatorController != null)
            {
                doorAnimator.SetTrigger(config.openTrigger);
                yield return new WaitForSeconds(GetAnimationLength(config.openTrigger));
            }
            else
            {
                float duration = config.openAngle / Mathf.Max(config.openSpeed, 0.1f);
                float elapsed = 0f;

                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);
                    transform.localRotation = Quaternion.Slerp(closedRotation, openRotation, t);
                    yield return null;
                }

                transform.localRotation = openRotation;
            }

            if (CurrentState != DoorState.Disabled && CurrentState != DoorState.LockedDown)
            {
                SetState(DoorState.Open);
                OnDoorOpened?.Invoke(doorID);

                if (config.canAutoClose)
                {
                    autoCloseRoutine = StartCoroutine(AutoCloseTimer());
                }
            }
        }

        private IEnumerator AnimateClose()
        {
            PlaySound(config.closeSound);

            if (doorAnimator != null && doorAnimator.runtimeAnimatorController != null)
            {
                doorAnimator.SetTrigger(config.closeTrigger);
                yield return new WaitForSeconds(GetAnimationLength(config.closeTrigger));
            }
            else
            {
                float duration = config.openAngle / Mathf.Max(config.closeSpeed, 0.1f);
                float elapsed = 0f;

                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);
                    transform.localRotation = Quaternion.Slerp(openRotation, closedRotation, t);
                    yield return null;
                }

                transform.localRotation = closedRotation;
            }

            if (CurrentState == DoorState.Disabled) yield break;

            bool shouldLock = config.startsLocked;
            SetState(shouldLock ? DoorState.Locked : DoorState.Unlocked);
            OnDoorClosed?.Invoke(doorID);

            if (shouldLock)
            {
                PlaySound(config.lockSound);
                OnDoorLocked?.Invoke(doorID);
            }
        }

        private IEnumerator AutoCloseTimer()
        {
            yield return new WaitForSeconds(config.autoCloseDelay);

            if (CurrentState == DoorState.Open)
            {
                BeginClose();
            }
        }

        private void CancelCurrentAnimation()
        {
            if (animationRoutine != null)
            {
                StopCoroutine(animationRoutine);
                animationRoutine = null;
            }
        }

        private void CancelAutoClose()
        {
            if (autoCloseRoutine != null)
            {
                StopCoroutine(autoCloseRoutine);
                autoCloseRoutine = null;
            }
        }

        private void SetState(DoorState newState)
        {
            CurrentState = newState;
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        private float GetAnimationLength(string trigger)
        {
            if (doorAnimator == null || doorAnimator.runtimeAnimatorController == null)
            {
                return 0.3f;
            }

            AnimationClip[] clips = doorAnimator.runtimeAnimatorController.animationClips;
            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i].name.IndexOf(trigger, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return clips[i].length;
                }
            }

            return 0.3f;
        }

        private void HandleAlarmChange(SecurityManager.AlarmLevel oldLevel, SecurityManager.AlarmLevel newLevel)
        {
            if (config == null) return;

            switch (newLevel)
            {
                case SecurityManager.AlarmLevel.Alert:
                    HandleAlert();
                    break;

                case SecurityManager.AlarmLevel.Lockdown:
                    HandleLockdown();
                    break;

                case SecurityManager.AlarmLevel.Normal:
                    HandleNormal();
                    break;
            }
        }

        private void HandleAlert()
        {
            if (!config.lockOnAlert) return;
            if (CurrentState == DoorState.LockedDown || CurrentState == DoorState.Locked) return;

            if (CurrentState == DoorState.Open)
            {
                BeginClose();
            }
            else if (CurrentState == DoorState.Unlocked)
            {
                SetState(DoorState.Locked);
                OnDoorLocked?.Invoke(doorID);
            }
        }

        private void HandleLockdown()
        {
            if (config.isEmergencyExit)
            {
                if (CurrentState == DoorState.Locked || CurrentState == DoorState.LockedDown)
                {
                    SetState(DoorState.Unlocked);
                    OnDoorUnlocked?.Invoke(doorID);
                }

                if (CurrentState != DoorState.Open && CurrentState != DoorState.Opening)
                {
                    BeginOpen();
                }
            }
            else if (config.lockDuringLockdown)
            {
                CancelCurrentAnimation();
                CancelAutoClose();

                SetState(DoorState.LockedDown);
                OnDoorLocked?.Invoke(doorID);

                if (doorAnimator == null || doorAnimator.runtimeAnimatorController == null)
                {
                    transform.localRotation = closedRotation;
                }
            }
        }

        private void HandleNormal()
        {
            if (CurrentState == DoorState.LockedDown)
            {
                SetState(config.startsLocked ? DoorState.Locked : DoorState.Unlocked);

                if (!config.startsLocked)
                {
                    OnDoorUnlocked?.Invoke(doorID);
                }
            }
        }

        void OnDrawGizmos()
        {
            if (!showGizmos || config == null) return;

            Color baseColor = config.debugColor;
            Color stateColor = CurrentState switch
            {
                DoorState.Locked     => Color.red,
                DoorState.Unlocked   => Color.green,
                DoorState.Opening    => Color.yellow,
                DoorState.Open       => Color.cyan,
                DoorState.Closing    => new Color(1f, 0.5f, 0f),
                DoorState.Disabled   => Color.gray,
                DoorState.LockedDown => Color.magenta,
                _                    => baseColor
            };

            Gizmos.color = stateColor;

            BoxCollider bc = GetComponent<BoxCollider>();
            if (bc != null)
            {
                Matrix4x4 matrix = Gizmos.matrix;
                Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
                Gizmos.DrawWireCube(bc.center, bc.size);
                Gizmos.matrix = matrix;
            }
            else
            {
                Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);
            }

            if (!Application.isPlaying)
            {
                Gizmos.color = stateColor * 0.5f;
                Vector3 openDir = Quaternion.Euler(0f, config.openAngle, 0f) * transform.forward;
                Gizmos.DrawRay(transform.position, openDir * 1.2f);
                Gizmos.DrawSphere(transform.position + openDir * 1.2f, 0.08f);
            }

#if UNITY_EDITOR
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 2.2f,
                $"[{doorID}] {config.doorName}\nState: {CurrentState}  Key: {config.requiredKeycard}"
            );
#endif
        }

        public void AssignConfig(DoorConfig newConfig)
        {
            config = newConfig;
        }
    }
}
