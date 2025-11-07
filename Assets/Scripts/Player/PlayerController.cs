using UnityEngine;

/// <summary>
/// Handles player-wide setup and delegates update calls to the currently active ability.
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("Component References")]
    [Tooltip("Reference to the input handler used by this player.")]
    public GatherInput gatherInput;
    
    [Tooltip("Reference to the physics used by this player.")]
    public PhysicsControl physicsControl;
    
    [Tooltip("Reference to the animator component used by this player.")]
    public Animator anim;

    [Tooltip("State machine responsible for managing the player's current state.")]
    public StateMachine stateMachine;
    
    private BaseAbility[] playerAbilities; // All abilities available to this player.

    public bool FacingRight { get; private set; } = true; // Check whether player is facing right or left.

    #region Unity Lifecycle

    /// <summary>
    /// Awake is called when the script instance is being loaded.
    /// Sets up input, state machine, and caches all attached abilities.
    /// </summary>
    private void Awake()
    {
        // Ensure we have all references from this GameObject.
        if (gatherInput == null)
            gatherInput = GetComponent<GatherInput>();
        if (physicsControl == null)
            physicsControl = GetComponent<PhysicsControl>();
        if (anim == null)
            anim = GetComponent<Animator>();
        
        // Create a new state machine instance for this player.
        stateMachine = new StateMachine();

        // Cache all BaseAbility components from children (the Abilities object)
        playerAbilities = GetComponentsInChildren<BaseAbility>();

        // Provide abilities to the state machine so it can notify them of state changes.
        stateMachine.abilities = playerAbilities;
    }

    /// <summary>
    /// Update is called once per frame.
    /// Delegates per-frame logic to the ability that matches the current state.
    /// </summary>
    private void Update()
    {
        foreach (BaseAbility ability in playerAbilities)
        {
            // Only process the ability tied to the current state.
            if (ability.thisAbilityState == stateMachine.currentState)
            {
                ability.ProcessUpdateAbility();
            }
            ability.UpdateAnimator();
        }
    }

    /// <summary>
    /// FixedUpdate is called at a fixed interval and is used for physics updates.
    /// Delegates fixed-step logic to the ability that matches the current state.
    /// </summary>
    private void FixedUpdate()
    {
        foreach (BaseAbility ability in playerAbilities)
        {
            // Only process the ability tied to the current state.
            if (ability.thisAbilityState == stateMachine.currentState)
            {
                ability.ProcessFixedAbility();
            }
        }
    }
    #endregion
    
    #region Helper Methods
    /// <summary>
    /// Flips the player towards the desired location.
    /// </summary>
    public void Flip()
    {
        if (FacingRight == true && gatherInput.HorizontalInput < 0)
        {
            transform.Rotate(0, 180f, 0); // Rotate to the left.
            FacingRight = !FacingRight;
        }
        else if (FacingRight == false && gatherInput.HorizontalInput > 0)
        {
            transform.Rotate(0, 180f, 0); // Rotate to the right.
            FacingRight = !FacingRight;
        }
    }
    #endregion
}
