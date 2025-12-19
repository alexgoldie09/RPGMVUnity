using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Ability that allows the Mage to spawn and grow an ice platform column beneath them,
/// implemented as a dedicated player state (e.g., PlayerStates.State.IceCast).
/// 
/// Flow:
/// - Input performed:
///     * Check conditions (grounded, no wall, correct class, ceiling clear, charges).
///     * Consume a charge.
///     * Spawn and initialize an IcePlatform under the player's feet.
///     * Change state machine to thisAbilityState (IceCast).
/// - While in IceCast:
///     * ProcessUpdateAbility moves the player with the platform,
///       handles timers, and calls GrowOneTile() at intervals.
///     * ProcessFixedAbility locks horizontal movement.
/// - Input canceled / max duration / max height:
///     * Casting stops, platform remains, charge is spent.
///     * State machine returns to the previous state.
/// </summary>
public class IcePlatformAbility : BaseAbility
{
    [Header("Ice Platform Input")]
    [Tooltip("Input System action reference used to trigger the ice platform ability.")]
    [SerializeField] private InputActionReference icePlatformActionRef;

    [Header("Ice Platform Prefab")]
    [Tooltip("Prefab that contains the IcePlatform component.")]
    [SerializeField] private GameObject icePlatformPrefab;

    [Header("Spawn Setup")]
    [Tooltip("Optional transform used as the base spawn position for the platform (e.g. under the player's feet). " +
             "If not assigned, the ability will use the player's Rigidbody position plus an offset.")]
    [SerializeField] private Transform platformSpawnPoint;

    [Tooltip("Offset from the player's position used as the base spawn position of the platform when no spawn point is provided.")]
    [SerializeField] private Vector2 platformSpawnOffset = new Vector2(0f, -0.5f);

    [Header("Growth Timing")]
    [Tooltip("Time between individual growth steps while the input is held.")]
    [SerializeField] private float growthInterval = 0.15f;

    [Tooltip("Maximum time (in seconds) the player can hold the input to keep growing the platform.")]
    [SerializeField] private float maxCastDuration = 1.5f;

    [Header("Environment Safety")]
    [Tooltip("Layer mask used when checking for blocking geometry above the player before starting and between growth steps.")]
    [SerializeField] private LayerMask environmentMask;

    [Tooltip("Distance above the player's feet to check for ceilings when starting the cast and between growth steps.")]
    [SerializeField] private float ceilingCheckDistance = 1f;

    [Header("Animation")]
    [Tooltip("Animator bool parameter used to trigger the ice platform casting animation.")]
    [SerializeField] private string castAnimParameterName = "IceCast";

    // Cached animator parameter ID for performance.
    private int castParameterID;

    // Runtime state
    private IcePlatform activePlatform;
    private bool isCasting;
    private float castTimer;
    private float growthTimer;

    // Tracks the top tile position from the previous frame so we can move the player with the same delta.
    private Vector2 previousTopPosition;

    #region BaseAbility Lifecycle

    protected override void Initialization()
    {
        base.Initialization();

        castParameterID = Animator.StringToHash(castAnimParameterName);
    }

    public override void EnterAbility()
    {
        // If we somehow enter this state without having started a cast,
        // consider this a no-op and reset.
        if (!isCasting || activePlatform == null)
        {
            isCasting = false;
        }
    }

    public override void ExitAbility()
    {
        isCasting = false;
        // Safety: make sure we don't keep the player force-grounded once we leave this state.
        linkedPhysics.ForceGrounded = false;
    }

    /// <summary>
    /// Called every frame while this ability is the active state.
    /// Handles cast timing, moving the player with the platform, and triggering growth steps.
    /// </summary>
    public override void ProcessUpdateAbility()
    {
        if (!isCasting || activePlatform == null)
            return;

        // 0) Move the player with the platform's top tile.
        Vector2 currentTopPos = activePlatform.TopPosition;
        Vector2 frameDelta = currentTopPos - previousTopPosition;
        previousTopPosition = currentTopPos;

        if (frameDelta.sqrMagnitude > 0f)
        {
            var rb = linkedPhysics.rb;
            rb.position += frameDelta;
            // Kill weird vertical velocity while riding.
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        }

        // While casting, keep the player grounded for state/anim logic.
        linkedPhysics.ForceGrounded = true;

        // 1) Timers.
        castTimer += Time.deltaTime;
        growthTimer -= Time.deltaTime;

        // Enforce maximum cast duration.
        if (castTimer >= maxCastDuration)
        {
            FinishCastingAndReturnToPreviousState();
            return;
        }

        // 2) Handle growth at intervals.
        if (growthTimer <= 0f)
        {
            // Ceiling safety: don't grow into geometry.
            Vector2 origin = linkedPhysics.rb.position;
            RaycastHit2D ceilingHit = Physics2D.Raycast(
                origin,
                Vector2.up,
                ceilingCheckDistance,
                environmentMask
            );

            if (ceilingHit.collider != null)
            {
                FinishCastingAndReturnToPreviousState();
                return;
            }

            // Don't try to grow if the platform is still in the middle of its last rise.
            if (activePlatform.IsRising)
            {
                // Wait a tiny bit, then check again.
                growthTimer = 0.02f;
                return;
            }

            // Safe to try another growth step.
            bool grew = activePlatform.GrowOneTile();

            // If the platform cannot grow further (max height / blocked), end the cast.
            if (!grew)
            {
                FinishCastingAndReturnToPreviousState();
                return;
            }

            growthTimer = growthInterval;
        }
    }

