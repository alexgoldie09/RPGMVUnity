using UnityEngine;

/// <summary>
/// Handles setup and access to the Rigidbody2D used for physics on this object,
/// as well as simple ground detection using raycasts.
/// </summary>
public class PhysicsControl : MonoBehaviour
{
    [Header("Physics References")]
    [Tooltip("Rigidbody2D component used to move and apply forces to this object.")]
    public Rigidbody2D rb;
    
    [Header("Ground")]
    [Tooltip("The distance of the downward raycast used for ground checking.")]
    [SerializeField] private float rayCheckDistance = 0.2f;

    [Tooltip("The transform used as the left origin point for ground checking.")]
    [SerializeField] private Transform leftGroundCheck;

    [Tooltip("The transform used as the right origin point for ground checking.")]
    [SerializeField] private Transform rightGroundCheck;

    [Tooltip("The physics layers considered to be ground.")]
    [SerializeField] private LayerMask groundLayer;
    
    /// <summary>
    /// Indicates whether the object is currently considered to be on the ground.
    /// </summary>
    public bool IsGrounded { get; private set; }
    private RaycastHit2D hitInfoLeft, hitInfoRight; // Raycasts for left and right

    #region Unity Lifecycle

    /// <summary>
    /// Awake is called when the script instance is being loaded.
    /// Ensures the Rigidbody2D reference is assigned.
    /// </summary>
    private void Awake()
    {
        // If the rigidbody has not been assigned in the inspector,
        // try to grab it from the current GameObject.
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();
    }
    
    /// <summary>
    /// FixedUpdate is called at a fixed interval and is used for physics updates.
    /// Updates the grounded state using ray-based ground checks.
    /// </summary>
    private void FixedUpdate()
    {
        // Update IsGrounded each physics step based on raycast results.
        IsGrounded = CheckGround();
    }
    #endregion
    
    #region Check and Raycast Methods
    /// <summary>
    /// Checks for ground underneath the left and right ground check positions.
    /// Casts two downward rays and returns true if either ray hits a ground layer.
    /// </summary>
    /// <returns>True if either the left or right ray hits ground; otherwise false.</returns>
    private bool CheckGround()
    {
        // If ground check transforms are missing, we cannot perform a valid check.
        if (leftGroundCheck == null || rightGroundCheck == null)
            return false;

        // Cast a ray straight down from each ground check transform.
        hitInfoLeft = Physics2D.Raycast(
            leftGroundCheck.position,
            Vector2.down,
            rayCheckDistance,
            groundLayer
        );

        hitInfoRight = Physics2D.Raycast(
            rightGroundCheck.position,
            Vector2.down,
            rayCheckDistance,
            groundLayer
        );

        // If either ray hits a collider on the ground layer, we consider the object grounded.
        if (hitInfoLeft || hitInfoRight)
            return true;

        return false;
    }
    #endregion
}