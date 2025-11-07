using UnityEngine;

/// <summary>
/// Handles setup and access to the Rigidbody2D used for physics on this object.
/// Ensures a valid reference is cached at startup.
/// </summary>
public class PhysicsControl : MonoBehaviour
{
    [Header("Physics References")]
    [Tooltip("Rigidbody2D component used to move and apply forces to this object.")]
    public Rigidbody2D rb;

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

    #endregion
}