/// <summary>
/// Simple state machine used to manage the player's current state and notify abilities.
/// </summary>
public class StateMachine
{
    public PlayerStates.State previousState; // The previously active state before the most recent state change.
    public PlayerStates.State currentState; // The currently active state for the player.
    public BaseAbility[] abilities; // All abilities that can respond to state changes.

    #region State Management

    /// <summary>
    /// Changes the current state to the provided state.
    /// Calls ExitAbility on the old state and EnterAbility on the new one (if permitted).
    /// </summary>
    /// <param name="newState">The desired new player state.</param>
    public void ChangeState(PlayerStates.State newState)
    {
        // Notify the ability associated with the current state that it is exiting.
        foreach (BaseAbility ability in abilities)
        {
            if (ability.thisAbilityState == currentState)
            {
                ability.ExitAbility();
                previousState = currentState; // Track the state we just left.
            }
        }

        // Find the ability that matches the new state and notify it that it is entering.
        foreach (BaseAbility ability in abilities)
        {
            if (ability.thisAbilityState == newState)
            {
                // Only allow transition if the ability is currently permitted.
                if (ability.isPermitted)
                {
                    currentState = newState; // Update the current state.
                    ability.EnterAbility();  // Invoke state enter logic.
                }

                // We found the matching ability, no need to continue searching.
                break;
            }
        }
    }

    /// <summary>
    /// Forces a state change without calling ExitAbility or EnterAbility on any abilities.
    /// </summary>
    /// <param name="newState">The state to switch to.</param>
    public void ForceChange(PlayerStates.State newState)
    {
        previousState = currentState; // Save the current state before overwriting it.
        currentState = newState;      // Directly override with the new state.
    }

    #endregion
}
