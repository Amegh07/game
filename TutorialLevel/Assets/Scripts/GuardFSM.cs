using UnityEngine;

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

    [Header("State")]
    public GuardState currentState = GuardState.Patrol;
    public Transform player;

    [Header("Patrol")]
    public Transform[] waypoints;
    public float patrolSpeed = 2f;
    public float patrolRotateSpeed = 5f;
    public bool loopPatrol = true;

    [Header("Vision")]
    public float visionRange = 10f;
    public float visionAngle = 60f;
    public float eyeHeight = 0.7f;
    public LayerMask visionMask = -1;

    [Header("Detection")]
    public float suspicionTime = 2f;
    public float suspicionDecayRate = 1f;
    [SerializeField] private float suspicionMeter = 0f;

    [Header("Investigate")]
    public float investigateSpeed = 3f;

    [Header("Search")]
    public float searchDuration = 5f;
    public float searchTurnSpeed = 90f;

    [Header("Chase")]
    public float chaseSpeed = 5f;
    public float chaseRotateSpeed = 8f;
    public float chaseLostTime = 3f;

    [Header("Return To Patrol")]
    public float returnSpeed = 2f;

    [Header("Animation")]
    public Animator animator;
    public Renderer guardRenderer;

    // --- private state ---
    private int currentWaypointIndex = 0;
    private int patrolDirection = 1;
    private Vector3 lastKnownPosition;
    private float chaseLostTimer = 0f;
    private float searchTimer = 0f;
    private int searchLookDir = 1;
    private bool playerInSight = false;
    private float initYRot;

    // --- Animation state enum ---
    public enum AnimState
    {
        Idle   = 0,   // Suspicious
        Walk   = 1,   // Patrol, ReturnToPatrol
        Run    = 2,   // Investigate, Chase
        Search = 3,   // Search
        Alert  = 4    // (reserved for future use — fully detected)
    }

    // --- events (for extensibility) ---
    public System.Action<GuardState, GuardState> OnStateChanged;
    public System.Action OnPlayerSpotted;
    public System.Action OnPlayerLost;

    void Awake()
    {
    }

    void OnEnable()
    {
        if (SecurityManager.Instance != null)
            SecurityManager.Instance.OnAlarmLevelChanged += HandleAlarmChange;
    }

    void OnDisable()
    {
        if (SecurityManager.Instance != null)
        {
            SecurityManager.Instance.OnAlarmLevelChanged -= HandleAlarmChange;
            SecurityManager.Instance.UnregisterGuard(this);
        }
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

        if (SecurityManager.Instance != null)
        {
            SecurityManager.Instance.RegisterGuard(this);
            SecurityManager.Instance.OnAlarmLevelChanged += HandleAlarmChange;
        }

        if (waypoints != null && waypoints.Length > 0 && waypoints[0] != null)
            transform.position = waypoints[0].position;

        UpdateAnimator();
    }

    void Update()
    {
        DetectPlayer();
        UpdateCurrentState();
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
                lastKnownPosition = player.position;
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

        Transform t = waypoints[currentWaypointIndex];
        if (t == null) return;

        MoveToward(t.position, patrolSpeed, patrolRotateSpeed);

        if (Vector3.Distance(transform.position, t.position) < 0.5f)
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
                ChangeState(GuardState.Investigate);
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

        if (Vector3.Distance(transform.position, lastKnownPosition) < 0.5f)
            ChangeState(GuardState.Search);
    }

    // ----------------------------------------------------------------
    //  Search
    // ----------------------------------------------------------------

    void UpdateSearch()
    {
        if (playerInSight)
        {
            ChangeState(GuardState.Chase);
            return;
        }

        searchTimer += Time.deltaTime;
        transform.Rotate(0f, searchLookDir * searchTurnSpeed * Time.deltaTime, 0f);

        if (searchTimer >= searchDuration)
        {
            searchTimer = 0f;
            ChangeState(GuardState.ReturnToPatrol);
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
        }
        else
        {
            chaseLostTimer += Time.deltaTime;
            if (chaseLostTimer >= chaseLostTime)
            {
                chaseLostTimer = 0f;
                ChangeState(GuardState.Investigate);
            }
            else
            {
                MoveToward(lastKnownPosition, chaseSpeed * 0.5f, chaseRotateSpeed);
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

        if (Vector3.Distance(transform.position, target.position) < 0.5f)
            ChangeState(GuardState.Patrol);
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
                break;
            case GuardState.Chase:
                chaseLostTimer = 0f;
                OnPlayerSpotted?.Invoke();
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
        }
    }

    void NotifySecurityManager()
    {
        if (SecurityManager.Instance == null) return;

        switch (currentState)
        {
            case GuardState.Chase:
                SecurityManager.Instance.SetAlarmLevel(SecurityManager.AlarmLevel.Alert);
                break;
            case GuardState.Suspicious:
                SecurityManager.Instance.SetAlarmLevel(SecurityManager.AlarmLevel.Suspicious);
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
    }

    AnimState FSMStateToAnimState(GuardState fs)
    {
        return fs switch
        {
            GuardState.Patrol         => AnimState.Walk,
            GuardState.Suspicious     => AnimState.Idle,
            GuardState.Investigate    => AnimState.Run,
            GuardState.Search         => AnimState.Search,
            GuardState.Chase          => AnimState.Run,
            GuardState.ReturnToPatrol => AnimState.Walk,
            _                         => AnimState.Idle,
        };
    }

    // Whitebox visual feedback — colour-codes the guard capsule per state
    void UpdateGuardColor()
    {
        if (guardRenderer == null) return;

        Color c = currentState switch
        {
            GuardState.Patrol         => Color.blue,
            GuardState.Suspicious     => Color.yellow,
            GuardState.Investigate    => new Color(1f, 0.5f, 0f),   // orange
            GuardState.Search         => Color.magenta,
            GuardState.Chase          => Color.red,
            GuardState.ReturnToPatrol => Color.cyan,
            _                         => Color.blue,
        };

        if (guardRenderer.material.color != c)
            guardRenderer.material.color = c;
    }

    // ----------------------------------------------------------------
    //  Debug
    // ----------------------------------------------------------------

    void OnDrawGizmos()
    {
        Vector3 eyePos = transform.position + Vector3.up * eyeHeight;

        // Vision cone
        Gizmos.color = playerInSight && Application.isPlaying ? Color.red : Color.yellow;
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

        // Waypoints
        if (waypoints != null)
        {
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

        // Last known position
        if (currentState == GuardState.Investigate || currentState == GuardState.Search)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(lastKnownPosition, 0.25f);
            Gizmos.DrawWireSphere(lastKnownPosition, 0.5f);
        }

        // Suspicion meter
        if (currentState == GuardState.Suspicious)
        {
            Gizmos.color = Color.Lerp(Color.yellow, Color.red, suspicionMeter / Mathf.Max(suspicionTime, 0.01f));
            Vector3 meterPos = transform.position + Vector3.up * 2f;
            float w = 1f;
            Gizmos.DrawWireCube(meterPos, new Vector3(w, 0.1f, 0.1f));
            Gizmos.DrawCube(meterPos, new Vector3(w * (suspicionMeter / Mathf.Max(suspicionTime, 0.01f)), 0.1f, 0.1f));
        }

        // State label
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2.5f,
            $"{currentState} {(currentState == GuardState.Suspicious ? $"({suspicionMeter:F1}/{suspicionTime:F1})" : "")}");
        #endif
    }
}
