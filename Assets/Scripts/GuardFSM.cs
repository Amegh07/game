using UnityEngine;
using System.Collections.Generic;
using MuseumHeist.AccessControl;

public class GuardFSM : MonoBehaviour
{
    public enum GuardState
    {
        Patrol,
        Suspicious,
        Investigate,
        Search,
        Chase,
        ReturnToPatrol
    }

    [Header("Elite")]
    public bool isElite;

    [Header("State")]
    public GuardState currentState = GuardState.Patrol;
    public Transform player;

    [Header("Patrol")]
    public Transform[] waypoints;
    public float patrolSpeed = 2f;
    public float patrolRotateSpeed = 5f;
    public bool loopPatrol = true;
    [SerializeField] private float patrolLookInterval = 3f;
    [SerializeField] private int patrolPhaseOffset = 0;

    [Header("Vision")]
    public float visionRange = 10f;
    public float visionAngle = 60f;
    public float eyeHeight = 0.7f;
    public LayerMask visionMask = -1;

    [Header("Detection")]
    public float suspicionTime = 2f;
    public float suspicionDecayRate = 1f;
    [SerializeField] private float suspicionMeter = 0f;

    [Header("Alert")]
    [SerializeField] private float alertDecayRate = 2f;
    [SerializeField] private float visionAlertRate = 15f;
    [SerializeField] private float noiseAlertFactor = 40f;

    [Header("Hearing")]
    [SerializeField] private float hearingRadius = 15f;
    [SerializeField] [Range(0f, 1f)] private float alertThreshold = 0.7f;
    [SerializeField] private float reactionDelay = 0.3f;
    [SerializeField] private float soundMemoryTime = 8f;

    [Header("Investigate")]
    public float investigateSpeed = 3f;

    [Header("Search")]
    public float searchDuration = 5f;
    public float searchTurnSpeed = 90f;
    [SerializeField] private int searchPointCount = 4;
    [SerializeField] private float searchRadius = 5f;
    [SerializeField] private float searchPointWaitTime = 2f;
    [SerializeField] private float searchMoveSpeed = 3f;

    [Header("Chase")]
    public float chaseSpeed = 5f;
    public float chaseRotateSpeed = 8f;
    public float chaseLostTime = 5f;

    [Header("Group Alert")]
    [SerializeField] private float groupAlertRadius = 12f;
    [SerializeField] private float groupAlertTransferRate = 15f;

    [Header("Environmental Awareness")]
    [SerializeField] private float envCheckInterval = 2f;

    [Header("Return To Patrol")]
    public float returnSpeed = 2f;

    [Header("Animation")]
    public Animator animator;
    public Renderer guardRenderer;

    [Header("Debug")]
    [SerializeField] private bool showHearingGizmos = true;
    [SerializeField] private bool showSearchGizmos = true;
    [SerializeField] private bool showAlertGizmo = true;

    // --- private state ---
    private int currentWaypointIndex = 0;
    private int patrolDirection = 1;
    private Vector3 lastKnownPosition;
    private float chaseLostTimer = 0f;
    private float searchTimer = 0f;
    private int searchLookDir = 1;
    private bool playerInSight = false;
    private float initYRot;
    private CharacterController characterController;
    private Material cachedGuardMaterial;

    // --- alert ---
    private float alertLevel = 0f;

    // --- hearing state ---
    private Vector3? pendingNoisePosition;
    private float pendingNoiseStrength;
    private NoiseType pendingNoiseType;
    private float pendingNoiseTimer;
    private Vector3 lastHeardPosition;
    private float hearingMemoryTimer;
    private float currentNoiseStrength;

    // --- dynamic search points ---
    private List<Vector3> searchPoints = new List<Vector3>();
    private int currentSearchPointIndex = 0;
    private float searchPointWaitTimer = 0f;
    private bool waitingAtSearchPoint = false;
    private Vector3 searchCenter;

    // --- patrol look-around ---
    private float patrolLookTimer = 0f;
    private bool patrolIsLooking = false;
    private Quaternion patrolLookStartRot;
    private Quaternion patrolLookTargetRot;
    private float patrolLookDuration = 1.5f;
    private float patrolLookElapsed = 0f;