    /// <summary>
    /// Called every fixed timestep while this ability is the active state.
    /// Used here to lock horizontal movement while casting so the player
    /// cannot run off the platform while it's being created.
    /// </summary>
    public override void ProcessFixedAbility()
    {
        if (!isCasting)
            return;

        // Freeze horizontal movement; vertical ride is handled via the frameDelta in ProcessUpdateAbility.
        linkedPhysics.rb.linearVelocityX = 0f;
    }

    public override void UpdateAnimator()
    {
        if (!IsAllowedForCurrentClass() || linkedAnim == null)
            return;

        bool shouldCast = isCasting && linkedStateMachine.currentState == thisAbilityState;
        linkedAnim.SetBool(castParameterID, shouldCast);
    }

    #endregion

    #region Unity Events
    private void OnEnable()
    {
        if (icePlatformActionRef != null && icePlatformActionRef.action != null)
        {
            icePlatformActionRef.action.performed += TryStartPlatformCast;
            icePlatformActionRef.action.canceled += StopPlatformCast;
        }
    }

    private void OnDisable()
    {
        if (icePlatformActionRef != null && icePlatformActionRef.action != null)
        {
            icePlatformActionRef.action.performed -= TryStartPlatformCast;
            icePlatformActionRef.action.canceled -= StopPlatformCast;
        }
    }
    #endregion

    #region Casting Actions
    private void TryStartPlatformCast(InputAction.CallbackContext context)
    {
        if (!isPermitted)
            return;

        if (!IsAllowedForCurrentClass())
            return;

        if (isCasting)
            return;

        if (linkedStateMachine.currentState == thisAbilityState)
            return;

        if (!EvaluateCastConditions())
            return;

        if (!TryConsumeCharge())
            return;

        if (icePlatformPrefab == null)
        {
            Debug.LogWarning("[IcePlatformAbility] Ice platform prefab is not assigned.");
            return;
        }

        Vector2 baseSpawnPos = GetPlatformBasePosition();
        GameObject platformInstance = Instantiate(
            icePlatformPrefab,
            baseSpawnPos,
            Quaternion.identity
        );

        activePlatform = platformInstance.GetComponent<IcePlatform>();
        if (activePlatform == null)
        {
            Debug.LogWarning("[IcePlatformAbility] Spawned prefab has no IcePlatform component.");
            return;
        }

        // Initialize at the calculated base position.
        activePlatform.Initialize(baseSpawnPos + platformSpawnOffset);

        // Start casting.
        isCasting = true;
        castTimer = 0f;
        growthTimer = 0f; // force immediate first growth

        // Set initial top position for frameDelta tracking.
        previousTopPosition = activePlatform.TopPosition;

        // First growth step to begin lifting the player.
        activePlatform.GrowOneTile();
        growthTimer = growthInterval;

        // Switch the state machine into this ability's state (e.g., IceCast).
        linkedStateMachine.ChangeState(thisAbilityState);
    }

    private void StopPlatformCast(InputAction.CallbackContext context)
    {
        if (!isCasting)
            return;

        FinishCastingAndReturnToPreviousState();
    }

    private bool EvaluateCastConditions()
    {
        if (!linkedPhysics.IsGrounded)
            return false;

        if (linkedPhysics.IsWallDetected)
            return false;

        // Prevent casting if there is very little head-room above at the start.
        Vector2 origin = linkedPhysics.rb.position;
        RaycastHit2D hit = Physics2D.Raycast(
            origin,
            Vector2.up,
            ceilingCheckDistance,
            environmentMask
        );

        if (hit.collider != null)
            return false;

        return true;
    }

    private Vector2 GetPlatformBasePosition()
    {
        // Designer-assigned spawn point takes priority.
        if (platformSpawnPoint != null)
            return platformSpawnPoint.position;

        // Fallback: use the player's Rigidbody position plus an offset.
        Vector2 playerPos = linkedPhysics.rb.position;
        return playerPos + platformSpawnOffset;
    }

    private void FinishCastingAndReturnToPreviousState()
    {
        if (!isCasting)
            return;

        isCasting = false;
        linkedPhysics.ForceGrounded = false;

        PlayerStates.State targetState = linkedStateMachine.previousState;

        if (targetState == thisAbilityState || targetState == PlayerStates.State.Ignore)
        {
            targetState = linkedPhysics.IsGrounded
                ? PlayerStates.State.Idle
                : PlayerStates.State.Jump;
        }

        linkedStateMachine.ChangeState(targetState);
    }

    #endregion
}
