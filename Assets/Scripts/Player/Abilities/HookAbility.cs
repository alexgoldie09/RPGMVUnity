using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Ability that handles the player's hook behavior, including
/// firing a projectile and pulling the player toward a latched point.
/// </summary>
public class HookAbility : BaseAbility
{
    [Header("Hook Ability Settings")]
    [Tooltip("Name of the animator bool parameter used to trigger the hook animation.")]
    [SerializeField] private string hookAnimParameterName = "Hook";

    [Tooltip("Where the hook ray starts from (e.g., a child transform on the Player).")]
    [SerializeField] private Transform hookSpawnPoint;

    [Tooltip("Speed at which the player is pulled toward the hook point.")]
    [SerializeField] private float hookPullSpeed = 15f;

    [Tooltip("How close to the hook point before we consider the pull complete.")]
    [SerializeField] private float hookArriveThreshold = 0.5f;

    [Tooltip("Initial angle to apply for quick shot, in degrees above horizontal.")]
    [SerializeField] private float quickShotAngleDegrees = 45f;

    [Tooltip("Maximum distance that the hook can travel.")]
    [SerializeField] private float maxHookDistance = 12f;

    [Tooltip("What physics layers the hook can attach to.")]
    [SerializeField] private LayerMask hookLayerMask;

    [Tooltip("Input System action reference used to trigger hook.")]
    [SerializeField] private InputActionReference hookActionRef;

    [Header("Hook Projectile Settings")]
    [Tooltip("Hook projectile prefab that will be spawned when firing.")]
    [SerializeField] private GameObject hookProjectilePrefab;

    [Tooltip("Speed at which the hook projectile travels toward its target.")]
    [SerializeField] private float hookProjectileSpeed = 30f;
    
    [Tooltip("Layers that will stop the hook projectile (usually includes hookable surfaces + walls/ground).")]
    [SerializeField] private LayerMask hookProjectileCollisionLayers;

    [Tooltip("Max time the player can stay in hook state before forcefully exiting (failsafe).")]
    [SerializeField] private float maxHookDuration = 2f;

    // Internal: where we are attached to.
    private Vector2 hookTargetPoint;
    private bool hasHookTarget;
    private float hookTimer;
    private int hookParameterID;

    // Projectile state.
    private HookProjectile activeProjectile;
    private bool projectileInFlight;

    #region Base Class Overrides

    /// <summary>
    /// Performs initialization for the hook ability.
    /// </summary>
    protected override void Initialization()
    {
        // Call the base initialization to set up shared references.
        base.Initialization();

        // Cache animation parameter hashes for performance.
        hookParameterID = Animator.StringToHash(hookAnimParameterName);
    }

    /// <summary>
    /// Actions to perform once leaving the ability.
    /// </summary>
    public override void ExitAbility()
    {
        // Re-enable gravity and reset velocity when leaving the hook state.
        linkedPhysics.EnableGravity();
        linkedPhysics.ResetVelocity();

        // Clear state flags so we don't accidentally keep pulling.
        hasHookTarget = false;
        hookTimer = 0f;

        // If something external forced us out of Hook, make sure we clean up visuals.
        CleanupActiveProjectile();
        projectileInFlight = false;
    }

    /// <summary>
    /// Per-frame logic while in the hook state.
    /// Acts as a safety timeout so we don't stay stuck in Hook forever.
    /// </summary>
    public override void ProcessUpdateAbility()
    {
        // Only care about the timer while actually in the Hook state.
        if (linkedStateMachine.currentState != PlayerStates.State.Hook)
            return;

        if (hookTimer > 0f)
        {
            hookTimer -= Time.deltaTime;

            if (hookTimer <= 0f)
            {
                hasHookTarget = false;

                // Clean up the hook visual now that we're done.
                CleanupActiveProjectile();
                projectileInFlight = false;

                // Exit to appropriate state: Idle if grounded, otherwise back to Jump (falling).
                linkedStateMachine.ChangeState(
                    linkedPhysics.IsGrounded ? PlayerStates.State.Idle : PlayerStates.State.Jump
                );
            }
        }
    }