    // --- environment ---
    private float envCheckTimer = 0f;
    private HashSet<DoorController> observedOpenDoors = new HashSet<DoorController>();
    private bool observedArtifactStolen = false;
    private bool observedAlarmActive = false;

    // --- group alert ---
    private List<GuardFSM> nearbyGuardsBuffer = new List<GuardFSM>();
    private float lastGroupAlertTime = 0f;

    // --- memory ---
    private float timeSinceSeen = Mathf.Infinity;
    private float timeSinceHeard = Mathf.Infinity;
    private bool eventsSubscribed = false;

    // --- Animation state enum ---
    public enum AnimState
    {
        Idle   = 0,
        Walk   = 1,
        Run    = 2,
        Search = 3,
        Alert  = 4
    }

    // --- events ---
    public System.Action<GuardState, GuardState> OnStateChanged;
    public System.Action OnPlayerSpotted;
    public System.Action OnPlayerLost;

    // --- public animation hooks ---
    public float AlertLevel => alertLevel;
    public GuardState CurrentState => currentState;
    public Vector3 CurrentTarget => currentTarget;
    public bool IsSearching => currentState == GuardState.Search;
    public bool IsInvestigating => currentState == GuardState.Investigate;
    public Vector3 LookDirection => transform.forward;

    // --- internal target tracking for debug ---
    private Vector3 currentTarget;

    public LayerMask VisionMask => visionMask;
    public bool HasPlayerInSight => playerInSight;
    public float CurrentSuspicionMeter => suspicionMeter;
    public float DisplayAlertLevel => alertLevel;

    void OnEnable()
    {
        SubscribeToEvents();
    }

    void OnDisable()
    {
        UnsubscribeFromEvents();
    }

    void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void SubscribeToEvents()
    {
        if (eventsSubscribed) return;
        if (SecurityManager.Instance != null)
            SecurityManager.Instance.OnAlarmLevelChanged += HandleAlarmChange;
        if (MissionHUD.Instance != null)
            MissionHUD.Instance.RegisterGuard(this);
        eventsSubscribed = true;
    }

    private void UnsubscribeFromEvents()
    {
        if (!eventsSubscribed) return;
        if (SecurityManager.Instance != null)
            SecurityManager.Instance.OnAlarmLevelChanged -= HandleAlarmChange;
        if (NoiseManager.Instance != null)
            NoiseManager.Instance.UnregisterGuard(this);
        if (MissionHUD.Instance != null)
            MissionHUD.Instance.UnregisterGuard(this);
        eventsSubscribed = false;
    }

    void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
        if (guardRenderer == null)
            guardRenderer = GetComponent<Renderer>();
        if (guardRenderer != null)
            cachedGuardMaterial = guardRenderer.material;
        if (characterController == null)
            characterController = GetComponent<CharacterController>();

        if (SecurityManager.Instance != null)
            SecurityManager.Instance.RegisterGuard(this);

        if (NoiseManager.Instance != null)
            NoiseManager.Instance.RegisterGuard(this);

        if (waypoints != null && waypoints.Length > 0 && waypoints[0] != null)
        {
            if (patrolPhaseOffset > 0 && waypoints.Length > 1)
            {
                currentWaypointIndex = patrolPhaseOffset % waypoints.Length;
                transform.position = waypoints[currentWaypointIndex].position;
            }
            else
            {
                transform.position = waypoints[0].position;
            }
        }

        if (isElite)
        {
            patrolSpeed = Mathf.Max(patrolSpeed, 2.5f);
            visionRange = Mathf.Max(visionRange, 14f);
            visionAngle = Mathf.Max(visionAngle, 80f);
            suspicionTime = Mathf.Min(suspicionTime, 1.2f);
            chaseSpeed = Mathf.Max(chaseSpeed, 6.5f);
            hearingRadius = Mathf.Max(hearingRadius, 18f);
            if (guardRenderer != null)
            {
                if (cachedGuardMaterial == null)
                    cachedGuardMaterial = guardRenderer.material;
                cachedGuardMaterial.color = new Color(0.6f, 0f, 0f);
            }
        }

