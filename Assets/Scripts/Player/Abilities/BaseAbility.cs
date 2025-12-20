using UnityEngine;
using System.Collections;

/// <summary>
/// Base class for all player abilities, providing shared references and lifecycle hooks.
/// </summary>
public class BaseAbility : MonoBehaviour
{
    protected PlayerController player; // Reference to the player.
    protected GatherInput linkedInput; // Reference to the player's input handler.
    protected PhysicsControl linkedPhysics; // Reference to the player's physics controller.
    protected StateMachine linkedStateMachine; // Reference to the player's state machine.
    protected Animator linkedAnim; // Reference to the player's animator component.

    [Header("Ability Settings")]
    [Tooltip("The player state that this ability is responsible for handling.")]
    public PlayerStates.State thisAbilityState;
    
    [Header("Character Restrictions")]
    [Tooltip("If false, this ability will be ignored when attempting to enter this state.")]
    public bool isPermitted = true;
    
    [Tooltip("If true, this ability can be used by all character classes.")]
    public bool availableToAllCharacters = true;
    
    [Tooltip("If not available to all, these are the classes that can use this ability.")]
    public PlayerStates.CharacterClass[] allowedClasses;
    
    [Header("Charge Settings")]
    [Tooltip("If true, this ability has a limited number of charges that recharge after a delay.")]
    [SerializeField] protected bool usesCharges = false;

    [Tooltip("Maximum number of uses available before recharging.")]
    [SerializeField] protected int maxCharges = 1;

    [Tooltip("Time in seconds before charges fully recharge after being depleted.")]
    [SerializeField] protected float rechargeDelay = 0f;

    protected int currentCharges; // Reference to the current amount of charges.
    protected float rechargeTimer; // Reference to how long the recharge time is.

    #region Unity Lifecycle

    /// <summary>
    /// Start is called before the first frame update.
    /// Initializes all core references for the ability.
    /// </summary>
    protected virtual void Start()
    {
        Initialization(); // Cache component references on Start.
    }

    #endregion

    #region Ability Lifecycle

    /// <summary>
    /// Called when this ability becomes the active state.
    /// Override this in derived abilities to implement enter logic.
    /// </summary>
    public virtual void EnterAbility() { }

    /// <summary>
    /// Called when this ability stops being the active state.
    /// Override this in derived abilities to implement exit logic.
    /// </summary>
    public virtual void ExitAbility() { }

    /// <summary>
    /// Called every frame while this ability is the active state.
    /// Override this in derived abilities to handle Update logic.
    /// </summary>
    public virtual void ProcessUpdateAbility() { }

    /// <summary>
    /// Called every fixed timestep while this ability is the active state.
    /// Override this in derived abilities to handle FixedUpdate logic.
    /// </summary>
    public virtual void ProcessFixedAbility() { }
    
    /// <summary>
    /// Called when this ability needs to update the animator state.
    /// </summary>
    public virtual void UpdateAnimator() { }
    #endregion

    #region Initialization

    /// <summary>
    /// Caches references to the PlayerController, GatherInput, and StateMachine components.
    /// </summary>
    protected virtual void Initialization()
    {
        // Get PlayerController attached to the same GameObject.
        player = GetComponentInParent<PlayerController>();

        if (player != null)
        {
            // Cache references from the PlayerController.
            linkedInput = player.gatherInput;          // Input handler used by the player.
            linkedPhysics = player.physicsControl;     // Physics controller used by the player.
            linkedStateMachine = player.stateMachine;  // State machine managing player states.
            linkedAnim = player.anim;                  // Animator handling player animations.
        }
        
        ResetCharges();
    }

    #endregion
    
    #region Charge Helpers
    /// <summary>
    /// Attempts to spend a charge. Returns true if the ability can be used.
    /// </summary>
    protected bool TryConsumeCharge()
    {
        if (!usesCharges)
            return true;

        if (currentCharges <= 0)
        {
            BeginRechargeIfNeeded();
            return false;
        }

        currentCharges--;

        if (currentCharges <= 0)
        {
            rechargeTimer = rechargeDelay;
            BeginRechargeIfNeeded();
        }

        return true;
    }
    
    /// <summary>
    /// Returns true if this ability currently has at least one available charge.
    /// </summary>
    protected bool HasAvailableCharges() => !usesCharges || currentCharges > 0;

    /// <summary>
    /// Refill charges to the configured maximum.
    /// </summary>
    private void ResetCharges()
    {
        if (!usesCharges)
            return;

        currentCharges = Mathf.Max(1, maxCharges);
        rechargeTimer = 0f;
    }
    
    /// <summary>
    /// Starts the recharge coroutine if charges are depleted and the ability tracks charges.
    /// </summary>
    private void BeginRechargeIfNeeded()
    {
        if (!usesCharges || currentCharges > 0 || rechargeCoroutine != null)
            return;

        if (rechargeTimer <= 0f)
        {
            ResetCharges();
            return;
        }

        rechargeCoroutine = StartCoroutine(RechargeRoutine());
    }

    /// <summary>
    /// Waits for the recharge delay to elapse before restoring charges.
    /// </summary>
    private IEnumerator RechargeRoutine()
    {
        while (usesCharges && currentCharges <= 0)
        {
            if (rechargeTimer > 0f)
            {
                rechargeTimer -= Time.deltaTime;
            }

            if (rechargeTimer <= 0f)
            {
                ResetCharges();
                break;
            }

            yield return null;
        }

        rechargeCoroutine = null;
    }

    private Coroutine rechargeCoroutine;
    #endregion
    
    #region Character Restriction Helpers
    /// <summary>
    /// Returns true if this ability is allowed for the player's current character class.
    /// </summary>
    protected bool IsAllowedForCurrentClass()
    {
        // If this ability is globally disabled, nothing can use it.
        if (!isPermitted)
            return false;

        // If we don't have a player reference yet, don't block anything.
        if (player == null)
            return true;

        // If available to all, ignore class restrictions.
        if (availableToAllCharacters)
            return true;

        // If we're restricted and no classes are listed, treat as "no one can use this".
        if (allowedClasses == null || allowedClasses.Length == 0)
            return false;

        // Check if the current class is in the allowed list.
        for (int i = 0; i < allowedClasses.Length; i++)
        {
            if (player.currentClass == allowedClasses[i])
                return true;
        }

        // Not found in the list then its not allowed.
        return false;
    }
    #endregion
}
