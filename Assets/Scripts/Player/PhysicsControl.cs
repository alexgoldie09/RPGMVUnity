using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Handles setup and access to the Rigidbody2D used for physics on this object,
/// as well as simple ground detection using raycasts.
/// </summary>
public class PhysicsControl : MonoBehaviour
{
    [Header("Physics References")]
    [Tooltip("Rigidbody2D component used to move and apply forces to this object.")]
    public Rigidbody2D rb;
    
    [FormerlySerializedAs("rayCheckDistance")]
    [Header("Ground")]
    [Tooltip("The distance of the downward raycast used for ground checking.")]
    [SerializeField] private float groundRayDistance = 0.2f;

    [Tooltip("The transform used as the left origin point for ground checking.")]
    [SerializeField] private Transform leftGroundCheck;

    [Tooltip("The transform used as the right origin point for ground checking.")]
    [SerializeField] private Transform rightGroundCheck;

    [Tooltip("The physics layers considered to be ground.")]
    [SerializeField] private LayerMask groundLayer;
    
    [Header("Wall")]
    [Tooltip("The distance of the outward raycast used for wall checking.")]
    [SerializeField] private float wallRayDistance = 0.2f;
    
    [Tooltip("The transform used as the lower origin point for wall checking.")]
    [SerializeField] private Transform wallCheckLower;
    
    [Tooltip("The transform used as the upper origin point for wall checking.")]
    [SerializeField] private Transform wallCheckUpper;
    
    [Tooltip("The physics layers considered to be wall.")]
    [SerializeField] private LayerMask wallLayer;
    
    
    /// <summary>
    /// Indicates whether the object is currently considered to be on the ground.
    /// </summary>
    public bool IsGrounded { get; private set; }
    
    /// <summary>
    /// Indicates whether the object is at/or near the wall.
    /// </summary>
    public bool IsWallDetected { get; private set; }
    
    private RaycastHit2D hitInfoLeft, hitInfoRight, hitInfoWallLower, hitInfoWallUpper; // Raycasts
    private float gravityValue;

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
        
        if(rb != null)
            gravityValue = rb.gravityScale;
    }
    
    /// <summary>
    /// FixedUpdate is called at a fixed interval and is used for physics updates.
    /// Updates the grounded state using ray-based ground checks.
    /// </summary>
    private void FixedUpdate()
    {
        // Update IsGrounded each physics step based on raycast results.
        IsGrounded = CheckGround();
        
        // Update IsWallDetected each physics step based on raycast results.
        IsWallDetected = CheckWall();
    }
    #endregion
    
    #region Gravity & Velocity Control Methods

    public void DisableGravity() => rb.gravityScale = 0;
    public void EnableGravity() => rb.gravityScale = gravityValue;
    public void ResetVelocity() => rb.linearVelocity = Vector2.zero;
    #endregion
    
    #region Check and Raycast Methods
    /// <summary>
    /// Checks for ground underneath the left and right ground check positions.
    /// Casts two downward rays and returns true if either ray hits a ground layer.
    /// </summary>
    private bool CheckGround()
    {
        // If ground check transforms are missing, we cannot perform a valid check.
        if (leftGroundCheck == null || rightGroundCheck == null)
            return false;

        // Cast a ray straight down from each ground check transform.
        hitInfoLeft = Physics2D.Raycast(
            leftGroundCheck.position,
            Vector2.down,
            groundRayDistance,
            groundLayer
        );

        hitInfoRight = Physics2D.Raycast(
            rightGroundCheck.position,
            Vector2.down,
            groundRayDistance,
            groundLayer
        );

        // If either ray hits a collider on the ground layer, we consider the object grounded.
        if (hitInfoLeft || hitInfoRight)
            return true;

        return false;
    }
    
    /// <summary>
    /// Checks for a wall nearby the wall upper and lower check positions.
    /// Casts two rays to the right and returns true if either ray hits a wall.
    /// </summary>
    private bool CheckWall()
    {
        // If ground check transforms are missing, we cannot perform a valid check.
        if (wallCheckLower == null || wallCheckUpper == null)
            return false;

        // Cast a ray straight down from each ground check transform.
        hitInfoWallLower = Physics2D.Raycast(
            wallCheckLower.position,
            transform.right,
            wallRayDistance,
            wallLayer
        );

        hitInfoWallUpper = Physics2D.Raycast(
            wallCheckUpper.position,
            transform.right,
            wallRayDistance,
            wallLayer
        );

        Debug.DrawRay(wallCheckLower.position, new Vector3(wallRayDistance, 0, 0), Color.red);
        Debug.DrawRay(wallCheckUpper.position, new Vector3(wallRayDistance, 0, 0), Color.red);
        
        // If either ray hits a collider on the ground layer, we consider the object grounded.
        if (hitInfoWallLower || hitInfoWallUpper)
            return true;

        return false;
    }
    #endregion
}