        UpdateAnimator();
    }

    void Update()
    {
        ProcessHearing();
        UpdateAlert();
        CheckEnvironment();
        DetectPlayer();
        UpdateCurrentState();
        UpdateMemoryTimers();
    }

    // ----------------------------------------------------------------
    //  Alert Level
    // ----------------------------------------------------------------

    private void UpdateAlert()
    {
        if (playerInSight)
            alertLevel = Mathf.Min(100f, alertLevel + visionAlertRate * Time.deltaTime);

        alertLevel = Mathf.Max(0f, alertLevel - alertDecayRate * Time.deltaTime);
    }

    private void AddAlert(float amount)
    {
        alertLevel = Mathf.Min(100f, alertLevel + amount);
    }

    // ----------------------------------------------------------------
    //  Hearing
    // ----------------------------------------------------------------

    public void HearNoise(Vector3 position, float perceivedStrength, NoiseType noiseType)
    {
        if (currentState == GuardState.Chase) return;

        pendingNoisePosition = position;
        pendingNoiseStrength = perceivedStrength;
        pendingNoiseType = noiseType;
        pendingNoiseTimer = reactionDelay;
    }

    private void ProcessHearing()
    {
        if (pendingNoisePosition == null) return;

        pendingNoiseTimer -= Time.deltaTime;
        if (pendingNoiseTimer > 0f) return;

        ReactToNoise(pendingNoisePosition.Value, pendingNoiseStrength, pendingNoiseType);
        pendingNoisePosition = null;
    }

    private void ReactToNoise(Vector3 position, float strength, NoiseType noiseType)
    {
        lastHeardPosition = position;
        hearingMemoryTimer = soundMemoryTime;
        timeSinceHeard = 0f;
        currentNoiseStrength = strength;
        AddAlert(strength * noiseAlertFactor);

        if (noiseType == NoiseType.Alarm)
        {
            lastKnownPosition = position;
            ChangeState(GuardState.Suspicious);
            return;
        }

        switch (currentState)
        {
            case GuardState.Patrol:
            case GuardState.ReturnToPatrol:
                lastKnownPosition = position;
                ChangeState(GuardState.Investigate);
                break;

            case GuardState.Investigate:
                if (strength >= alertThreshold && playerInSight)
                    ChangeState(GuardState.Chase);
                else
                    lastKnownPosition = position;
                break;

            case GuardState.Search:
                if (strength >= alertThreshold && playerInSight)
                    ChangeState(GuardState.Chase);
                else
                {
                    searchCenter = position;
                    lastKnownPosition = position;
                    searchTimer = 0f;
                    ChangeState(GuardState.Investigate);
                }
                break;

            case GuardState.Suspicious:
                if (strength >= alertThreshold)
                {
                    lastKnownPosition = position;
                    if (playerInSight)
                        ChangeState(GuardState.Chase);
                }
                break;
        }
    }

    // ----------------------------------------------------------------
    //  Detection
    // ----------------------------------------------------------------

    void DetectPlayer()
    {
        playerInSight = false;
        if (player == null) return;

        Vector3 eyePos = transform.position + Vector3.up * eyeHeight;
        Vector3 toPlayer = player.position - eyePos;
        float dist = toPlayer.magnitude;

        if (dist > visionRange || dist < 0.01f) return;

        float angle = Vector3.Angle(transform.forward, toPlayer.normalized);
        if (angle > visionAngle * 0.5f) return;

        if (Physics.Raycast(eyePos, toPlayer.normalized, out RaycastHit hit, dist, visionMask))
        {
            if (hit.transform == player || hit.transform.IsChildOf(player))
            {
                playerInSight = true;
                timeSinceSeen = 0f;
                lastKnownPosition = player.position;
            }
        }
    }

    // ----------------------------------------------------------------
    //  Environmental Awareness
    // ----------------------------------------------------------------

    private void CheckEnvironment()
    {
        envCheckTimer -= Time.deltaTime;
        if (envCheckTimer > 0f) return;
        envCheckTimer = envCheckInterval;

        if (currentState == GuardState.Chase) return;

        Collider[] hits = Physics.OverlapSphere(transform.position, visionRange, visionMask);
        for (int i = 0; i < hits.Length; i++)
        {
            var door = hits[i].GetComponentInParent<DoorController>();
            if (door != null && door.CurrentState == DoorState.Open && !observedOpenDoors.Contains(door))
            {
                observedOpenDoors.Add(door);
                AddAlert(15f);
                if (currentState == GuardState.Patrol || currentState == GuardState.ReturnToPatrol)
                {
                    lastKnownPosition = door.transform.position;
                    ChangeState(GuardState.Investigate);
                }
            }
        }

        if (!observedArtifactStolen && MissionManager.Instance != null && MissionManager.Instance.isInEscapePhase)
        {
            observedArtifactStolen = true;
            AddAlert(30f);
        }

        if (!observedAlarmActive && SecurityManager.Instance != null && SecurityManager.Instance.currentAlarmLevel >= SecurityManager.AlarmLevel.Alert)
        {
            observedAlarmActive = true;
            AddAlert(25f);
            SecurityManager.Instance.ReportTrigger(SecurityManager.SecurityTrigger.MultipleGuardsAlerted, gameObject.name);
            if (currentState == GuardState.Patrol || currentState == GuardState.ReturnToPatrol)
                ChangeState(GuardState.Suspicious);
        }
    }

    // ----------------------------------------------------------------
    //  Group Alert
    // ----------------------------------------------------------------

    private void PropagateGroupAlert()
    {
        if (Time.time - lastGroupAlertTime < 1f) return;
        lastGroupAlertTime = Time.time;

        NoiseManager.GetGuardsInRadius(transform.position, groupAlertRadius, nearbyGuardsBuffer);
        for (int i = 0; i < nearbyGuardsBuffer.Count; i++)
        {
            GuardFSM other = nearbyGuardsBuffer[i];
            if (other == this || other == null) continue;
            if (other.currentState == GuardState.Chase) continue;

            other.AddAlert(groupAlertTransferRate);

            if (other.currentState == GuardState.Patrol || other.currentState == GuardState.ReturnToPatrol)
            {
                other.lastKnownPosition = lastKnownPosition;
                other.ChangeState(GuardState.Investigate);
            }
            else if (other.currentState == GuardState.Search || other.currentState == GuardState.Investigate)
            {
                if (alertLevel > 60f)
                {
                    other.lastKnownPosition = lastKnownPosition;
                    other.ChangeState(GuardState.Investigate);
                }
            }
        }
    }

    // ----------------------------------------------------------------
    //  State machine tick
    // ----------------------------------------------------------------

    void UpdateCurrentState()
    {
        switch (currentState)
        {
            case GuardState.Patrol:          UpdatePatrol(); break;
            case GuardState.Suspicious:      UpdateSuspicious(); break;
            case GuardState.Investigate:     UpdateInvestigate(); break;
            case GuardState.Search:          UpdateSearch(); break;
            case GuardState.Chase:           UpdateChase(); break;
            case GuardState.ReturnToPatrol:  UpdateReturnToPatrol(); break;
        }

        UpdateAnimator();
        UpdateGuardColor();
    }

    private void UpdateMemoryTimers()
    {
        if (!playerInSight) timeSinceSeen += Time.deltaTime;
        timeSinceHeard += Time.deltaTime;
        if (hearingMemoryTimer > 0f) hearingMemoryTimer -= Time.deltaTime;
    }

    // ----------------------------------------------------------------
    //  Patrol
    // ----------------------------------------------------------------

    void UpdatePatrol()
    {
        if (playerInSight)
        {
            suspicionMeter = 0f;
            ChangeState(GuardState.Suspicious);
            return;
        }

        if (waypoints == null || waypoints.Length == 0) return;

        if (patrolIsLooking)
        {
            patrolLookElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(patrolLookElapsed / patrolLookDuration);
            transform.rotation = Quaternion.Slerp(patrolLookStartRot, patrolLookTargetRot, t);
            if (t >= 1f)
            {
                patrolIsLooking = false;
                patrolLookTimer = patrolLookInterval;
            }
            return;
        }

        patrolLookTimer -= Time.deltaTime;
        if (patrolLookTimer <= 0f)
        {
            patrolLookTimer = 0f;
            patrolIsLooking = true;
            patrolLookStartRot = transform.rotation;
            patrolLookTargetRot = transform.rotation * Quaternion.Euler(0f, Random.Range(-60f, 60f), 0f);
            patrolLookElapsed = 0f;
            return;
        }

        Transform tgt = waypoints[currentWaypointIndex];
        if (tgt == null) return;

        MoveToward(tgt.position, patrolSpeed, patrolRotateSpeed);
        currentTarget = tgt.position;

        if (Vector3.Distance(transform.position, tgt.position) < 0.5f)
        {
            if (loopPatrol)
            {
                currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
            }
            else
            {
                if (currentWaypointIndex >= waypoints.Length - 1) patrolDirection = -1;
                else if (currentWaypointIndex <= 0) patrolDirection = 1;
                currentWaypointIndex += patrolDirection;
            }
        }
    }

    // ----------------------------------------------------------------
    //  Suspicious
    // ----------------------------------------------------------------

    void UpdateSuspicious()
    {
        if (playerInSight)
        {
            suspicionMeter += Time.deltaTime;
            lastKnownPosition = player.position;
            RotateToward(player.position, patrolRotateSpeed);
            currentTarget = player.position;

            if (suspicionMeter >= suspicionTime)
            {
                suspicionMeter = suspicionTime;
                ChangeState(GuardState.Chase);
            }
        }
        else
        {
            suspicionMeter -= Time.deltaTime * suspicionDecayRate;
            if (suspicionMeter <= 0f)
            {
                suspicionMeter = 0f;
                ChangeState(GuardState.ReturnToPatrol);
            }
        }
    }

    // ----------------------------------------------------------------
    //  Investigate
    // ----------------------------------------------------------------

    void UpdateInvestigate()
    {
        if (playerInSight)
        {
            ChangeState(GuardState.Chase);
            return;
        }

        MoveToward(lastKnownPosition, investigateSpeed, patrolRotateSpeed);
        currentTarget = lastKnownPosition;

        if (Vector3.Distance(transform.position, lastKnownPosition) < 0.5f)
        {
            searchCenter = lastKnownPosition;
            ChangeState(GuardState.Search);
        }
    }

    // ----------------------------------------------------------------
    //  Search (Dynamic Search Points)
    // ----------------------------------------------------------------

    void UpdateSearch()
    {
        if (playerInSight)
        {
            ChangeState(GuardState.Chase);
            return;
        }

        searchTimer += Time.deltaTime;

        if (searchTimer >= searchDuration)
        {
            searchTimer = 0f;
            ChangeState(GuardState.ReturnToPatrol);
            return;
        }

        if (searchPoints.Count == 0)
        {
            searchTimer = 0f;
            ChangeState(GuardState.ReturnToPatrol);
            return;
        }

        if (waitingAtSearchPoint)
        {
            searchPointWaitTimer -= Time.deltaTime;
            transform.Rotate(0f, searchTurnSpeed * Time.deltaTime, 0f);
            if (searchPointWaitTimer <= 0f)
            {
                waitingAtSearchPoint = false;
                currentSearchPointIndex++;
                if (currentSearchPointIndex >= searchPoints.Count)
                {
                    searchTimer = 0f;
                    ChangeState(GuardState.ReturnToPatrol);
                }
            }
            return;
        }

        Vector3 target = searchPoints[currentSearchPointIndex];
        MoveToward(target, searchMoveSpeed, patrolRotateSpeed);
        currentTarget = target;

        if (Vector3.Distance(transform.position, target) < 0.5f)
        {
            waitingAtSearchPoint = true;
            searchPointWaitTimer = searchPointWaitTime;
        }
    }

    // ----------------------------------------------------------------
    //  Chase
    // ----------------------------------------------------------------

    void UpdateChase()
    {
        if (playerInSight)
        {
            chaseLostTimer = 0f;
            lastKnownPosition = player.position;
            MoveToward(player.position, chaseSpeed, chaseRotateSpeed);
            currentTarget = player.position;
        }
        else
        {
            chaseLostTimer += Time.deltaTime;
            if (chaseLostTimer >= chaseLostTime)
            {
                chaseLostTimer = 0f;
                searchCenter = lastKnownPosition;
                ChangeState(GuardState.Search);
            }
            else
            {
                MoveToward(lastKnownPosition, chaseSpeed * 0.5f, chaseRotateSpeed);
                currentTarget = lastKnownPosition;
            }
        }
    }

    // ----------------------------------------------------------------
    //  Return to Patrol
    // ----------------------------------------------------------------

    void UpdateReturnToPatrol()
    {
        if (playerInSight)
        {
            suspicionMeter = 0f;
            ChangeState(GuardState.Suspicious);
            return;
        }

        if (waypoints == null || waypoints.Length == 0)
        {
            ChangeState(GuardState.Patrol);
            return;
        }

        int nearest = 0;
        float nearestDist = float.MaxValue;
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null) continue;
            float d = Vector3.Distance(transform.position, waypoints[i].position);
            if (d < nearestDist) { nearestDist = d; nearest = i; }
        }

        currentWaypointIndex = nearest;
        Transform target = waypoints[nearest];
        if (target == null) return;

        MoveToward(target.position, returnSpeed, patrolRotateSpeed);
        currentTarget = target.position;

        if (Vector3.Distance(transform.position, target.position) < 0.5f)
            ChangeState(GuardState.Patrol);
    }

    // ----------------------------------------------------------------
    //  Search Points Generation
    // ----------------------------------------------------------------

    private void GenerateSearchPoints()
    {
        searchPoints.Clear();
        currentSearchPointIndex = 0;
        waitingAtSearchPoint = false;

        for (int i = 0; i < searchPointCount; i++)
        {
            Vector2 offset = Random.insideUnitCircle * searchRadius;
            Vector3 point = searchCenter + new Vector3(offset.x, 0f, offset.y);

            if (Physics.Raycast(point + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 4f, visionMask))
                point.y = hit.point.y;
            else
                point.y = searchCenter.y;

            searchPoints.Add(point);
        }
    }

    // ----------------------------------------------------------------
    //  Movement helpers
    // ----------------------------------------------------------------

    void MoveToward(Vector3 target, float speed, float rotateSpeed)
    {
        Vector3 dir = target - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) return;

        Vector3 step = dir.normalized * speed * Time.deltaTime;
        if (characterController != null)
            characterController.Move(step);
        else
            transform.position += step;

        Quaternion lookRot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, rotateSpeed * Time.deltaTime);
    }

    void RotateToward(Vector3 target, float speed)
    {
        Vector3 dir = target - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) return;

        Quaternion lookRot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, speed * Time.deltaTime);
    }

    // ----------------------------------------------------------------
    //  State transitions
    // ----------------------------------------------------------------

    void ChangeState(GuardState next)
    {
        GuardState prev = currentState;
        currentState = next;

        switch (next)
        {
            case GuardState.Search:
                searchTimer = 0f;
                searchLookDir = Random.value > 0.5f ? 1 : -1;
                initYRot = transform.eulerAngles.y;
                GenerateSearchPoints();
                break;
            case GuardState.Chase:
                chaseLostTimer = 0f;
                OnPlayerSpotted?.Invoke();
                PropagateGroupAlert();
                break;
            case GuardState.Investigate:
                OnPlayerLost?.Invoke();
                break;
        }

        OnStateChanged?.Invoke(prev, next);
        UpdateAnimator();
        NotifySecurityManager();
    }

    // ----------------------------------------------------------------
    //  SecurityManager integration
    // ----------------------------------------------------------------

    void HandleAlarmChange(SecurityManager.AlarmLevel oldLevel, SecurityManager.AlarmLevel newLevel)
    {
        if (newLevel > oldLevel && newLevel != SecurityManager.AlarmLevel.Recovery)
            AddAlert(15f * (int)(newLevel - oldLevel));

        switch (newLevel)
        {
            case SecurityManager.AlarmLevel.Suspicious:
                if (currentState == GuardState.Patrol || currentState == GuardState.ReturnToPatrol)
                    ChangeState(GuardState.Suspicious);
                break;

            case SecurityManager.AlarmLevel.Alert:
                if (currentState != GuardState.Chase && currentState != GuardState.Suspicious)
                    ChangeState(GuardState.Suspicious);
                break;

            case SecurityManager.AlarmLevel.Lockdown:
                if (currentState != GuardState.Chase)
                    ChangeState(GuardState.Suspicious);
                break;

            case SecurityManager.AlarmLevel.Recovery:
            case SecurityManager.AlarmLevel.Normal:
                if (currentState == GuardState.Suspicious)
                {
                    suspicionMeter = 0f;
                    ChangeState(GuardState.Investigate);
                }
                else if (currentState == GuardState.Investigate || currentState == GuardState.Search)
                {
                    searchTimer = 0f;
                    ChangeState(GuardState.ReturnToPatrol);
                }
                break;
        }
    }

    void NotifySecurityManager()
    {
        if (SecurityManager.Instance == null) return;

        switch (currentState)
        {
            case GuardState.Chase:
                SecurityManager.Instance.ReportTrigger(
                    SecurityManager.SecurityTrigger.GuardChase, gameObject.name);
                break;
            case GuardState.Suspicious:
                SecurityManager.Instance.ReportTrigger(
                    SecurityManager.SecurityTrigger.GuardSuspicious, gameObject.name);
                break;
        }
    }

    // ----------------------------------------------------------------
    //  Animation
    // ----------------------------------------------------------------

    void UpdateAnimator()
    {
        if (animator == null) return;
        animator.SetInteger("State", (int)FSMStateToAnimState(currentState));
        animator.SetFloat("AlertLevel", alertLevel / 100f);
    }

    AnimState FSMStateToAnimState(GuardState fs)
    {
        return fs switch
        {
            GuardState.Patrol         => alertLevel > 30f ? AnimState.Alert : AnimState.Walk,
            GuardState.Suspicious     => AnimState.Idle,
            GuardState.Investigate    => AnimState.Run,
            GuardState.Search         => AnimState.Search,
            GuardState.Chase          => AnimState.Run,
            GuardState.ReturnToPatrol => AnimState.Walk,
            _                         => AnimState.Idle,
        };
    }

    void UpdateGuardColor()
    {
        if (guardRenderer == null) return;

        Color c = currentState switch
        {
            GuardState.Patrol         => Color.blue,
            GuardState.Suspicious     => Color.yellow,
            GuardState.Investigate    => new Color(1f, 0.5f, 0f),
            GuardState.Search         => Color.magenta,
            GuardState.Chase          => Color.red,
            GuardState.ReturnToPatrol => Color.cyan,
            _                         => Color.blue,
        };

        if (cachedGuardMaterial == null)
            cachedGuardMaterial = guardRenderer.material;
        if (cachedGuardMaterial.color != c)
            cachedGuardMaterial.color = c;
    }

    // ----------------------------------------------------------------
    //  Debug
    // ----------------------------------------------------------------

    void OnDrawGizmos()
    {
        Vector3 eyePos = transform.position + Vector3.up * eyeHeight;

        if (!Application.isPlaying)
        {
            DrawVisionGizmo(eyePos);
            DrawWaypointGizmo();
            return;
        }

        DrawVisionGizmo(eyePos);
        DrawHearingGizmo();
        DrawWaypointGizmo();
        DrawInvestigationGizmo();
        DrawSearchGizmo();
        DrawAlertGizmo();
        DrawStateLabel();
    }

    private void DrawVisionGizmo(Vector3 eyePos)
    {
        Gizmos.color = playerInSight ? Color.red : Color.yellow;
        Vector3 fwd = transform.forward;
        Vector3 left = Quaternion.Euler(0f, -visionAngle * 0.5f, 0f) * fwd;
        Vector3 right = Quaternion.Euler(0f, visionAngle * 0.5f, 0f) * fwd;

        Gizmos.DrawLine(eyePos, eyePos + left * visionRange);
        Gizmos.DrawLine(eyePos, eyePos + right * visionRange);
        Gizmos.DrawLine(eyePos, eyePos + fwd * visionRange);

        Gizmos.color = new Color(1f, 1f, 0f, 0.08f);
        Vector3 leftEnd = eyePos + left * visionRange;
        Vector3 rightEnd = eyePos + right * visionRange;
        Vector3 fwdEnd = eyePos + fwd * visionRange;
        Gizmos.DrawLine(eyePos, leftEnd);
        Gizmos.DrawLine(eyePos, rightEnd);
        Gizmos.DrawLine(leftEnd, fwdEnd);
        Gizmos.DrawLine(rightEnd, fwdEnd);
    }

    private void DrawHearingGizmo()
    {
        if (!showHearingGizmos) return;

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.08f);
        Gizmos.DrawWireSphere(transform.position, hearingRadius);
    }

    private void DrawWaypointGizmo()
    {
        if (waypoints == null) return;

        Gizmos.color = Color.green;
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] != null)
            {
                Gizmos.DrawSphere(waypoints[i].position, 0.2f);
                int next = (i + 1) % waypoints.Length;
                if (waypoints[next] != null)
                    Gizmos.DrawLine(waypoints[i].position, waypoints[next].position);
            }
        }
    }

    private void DrawInvestigationGizmo()
    {
        if (currentState != GuardState.Investigate && currentState != GuardState.Search)
            return;

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(lastKnownPosition, 0.25f);
        Gizmos.DrawWireSphere(lastKnownPosition, 0.5f);

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
        Gizmos.DrawLine(transform.position, lastKnownPosition);

        if (showHearingGizmos && hearingMemoryTimer > 0f && Vector3.Distance(lastHeardPosition, lastKnownPosition) > 0.5f)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f);
            Gizmos.DrawSphere(lastHeardPosition, 0.2f);
            Gizmos.DrawWireSphere(lastHeardPosition, 0.4f);
        }
    }

    private void DrawSearchGizmo()
    {
        if (!showSearchGizmos || searchPoints.Count == 0) return;

        Gizmos.color = Color.magenta;
        for (int i = 0; i < searchPoints.Count; i++)
        {
            Gizmos.DrawSphere(searchPoints[i], 0.15f);
            if (i > 0)
                Gizmos.DrawLine(searchPoints[i - 1], searchPoints[i]);
        }

        Gizmos.color = new Color(1f, 0f, 1f, 0.1f);
        Gizmos.DrawWireSphere(searchCenter, searchRadius);
    }

    private void DrawAlertGizmo()
    {
        if (!showAlertGizmo) return;

        Vector3 meterPos = transform.position + Vector3.up * 2.2f;
        float w = 1f;
        float alertFrac = alertLevel / 100f;
        Gizmos.color = Color.Lerp(Color.green, Color.red, alertFrac);
        Gizmos.DrawWireCube(meterPos, new Vector3(w, 0.08f, 0.08f));
        Gizmos.DrawCube(meterPos, new Vector3(w * alertFrac, 0.08f, 0.08f));

        if (currentState == GuardState.Suspicious)
        {
            Vector3 suspPos = transform.position + Vector3.up * 2f;
            float sFrac = suspicionMeter / Mathf.Max(suspicionTime, 0.01f);
            Gizmos.color = Color.Lerp(Color.yellow, Color.red, sFrac);
            Gizmos.DrawWireCube(suspPos, new Vector3(w, 0.1f, 0.1f));
            Gizmos.DrawCube(suspPos, new Vector3(w * sFrac, 0.1f, 0.1f));
        }
    }

    private void DrawStateLabel()
    {
#if UNITY_EDITOR
        string info = $"{currentState} | Alert: {alertLevel:F0}%";
        if (currentState == GuardState.Suspicious)
            info += $" | Suspicion: {suspicionMeter:F1}/{suspicionTime:F1}";
        if (currentState == GuardState.Search)
            info += $" | Point: {currentSearchPointIndex + 1}/{searchPoints.Count}";
        if (showHearingGizmos && currentNoiseStrength > 0f)
            info += $" | Noise: {currentNoiseStrength:F2}";

        UnityEditor.Handles.Label(transform.position + Vector3.up * 2.5f, info);
#endif
    }
}