    /// <summary>
    /// Physics step for the hook ability while in the Hook state.
    /// Pulls the player toward the latched hook point.
    /// </summary>
    public override void ProcessFixedAbility()
    {
        // Only handle pulling physics while in the Hook state and with a valid target.
        if (linkedStateMachine.currentState != PlayerStates.State.Hook || !hasHookTarget)
            return;

        // Current position of the player.
        Vector2 currentPos = linkedPhysics.rb.position;
        Vector2 toTarget = hookTargetPoint - currentPos;
        float distance = toTarget.magnitude;

        // If we're close enough, finish the hook.
        if (distance <= hookArriveThreshold)
        {
            hasHookTarget = false;

            // Clean up the hook visual now that we're done pulling.
            CleanupActiveProjectile();
            projectileInFlight = false;

            // Exit to appropriate state: Idle if grounded, otherwise back to Jump (falling).
            linkedStateMachine.ChangeState(
                linkedPhysics.IsGrounded ? PlayerStates.State.Idle : PlayerStates.State.Jump
            );

            return;
        }

        // Otherwise, pull the player toward the target at a constant speed.
        Vector2 pullDir = toTarget.normalized;
        linkedPhysics.rb.linearVelocity = pullDir * hookPullSpeed;
    }

    /// <summary>
    /// Updates the animator for the hook ability.
    /// </summary>
    public override void UpdateAnimator()
    {
        if (!IsAllowedForCurrentClass())
            return;

        // Play hook animation while:
        // - the projectile is in flight, OR
        // - the player is actively being pulled (state == Hook).
        bool isHookActive =
            projectileInFlight ||
            linkedStateMachine.currentState == PlayerStates.State.Hook;

        linkedAnim.SetBool(hookParameterID, isHookActive);
    }
    #endregion

    #region Unity Events
    /// <summary>
    /// Called when the component becomes enabled.
    /// </summary>
    private void OnEnable()
    {
        // Subscribe to perform hook function.
        if (hookActionRef != null && hookActionRef.action != null)
        {
            hookActionRef.action.performed += TryToHook;
        }
    }

    /// <summary>
    /// Called when the component becomes disabled.
    /// </summary>
    private void OnDisable()
    {
        // Unsubscribe from hook function.
        if (hookActionRef != null && hookActionRef.action != null)
        {
            hookActionRef.action.performed -= TryToHook;
        }
    }
    #endregion

    #region Hook Actions
    /// <summary>
    /// Initiates a hook action when the hook binding is performed.
    /// Spawns a projectile that travels outward and reports back on hit or miss.
    /// </summary>
    /// <param name="value">Callback context from the input system.</param>
    private void TryToHook(InputAction.CallbackContext value)
    {
        // If this ability is not globally permitted, do nothing.
        if (!isPermitted)
            return;

        // Only allow hook for the correct character class (e.g. Rogue).
        if (!IsAllowedForCurrentClass())
            return;

        // If out of charges, can't hook.
        if (!HasAvailableCharges())
            return;

        // If a projectile is already in flight or we're already pulling, don't fire again.
        if (projectileInFlight || linkedStateMachine.currentState == PlayerStates.State.Hook)
            return;

        // Determine hook origin.
        Vector2 origin = hookSpawnPoint != null
            ? hookSpawnPoint.position
            : transform.position;

        // Compute quick-shot direction (angle above horizontal, based on facing).
        float radians = quickShotAngleDegrees * Mathf.Deg2Rad;
        float facingSign = player.FacingRight ? 1f : -1f;

        Vector2 direction = new Vector2(
            Mathf.Cos(radians) * facingSign,
            Mathf.Sin(radians)
        ).normalized;

        // Draw a debug ray so we can see the intended trajectory.
        Debug.DrawRay(origin, direction * maxHookDistance, Color.cyan, 2f);

        // Spawn the projectile and initialize it.
        if (hookProjectilePrefab == null)
        {
            Debug.LogWarning("[HookAbility] No hookProjectilePrefab assigned.");
            return;
        }

        GameObject projInstance = Instantiate(hookProjectilePrefab, origin, Quaternion.identity);
        activeProjectile = projInstance.GetComponent<HookProjectile>();

        if (activeProjectile == null)
        {
            Debug.LogWarning("[HookAbility] Spawned hookProjectilePrefab but it has no HookProjectile component.");
            Destroy(projInstance);
            projectileInFlight = false;
            return;
        }

        // Consume a charge as soon as we fire, regardless of whether it hits.
        // (HasAvailableCharges was already checked above; this is the actual spend.)
        if (!TryConsumeCharge())
        {
            // Just in case charge state changed since HasAvailableCharges, clean up and bail.
            CleanupActiveProjectile();
            projectileInFlight = false;
            return;
        }

        activeProjectile.Initialize(
            this,
            origin,
            direction,
            hookProjectileSpeed,
            maxHookDistance,
            hookProjectileCollisionLayers,
            hookLayerMask
        );
        
        // Tell the projectile where the rope starts (your hookSpawnPoint)
        activeProjectile.SetRopeStart(hookSpawnPoint);
        projectileInFlight = true;

        // If we are on the ground when we fire, enter Hook state immediately
        // to freeze horizontal movement while the projectile travels.
        if (linkedPhysics.IsGrounded)
        {
            linkedStateMachine.ChangeState(PlayerStates.State.Hook);

            // Zero horizontal velocity so we don't slide during the shot.
            // (Gravity stays on, so we stay planted on the ground.)
            linkedPhysics.rb.linearVelocityX = 0f;
        }
    }

