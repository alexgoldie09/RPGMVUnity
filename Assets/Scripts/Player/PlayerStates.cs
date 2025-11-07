/// <summary>
/// Contains all valid player states used by the StateMachine and abilities.
/// </summary>
public class PlayerStates
{
    #region Player State Enum

    /// <summary>
    /// Defines the different high-level states that the player can be in.
    /// </summary>
    public enum State
    {
        Idle,       // Player is standing still.
        Run,        // Player is moving along the ground.
        Jump,       // Player is performing a standard jump.
        DoubleJump, // Player is performing an additional jump while airborne.
        WallJump,   // Player is jumping off a wall.
        WallSlide,  // Player is sliding down a wall.
        Dash,       // Player performs a quick burst of movement.
        Crouch,     // Player is crouching.
        Ladders,    // Player is interacting with ladders.
        Ignore      // Utility state for ignoring state checks or transitions.
    }

    #endregion
}