    /// <summary>
    /// Called by the HookProjectile when it hits a valid hook target.
    /// </summary>
    /// <param name="hitPoint">World-space point where the hook latched.</param>
    /// <param name="projectile">Projectile instance that reported the hit.</param>
    public void OnHookProjectileHit(Vector2 hitPoint, HookProjectile projectile)
    {
        // Ignore if this is not our current projectile.
        if (projectile != activeProjectile)
            return;

        projectileInFlight = false;

        // Compute how far we are from the hit point right now.
        Vector2 playerCenter = linkedPhysics.rb.position;
        float startDistance = Vector2.Distance(playerCenter, hitPoint);

        // If we're already close enough to count as 'arrived', don't bother starting a hook.
        // Charge has already been consumed on fire, so no refund here.
        if (startDistance <= hookArriveThreshold)
        {
            CleanupActiveProjectile();
            return;
        }

        // Charge was already consumed when fired; we just latch and pull now.

        // Cache the hook target and mark that we have something to pull toward.
        hookTargetPoint = hitPoint;
        hasHookTarget = true;

        // Switch to Hook state and prep physics.
        if (linkedStateMachine.currentState != PlayerStates.State.Hook)
        {
            linkedStateMachine.ChangeState(PlayerStates.State.Hook);
        }
        linkedPhysics.DisableGravity();
        linkedPhysics.ResetVelocity();

        // Start a simple failsafe timer (not distance-based this time).
        hookTimer = maxHookDuration;
    }

    /// <summary>
    /// Called by the HookProjectile when it reaches max distance without hitting.
    /// </summary>
    /// <param name="projectile">Projectile instance that reported the miss.</param>
    public void OnHookProjectileMiss(HookProjectile projectile)
    {
        // Ignore if this is not our current projectile.
        if (projectile != activeProjectile)
            return;

        projectileInFlight = false;
        CleanupActiveProjectile();
        
        // If we had gone into Hook state (ground shot), exit back to a normal state.
        if (linkedStateMachine.currentState == PlayerStates.State.Hook)
        {
            linkedStateMachine.ChangeState(
                linkedPhysics.IsGrounded ? PlayerStates.State.Idle : PlayerStates.State.Jump
            );
        }
    }

    /// <summary>
    /// Destroys and clears the reference to the active projectile, if any.
    /// </summary>
    private void CleanupActiveProjectile()
    {
        if (activeProjectile != null)
        {
            Destroy(activeProjectile.gameObject);
            activeProjectile = null;
        }
    }
    #endregion
}
